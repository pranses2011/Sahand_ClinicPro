using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Application.Features.Billing.Commands.CreateInvoice;
using Sahand.ClinicPro.Application.Features.Payroll.GenerateSalaries;
using Sahand.ClinicPro.Application.Features.Reports;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست‌های یکپارچگی چرخه دهم (v1.8.0) — حقوق درصدی پزشکان + گزارش تفصیلی + اکسل
/// ۱) اقلام پزشکِ دارای پرسنل مرتبط در مبنا لحاظ می‌شود
/// ۲) انتساب خودکار انجام‌دهنده از پرسنل مرتبط پزشک در صدور فاکتور
/// ۳) اولویت انجام‌دهنده صریح بر انتساب خودکار پزشک
/// ۴) عدم شمارش دوبار قلمی که هر دو مسیر آن به یک پرسنل می‌رسد
/// ۵) خارج‌ماندن فاکتورهای پیش‌نویس/ابطال/برگشتی
/// ۶) خروجی اکسل معتبر با دو برگ خلاصه/جزئیات
/// </summary>
public class CommissionCycle10Tests
{
    // دوره آزمون: مهر ۱۴۰۴ → بازه دقیق میلادی [2025-09-23, 2025-10-23)
    private const int PeriodYear = 1404;
    private const int PeriodMonth = 7;
    private static readonly DateTime InPeriod = new(2025, 10, 5, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OutOfPeriod = new(2025, 8, 15, 10, 0, 0, DateTimeKind.Utc);

    /// <summary>ساخت UnitOfWork روی دیتابیس درون‌حافظه‌ای</summary>
    private static (UnitOfWork UoW, ClinicProDbContext Context) CreateUnitOfWork()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"C10Db_{Guid.NewGuid():N}")
            .Options;
        var context = new ClinicProDbContext(options);
        var mediator = new Mock<MediatR.IMediator>().Object;
        return (new UnitOfWork(context, mediator), context);
    }

    /// <summary>بیمار پایه</summary>
    private static async Task<Patient> SeedPatientAsync(ClinicProDbContext context)
    {
        var patient = new Patient
        {
            NationalCode = "0012345679",
            MedicalRecordNo = $"MR-{Guid.NewGuid():N}".Substring(0, 15),
            FirstName = "زهرا",
            LastName = "کریمی",
            Mobile = "09121234567",
            RegistrationDate = DateTime.UtcNow,
            IsActive = true
        };
        context.Patients.Add(patient);
        await context.SaveChangesAsync();
        return patient;
    }

    /// <summary>پرسنل فعال با درصد از سود</summary>
    private static async Task<Employee> SeedEmployeeAsync(ClinicProDbContext context,
        string firstName, string lastName, decimal commissionPercent)
    {
        var employee = new Employee
        {
            FirstName = firstName,
            LastName = lastName,
            NationalCode = "0076409627",
            Mobile = "09351234567",
            CommissionPercent = commissionPercent,
            BaseSalary = 50_000_000m,
            IsActive = true
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        return employee;
    }

    /// <summary>پزشک متصل به رکورد پرسنل</summary>
    private static async Task<Doctor> SeedDoctorAsync(ClinicProDbContext context, long employeeId)
    {
        var doctor = new Doctor
        {
            EmployeeId = employeeId,
            MedicalLicenseNo = $"L-{Guid.NewGuid():N}".Substring(0, 12),
            NezamPezeshkiCode = $"N-{Guid.NewGuid():N}".Substring(0, 12),
            IsActive = true
        };
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();
        return doctor;
    }

    /// <summary>فاکتور با اقلام دلخواه در بازه دوره</summary>
    private static async Task<Invoice> SeedInvoiceAsync(ClinicProDbContext context,
        long patientId, DateTime date, InvoiceStatus status, params InvoiceItem[] items)
    {
        var invoice = new Invoice
        {
            PatientId = patientId,
            InvoiceDate = date,
            InvoiceDateShamsi = "1404/07/23",
            InvoiceType = InvoiceType.Visit,
            Status = status,
            PaymentStatus = PaymentStatus.Unpaid
        };
        foreach (var item in items)
        {
            item.InvoiceId = invoice.Id;
            invoice.Items.Add(item);
        }
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();
        return invoice;
    }

    // ═══════════════════════════════════════════════════════════
    // ۱) حقوق درصدی پزشکان در تولید دوره
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateSalaries_DoctorLinkedItems_AreCountedInProfitBase()
    {
        var (uow, context) = CreateUnitOfWork();
        var patient = await SeedPatientAsync(context);
        var employee = await SeedEmployeeAsync(context, "سارا", "محمدی", 20m);
        var doctor = await SeedDoctorAsync(context, employee.Id);

        // قلم بدون انجام‌دهنده صریح — فقط پزشک متصل؛ سود = ۱٬۰۰۰٬۰۰۰ − ۴۰۰٬۰۰۰ = ۶۰۰٬۰۰۰
        await SeedInvoiceAsync(context, patient.Id, InPeriod, InvoiceStatus.Issued,
            new InvoiceItem
            {
                DoctorId = doctor.Id,
                Quantity = 1m,
                UnitPrice = 1_000_000m,
                UnitCost = 400_000m,
                TotalPrice = 1_000_000m
            });

        var handler = new GenerateSalariesCommandHandler(uow);
        var result = await handler.Handle(
            new GenerateSalariesCommand(PeriodYear, PeriodMonth), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var salary = await context.EmployeeSalaries.SingleAsync(
            s => s.EmployeeId == employee.Id &&
                 s.PeriodYear == PeriodYear && s.PeriodMonth == PeriodMonth);
        salary.ServiceProfitBase.Should().Be(600_000m,
            because: "سود خدمت پزشکِ متصل باید در مبنا لحاظ شود — حقوق درصدی پزشکان");
        salary.ServiceCommission.Should().Be(120_000m,
            because: "۲۰٪ از سود ۶۰۰٬۰۰۰ ریالی");
    }

    [Fact]
    public async Task GenerateSalaries_InvalidAndOutOfPeriod_DoctorItems_AreExcluded()
    {
        var (uow, context) = CreateUnitOfWork();
        var patient = await SeedPatientAsync(context);
        var employee = await SeedEmployeeAsync(context, "نیما", "رضایی", 10m);
        var doctor = await SeedDoctorAsync(context, employee.Id);

        // سه قلم نامعتبر: پیش‌نویس، ابطال، خارج از بازه دوره
        await SeedInvoiceAsync(context, patient.Id, InPeriod, InvoiceStatus.Draft,
            new InvoiceItem { DoctorId = doctor.Id, Quantity = 1, UnitPrice = 500_000m, UnitCost = 0m, TotalPrice = 500_000m });
        await SeedInvoiceAsync(context, patient.Id, InPeriod, InvoiceStatus.Cancelled,
            new InvoiceItem { DoctorId = doctor.Id, Quantity = 1, UnitPrice = 500_000m, UnitCost = 0m, TotalPrice = 500_000m });
        await SeedInvoiceAsync(context, patient.Id, OutOfPeriod, InvoiceStatus.Paid,
            new InvoiceItem { DoctorId = doctor.Id, Quantity = 1, UnitPrice = 500_000m, UnitCost = 0m, TotalPrice = 500_000m });

        var handler = new GenerateSalariesCommandHandler(uow);
        await handler.Handle(new GenerateSalariesCommand(PeriodYear, PeriodMonth), CancellationToken.None);

        var salary = await context.EmployeeSalaries.SingleAsync(
            s => s.EmployeeId == employee.Id &&
                 s.PeriodYear == PeriodYear && s.PeriodMonth == PeriodMonth);
        salary.ServiceProfitBase.Should().Be(0m,
            because: "فاکتور پیش‌نویس/ابطالی و اقلام خارج از بازه دوره نباید شمرده شوند");
        salary.ServiceCommission.Should().Be(0m);
    }

    [Fact]
    public async Task GenerateSalaries_ServiceReception_AttributesPerformerAndDeductsConsumableCost()
    {
        var (uow, context) = CreateUnitOfWork();
        var patient = await SeedPatientAsync(context);
        var employee = await SeedEmployeeAsync(context, "مریم", "احمدی", 20m);
        var medication = new Sahand.ClinicPro.Domain.Entities.Clinical.Medication
        {
            GenericName = "Syringe",
            GenericNameFa = "سرنگ",
            ItemKind = MedicationItemKind.Consumable,
            IsActive = true
        };
        context.Medications.Add(medication);
        await context.SaveChangesAsync();

        var inventory = new Sahand.ClinicPro.Domain.Entities.Pharmacy.PharmacyInventory
        {
            MedicationId = medication.Id,
            PurchasePrice = 100_000m,
            SellPrice = 150_000m,
            Quantity = 0m
        };
        context.PharmacyInventories.Add(inventory);
        var reception = new Sahand.ClinicPro.Domain.Entities.Scheduling.Reception
        {
            PatientId = patient.Id,
            PerformedByEmployeeId = employee.Id,
            ReceptionDate = InPeriod,
            ReceptionNumber = "RCP-COMMISSION"
        };
        context.Receptions.Add(reception);
        await context.SaveChangesAsync();

        var invoice = new Invoice
        {
            PatientId = patient.Id,
            ReceptionId = reception.Id,
            Reception = reception,
            InvoiceDate = InPeriod,
            InvoiceDateShamsi = "1404/07/23",
            InvoiceType = InvoiceType.Other,
            Status = InvoiceStatus.Issued,
            PaymentStatus = PaymentStatus.Unpaid
        };
        invoice.Items.Add(new InvoiceItem
        {
            Description = "تزریق",
            Quantity = 1m,
            UnitPrice = 1_000_000m,
            UnitCost = 200_000m,
            TotalPrice = 1_000_000m
        });
        // ردیف قدیمی بدون UnitCost — هزینه‌اش از رسید انبار همان پذیرش بازیابی می‌شود.
        invoice.Items.Add(new InvoiceItem
        {
            Description = "لوازم مصرفی: سرنگ",
            Quantity = 2m,
            UnitPrice = 150_000m,
            UnitCost = 0m,
            TotalPrice = 300_000m
        });
        context.Invoices.Add(invoice);
        context.PharmacyTransactions.Add(new Sahand.ClinicPro.Domain.Entities.Pharmacy.PharmacyTransaction
        {
            InventoryId = inventory.Id,
            Inventory = inventory,
            TransactionType = PharmacyTransactionType.StockOut,
            Quantity = 2m,
            UnitPrice = 150_000m,
            PurchaseUnitCost = 100_000m,
            TotalPrice = 300_000m,
            ReferenceType = PharmacyReferenceType.Reception,
            ReferenceId = reception.Id,
            TransactionDate = InPeriod
        });
        await context.SaveChangesAsync();

        var handler = new GenerateSalariesCommandHandler(uow);
        await handler.Handle(new GenerateSalariesCommand(PeriodYear, PeriodMonth), CancellationToken.None);

        var salary = await context.EmployeeSalaries.SingleAsync(s => s.EmployeeId == employee.Id &&
            s.PeriodYear == PeriodYear && s.PeriodMonth == PeriodMonth);
        salary.ServiceProfitBase.Should().Be(900_000m,
            "۸۰۰٬۰۰۰ سود خدمت + ۳۰۰٬۰۰۰ فروش مصرفی − ۲۰۰٬۰۰۰ بهای خرید مصرفی");
        salary.ServiceCommission.Should().Be(180_000m,
            "حقوق درصدی باید از سود خالص خدمت و مصرفی، نه از دریافتی ناخالص، محاسبه شود");
    }

    // ═══════════════════════════════════════════════════════════
    // ۲ و ۳) انتساب خودکار در صدور فاکتور
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateInvoice_AutoAttribution_FromDoctorLinkedEmployee()
    {
        var (uow, context) = CreateUnitOfWork();
        var patient = await SeedPatientAsync(context);
        var employee = await SeedEmployeeAsync(context, "پرستو", "احمدی", 30m);
        var doctor = await SeedDoctorAsync(context, employee.Id);

        var service = new Service
        {
            ServiceName = "ویزیت تخصصی",
            DefaultPrice = 800_000m,
            DefaultCost = 300_000m,
            IsActive = true
        };
        context.Services.Add(service);
        await context.SaveChangesAsync();

        var handler = new CreateInvoiceCommandHandler(uow,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CreateInvoiceCommandHandler>.Instance);
        var result = await handler.Handle(new CreateInvoiceCommand(new CreateInvoiceDto(
            patient.Id, null, null, InvoiceType.Visit, null, 0m,
            new List<InvoiceItemDto>
            {
                new(service.Id, "ویزیت تخصصی", 2m, 800_000m, 0m, doctor.Id, 0m)
            })), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = await context.InvoiceItems.SingleAsync(i => i.InvoiceId == result.Value);
        item.PerformedById.Should().Be(employee.Id,
            because: "قلم پزشک‌دار بدون انجام‌دهنده صریح باید به پرسنل مرتبط پزشک منتسب شود");
        item.UnitCost.Should().Be(300_000m,
            because: "عکس‌فوری هزینه تمام‌شده در لحظه صدور ثبت می‌شود");
    }

    [Fact]
    public async Task CreateInvoice_ConsumableCost_IsSnapshottedFromReceptionStockOut()
    {
        var (uow, context) = CreateUnitOfWork();
        var patient = await SeedPatientAsync(context);
        var employee = await SeedEmployeeAsync(context, "نگار", "موسوی", 20m);
        var medication = new Sahand.ClinicPro.Domain.Entities.Clinical.Medication
        {
            GenericName = "Gauze",
            GenericNameFa = "گاز استریل",
            ItemKind = MedicationItemKind.Consumable,
            IsActive = true
        };
        context.Medications.Add(medication);
        await context.SaveChangesAsync();

        var inventory = new Sahand.ClinicPro.Domain.Entities.Pharmacy.PharmacyInventory
        {
            MedicationId = medication.Id,
            PurchasePrice = 80_000m,
            SellPrice = 120_000m,
            Quantity = 0m
        };
        context.PharmacyInventories.Add(inventory);
        var reception = new Sahand.ClinicPro.Domain.Entities.Scheduling.Reception
        {
            PatientId = patient.Id,
            PerformedByEmployeeId = employee.Id,
            ReceptionDate = InPeriod,
            ReceptionNumber = "RCP-COST-SNAPSHOT"
        };
        context.Receptions.Add(reception);
        await context.SaveChangesAsync();
        context.PharmacyTransactions.Add(new Sahand.ClinicPro.Domain.Entities.Pharmacy.PharmacyTransaction
        {
            InventoryId = inventory.Id,
            Inventory = inventory,
            TransactionType = PharmacyTransactionType.StockOut,
            Quantity = 2m,
            UnitPrice = 120_000m,
            PurchaseUnitCost = 80_000m,
            TotalPrice = 240_000m,
            ReferenceType = PharmacyReferenceType.Reception,
            ReferenceId = reception.Id,
            TransactionDate = InPeriod
        });
        await context.SaveChangesAsync();

        var handler = new CreateInvoiceCommandHandler(uow,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CreateInvoiceCommandHandler>.Instance);
        var result = await handler.Handle(new CreateInvoiceCommand(new CreateInvoiceDto(
            patient.Id, reception.Id, null, InvoiceType.Other, null, 0m,
            new List<InvoiceItemDto>
            {
                new(null, "لوازم مصرفی: گاز استریل", 2m, 120_000m, 0m, null, 0m,
                    PerformedById: employee.Id, IsInsuranceExempt: true, MedicationId: medication.Id)
            })), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var invoiceItem = await context.InvoiceItems.SingleAsync(i => i.InvoiceId == result.Value);
        invoiceItem.UnitCost.Should().Be(80_000m,
            "فاکتور باید بهای خرید واقعی همان مصرف را نگه دارد، نه قیمت فروش به بیمار");
        invoiceItem.PerformedById.Should().Be(employee.Id);
    }

    [Fact]
    public async Task CreateInvoice_ExplicitPerformer_TakesPrecedence_OverDoctorLink()
    {
        var (uow, context) = CreateUnitOfWork();
        var patient = await SeedPatientAsync(context);
        var performer = await SeedEmployeeAsync(context, "کارلو", "عزیزی", 0m);
        var doctorEmployee = await SeedEmployeeAsync(context, "دکتر", "توابعی", 15m);
        var doctor = await SeedDoctorAsync(context, doctorEmployee.Id);

        var handler = new CreateInvoiceCommandHandler(uow,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CreateInvoiceCommandHandler>.Instance);
        var result = await handler.Handle(new CreateInvoiceCommand(new CreateInvoiceDto(
            patient.Id, null, null, InvoiceType.Visit, null, 0m,
            new List<InvoiceItemDto>
            {
                new(null, "خدمت آزمایشی", 1m, 400_000m, 0m, doctor.Id, 0m, performer.Id)
            })), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = await context.InvoiceItems.SingleAsync(i => i.InvoiceId == result.Value);
        item.PerformedById.Should().Be(performer.Id,
            because: "انجام‌دهنده صریح اولویت دارد؛ انتساب خودکار پزشک جایگزین آن نمی‌شود");
    }

    // ═══════════════════════════════════════════════════════════
    // ۴) عدم شمارش دوبار — هر دو مسیر به یک پرسنل
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task CommissionDetailReport_ItemWithBothPaths_IsCountedOnce()
    {
        var (uow, context) = CreateUnitOfWork();
        var patient = await SeedPatientAsync(context);
        var employee = await SeedEmployeeAsync(context, "هومن", "توکلی", 25m);
        var doctor = await SeedDoctorAsync(context, employee.Id);

        // همان قلم هم PerformedById خود پرسنل است هم DoctorId پزشک متصل به او
        await SeedInvoiceAsync(context, patient.Id, InPeriod, InvoiceStatus.Paid,
            new InvoiceItem
            {
                PerformedById = employee.Id,
                DoctorId = doctor.Id,
                Quantity = 1m,
                UnitPrice = 2_000_000m,
                UnitCost = 500_000m,
                TotalPrice = 2_000_000m
            });

        var handler = new GetCommissionDetailReportQueryHandler(uow);
        var result = await handler.Handle(
            new GetCommissionDetailReportQuery(PeriodYear, PeriodMonth), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.Summary.Single(s => s.EmployeeId == employee.Id);
        row.ServiceCount.Should().Be(1,
            because: "قلمی که هر دو مسیر آن به یک پرسنل می‌رسد فقط یک بار شمرده می‌شود");
        row.TotalProfit.Should().Be(1_500_000m);
        row.CommissionBase.Should().Be(1_500_000m);
        row.ServiceCommission.Should().Be(375_000m,
            because: "۲۵٪ از سود ۱٬۵۰۰٬۰۰۰ ریالی — چون فیش تولید نشده محاسبه لحظه‌ای");
        row.IsDoctorLinked.Should().BeTrue();
        row.DoctorNames.Should().Contain("دکتر");
        result.Value.Details.Count.Should().Be(1);
        result.Value.Details[0].Attribution.Should().Be("انجام‌دهنده + پزشک");
    }

    [Fact]
    public async Task CommissionDetailReport_PeriodFilter_AndTotals()
    {
        var (uow, context) = CreateUnitOfWork();
        var patient = await SeedPatientAsync(context);
        var employee = await SeedEmployeeAsync(context, "لیلا", "صادقی", 40m);
        var doctor = await SeedDoctorAsync(context, employee.Id);

        // یک قلم معتبر داخل دوره + یک قلم نامعتبر (خارج از دوره)
        await SeedInvoiceAsync(context, patient.Id, InPeriod, InvoiceStatus.Issued,
            new InvoiceItem
            {
                DoctorId = doctor.Id,
                Quantity = 1m,
                UnitPrice = 1_000_000m,
                UnitCost = 600_000m,
                TotalPrice = 1_000_000m
            });
        await SeedInvoiceAsync(context, patient.Id, OutOfPeriod, InvoiceStatus.Paid,
            new InvoiceItem
            {
                DoctorId = doctor.Id,
                Quantity = 1m,
                UnitPrice = 9_000_000m,
                UnitCost = 0m,
                TotalPrice = 9_000_000m
            });

        var handler = new GetCommissionDetailReportQueryHandler(uow);
        var result = await handler.Handle(
            new GetCommissionDetailReportQuery(PeriodYear, PeriodMonth), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalProfit.Should().Be(400_000m,
            because: "فقط قلم داخل دوره شمرده می‌شود");
        result.Value.TotalCommission.Should().Be(160_000m);
        result.Value.Details.Should().ContainSingle(
            because: "قلم خارج از دوره در جزئیات نمی‌آید");
    }

    // ═══════════════════════════════════════════════════════════
    // ۵) خروجی اکسل — ورک‌بوک معتبر دو برگ
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task ExportCommissionDetailExcel_ProducesValidTwoSheetWorkbook()
    {
        var (uow, context) = CreateUnitOfWork();
        var patient = await SeedPatientAsync(context);
        var employee = await SeedEmployeeAsync(context, "مینا", "فرهادی", 15m);
        var doctor = await SeedDoctorAsync(context, employee.Id);

        await SeedInvoiceAsync(context, patient.Id, InPeriod, InvoiceStatus.Issued,
            new InvoiceItem
            {
                DoctorId = doctor.Id,
                                Quantity = 1m,
                UnitPrice = 1_200_000m,
                UnitCost = 200_000m,
                TotalPrice = 1_200_000m
            });

        var handler = new ExportCommissionDetailExcelQueryHandler(uow);
        var result = await handler.Handle(
            new ExportCommissionDetailExcelQuery(PeriodYear, PeriodMonth), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var bytes = result.Value!;

        // امضای ZIP (xlsx بسته OpenXML است)
        bytes.Length.Should().BeGreaterThan(0);
        bytes[0].Should().Be(0x50, because: "بایت اول PK امضای ZIP است");
        bytes[1].Should().Be(0x4B);

        // بازخوانی با ClosedXML — دو برگ با نام‌های فارسی
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        workbook.Worksheets.Count.Should().Be(2);
        workbook.Worksheets.Select(w => w.Name).Should().Contain("خلاصه پرسنل");
        workbook.Worksheets.Select(w => w.Name).Should().Contain("جزئیات اقلام");

        // سطر جمع کل برگ خلاصه — حقوق درصدی ۱۵٪ از سود ۱٬۰۰۰٬۰۰۰ = ۱۵۰٬۰۰۰
        var summary = workbook.Worksheet("خلاصه پرسنل");
        var totalCell = summary.Cells()
            .Single(c => string.Equals(c.GetString(), "جمع کل", StringComparison.Ordinal));
        totalCell.Address.RowNumber.Should().BeGreaterThan(4);
        summary.Cell(totalCell.Address.RowNumber, 12).GetValue<decimal>()
            .Should().Be(150_000m, because: "ستون ۱۲ حقوق درصدی است");

        // جزئیات — یک ردیف قلم + سطر جمع
        var details = workbook.Worksheet("جزئیات اقلام");
        details.LastRowUsed()!.RowNumber().Should().Be(3,
            because: "سطر عنوان + یک قلم + سطر جمع = ۳");
    }
}

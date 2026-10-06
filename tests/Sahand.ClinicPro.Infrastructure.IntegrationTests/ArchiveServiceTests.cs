using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Sahand.ClinicPro.Infrastructure.Services.Archive;
using Xunit;
using Reception = Sahand.ClinicPro.Domain.Entities.Scheduling.Reception;
using Visit = Sahand.ClinicPro.Domain.Entities.Clinical.Visit;
using Prescription = Sahand.ClinicPro.Domain.Entities.Clinical.Prescription;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست‌های آرشیو سالانه — چرخه سیزدهم (درخواست ۷-۷):
/// برش سال شمسی، جابه‌جایی کامل زنجیره (پرداخت تا نوبت)، محافظت ارجاعی
/// ردیف مسدودشده، جستجوی پرونده قدیمی و رفت‌وبرگشت تصویر JSON
/// </summary>
public class ArchiveServiceTests
{
    /// <summary>ساخت سرویس آرشیو روی بانک درون‌حافظه‌ای</summary>
    private static (ArchiveService Service, ClinicProDbContext Ctx) MakeService()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"archive-{Guid.NewGuid():N}")
            .Options;
        var ctx = new ClinicProDbContext(options);
        var mediator = new Moq.Mock<MediatR.IMediator>().Object;
        var uow = new UnitOfWork(ctx, mediator);
        return (new ArchiveService(uow, ctx, NullLogger<ArchiveService>.Instance), ctx);
    }

    /// <summary>بیمار نمونه</summary>
    private static Patient Patient(string first = "علی", string last = "رضایی")
        => new()
        {
            FirstName = first,
            LastName = last,
            NationalCode = $"00{Random.Shared.Next(10000000, 99999999)}",
            MedicalRecordNo = $"MR-{Random.Shared.Next(1000, 9999)}",
            Mobile = "09140000000",
            Gender = Domain.Enums.Gender.Male,
        };

    /// <summary>نوبت نمونه با تاریخ دلخواه</summary>
    private static Domain.Entities.Scheduling.Appointment Appointment(long patientId, DateTime date)
        => new()
        {
            PatientId = patientId,
            DoctorId = 1,
            AppointmentDate = DateOnly.FromDateTime(date),
            AppointmentTime = new TimeOnly(10, 0),
            Status = Domain.Enums.AppointmentStatus.Completed,
        };

    [Fact]
    public async Task Run_ArchivesWholeOldChain_AndKeepsReferencedRows()
    {
        var (service, ctx) = MakeService();
        var p = Patient();
        ctx.Patients.Add(p);
        await ctx.SaveChangesAsync();

        // زنجیره قدیمی (پیش از ۱۴۰۳ = 2024-03-20)
        var oldDate = new DateTime(2023, 5, 10);
        var oldVisit = new Visit { PatientId = p.Id, DoctorId = 1, VisitDate = oldDate, ChiefComplaint = "سردرد" };
        ctx.Visits.Add(oldVisit);
        await ctx.SaveChangesAsync();
        var oldInvoice = new Invoice { PatientId = p.Id, VisitId = oldVisit.Id, InvoiceDate = oldDate, TotalAmount = 500_000, PaidAmount = 500_000 };
        var oldPayment = new Domain.Entities.Billing.Payment { PatientId = p.Id, InvoiceId = oldInvoice.Id, Amount = 500_000, PaymentDate = oldDate };
        ctx.Invoices.Add(oldInvoice);
        ctx.Payments.Add(oldPayment);
        ctx.Appointments.Add(Appointment(p.Id, oldDate));
        // پذیرش قدیمی بدون ارجاع از آینده
        ctx.Receptions.Add(new Reception { PatientId = p.Id, DoctorId = 1, ReceptionDate = oldDate, AppointmentId = null });

        // زنجیره جدید (۱۴۰۴) — پرداختِ جدید به فاکتور قدیمی اشاره می‌کند → فاکتور باید مسدود بماند
        var newDate = new DateTime(2025, 6, 1);
        var newPayment = new Domain.Entities.Billing.Payment { PatientId = p.Id, InvoiceId = oldInvoice.Id, Amount = 100_000, PaymentDate = newDate };
        ctx.Payments.Add(newPayment);
        // نوبت جدید — نباید آرشیو شود
        ctx.Appointments.Add(Appointment(p.Id, newDate));
        await ctx.SaveChangesAsync();

        var report = await service.RunAsync(1403, archivedBy: 7);

        // کل ردیف‌های قدیمیِ آزاد آرشیو شدند
        report.TotalArchived.Should().BeGreaterThan(0);

        // فاکتور قدیمی به‌دلیل ارجاع پرداخت جدید مسدود است
        var invItem = report.Items.First(i => i.Table == "Invoices");
        invItem.Blocked.Should().Be(1, "فاکتور قدیمی توسط پرداخت جدید ارجاع شده است");

        // نوبت جدید سر جای خودش است — جدول عملیاتی فقط داده‌های جاری را دارد
        var remainingAppointments = await ctx.Appointments.IgnoreQueryFilters().Where(a => !a.IsDeleted).ToListAsync();
        remainingAppointments.Should().ContainSingle("تنها نوبت جدیدِ ۱۴۰۴ در جدول عملیاتی می‌ماند")
            .Which.AppointmentDate.Should().Be(DateOnly.FromDateTime(newDate));

        // پرداخت قدیمی آرشیو شد؛ پرداخت جدید مانده
        var remainingPayments = await ctx.Payments.IgnoreQueryFilters().Where(x => !x.IsDeleted).ToListAsync();
        remainingPayments.Should().ContainSingle().Which.Amount.Should().Be(100_000);

        // رکوردهای آرشیو با نام بیمار و سال
        var archives = await ctx.ArchiveRecords.IgnoreQueryFilters().ToListAsync();
        archives.Should().Contain(a => a.SourceTable == "Visits" && a.PatientName!.Contains("علی") && a.YearJalali == 1403);
        archives.Should().Contain(a => a.SourceTable == "Appointments");
        archives.All(a => a.ArchivedBy == 7).Should().BeTrue("کاربر آرشیوکننده ثبت شد");
    }

    [Fact]
    public async Task Preview_CountsWithoutChangingData()
    {
        var (service, ctx) = MakeService();
        var p = Patient();
        ctx.Patients.Add(p);
        ctx.Appointments.Add(Appointment(p.Id, new DateTime(2022, 4, 1)));
        ctx.Appointments.Add(Appointment(p.Id, new DateTime(2025, 4, 1)));
        await ctx.SaveChangesAsync();

        var preview = await service.PreviewAsync(1403);

        var apt = preview.First(x => x.Table == "Appointments");
        apt.Candidates.Should().Be(1, "فقط نوبت پیش از ۱۴۰۳ کاندید است");
        apt.Blocked.Should().Be(0);

        // هیچ داده‌ای تغییر نکرده
        (await ctx.Appointments.IgnoreQueryFilters().Where(a => !a.IsDeleted).CountAsync())
            .Should().Be(2, "پیش‌نمایش بدون تغییر است");
    }

    [Fact]
    public async Task Search_FindsOldPatientRecords_ByTerm()
    {
        var (service, ctx) = MakeService();
        var p = Patient("مریم", "احمدی");
        ctx.Patients.Add(p);
        await ctx.SaveChangesAsync();
        var visit = new Visit { PatientId = p.Id, DoctorId = 1, VisitDate = new DateTime(2022, 7, 2), ChiefComplaint = "کمردرد" };
        ctx.Visits.Add(visit);
        await ctx.SaveChangesAsync();

        await service.RunAsync(1403);

        var rows = await service.SearchAsync(null, "مریم");
        rows.Should().NotBeEmpty("رکورد آرشیو با نام بیمار پیدا می‌شود");
        rows.First().SourceTable.Should().Be("Visits");

        // تصویر JSON کامل است و قابل بازخوانی
        var payload = await service.GetPayloadAsync(rows.First().Id);
        payload.Should().NotBeNull();
        using var doc = JsonDocument.Parse(payload!);
        doc.RootElement.GetProperty("Id").GetInt64().Should().Be(visit.Id);
        doc.RootElement.GetProperty("ChiefComplaint").GetString().Should().Be("کمردرد");
    }

    [Fact]
    public void Cutoff_OfJalaliYear_IsStartOfYear()
    {
        // آغاز ۱۴۰۳ شمسی = ۲۱ مارس ۲۰۲۴
        ArchiveService.CutoffOf(1403).Should().Be(new DateTime(2024, 3, 20).AddDays(1).AddDays(-1).Date)
            .And.BeOnOrAfter(new DateTime(2024, 3, 20));
        ArchiveService.CutoffOf(1404).Year.Should().Be(2025);
    }
}

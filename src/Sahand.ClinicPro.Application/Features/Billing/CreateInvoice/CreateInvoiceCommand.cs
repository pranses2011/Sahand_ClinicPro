using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.Common.Security;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Accounting;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Pharmacy;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Events;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Billing.Commands.CreateInvoice;

/// <summary>
/// فرمان صدور فاکتور — محاسبه سهم‌ها و تولید خودکار سند حسابداری دوبل
/// </summary>
/// <param name="Dto">داده فاکتور</param>
public record CreateInvoiceCommand(CreateInvoiceDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج صدور فاکتور</summary>
public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.Dto.PatientId).GreaterThan(0).WithMessage("شناسه بیمار الزامی است");
        RuleFor(x => x.Dto.Items).NotEmpty().WithMessage("فاکتور باید حداقل یک قلم داشته باشد");
        RuleForEach(x => x.Dto.Items).SetValidator(new InvoiceItemDtoValidator());
        RuleFor(x => x.Dto.TaxPercent).InclusiveBetween(0, 100)
            .WithMessage("درصد مالیات باید بین صفر تا صد باشد");
    }
}

/// <summary>اعتبارسنج قلم فاکتور</summary>
public class InvoiceItemDtoValidator : AbstractValidator<InvoiceItemDto>
{
    /// <summary>قواعد اعتبارسنجی قلم</summary>
    public InvoiceItemDtoValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("تعداد باید بیشتر از صفر باشد");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("قیمت واحد نمی‌تواند منفی باشد");
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100)
            .WithMessage("درصد تخفیف بین صفر تا صد است");
    }
}

/// <summary>
/// گرداننده صدور فاکتور — سهم بیمه و بیمار محاسبه و سند حسابداری خودکار تولید می‌شود
/// </summary>
public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateInvoiceCommandHandler> _logger;

    /// <summary>کدهای حساب پیش‌فرض — در راه‌اندازی حسابداری قابل تغییر است</summary>
    private const string PatientReceivableCode = "1103";
    private const string MedicalIncomeCode = "4101";

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateInvoiceCommandHandler(IUnitOfWork unitOfWork,
        ILogger<CreateInvoiceCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>اجرای صدور فاکتور</summary>
    public async Task<Result<long>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(dto.PatientId, cancellationToken);
        if (patient is null || patient.IsDeleted)
            return Result<long>.Failure(Error.NotFound("بیمار", dto.PatientId));

        if (dto.ReceptionId is long receptionId)
        {
            var reception = await _unitOfWork.Repository<Reception>().QueryableNoTracking
                .FirstOrDefaultAsync(r => r.Id == receptionId && !r.IsDeleted, cancellationToken);
            if (reception is null) return Result<long>.Failure(Error.NotFound("پذیرش", receptionId));
            if (reception.PatientId != dto.PatientId)
                return Result<long>.Failure(new Error("BILLING.RECEPTION_PATIENT", "بیمار فاکتور با بیمار پذیرش مطابقت ندارد."));
        }
        if (dto.VisitId is long visitId)
        {
            var visit = await _unitOfWork.Repository<Visit>().QueryableNoTracking
                .FirstOrDefaultAsync(v => v.Id == visitId && !v.IsDeleted, cancellationToken);
            if (visit is null) return Result<long>.Failure(Error.NotFound("ویزیت", visitId));
            if (visit.PatientId != dto.PatientId || (dto.ReceptionId.HasValue && visit.ReceptionId.HasValue && visit.ReceptionId != dto.ReceptionId))
                return Result<long>.Failure(new Error("BILLING.VISIT_MISMATCH", "ویزیت با بیمار یا پذیرش فاکتور مطابقت ندارد."));
        }

        // بیمه فقط وقتی مؤثر است که شرکت طرف قرارداد باشد و تنظیمات درمانگاه
        // برای دست‌کم یک دستهٔ موجود در فاکتور اجازهٔ اعمال بیمه بدهد.
        var requestedInsurance = dto.InsuranceCompanyId is long requestedInsuranceId
            ? await _unitOfWork.Repository<InsuranceCompany>().QueryableNoTracking
                .FirstOrDefaultAsync(c => c.Id == requestedInsuranceId && c.IsActive && !c.IsDeleted, cancellationToken)
            : null;
        var insuranceSettings = await _unitOfWork.Repository<SystemSetting>().QueryableNoTracking
            .Where(s => !s.IsDeleted && s.Category == "Insurance")
            .Select(s => new { s.Key, s.Value }).ToListAsync(cancellationToken);
        var billingPolicy = InsuranceBillingPolicy.FromSettings(insuranceSettings.Select(s =>
            new KeyValuePair<string, string?>(s.Key, s.Value)));
        var insurance = requestedInsurance is { IsActive: true, IsContracted: true } ? requestedInsurance : null;
        var hasInsuranceEligibleItems = insurance is not null && dto.Items.Any(item =>
            !item.IsInsuranceExempt && (item.DoctorId.HasValue && !item.ServiceId.HasValue
                ? billingPolicy.ApplyToDoctorVisits
                : billingPolicy.ApplyToMedicalServices));
        var invoiceInsuranceCompanyId = hasInsuranceEligibleItems ? insurance!.Id : (long?)null;
        var defaultInsurancePercent = insurance is null ? 0m
            : insurance.DefaultCoveragePercent > 0 ? insurance.DefaultCoveragePercent
            : insurance.DefaultInsuranceShare > 0 ? insurance.DefaultInsuranceShare : 70m;

        var now = DateTime.UtcNow;
        var invoice = new Invoice
        {
            PatientId = dto.PatientId,
            ReceptionId = dto.ReceptionId,
            VisitId = dto.VisitId,
            InvoiceDate = now,
            InvoiceDateShamsi = PersianDateHelper.ToShamsi(now),
            InvoiceType = dto.Type,
            InsuranceCompanyId = invoiceInsuranceCompanyId,
            Status = InvoiceStatus.Issued,
            PaymentStatus = PaymentStatus.Unpaid,
            Description = dto.Description
        };

        // ═══ آماده‌سازی حقوق درصدی پرسنل (چرخه نهم) ═══
        // اعتبارسنجی همه پرسنل‌های انجام‌دهنده در یک کوئری دسته‌ای
        var performerIds = dto.Items.Where(i => i.PerformedById.HasValue)
            .Select(i => i.PerformedById!.Value).Distinct().ToList();
        if (performerIds.Count > 0)
        {
            var validIds = await _unitOfWork.Repository<Employee>().QueryableNoTracking
                .Where(e => performerIds.Contains(e.Id) && !e.IsDeleted && e.IsActive)
                .Select(e => e.Id).ToListAsync(cancellationToken);
            if (validIds.Count != performerIds.Count)
                return Result<long>.Failure(new Error("BILLING.INVALID_PERFORMER",
                    "پرسنل انجام‌دهنده انتخاب‌شده معتبر یا فعال نیست"));
        }

        // ═══ انتساب خودکار انجام‌دهنده از پزشک (چرخه دهم — حقوق درصدی پزشکان) ═══
        // اقلامی که پزشک دارند ولی انجام‌دهنده صریح ندارند: اگر پزشک مربوطه
        // «رکورد پرسنل مرتبط فعال» داشته باشد، همان پرسنل به‌عنوان انجام‌دهنده
        // ثبت می‌شود تا سود این خدمت در فیش حقوقی پزشک (حقوق درصدی از سود)
        // بی‌کم‌کاست لحاظ شود. اولویت با انجام‌دهنده صریح است؛ پزشک بدون
        // پرسنل مرتبط فقط DoctorId خود را حفظ می‌کند و چیزی از دست نمی‌رود.
        var doctorIdsNeedingPerformer = dto.Items
            .Where(i => i.DoctorId.HasValue && !i.PerformedById.HasValue)
            .Select(i => i.DoctorId!.Value).Distinct().ToList();

        var doctorPerformerById = new Dictionary<long, long>();
        if (doctorIdsNeedingPerformer.Count > 0)
        {
            doctorPerformerById = await _unitOfWork.Repository<Doctor>().QueryableNoTracking
                .Where(d => doctorIdsNeedingPerformer.Contains(d.Id) && !d.IsDeleted &&
                            d.EmployeeId != null && d.Employee != null &&
                            !d.Employee.IsDeleted && d.Employee.IsActive)
                .ToDictionaryAsync(d => d.Id, d => d.EmployeeId!.Value, cancellationToken);
        }

        // عکس‌فوری هزینه تمام‌شده خدمات این فاکتور در لحظه صدور —
        // سود هر قلم بعداً از همین عکس‌فوری محاسبه می‌شود (حقوق درصدی)
        var serviceIds = dto.Items.Where(i => i.ServiceId.HasValue)
            .Select(i => i.ServiceId!.Value).Distinct().ToList();
        var insuredTariffShareByService = new Dictionary<long, decimal>();
        if (insurance is not null && billingPolicy.ApplyToMedicalServices && serviceIds.Count > 0)
        {
            var insuredTariffs = await _unitOfWork.Repository<Tariff>().QueryableNoTracking
                .Where(t => serviceIds.Contains(t.ServiceId) && t.InsuranceCompanyId == insurance.Id &&
                            t.IsActive && !t.IsDeleted)
                .OrderByDescending(t => t.Year)
                .ToListAsync(cancellationToken);
            insuredTariffShareByService = insuredTariffs
                .GroupBy(t => t.ServiceId).ToDictionary(g => g.Key, g => g.First().InsuranceShare);
        }
        var costById = new Dictionary<long, decimal>();
        if (serviceIds.Count > 0)
        {
            costById = await _unitOfWork.Repository<Service>().QueryableNoTracking
                .Where(s => serviceIds.Contains(s.Id) && !s.IsDeleted)
                .ToDictionaryAsync(s => s.Id, s => s.DefaultCost, cancellationToken);
        }

        // بهای تمام‌شدهٔ لوازم مصرفی از تراکنش‌های واقعی برداشت همان پذیرش
        // خوانده می‌شود؛ هیچ هزینه‌ای از کلاینت پذیرفته نمی‌شود.
        var medicationIds = dto.Items.Where(i => i.MedicationId.HasValue)
            .Select(i => i.MedicationId!.Value).Distinct().ToList();
        var consumableCostByMedication = new Dictionary<long, (decimal Quantity, decimal TotalCost)>();
        if (medicationIds.Count > 0)
        {
            if (!dto.ReceptionId.HasValue)
                return Result<long>.Failure(new Error("BILLING.CONSUMABLE_RECEPTION",
                    "ثبت لوازم مصرفی در فاکتور نیازمند پذیرش مرتبط است"));

            var consumedRows = await _unitOfWork.Repository<PharmacyTransaction>().QueryableNoTracking
                .Where(t => !t.IsDeleted &&
                            t.ReferenceType == PharmacyReferenceType.Reception &&
                            t.ReferenceId == dto.ReceptionId.Value &&
                            t.TransactionType == PharmacyTransactionType.StockOut &&
                            t.Inventory != null && medicationIds.Contains(t.Inventory.MedicationId))
                .Select(t => new
                {
                    MedicationId = t.Inventory!.MedicationId,
                    t.Quantity,
                    UnitCost = t.PurchaseUnitCost ?? t.Inventory!.PurchasePrice
                })
                .ToListAsync(cancellationToken);

            consumableCostByMedication = consumedRows
                .GroupBy(x => x.MedicationId)
                .ToDictionary(g => g.Key,
                    g => (g.Sum(x => x.Quantity), g.Sum(x => x.Quantity * x.UnitCost)));

            foreach (var requested in dto.Items.Where(i => i.MedicationId.HasValue)
                         .GroupBy(i => i.MedicationId!.Value))
            {
                var requestedQuantity = requested.Sum(i => i.Quantity);
                if (!consumableCostByMedication.TryGetValue(requested.Key, out var consumed) ||
                    consumed.Quantity < requestedQuantity)
                    return Result<long>.Failure(new Error("BILLING.CONSUMABLE_NOT_CONSUMED",
                        "اقلام مصرفی فاکتور باید پیش‌تر در همین پذیرش از انبار برداشت شده باشند"));
            }
        }

        decimal subTotal = 0, totalDiscount = 0, totalInsuranceShare = 0;

        foreach (var item in dto.Items)
        {
            var gross = item.Quantity * item.UnitPrice;
            var discount = gross * item.DiscountPercent / 100m;
            var net = gross - discount;

            // ═══ چرخهٔ ۳۹ — معافیت بیمه‌ای قلم ═══ اقلام کالایی (لوازم مصرفی
            // درمانگاه: سرنگ/سرم/گاز و…) سهم بیمه ندارند و ۱۰۰٪ بر عهدهٔ بیمارند؛
            // این پرچم فقط از پذیرش خدمتی برای اقلام مصرفی true می‌آید و سایر
            // فراخوانی‌ها (بدون پرچم = false) دقیقاً رفتار پیشین را می‌بینند.
            var isDoctorVisit = item.DoctorId.HasValue && !item.ServiceId.HasValue;
            var categoryEnabled = isDoctorVisit
                ? billingPolicy.ApplyToDoctorVisits
                : billingPolicy.ApplyToMedicalServices;
            var itemInsurancePercent = insurance is null || item.IsInsuranceExempt || !categoryEnabled
                ? 0m
                : item.ServiceId is long itemServiceId && insuredTariffShareByService.TryGetValue(itemServiceId, out var tariffShare)
                    ? tariffShare
                    : defaultInsurancePercent;
            var insuranceShare = net * itemInsurancePercent / 100m;
            var patientShare = net - insuranceShare;

            subTotal += gross;
            totalDiscount += discount;
            totalInsuranceShare += insuranceShare;

            // انجام‌دهنده نهایی قلم: انجام‌دهنده صریح اولویت دارد؛
            // وگرنه انتساب خودکار از پرسنل مرتبطِ پزشک همین قلم (چرخه دهم)
            var performedById = item.PerformedById ??
                (item.DoctorId.HasValue &&
                  doctorPerformerById.TryGetValue(item.DoctorId.Value, out var autoPerformer)
                    ? autoPerformer
                    : null);

            invoice.Items.Add(new InvoiceItem
            {
                ServiceId = item.ServiceId,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = discount,
                DiscountPercent = item.DiscountPercent,
                InsuranceShare = insuranceShare,
                InsurancePercent = itemInsurancePercent,
                PatientShare = patientShare,
                TotalPrice = net,
                DoctorId = item.DoctorId,
                DoctorSharePercent = item.DoctorSharePercent,
                DoctorShareAmount = net * item.DoctorSharePercent / 100m,
                PerformedById = performedById,
                UnitCost = item.ServiceId.HasValue &&
                           costById.TryGetValue(item.ServiceId.Value, out var serviceUnitCost)
                    ? serviceUnitCost
                    : item.MedicationId.HasValue &&
                      consumableCostByMedication.TryGetValue(item.MedicationId.Value, out var consumedCost) &&
                      consumedCost.Quantity > 0m
                        ? consumedCost.TotalCost / consumedCost.Quantity
                        : 0m
            });
        }

        var tax = (subTotal - totalDiscount) * dto.TaxPercent / 100m;
        invoice.SubTotal = subTotal;
        invoice.DiscountAmount = totalDiscount;
        invoice.InsuranceDiscount = totalInsuranceShare;
        invoice.Tax = tax;
        invoice.TotalAmount = subTotal - totalDiscount - totalInsuranceShare + tax;
        invoice.PaidAmount = 0;
        invoice.RemainingAmount = invoice.TotalAmount;

        await _unitOfWork.Repository<Invoice>().AddAsync(invoice, cancellationToken);

        // ═══ چرخهٔ ۷۵ — ریشهٔ «فاکتور شماره — سعید نجفی» در دفتر روزنامه ═══
        // گزارش مالک: «در لیست اسناد، شماره فاکتور رو نمیندازه و به جاش خط
        // تیره میندازه». ریشه: InvoiceNumber با «مقدار پیش‌فرض SQL» (سکانس
        // SeqInvoiceNumber) فقط هنگام INSERT ساخته می‌شود؛ پیش‌تر سند حسابداری
        // «پیش از» ذخیره تولید می‌شد و شماره هنوز تهی بود ⇒ توضیح سند با
        // «فاکتور شماره (خالی)» ثبت می‌شد. اکنون: نخست فاکتور ذخیره می‌شود تا
        // شمارهٔ واقعی از دیتابیس برگردد (EF مقدار پیش‌فرض تولیدی را پس از
        // INSERT روی خود موجودیت برمی‌خواند)، سپس سند با همان شماره ساخته و
        // در همان تراکنشِ فرمان (TransactionBehavior) ذخیره می‌شود — اتمیک.
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        // شناسهٔ تولیدشدهٔ SQL باید پیش از ساخت رویداد دریافت شود.
        invoice.AddDomainEvent(new InvoiceCreatedEvent(
            invoice.Id, dto.PatientId, invoice.TotalAmount));

        // ── سند حسابداری خودکار — قانون طلایی دوبل (اکنون با شمارهٔ واقعی) ──
        await GenerateJournalEntryAsync(invoice, patient, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation(
            "فاکتور {Number} به مبلغ {Amount} ریال صادر شد — سهم بیمه {Insurance}",
            invoice.InvoiceNumber, invoice.TotalAmount, totalInsuranceShare);

        return Result<long>.Success(invoice.Id);
    }

    /// <summary>تولید سند حسابداری متوازن برای فاکتور — بدهکار بیمار، بستانکار درآمد</summary>
    private async Task GenerateJournalEntryAsync(Invoice invoice, Patient patient,
        CancellationToken ct)
    {
        var receivable = await _unitOfWork.Repository<AccountingAccount>()
            .FirstOrDefaultAsync(a => a.AccountCode == PatientReceivableCode, ct);
        var income = await _unitOfWork.Repository<AccountingAccount>()
            .FirstOrDefaultAsync(a => a.AccountCode == MedicalIncomeCode, ct);

        // اگر کدینگ هنوز تنظیم نشده سند تولید نمی‌شود — فاکتور مستقل کار می‌کند
        if (receivable is null || income is null)
        {
            _logger.LogWarning("کدینگ حسابداری آماده نیست — سند خودکار فاکتور {Id} تولید نشد", invoice.Id);
            return;
        }

        var entry = new JournalEntry
        {
            EntryDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EntryDateShamsi = PersianDateHelper.ToShamsi(DateTime.UtcNow),
            Description = $"فاکتور شماره {invoice.InvoiceNumber} — {patient.FirstName} {patient.LastName}",
            ReferenceType = JournalReferenceType.Invoice,
            ReferenceId = invoice.Id,
            TotalDebit = invoice.TotalAmount,
            TotalCredit = invoice.TotalAmount,
            Status = JournalEntryStatus.Approved
        };

        // بدهکار: حساب دریافتنی از بیمار
        entry.Lines.Add(new JournalEntryLine
        {
            LineNumber = 1,
            AccountId = receivable.Id,
            Debit = invoice.TotalAmount,
            Credit = 0,
            Description = "مطالبات از بیمار"
        });

        // بستانکار: درآمد خدمات درمانی
        entry.Lines.Add(new JournalEntryLine
        {
            LineNumber = 2,
            AccountId = income.Id,
            Debit = 0,
            Credit = invoice.TotalAmount,
            Description = "درآمد خدمات درمانی"
        });

        await _unitOfWork.Repository<JournalEntry>().AddAsync(entry, ct);
    }
}

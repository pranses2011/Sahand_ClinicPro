using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Accounting;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
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
        if (patient is null)
            return Result<long>.Failure(Error.NotFound("بیمار", dto.PatientId));

        // درصد سهم بیمه از شرکت بیمه — پیش‌فرض صفر (آزاد)
        decimal insurancePercent = 0;
        if (dto.InsuranceCompanyId.HasValue)
        {
            var insurance = await _unitOfWork.Repository<InsuranceCompany>()
                .GetByIdAsync(dto.InsuranceCompanyId.Value, cancellationToken);
            insurancePercent = insurance?.DefaultInsuranceShare ?? 0;
        }

        var now = DateTime.UtcNow;
        var invoice = new Invoice
        {
            PatientId = dto.PatientId,
            ReceptionId = dto.ReceptionId,
            VisitId = dto.VisitId,
            InvoiceDate = now,
            InvoiceDateShamsi = PersianDateHelper.ToShamsi(now),
            InvoiceType = dto.Type,
            InsuranceCompanyId = dto.InsuranceCompanyId,
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

        // عکس‌فوری هزینه تمام‌شده خدمات این فاکتور در لحظه صدور —
        // سود هر قلم بعداً از همین عکس‌فوری محاسبه می‌شود (حقوق درصدی)
        var serviceIds = dto.Items.Where(i => i.ServiceId.HasValue)
            .Select(i => i.ServiceId!.Value).Distinct().ToList();
        var costById = new Dictionary<long, decimal>();
        if (serviceIds.Count > 0)
        {
            costById = await _unitOfWork.Repository<Service>().QueryableNoTracking
                .Where(s => serviceIds.Contains(s.Id) && !s.IsDeleted)
                .ToDictionaryAsync(s => s.Id, s => s.DefaultCost, cancellationToken);
        }

        decimal subTotal = 0, totalDiscount = 0, totalInsuranceShare = 0;

        foreach (var item in dto.Items)
        {
            var gross = item.Quantity * item.UnitPrice;
            var discount = gross * item.DiscountPercent / 100m;
            var net = gross - discount;
            var insuranceShare = net * insurancePercent / 100m;
            var patientShare = net - insuranceShare;

            subTotal += gross;
            totalDiscount += discount;
            totalInsuranceShare += insuranceShare;

            invoice.Items.Add(new InvoiceItem
            {
                ServiceId = item.ServiceId,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = discount,
                DiscountPercent = item.DiscountPercent,
                InsuranceShare = insuranceShare,
                InsurancePercent = insurancePercent,
                PatientShare = patientShare,
                TotalPrice = net,
                DoctorId = item.DoctorId,
                DoctorSharePercent = item.DoctorSharePercent,
                DoctorShareAmount = net * item.DoctorSharePercent / 100m,
                PerformedById = item.PerformedById,
                UnitCost = item.ServiceId.HasValue &&
                           costById.TryGetValue(item.ServiceId.Value, out var unitCost)
                    ? unitCost : 0m
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

        // ── سند حسابداری خودکار — قانون طلایی دوبل ──
        await GenerateJournalEntryAsync(invoice, patient, cancellationToken);

        invoice.AddDomainEvent(new InvoiceCreatedEvent(
            invoice.Id, dto.PatientId, invoice.TotalAmount));

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

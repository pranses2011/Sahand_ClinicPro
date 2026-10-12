# -*- coding: utf-8 -*-
"""گام ۱۷ — فاکتور + پرداخت با تولید خودکار سند حسابداری دوبل"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
APP = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Application"))
API = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.API"))


def write(rel, content, base=APP):
    full = os.path.join(base, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


write("DTOs/Billing/BillingDtos.cs", """
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Billing;

/// <summary>ردیف فهرست فاکتورها</summary>
public record InvoiceListDto(
    long Id,
    string? InvoiceNumber,
    string PatientName,
    string DateShamsi,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    PaymentStatus PaymentStatus,
    InvoiceStatus Status);

/// <summary>قلم فاکتور برای ثبت</summary>
public record InvoiceItemDto(
    long? ServiceId,
    string? Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    long? DoctorId,
    decimal DoctorSharePercent);

/// <summary>درخواست صدور فاکتور</summary>
public record CreateInvoiceDto(
    long PatientId,
    long? ReceptionId,
    long? VisitId,
    InvoiceType Type,
    long? InsuranceCompanyId,
    decimal TaxPercent,
    List<InvoiceItemDto> Items,
    string? Description = null);

/// <summary>قلم فاکتور در پاسخ</summary>
public record InvoiceItemDetailDto(
    long? ServiceId,
    string? Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal PatientShare,
    decimal InsuranceShare,
    decimal TotalPrice);

/// <summary>جزئیات فاکتور</summary>
public record InvoiceDetailDto(
    long Id,
    string? InvoiceNumber,
    long PatientId,
    string PatientName,
    string DateShamsi,
    InvoiceType Type,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal Tax,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    PaymentStatus PaymentStatus,
    InvoiceStatus Status,
    List<InvoiceItemDetailDto> Items);

/// <summary>درخواست ثبت پرداخت</summary>
public record RecordPaymentDto(
    long? InvoiceId,
    long PatientId,
    decimal Amount,
    PaymentMethod Method,
    string? ReferenceNumber = null,
    string? CardNumber = null);

/// <summary>ردیف فهرست پرداخت‌ها</summary>
public record PaymentListDto(
    long Id,
    string? PaymentNumber,
    string PatientName,
    decimal Amount,
    PaymentMethod Method,
    string DateShamsi,
    string? ReferenceNumber);
""")

write("Features/Billing/CreateInvoice/CreateInvoiceCommand.cs", """
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Accounting;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Application.Interfaces;
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
                DoctorShareAmount = net * item.DoctorSharePercent / 100m
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
""")

write("Features/Billing/RecordPayment/RecordPaymentCommand.cs", """
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Accounting;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Events;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Billing.Commands.RecordPayment;

/// <summary>
/// فرمان ثبت پرداخت — به‌روزرسانی وضعیت فاکتور و سند حسابداری دریافتی
/// </summary>
/// <param name="Dto">داده پرداخت</param>
public record RecordPaymentCommand(RecordPaymentDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج ثبت پرداخت</summary>
public class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.Dto.PatientId).GreaterThan(0).WithMessage("شناسه بیمار الزامی است");
        RuleFor(x => x.Dto.Amount).GreaterThan(0).WithMessage("مبلغ پرداخت باید بیشتر از صفر باشد");
        RuleFor(x => x.Dto.InvoiceId).NotNull()
            .When(x => x.Dto.Amount > 0)
            .WithMessage("فاکتور مقصد پرداخت الزامی است");
    }
}

/// <summary>گرداننده ثبت پرداخت</summary>
public class RecordPaymentCommandHandler : IRequestHandler<RecordPaymentCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<RecordPaymentCommandHandler> _logger;

    private const string CashAccountCode = "1101";
    private const string PatientReceivableCode = "1103";

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public RecordPaymentCommandHandler(IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ILogger<RecordPaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>اجرای ثبت پرداخت</summary>
    public async Task<Result<long>> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(dto.PatientId, cancellationToken);
        if (patient is null)
            return Result<long>.Failure(Error.NotFound("بیمار", dto.PatientId));

        var invoice = await _unitOfWork.Repository<Invoice>().GetByIdAsync(dto.InvoiceId!.Value, cancellationToken);
        if (invoice is null)
            return Result<long>.Failure(Error.NotFound("فاکتور", dto.InvoiceId.Value));

        if (invoice.RemainingAmount < dto.Amount)
            return Result<long>.Failure(new Error("OVERPAYMENT",
                $"مبلغ پرداخت بیش از مانده فاکتور است — مانده {invoice.RemainingAmount:N0} ریال"));

        var now = DateTime.UtcNow;
        var payment = new Payment
        {
            PatientId = dto.PatientId,
            InvoiceId = invoice.Id,
            Amount = dto.Amount,
            Method = dto.Method,
            PaymentDate = now,
            ReferenceNumber = dto.ReferenceNumber,
            CardNumber = dto.CardNumber,
            Status = PaymentRecordStatus.Completed,
            CashierId = _currentUser.UserId
        };

        await _unitOfWork.Repository<Payment>().AddAsync(payment, cancellationToken);

        // به‌روزرسانی وضعیت مالی فاکتور
        invoice.PaidAmount += dto.Amount;
        invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;
        invoice.PaymentStatus = invoice.RemainingAmount <= 0
            ? PaymentStatus.Paid
            : PaymentStatus.PartiallyPaid;

        if (invoice.PaymentStatus == PaymentStatus.Paid)
            invoice.AddDomainEvent(new InvoicePaidEvent(invoice.Id, dto.PatientId, invoice.PaidAmount));

        // ── سند حسابداری دریافتی — بدهکار صندوق، بستانکار مطالبات بیمار ──
        await GenerateReceiptJournalAsync(payment, invoice, cancellationToken);

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("پرداخت {Amount} ریال برای فاکتور {Number} ثبت شد",
            dto.Amount, invoice.InvoiceNumber);

        return Result<long>.Success(payment.Id);
    }

    /// <summary>سند حسابداری دریافت وجه</summary>
    private async Task GenerateReceiptJournalAsync(Payment payment, Invoice invoice, CancellationToken ct)
    {
        var cash = await _unitOfWork.Repository<AccountingAccount>()
            .FirstOrDefaultAsync(a => a.AccountCode == CashAccountCode, ct);
        var receivable = await _unitOfWork.Repository<AccountingAccount>()
            .FirstOrDefaultAsync(a => a.AccountCode == PatientReceivableCode, ct);

        if (cash is null || receivable is null)
        {
            _logger.LogWarning("کدینگ حسابداری آماده نیست — سند پرداخت {Id} تولید نشد", payment.Id);
            return;
        }

        var entry = new JournalEntry
        {
            EntryDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EntryDateShamsi = PersianDateHelper.ToShamsi(DateTime.UtcNow),
            Description = $"دریافت وجه فاکتور {invoice.InvoiceNumber}",
            ReferenceType = JournalReferenceType.Payment,
            ReferenceId = payment.Id,
            TotalDebit = payment.Amount,
            TotalCredit = payment.Amount,
            Status = JournalEntryStatus.Approved
        };

        entry.Lines.Add(new JournalEntryLine
        {
            LineNumber = 1,
            AccountId = cash.Id,
            Debit = payment.Amount,
            Credit = 0,
            Description = "دریافت نقدی/کارتی"
        });
        entry.Lines.Add(new JournalEntryLine
        {
            LineNumber = 2,
            AccountId = receivable.Id,
            Debit = 0,
            Credit = payment.Amount,
            Description = "تسویه مطالبات بیمار"
        });

        await _unitOfWork.Repository<JournalEntry>().AddAsync(entry, ct);
    }
}
""")

write("Features/Billing/GetInvoices/GetInvoicesQueries.cs", """
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Billing.Queries;

/// <summary>پرس‌وجوی فهرست فاکتورها با صفحه‌بندی</summary>
public record GetInvoicesQuery(PagedRequest? Page = null, long? PatientId = null,
    PaymentStatus? Status = null) : IRequest<Result<PagedResponse<InvoiceListDto>>>;

/// <summary>گرداننده فهرست فاکتورها</summary>
public class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, Result<PagedResponse<InvoiceListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetInvoicesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<PagedResponse<InvoiceListDto>>> Handle(GetInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page ?? new PagedRequest();
        IQueryable<Invoice> query = _unitOfWork.Repository<Invoice>()
            .QueryableNoTracking.Include(i => i.Patient);

        if (request.PatientId.HasValue)
            query = query.Where(i => i.PatientId == request.PatientId.Value);
        if (request.Status.HasValue)
            query = query.Where(i => i.PaymentStatus == request.Status.Value);
        if (!string.IsNullOrWhiteSpace(page.Search))
        {
            var term = page.Search.Trim();
            query = query.Where(i =>
                i.InvoiceNumber != null && i.InvoiceNumber.Contains(term) ||
                i.Patient != null && i.Patient.LastName.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page.PageNumber - 1) * page.PageSize)
            .Take(page.PageSize)
            .Select(i => new InvoiceListDto(
                i.Id, i.InvoiceNumber,
                i.Patient != null ? i.Patient.FirstName + " " + i.Patient.LastName : "",
                i.InvoiceDateShamsi ?? string.Empty,
                i.TotalAmount, i.PaidAmount, i.RemainingAmount,
                i.PaymentStatus, i.Status))
            .ToListAsync(cancellationToken);

        return Result<PagedResponse<InvoiceListDto>>.Success(
            PagedResponse<InvoiceListDto>.Create(items, page.PageNumber, page.PageSize, total));
    }
}

/// <summary>پرس‌وجوی جزئیات فاکتور با اقلام</summary>
public record GetInvoiceByIdQuery(long Id) : IRequest<Result<InvoiceDetailDto>>;

/// <summary>گرداننده جزئیات فاکتور</summary>
public class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetInvoiceByIdQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<InvoiceDetailDto>> Handle(GetInvoiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var i = await _unitOfWork.Repository<Invoice>()
            .QueryableNoTracking
            .Include(x => x.Patient)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (i is null)
            return Result<InvoiceDetailDto>.Failure(Error.NotFound("فاکتور", request.Id));

        var dto = new InvoiceDetailDto(
            i.Id, i.InvoiceNumber, i.PatientId,
            i.Patient != null ? i.Patient.FirstName + " " + i.Patient.LastName : "",
            i.InvoiceDateShamsi ?? string.Empty,
            i.InvoiceType, i.SubTotal, i.DiscountAmount, i.Tax,
            i.TotalAmount, i.PaidAmount, i.RemainingAmount,
            i.PaymentStatus, i.Status,
            i.Items.Select(it => new InvoiceItemDetailDto(
                it.ServiceId, it.Description, it.Quantity, it.UnitPrice,
                it.DiscountAmount, it.PatientShare, it.InsuranceShare, it.TotalPrice)).ToList());

        return Result<InvoiceDetailDto>.Success(dto);
    }
}
""")

write("Controllers/InvoicesController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Application.Features.Billing.Commands.CreateInvoice;
using Sahand.ClinicPro.Application.Features.Billing.Queries;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر فاکتور — صدور صورت‌حساب با سند حسابداری خودکار
/// مسیرها: /api/v1/invoices
/// </summary>
[ApiController]
[Route("api/v1/invoices")]
[Authorize]
public class InvoicesController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public InvoicesController(IMediator mediator) => _mediator = mediator;

    /// <summary>فهرست فاکتورها با فیلتر بیمار و وضعیت پرداخت</summary>
    [HttpGet]
    [HasPermission("invoices.view")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] long? patientId = null, [FromQuery] PaymentStatus? status = null,
        [FromQuery] string? search = null)
    {
        var result = await _mediator.Send(new GetInvoicesQuery(
            new PagedRequest { PageNumber = page, PageSize = pageSize, Search = search },
            patientId, status));
        return ToResponse(result);
    }

    /// <summary>جزئیات کامل فاکتور با اقلام و سهم‌ها</summary>
    /// <param name="id">شناسه فاکتور</param>
    [HttpGet("{id:long}")]
    [HasPermission("invoices.view")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _mediator.Send(new GetInvoiceByIdQuery(id));
        return ToResponse(result);
    }

    /// <summary>صدور فاکتور جدید — سهم بیمه محاسبه و سند حسابداری تولید می‌شود</summary>
    [HttpPost]
    [HasPermission("invoices.create")]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
    {
        var result = await _mediator.Send(new CreateInvoiceCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetById), new { id = result.Value }, "فاکتور صادر شد");
        return ToResponse(result);
    }
}
""", API)

write("Controllers/PaymentsController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Application.Features.Billing.Commands.RecordPayment;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر پرداخت — ثبت دریافتی با به‌روزرسانی خودکار وضعیت فاکتور
/// مسیرها: /api/v1/payments
/// </summary>
[ApiController]
[Route("api/v1/payments")]
[Authorize]
public class PaymentsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public PaymentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>ثبت پرداخت — نقدی یا کارتی یا آنلاین</summary>
    [HttpPost]
    [HasPermission("payments.create")]
    public async Task<IActionResult> Record([FromBody] RecordPaymentDto dto)
    {
        var result = await _mediator.Send(new RecordPaymentCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, "GetAll", new { }, "پرداخت با موفقیت ثبت شد");
        return ToResponse(result);
    }
}
""", API)

print("✅ گام ۱۷ — Invoice + Payment کامل شد")

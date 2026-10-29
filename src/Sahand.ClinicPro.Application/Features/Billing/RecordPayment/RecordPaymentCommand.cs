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
        if (dto.InvoiceId is not > 0 || dto.Amount <= 0 || dto.Amount > 999999999999999999m ||
            decimal.Truncate(dto.Amount) != dto.Amount || dto.ReferenceNumber?.Length > 50 || dto.CardNumber?.Length > 20)
            return Result<long>.Failure(new Error("PAYMENT.INVALID", "شناسه فاکتور، مبلغ صحیح ریالی یا اطلاعات پرداخت نامعتبر است."));

        // FindAsync ممکن است رکورد ردیابی‌شده را بدون اعمال فیلتر شعبه برگرداند.
        // پیش از دسترسی به موجودیت‌های قابل ویرایش، دیده‌شدن هر دو را کنترل کن.
        if (!await _unitOfWork.Repository<Patient>().QueryableNoTracking
                .AnyAsync(p => p.Id == dto.PatientId && !p.IsDeleted, cancellationToken))
            return Result<long>.Failure(Error.NotFound("بیمار", dto.PatientId));
        if (!await _unitOfWork.Repository<Invoice>().QueryableNoTracking
                .AnyAsync(i => i.Id == dto.InvoiceId.Value && !i.IsDeleted, cancellationToken))
            return Result<long>.Failure(Error.NotFound("فاکتور", dto.InvoiceId.Value));

        var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(dto.PatientId, cancellationToken);
        if (patient is null || patient.IsDeleted)
            return Result<long>.Failure(Error.NotFound("بیمار", dto.PatientId));

        var invoice = await _unitOfWork.Repository<Invoice>().GetByIdAsync(dto.InvoiceId!.Value, cancellationToken);
        if (invoice is null || invoice.IsDeleted)
            return Result<long>.Failure(Error.NotFound("فاکتور", dto.InvoiceId.Value));

        if (invoice.PatientId != dto.PatientId)
            return Result<long>.Failure(new Error("PAYMENT.PATIENT_MISMATCH", "بیمار پرداخت با بیمار فاکتور مطابقت ندارد."));
        if (invoice.Status is InvoiceStatus.Cancelled or InvoiceStatus.Refunded or InvoiceStatus.Draft)
            return Result<long>.Failure(new Error("PAYMENT.INVOICE_STATUS", "ثبت پرداخت برای فاکتور پیش‌نویس، ابطال‌شده یا برگشت‌خورده مجاز نیست."));
        if (!Enum.IsDefined(dto.Method))
            return Result<long>.Failure(new Error("PAYMENT.METHOD", "روش پرداخت معتبر نیست."));

        if (invoice.TotalAmount < 0 || invoice.PaidAmount < 0 || invoice.RemainingAmount < 0 ||
            invoice.RemainingAmount != invoice.TotalAmount - invoice.PaidAmount)
            return Result<long>.Failure(new Error("PAYMENT.INVOICE_BALANCE", "مانده فاکتور ناسازگار است؛ پیش از ثبت پرداخت بررسی مالی لازم است."));

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

        // پرداخت و فاکتور در تراکنش فرمان ذخیره می‌شوند تا شناسهٔ واقعی پرداخت
        // پیش از ایجاد سند حسابداری در دسترس باشد.
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
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

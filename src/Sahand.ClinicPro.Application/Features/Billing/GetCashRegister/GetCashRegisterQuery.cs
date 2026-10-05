using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Billing.GetCashRegister;

/// <summary>خلاصه صندوق امروز — فرم ۱۴ و ۱۵</summary>
public record CashRegisterSummaryDto(
    int InvoiceCount,
    decimal CashTotal,
    decimal CardTotal,
    decimal OnlineTotal,
    decimal RefundTotal,
    decimal NetTotal);

/// <summary>تراکنش امروز صندوق</summary>
public record CashTransactionDto(
    long PaymentId,
    string? PaymentNumber,
    string Time,
    long? InvoiceId,
    string PatientName,
    decimal Amount,
    int Method,
    string MethodText);

/// <summary>پرس‌وجوی خلاصه صندوق روز</summary>
public record GetCashRegisterQuery : IRequest<Result<CashRegisterSummaryDto>>;

/// <summary>گرداننده خلاصه صندوق</summary>
public class GetCashRegisterQueryHandler
    : IRequestHandler<GetCashRegisterQuery, Result<CashRegisterSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetCashRegisterQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<CashRegisterSummaryDto>> Handle(GetCashRegisterQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var payments = await _unitOfWork.Repository<Payment>().QueryableNoTracking
            .Where(p => p.PaymentDate >= today && p.PaymentDate < tomorrow)
            .Include(p => p.Patient)
            .ToListAsync(cancellationToken);

        var invoiceCount = await _unitOfWork.Repository<Invoice>().QueryableNoTracking
            .CountAsync(i => i.InvoiceDate >= today && i.InvoiceDate < tomorrow, cancellationToken);

        var summary = new CashRegisterSummaryDto(
            invoiceCount,
            payments.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.Amount),
            payments.Where(p => p.Method == PaymentMethod.Card).Sum(p => p.Amount),
            payments.Where(p => p.Method == PaymentMethod.Online).Sum(p => p.Amount),
            payments.Where(p => p.Method == PaymentMethod.Cheque).Sum(p => p.Amount),
            payments.Sum(p => p.Amount));

        return Result<CashRegisterSummaryDto>.Success(summary);
    }
}

/// <summary>پرس‌وجوی تراکنش‌های امروز صندوق</summary>
public record GetTodayTransactionsQuery : IRequest<Result<List<CashTransactionDto>>>;

/// <summary>گرداننده تراکنش‌های امروز</summary>
public class GetTodayTransactionsQueryHandler
    : IRequestHandler<GetTodayTransactionsQuery, Result<List<CashTransactionDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetTodayTransactionsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<CashTransactionDto>>> Handle(GetTodayTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var items = await _unitOfWork.Repository<Payment>().QueryableNoTracking
            .Where(p => p.PaymentDate >= today && p.PaymentDate < tomorrow)
            .Include(p => p.Patient)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new CashTransactionDto(
                p.Id,
                p.PaymentNumber,
                p.PaymentDate.ToString("HH:mm"),
                p.InvoiceId,
                p.Patient != null ? p.Patient.FirstName + " " + p.Patient.LastName : "",
                p.Amount,
                (int)p.Method,
                p.Method == PaymentMethod.Cash ? "💵 نقد" :
                p.Method == PaymentMethod.Card ? "💳 کارت" :
                p.Method == PaymentMethod.Online ? "🏦 آنلاین" :
                p.Method == PaymentMethod.Cheque ? "🧾 چک" : "سایه"))
            .ToListAsync(cancellationToken);

        return Result<List<CashTransactionDto>>.Success(items);
    }
}

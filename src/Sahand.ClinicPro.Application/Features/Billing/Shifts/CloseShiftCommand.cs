using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Billing.Shifts;

/// <summary>دستور بستن شیفت صندوق و تحویل — چرخه ۱۳ (درخواست ۲-۲)</summary>
public record CloseShiftCommand(CloseShiftDto Dto) : IRequest<Result<ShiftReceiptDto>>;

/// <summary>ورودی بستن شیفت — شمارش فیزیکی + تحویل‌گیرنده</summary>
public record CloseShiftDto(
    decimal CountedCash,
    decimal CountedCard,
    long? HandoverToUserId,
    string? HandoverNote);

/// <summary>رسید تحویل شیفت — قابل چاپ</summary>
public record ShiftReceiptDto(
    long ShiftId,
    string ReceiptNumber,
    string RegisterName,
    string OpenedText,
    string ClosedText,
    string ClosedByName,
    decimal OpeningBalance,
    decimal TotalCash,
    decimal TotalCard,
    decimal TotalOnline,
    decimal SystemTotal,
    decimal CountedCash,
    decimal CountedCard,
    decimal CountedTotal,
    decimal CashDifference,
    decimal CardDifference,
    decimal NetDifference,
    string DifferenceState, // کم/اضافه/مطابقت
    string DifferenceColor,
    string HandoverNote,
    int PaymentCount);

/// <summary>ردیف تاریخچه شیفت‌ها</summary>
public record ShiftHistoryRowDto(
    long Id,
    string ReceiptNumber,
    string RegisterName,
    string ClosedText,
    string ClosedByName,
    decimal TotalCash,
    decimal TotalCard,
    decimal TotalOnline,
    decimal Difference,
    string DifferenceState,
    string DifferenceColor,
    string HandoverNote);

/// <summary>پرس‌وجوی تاریخچه شیفت‌های بسته‌شده</summary>
public record GetShiftsQuery : IRequest<Result<List<ShiftHistoryRowDto>>>;

/// <summary>
/// گرداننده بستن شیفت — اختلاف‌سنجی سیستم↔شمارش فیزیکی + رسید تحویل
/// </summary>
public class CloseShiftCommandHandler : IRequestHandler<CloseShiftCommand, Result<ShiftReceiptDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    /// <summary>سازنده</summary>
    public CloseShiftCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <summary>اجرا</summary>
    public async Task<Result<ShiftReceiptDto>> Handle(CloseShiftCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var now = DateTime.UtcNow;

        // صندوق بازِ امروز یا ساخت صندوق ضمنی
        var register = await _unitOfWork.Repository<CashRegister>().QueryableTracking
            .Where(r => !r.IsDeleted && r.Status == CashRegisterStatus.Open)
            .OrderByDescending(r => r.OpenedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (register is null)
        {
            // صندوق ضمنی — از پایان آخرین شیفت بسته‌شده؛ در نبود آن، پنجره ۱۰ساعته شیفت استاندارد
            var lastClosed = await _unitOfWork.Repository<CashRegister>().QueryableNoTracking
                .Where(r => !r.IsDeleted && r.Status == CashRegisterStatus.Closed)
                .OrderByDescending(r => r.ClosedAt)
                .Select(r => r.ClosedAt)
                .FirstOrDefaultAsync(cancellationToken);
            var implicitOpen = lastClosed is { } lc && lc > now.AddHours(-24)
                ? lc
                : now.AddHours(-10);

            register = new CashRegister
            {
                RegisterName = "صندوق اصلی",
                OpenedAt = implicitOpen,
                OpeningBalance = 0m,
                Status = CashRegisterStatus.Open
            };
            await _unitOfWork.Repository<CashRegister>().AddAsync(register, cancellationToken);
        }

        // جمع پرداخت‌های ثبت‌شده پس از بازشدن صندوق (همان شیفت)
        var payments = await _unitOfWork.Repository<Payment>().QueryableNoTracking
            .Where(p => !p.IsDeleted && p.PaymentDate >= register.OpenedAt && p.PaymentDate <= now)
            .ToListAsync(cancellationToken);

        var totalCash = payments.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.Amount);
        var totalCard = payments.Where(p => p.Method == PaymentMethod.Card).Sum(p => p.Amount);
        var totalOnline = payments.Where(p => p.Method == PaymentMethod.Online).Sum(p => p.Amount);
        var systemTotal = totalCash + totalCard + totalOnline;

        var cashDiff = dto.CountedCash - totalCash;
        var cardDiff = dto.CountedCard - totalCard;
        var netDiff = cashDiff + cardDiff;

        // شماره رسید ترتیبی — بر اساس تعداد شیفت‌های بسته‌شده
        var closedCount = await _unitOfWork.Repository<CashRegister>().QueryableNoTracking
            .CountAsync(r => !r.IsDeleted && r.Status == CashRegisterStatus.Closed, cancellationToken);

        register.TotalCash = totalCash;
        register.TotalCard = totalCard;
        register.TotalOnline = totalOnline;
        register.ClosingBalance = systemTotal;
        register.CountedCash = dto.CountedCash;
        register.CountedCard = dto.CountedCard;
        register.Difference = netDiff;
        register.Status = CashRegisterStatus.Closed;
        register.ClosedAt = now;
        register.ClosedByUserId = _currentUser.UserId;
        register.ClosedByName = _currentUser.Username;
        register.HandoverToUserId = dto.HandoverToUserId;
        register.HandoverNote = dto.HandoverNote?.Trim();
        register.ReceiptNumber = $"SHR-{closedCount + 1:D4}";

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        var (state, color) = netDiff switch
        {
            0m => ("مطابقت کامل", "#2E7D32"),
            > 0 => ($"اضافه‌شمارش {netDiff:N0} ریال", "#F9A825"),
            _ => ($"کم‌شمارش {Math.Abs(netDiff):N0} ریال", "#C62828")
        };

        var receipt = new ShiftReceiptDto(
            register.Id,
            register.ReceiptNumber,
            register.RegisterName,
            register.OpenedAt is { } opened ? PersianDateHelper.ToShamsiWithTime(opened) : "—",
            PersianDateHelper.ToShamsiWithTime(now),
            _currentUser.Username ?? "—",
            register.OpeningBalance,
            totalCash, totalCard, totalOnline, systemTotal,
            dto.CountedCash, dto.CountedCard, dto.CountedCash + dto.CountedCard,
            cashDiff, cardDiff, netDiff,
            state, color,
            register.HandoverNote ?? "",
            payments.Count);

        return Result<ShiftReceiptDto>.Success(receipt);
    }
}

/// <summary>گرداننده تاریخچه شیفت‌ها</summary>
public class GetShiftsQueryHandler : IRequestHandler<GetShiftsQuery, Result<List<ShiftHistoryRowDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetShiftsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<ShiftHistoryRowDto>>> Handle(GetShiftsQuery request,
        CancellationToken cancellationToken)
    {
        var shifts = await _unitOfWork.Repository<CashRegister>().QueryableNoTracking
            .Where(r => !r.IsDeleted && r.Status == CashRegisterStatus.Closed)
            .OrderByDescending(r => r.ClosedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        var rows = shifts.Select(r =>
        {
            var netDiff = r.Difference;
            var (state, color) = netDiff switch
            {
                0m => ("مطابقت", "#2E7D32"),
                > 0 => ($"اضافه {netDiff:N0}", "#F9A825"),
                _ => ($"کم {Math.Abs(netDiff):N0}", "#C62828")
            };
            return new ShiftHistoryRowDto(
                r.Id,
                r.ReceiptNumber ?? $"SHR-{r.Id}",
                r.RegisterName,
                r.ClosedAt is { } c ? PersianDateHelper.ToShamsiWithTime(c) : "—",
                r.ClosedByName ?? "—",
                r.TotalCash, r.TotalCard, r.TotalOnline, r.Difference,
                state, color,
                r.HandoverNote ?? "");
        }).ToList();

        return Result<List<ShiftHistoryRowDto>>.Success(rows);
    }
}

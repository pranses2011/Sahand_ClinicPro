using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Receptions.Queries.GetReceptionReceipt;

/// <summary>
/// پرس‌وجوی دادهٔ قبض یک پذیرش — برای «چاپ مجدد قبض» پذیرش‌های امروز و
/// گذشته؛ قبض دقیقاً از دادهٔ واقعی ثبت‌شده (فاکتور و اقلام و پرداخت‌های
/// همان پذیرش) بازسازی می‌شود، نه از حافظهٔ فرم پذیرش.
/// </summary>
/// <param name="ReceptionId">شناسهٔ پذیرش</param>
public record GetReceptionReceiptQuery(long ReceptionId) : IRequest<Result<ReceptionReceiptDto>>;

/// <summary>گردانندهٔ بازسازی قبض پذیرش از دادهٔ ذخیره‌شده</summary>
public class GetReceptionReceiptQueryHandler
    : IRequestHandler<GetReceptionReceiptQuery, Result<ReceptionReceiptDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetReceptionReceiptQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<ReceptionReceiptDto>> Handle(GetReceptionReceiptQuery request,
        CancellationToken cancellationToken)
    {
        var reception = await _unitOfWork.Repository<Reception>().QueryableNoTracking
            .Include(r => r.Patient)
            .Include(r => r.Doctor).ThenInclude(d => d!.Employee)
            .Include(r => r.InsuranceCompany)
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId, cancellationToken);
        if (reception is null)
            return Result<ReceptionReceiptDto>.Failure(Error.NotFound("پذیرش", request.ReceptionId));

        var invoices = await _unitOfWork.Repository<Invoice>().QueryableNoTracking
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => !i.IsDeleted && i.ReceptionId == request.ReceptionId)
            .OrderBy(i => i.Id)
            .ToListAsync(cancellationToken);
        if (invoices.Count == 0)
            return Result<ReceptionReceiptDto>.Failure(new Error(
                "Reception.NoInvoice", "برای این پذیرش فاکتوری صادر نشده است؛ قبضی برای چاپ وجود ندارد"));

        var items = invoices
            .SelectMany(i => i.Items.Where(x => !x.IsDeleted))
            .Select(x => new ReceptionReceiptItemDto(
                x.Description ?? "قلم فاکتور", x.Quantity, x.UnitPrice))
            .ToList();

        var payments = invoices.SelectMany(i => i.Payments.Where(p => !p.IsDeleted)).ToList();
        var lastPayment = payments.OrderBy(p => p.Id).LastOrDefault();
        var itemsTotal = invoices.Sum(i => i.TotalAmount);
        var paid = invoices.Sum(i => i.PaidAmount);

        var doctorName = reception.Doctor?.Employee is { } emp
            ? $"{emp.FirstName} {emp.LastName}"
            : reception.ServiceName ?? "خدمت درمانی";

        return Result<ReceptionReceiptDto>.Success(new ReceptionReceiptDto(
            ReceptionId: reception.Id,
            ReceptionNumber: reception.ReceptionNumber,
            InvoiceNumbers: string.Join("، ", invoices.Select(i => i.InvoiceNumber ?? i.Id.ToString())),
            PatientName: reception.Patient is { } p ? $"{p.FirstName} {p.LastName}" : "—",
            NationalCode: reception.Patient?.NationalCode,
            Mobile: reception.Patient?.Mobile,
            DoctorName: doctorName,
            ServiceName: reception.ServiceName,
            RoomName: reception.Room != null ? (reception.Room.RoomName ?? reception.Room.RoomNumber) : null,
            InsuranceName: reception.InsuranceCompany?.Name ?? "آزاد (بدون بیمه)",
            DateText: PersianDateHelper.ToShamsi(reception.ReceptionDate),
            TimeText: reception.ReceptionDate.ToString("HH:mm"),
            Items: items,
            ItemsTotal: itemsTotal,
            InsuranceShare: invoices.Sum(i => i.InsuranceDiscount),
            PatientShare: Math.Max(0, itemsTotal - invoices.Sum(i => i.InsuranceDiscount)),
            Payable: itemsTotal - invoices.Sum(i => i.InsuranceDiscount),
            Paid: paid,
            PaymentMethod: lastPayment is null ? 1 : (int)lastPayment.Method,
            ReferenceNumber: lastPayment?.ReferenceNumber,
            CardNumber: lastPayment?.CardNumber));
    }
}

/// <summary>
/// پرس‌وجوی پذیرش‌های یک تاریخ مشخص — برای فهرست «تاریخچهٔ پذیرش و چاپ
/// مجدد قبض» روزهای گذشته (همان شکل ردیف‌های امروز + تاریخ شمسی).
/// </summary>
/// <param name="Date">تاریخ میلادی روز موردنظر</param>
public record GetReceptionsByDateQuery(DateOnly Date) : IRequest<Result<List<ReceptionListDto>>>;

/// <summary>گردانندهٔ پذیرش‌های تاریخ مشخص</summary>
public class GetReceptionsByDateQueryHandler
    : IRequestHandler<GetReceptionsByDateQuery, Result<List<ReceptionListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetReceptionsByDateQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<ReceptionListDto>>> Handle(GetReceptionsByDateQuery request,
        CancellationToken cancellationToken)
    {
        var invoiceTotals = await _unitOfWork.Repository<Invoice>().QueryableNoTracking
            .Where(i => !i.IsDeleted && i.ReceptionId != null)
            .GroupBy(i => i.ReceptionId!.Value)
            .Select(g => new { ReceptionId = g.Key, Total = g.Sum(x => x.TotalAmount), Paid = g.Sum(x => x.PaidAmount) })
            .ToDictionaryAsync(x => x.ReceptionId, cancellationToken);

        var rawRows = await _unitOfWork.Repository<Reception>().QueryableNoTracking
            .Include(r => r.Patient)
            .Include(r => r.Doctor).ThenInclude(d => d!.Employee)
            .Include(r => r.InsuranceCompany)
            .Where(r => DateOnly.FromDateTime(r.ReceptionDate) == request.Date)
            .OrderBy(r => r.ReceptionDate)
            .Select(r => new
            {
                r.Id,
                r.ReceptionNumber,
                PatientName = r.Patient != null ? r.Patient.FirstName + " " + r.Patient.LastName : "",
                DoctorName = r.Doctor != null && r.Doctor.Employee != null
                    ? r.Doctor.Employee.FirstName + " " + r.Doctor.Employee.LastName
                    : (r.ServiceName ?? "خدمت درمانی"),
                r.ReceptionDate,
                r.Status,
                InsuranceName = r.InsuranceCompany != null ? r.InsuranceCompany.Name : null,
                RoomName = r.Room != null ? (r.Room.RoomName ?? r.Room.RoomNumber) : null,
                r.WaitingTime,
                r.ServiceId
            })
            .ToListAsync(cancellationToken);

        var items = rawRows.Select(r => new ReceptionListDto(
            r.Id, r.ReceptionNumber, r.PatientName, r.DoctorName,
            r.ReceptionDate.ToString("HH:mm"), r.Status, r.InsuranceName, r.WaitingTime,
            r.RoomName,
            invoiceTotals.TryGetValue(r.Id, out var fin) ? fin.Total : 0m,
            invoiceTotals.TryGetValue(r.Id, out var fin2) ? fin2.Paid : 0m,
            r.ServiceId)).ToList();

        return Result<List<ReceptionListDto>>.Success(items);
    }
}

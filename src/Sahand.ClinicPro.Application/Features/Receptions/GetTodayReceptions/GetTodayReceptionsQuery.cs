using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Receptions.Queries.GetTodayReceptions;

/// <summary>
/// پرس‌وجوی پذیرش‌های امروز — صفحه پذیرش فعال
/// </summary>
/// <param name="DoctorId">فیلتر پزشک — اختیاری</param>
public record GetTodayReceptionsQuery(long? DoctorId = null) : IRequest<Result<List<ReceptionListDto>>>;

/// <summary>گرداننده پذیرش‌های روز</summary>
public class GetTodayReceptionsQueryHandler
    : IRequestHandler<GetTodayReceptionsQuery, Result<List<ReceptionListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetTodayReceptionsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<ReceptionListDto>>> Handle(GetTodayReceptionsQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(PersianDateHelper.IranNow());

        // ═══ چرخه سی‌ویکم (گزارش مالک) — جمع مالی هر پذیرش ═══
        // «هیچ مبلغی در هیچ‌کجای برنامه نمایش داده نمیشود» — فاکتورهای صادرشدهٔ
        // متصل به هر پذیرش (Invoice.ReceptionId) جمع‌بندی و همراه هر ردیف
        // فهرست پذیرش روز برگردانده می‌شود: جمع فاکتور + پرداخت‌شده. پذیرش بدون
        // فاکتور (صادرنشده/شکست‌خورده) با صفر نمایش داده می‌شود تا خلأ مالی
        // همان‌جا دیده شود، نه اینکه ستون خالی بماند.
        var invoiceTotals = await _unitOfWork.Repository<Invoice>().QueryableNoTracking
            .Where(i => !i.IsDeleted && i.ReceptionId != null)
            .GroupBy(i => i.ReceptionId!.Value)
            .Select(g => new { ReceptionId = g.Key, Total = g.Sum(x => x.TotalAmount), Paid = g.Sum(x => x.PaidAmount) })
            .ToDictionaryAsync(x => x.ReceptionId, cancellationToken);

        IQueryable<Reception> query = _unitOfWork.Repository<Reception>()
            .QueryableNoTracking
            .Include(r => r.Patient)
            .Include(r => r.Doctor).ThenInclude(d => d!.Employee)
            .Include(r => r.InsuranceCompany)
            .Where(r => DateOnly.FromDateTime(r.ReceptionDate) == today);

        if (request.DoctorId.HasValue)
            query = query.Where(r => r.DoctorId == request.DoctorId.Value);

        // نکتهٔ پیاده‌سازی: نگاشت دیکشنری مالی باید در حافظه انجام شود —
        // فراخوانی TryGetValue داخل درخت عبارت EF به SQL ترجمه نمی‌شود؛
        // پس نخست ردیف‌های خام خوانده می‌شوند سپس DTO در حافظه ساخته می‌شود
        var rawRows = await query
            .OrderBy(r => r.ReceptionDate)
            .Select(r => new
            {
                r.Id,
                r.ReceptionNumber,
                PatientName = r.Patient != null ? r.Patient.FirstName + " " + r.Patient.LastName : "",
                // چرخه ۲۴ — پذیرش خدمتی: به‌جای پزشک، نام خدمت نمایش داده می‌شود
                DoctorName = r.Doctor != null && r.Doctor.Employee != null
                    ? r.Doctor.Employee.FirstName + " " + r.Doctor.Employee.LastName
                    : (r.ServiceName ?? "خدمت درمانی"),
                r.ReceptionDate,
                r.Status,
                InsuranceName = r.InsuranceCompany != null ? r.InsuranceCompany.Name : null,
                // چرخهٔ ۳۹ — نام نمایشی اتاق خدمت (نام کامل یا شماره)؛ پذیرش ویزیت
                // پزشک تهی می‌ماند چون اتاق آن از برنامهٔ کاری خود پزشک می‌آید
                RoomName = r.Room != null ? (r.Room.RoomName ?? r.Room.RoomNumber) : null,
                r.WaitingTime,
                // چرخهٔ ۷۴ — شناسهٔ خدمت برای دسته‌بندی صف خدمات در نمایشگر سالن
                r.ServiceId
            })
            .ToListAsync(cancellationToken);

        // چرخه سی‌ویکم — ساخت نهایی DTO با مبالغ مالی (صفر وقتی فاکتوری صادر نشده)
        var items = rawRows.Select(r => new ReceptionListDto(
            r.Id,
            r.ReceptionNumber,
            r.PatientName,
            r.DoctorName,
            r.ReceptionDate.ToString("HH:mm"),
            r.Status,
            r.InsuranceName,
            r.WaitingTime,
            // چرخهٔ ۳۹ — اتاق انجام خدمت
            r.RoomName,
            invoiceTotals.TryGetValue(r.Id, out var fin) ? fin.Total : 0m,
            invoiceTotals.TryGetValue(r.Id, out var fin2) ? fin2.Paid : 0m,
            // چرخهٔ ۷۴ — شناسهٔ خدمت (دسته‌بندی صف خدمات در نمایشگر سالن)
            r.ServiceId)).ToList();

        return Result<List<ReceptionListDto>>.Success(items);
    }
}

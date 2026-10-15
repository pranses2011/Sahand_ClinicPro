using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Appointments;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Appointments.Queries.GetAppointmentsByDate;

/// <summary>
/// پرس‌وجوی نوبت‌های یک تاریخ — با فیلتر اختیاری پزشک و وضعیت
/// </summary>
/// <param name="DateShamsi">تاریخ شمسی مثل 1403/07/12 — تهی یعنی امروز</param>
/// <param name="DoctorId">فیلتر پزشک</param>
/// <param name="Status">فیلتر وضعیت</param>
public record GetAppointmentsByDateQuery(
    string? DateShamsi = null,
    long? DoctorId = null,
    Domain.Enums.AppointmentStatus? Status = null) : IRequest<Result<List<AppointmentListDto>>>;

/// <summary>گرداننده نوبت‌های روز — مرتب بر اساس ساعت</summary>
public class GetAppointmentsByDateQueryHandler
    : IRequestHandler<GetAppointmentsByDateQuery, Result<List<AppointmentListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetAppointmentsByDateQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<AppointmentListDto>>> Handle(GetAppointmentsByDateQuery request,
        CancellationToken cancellationToken)
    {
        var date = PersianDateHelper.ToGregorian(request.DateShamsi)?.Date
                   ?? DateTime.UtcNow.Date;

        IQueryable<Appointment> query = _unitOfWork.Repository<Appointment>()
            .QueryableNoTracking
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.Employee)
            .Where(a => a.AppointmentDate == DateOnly.FromDateTime(date));

        if (request.DoctorId.HasValue)
            query = query.Where(a => a.DoctorId == request.DoctorId.Value);
        if (request.Status.HasValue)
            query = query.Where(a => a.Status == request.Status.Value);

        var items = await query
            .OrderBy(a => a.AppointmentTime)
            .Select(a => new AppointmentListDto(
                a.Id,
                a.AppointmentCode,
                a.Patient != null ? a.Patient.FirstName + " " + a.Patient.LastName : "",
                a.Doctor != null && a.Doctor.Employee != null
                    ? a.Doctor.Employee.FirstName + " " + a.Doctor.Employee.LastName
                    : "پزشک",
                a.AppointmentDateShamsi ?? string.Empty,
                a.AppointmentTime.ToString("HH:mm"),
                a.Status,
                a.AppointmentType,
                a.QueueNumber ?? 0,
                // چرخهٔ ۷۸ — اولویت واقعی نوبت برمی‌گردد (پیش‌تر حذف بود و کلاینت
                // همیشه «عادی» را نمایش می‌داد)
                a.Priority))
            .ToListAsync(cancellationToken);

        return Result<List<AppointmentListDto>>.Success(items);
    }
}

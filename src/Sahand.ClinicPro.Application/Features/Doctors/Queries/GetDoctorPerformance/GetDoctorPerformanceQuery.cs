using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Queries.GetDoctorPerformance;

/// <summary>
/// پرس‌وجوی آمار عملکرد پزشک — ماه جاری + امروز
/// </summary>
public record GetDoctorPerformanceQuery(long DoctorId) : IRequest<Result<DoctorPerformanceDto>>;

/// <summary>
/// گرداننده آمار عملکرد
/// </summary>
public class GetDoctorPerformanceQueryHandler : IRequestHandler<GetDoctorPerformanceQuery, Result<DoctorPerformanceDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetDoctorPerformanceQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<DoctorPerformanceDto>> Handle(GetDoctorPerformanceQuery request,
        CancellationToken cancellationToken)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().QueryableNoTracking
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId, cancellationToken);
        if (doctor is null)
            return Result<DoctorPerformanceDto>.Failure(new Error("DOCTOR.NOTFOUND", "پزشک یافت نشد"));

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var todayStart = today;
        var todayEnd = todayStart.AddDays(1);

        var visits = _unitOfWork.Repository<Domain.Entities.Clinical.Visit>().QueryableNoTracking
            .Where(v => v.DoctorId == request.DoctorId);

        var visitsMonth = await visits.Where(v => v.VisitDate >= monthStart && v.VisitDate < todayEnd).CountAsync(cancellationToken);
        var visitsToday = await visits.Where(v => v.VisitDate >= todayStart && v.VisitDate < todayEnd).CountAsync(cancellationToken);

        var visitIds = visits.Where(v => v.VisitDate >= monthStart && v.VisitDate < todayEnd).Select(v => v.Id);
        var revenueMonth = await _unitOfWork.Repository<Invoice>().QueryableNoTracking
            .Where(i => i.VisitId != null && visitIds.Contains(i.VisitId.Value) &&
                        i.Status != Domain.Enums.InvoiceStatus.Cancelled)
            .SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;

        // میانگین مدت ویزیت — محاسبه در حافظه برای سازگاری با InMemory
        var timedVisits = await visits
            .Where(v => v.StartTime != null && v.EndTime != null && v.VisitDate >= monthStart)
            .Select(v => new { v.StartTime, v.EndTime })
            .ToListAsync(cancellationToken);
        var avgMinutes = timedVisits.Count == 0 ? 0 :
            Math.Round(timedVisits.Average(v => (v.EndTime!.Value - v.StartTime!.Value).TotalMinutes), 1);

        var appointmentsMonth = await _unitOfWork.Repository<Appointment>().QueryableNoTracking
            .CountAsync(a => a.DoctorId == request.DoctorId && a.AppointmentDate >= DateOnly.FromDateTime(monthStart), cancellationToken);
        var noShows = await _unitOfWork.Repository<Appointment>().QueryableNoTracking
            .CountAsync(a => a.DoctorId == request.DoctorId && a.AppointmentDate >= DateOnly.FromDateTime(monthStart) &&
                             a.Status == AppointmentStatus.NoShow, cancellationToken);
        var noShowRate = appointmentsMonth == 0 ? 0 : Math.Round(noShows * 100.0 / appointmentsMonth, 1);

        return Result<DoctorPerformanceDto>.Success(new DoctorPerformanceDto
        {
            VisitsThisMonth = visitsMonth,
            VisitsToday = visitsToday,
            RevenueThisMonth = revenueMonth,
            CommissionThisMonth = Math.Round(revenueMonth * doctor.DoctorPercentage / 100m),
            AverageVisitMinutes = Math.Round(avgMinutes, 1),
            Rating = doctor.Rating,
            NoShowRate = noShowRate
        });
    }
}

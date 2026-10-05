using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Queries.GetDoctorAbsences;

/// <summary>
/// پرس‌وجوی غیبت‌های آینده و امروز پزشک
/// </summary>
public record GetDoctorAbsencesQuery(long DoctorId) : IRequest<Result<List<AbsenceDto>>>;

/// <summary>
/// گرداننده غیبت‌ها — همراه با شمارش نوبت‌های رزروشده برای هشدار
/// </summary>
public class GetDoctorAbsencesQueryHandler : IRequestHandler<GetDoctorAbsencesQuery, Result<List<AbsenceDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetDoctorAbsencesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<AbsenceDto>>> Handle(GetDoctorAbsencesQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var absences = await _unitOfWork.Repository<DoctorAbsence>().QueryableNoTracking
            .Where(a => a.DoctorId == request.DoctorId && a.AbsenceDate >= today)
            .OrderBy(a => a.AbsenceDate)
            .Take(100)
            .ToListAsync(cancellationToken);

        var doctor = await _unitOfWork.Repository<Domain.Entities.People.Doctor>()
            .QueryableNoTracking.FirstOrDefaultAsync(d => d.Id == request.DoctorId, cancellationToken);
        if (doctor is null)
            return Result<List<AbsenceDto>>.Failure(new Error("DOCTOR.NOTFOUND", "پزشک یافت نشد"));

        var result = new List<AbsenceDto>();
        foreach (var a in absences)
        {
            var booked = await _unitOfWork.Repository<Appointment>().QueryableNoTracking
                .CountAsync(x => x.DoctorId == request.DoctorId &&
                                 x.AppointmentDate == a.AbsenceDate &&
                                 x.Status != AppointmentStatus.Cancelled &&
                                 x.Status != AppointmentStatus.NoShow, cancellationToken);

            result.Add(new AbsenceDto
            {
                Id = a.Id,
                DateShamsi = PersianDateHelper.ToShamsi(a.AbsenceDate.ToDateTime(TimeOnly.MinValue)),
                FromTime = a.FromTime?.ToString("HH:mm"),
                ToTime = a.ToTime?.ToString("HH:mm"),
                AbsenceTypeName = AbsenceName(a.AbsenceType),
                Reason = a.Reason,
                BookedAppointments = booked
            });
        }

        return Result<List<AbsenceDto>>.Success(result);
    }

    /// <summary>نام فارسی نوع غیبت</summary>
    private static string AbsenceName(AbsenceType type) => type switch
    {
        AbsenceType.Vacation => "مرخصی",
        AbsenceType.SickLeave => "استعلاجی",
        AbsenceType.Conference => "کنگره",
        AbsenceType.Other => "سایر",
        _ => type.ToString()
    };
}

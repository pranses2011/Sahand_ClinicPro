using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Queries.GetDoctorSchedule;

/// <summary>
/// پرس‌وجوی برنامه هفتگی پزشک — همیشه هفت ردیف برمی‌گرداند
/// </summary>
public record GetDoctorScheduleQuery(long DoctorId) : IRequest<Result<List<ScheduleRowDto>>>;

/// <summary>
/// گرداننده برنامه هفتگی — ردیف‌های ذخیره‌شده + تکمیل روزهای خالی
/// </summary>
public class GetDoctorScheduleQueryHandler : IRequestHandler<GetDoctorScheduleQuery, Result<List<ScheduleRowDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetDoctorScheduleQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — هفت ردیف کامل فارسی از شنبه تا جمعه</summary>
    public async Task<Result<List<ScheduleRowDto>>> Handle(GetDoctorScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _unitOfWork.Repository<Domain.Entities.Scheduling.DoctorSchedule>().QueryableNoTracking
            .Where(s => s.DoctorId == request.DoctorId && s.IsActive)
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync(cancellationToken);

        var rooms = await _unitOfWork.Repository<Domain.Entities.Scheduling.ExaminationRoom>()
            .QueryableNoTracking.ToListAsync(cancellationToken);

        var result = new List<ScheduleRowDto>();
        for (byte day = 0; day <= 6; day++)
        {
            var row = rows.FirstOrDefault(r => r.DayOfWeek == day);
            if (row is null)
            {
                result.Add(new ScheduleRowDto { DayOfWeek = day, IsEnabled = false });
                continue;
            }

            var room = rooms.FirstOrDefault(r => r.Id == row.RoomId);
            result.Add(new ScheduleRowDto
            {
                DayOfWeek = day,
                IsEnabled = true,
                StartTime = row.StartTime.ToString("HH:mm"),
                EndTime = row.EndTime.ToString("HH:mm"),
                BreakStart = row.BreakStart?.ToString("HH:mm"),
                BreakEnd = row.BreakEnd?.ToString("HH:mm"),
                RoomId = row.RoomId,
                RoomName = room is null ? null : RoomDisplay(room),
                MaxPatients = row.MaxPatients
            });
        }

        return Result<List<ScheduleRowDto>>.Success(result);
    }

    /// <summary>نام نمایشی اتاق</summary>
    private static string RoomDisplay(Domain.Entities.Scheduling.ExaminationRoom room) =>
        string.IsNullOrWhiteSpace(room.RoomName)
            ? $"اتاق {room.RoomNumber ?? room.Id.ToString()}"
            : room.RoomName;
}

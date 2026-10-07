using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Visits.GetVisitQueue;

/// <summary>ردیف صف ویزیت</summary>
public record QueueItemDto(
    long VisitId,
    long PatientId,
    string PatientName,
    string Mobile,
    int QueueNumber,
    string AppointmentTime,
    string? InsuranceName,
    string VisitTypeText,
    int WaitingMinutes,
    string? ChiefComplaint,
    string? Allergies);

/// <summary>بخش‌های صف روز</summary>
public record VisitQueueDto(
    QueueItemDto? Current,
    List<QueueItemDto> Waiting,
    List<DoneItemDto> Done,
    int VisitedCount,
    int InProgressCount,
    int WaitingCount,
    double AverageVisitMinutes);

/// <summary>ردیف ویزیت‌شده‌ها</summary>
public record DoneItemDto(
    long VisitId,
    string PatientName,
    string TimeRange,
    string? DiagnosisText);

/// <summary>پرس‌وجوی صف ویزیت امروز یک پزشک — فرم ۱۳</summary>
public record GetVisitQueueQuery(long? DoctorId) : IRequest<Result<VisitQueueDto>>;

/// <summary>گرداننده صف ویزیت</summary>
public class GetVisitQueueQueryHandler : IRequestHandler<GetVisitQueueQuery, Result<VisitQueueDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetVisitQueueQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — صف امروز بر اساس پذیرش‌ها</summary>
    public async Task<Result<VisitQueueDto>> Handle(GetVisitQueueQuery request,
        CancellationToken cancellationToken)
    {
        // چرخه بیست‌ویکم (گزارش پانزدهم مالک): «بعد از پذیرش، بیمار در لیست ویزیت و
        // معاینه نمایش داده نمی‌شود» — مرز روز بر پایهٔ UTC همانند زمان ثبت پذیرش
        // (DateTime.UtcNow) گرفته شد؛ پیش‌تر DateTime.Today (زمان محلی سرور) بود و
        // هرگاه منطقهٔ زمانی سرور با UTC فرق می‌کرد پذیرش تازه در مرز روز گم می‌شد.
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var receptions = await _unitOfWork.Repository<Reception>().QueryableNoTracking
            .Where(r => r.ReceptionDate >= today && r.ReceptionDate < tomorrow &&
                        r.Status != ReceptionStatus.Cancelled &&
                        (request.DoctorId == null || r.DoctorId == request.DoctorId))
            .Include(r => r.Patient)
            .Include(r => r.Doctor).ThenInclude(d => d!.Employee)
            .Include(r => r.Appointment) // چرخه بیست‌ویکم: نوع ویزیت (اولیه/پیگیری/اورژانس) از نوبت مرتبط
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var todayVisits = await _unitOfWork.Repository<Visit>().QueryableNoTracking
            .Where(v => v.VisitDate >= today && v.VisitDate < tomorrow &&
                        (request.DoctorId == null || v.DoctorId == request.DoctorId))
            .Include(v => v.PrimaryDiagnosis)
            .ToListAsync(cancellationToken);

        var visitsByReception = todayVisits
            .Where(v => v.ReceptionId.HasValue)
            .GroupBy(v => v.ReceptionId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(v => v.Id).First());

        var waiting = new List<QueueItemDto>();
        QueueItemDto? current = null;
        var done = new List<DoneItemDto>();

        foreach (var r in receptions)
        {
            var patientName = $"{r.Patient?.FirstName} {r.Patient?.LastName}";
            var queueNumber = (int)(r.Id % 100);
            var appointmentTime = r.ReceptionDate.ToString("HH:mm");
            var waitingMinutes = (int)(DateTime.UtcNow - r.ReceptionDate).TotalMinutes;
            var visitTypeText = r.Appointment != null && r.Appointment.AppointmentType == AppointmentType.FollowUp ? "پیگیری" :
                r.Appointment != null && r.Appointment.AppointmentType == AppointmentType.Emergency ? "اورژانس" : "اولیه";

            var item = new QueueItemDto(
                r.Id, r.PatientId, patientName, r.Patient?.Mobile ?? "",
                queueNumber, appointmentTime, null, visitTypeText,
                waitingMinutes, r.Notes, r.Patient?.Allergies);

            var visit = visitsByReception.TryGetValue(r.Id, out var v) ? v : null;
            if (visit is not null && visit.Status == VisitStatus.InProgress)
            {
                current = item with { VisitId = visit.Id };
            }
            else if (visit is not null && visit.Status == VisitStatus.Completed)
            {
                done.Add(new DoneItemDto(visit.Id, patientName,
                    $"{visit.StartTime:HH\\:mm}-{visit.EndTime:HH\\:mm}",
                    visit.PrimaryDiagnosis?.DescriptionFa));
            }
            else
            {
                waiting.Add(item);
            }
        }

        var avgMinutes = todayVisits
            .Where(v => v.StartTime is not null && v.EndTime is not null)
            .Select(v => (v.EndTime!.Value - v.StartTime!.Value).TotalMinutes)
            .DefaultIfEmpty(0)
            .Average();

        return Result<VisitQueueDto>.Success(new VisitQueueDto(
            current, waiting, done,
            done.Count, current is not null ? 1 : 0, waiting.Count,
            Math.Round(avgMinutes, 1)));
    }
}

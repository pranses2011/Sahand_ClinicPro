using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Visits.GetVisitQueue;

/// <summary>ردیف صف ویزیت</summary>
/// <remarks>
/// چرخهٔ ۷۵ (گزارش مالک): «در صف انتظار ویزیت اگه پزشک انتخاب نشده باشه و
/// عمومی باشه، زیر اسم بیمار اسم پزشک هم نوشته بشه» — DoctorName اضافه شد؛
/// در صفِ یک پزشکِ مشخص تهی می‌ماند (تکراری است) و فقط در صف عمومی پر می‌شود.
/// چرخهٔ ۷۸: RoomName برای «نام اتاق زیر نام بیمار» در نمایشگر سالن انتظار
/// (گزارش مالک همین چرخه) اضافه شد — در صف ویزیت و صف خدمات هر دو.
/// </remarks>
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
    string? Allergies,
    string? DoctorName = null,
    string? RoomName = null);

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
                        // ═══ چرخهٔ ۷۸ (گزارش مالک): «در صفحه ویزیت و معاینه فقط صف
                        // پذیرش‌های پزشکان رو نشون بده» — پذیرش‌های خدمتیِ بدون پزشک
                        // از این صف کنار رفتند و به صفحهٔ مستقل «صف خدمات» رفتند ═══
                        r.DoctorId != null &&
                        (request.DoctorId == null || r.DoctorId == request.DoctorId))
            .Include(r => r.Patient)
            .Include(r => r.Doctor).ThenInclude(d => d!.Employee)
            .Include(r => r.Appointment) // چرخه بیست‌ویکم: نوع ویزیت (اولیه/پیگیری/اورژانس) از نوبت مرتبط
            .Include(r => r.Room) // چرخهٔ ۷۸: نام اتاق زیر نام بیمار (نمایشگر سالن)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

        // ═══ چرخهٔ ۷۸ (گزارش مالک: «زیر نام بیماران اسم اتاق هم نوشته بشه») ═══
        // پذیرش‌های پزشکی RoomId ندارند (اتاق فقط روی پذیرش خدمتی ست می‌شود)؛
        // اتاق ویزیت هر پزشک از «برنامهٔ کاری هفتگی» او برای امروز حل می‌شود:
        // DayOfWeek همان تقویم ایرانی (۰=شنبه..۶=جمعه) + بازهٔ اعتبار + فعال.
        // نگاشت doctorId → نام اتاق، یک‌جا برای کل صف (بدون N+1).
        var persianToday = (byte)((int)today.DayOfWeek + 1) % 7; // شنبه=.NET Saturday=6 → 0
        var scheduleRooms = await _unitOfWork.Repository<DoctorSchedule>().QueryableNoTracking
            .Where(s => s.IsActive
                        && s.RoomId != null
                        && s.DayOfWeek == persianToday
                        && (s.ValidFrom == null || s.ValidFrom <= DateOnly.FromDateTime(today))
                        && (s.ValidTo == null || s.ValidTo >= DateOnly.FromDateTime(today)))
            .Select(s => new { s.DoctorId, RoomName = s.Room!.RoomName ?? s.Room.RoomNumber })
            .ToListAsync(cancellationToken);
        // اگر چند برنامهٔ هم‌روز اتاق‌های متفاوت داشتند، نخستین (پرانتخاب) برنده است؛
        // ردیف‌های با نام تهی (اتاق بدون نام و شماره) کنار گذاشته می‌شوند
        var roomByDoctor = scheduleRooms
            .Where(s => !string.IsNullOrWhiteSpace(s.RoomName))
            .GroupBy(s => s.DoctorId)
            .ToDictionary(g => g.Key, g => g.First().RoomName!);

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
                waitingMinutes, r.Notes, r.Patient?.Allergies,
                // ═══ چرخهٔ ۷۹ (گزارش مالک: «زیر نام بیماران نام پزشک و نام اتاق
                // نیست و بجاش نوشته «اولیه» هست») ═══
                // پیش‌تر نام پزشک «فقط در صف عمومی» پر می‌شد و در صف یک پزشکِ
                // مشخص تهی می‌ماند ⇒ نمایشگر سالن به‌جای نام پزشک به زیرنویس
                // نوع ویزیت («اولیه») می‌افتاد. اکنون نام پزشک «همیشه» ارسال
                // می‌شود و مصرف‌کننده (نمایشگر سالن/صف ویزیت) خودش تصمیم می‌گیرد.
                r.Doctor?.Employee != null
                    ? $"دکتر {r.Doctor.Employee.FirstName} {r.Doctor.Employee.LastName}".Trim()
                    : null,
                // چرخهٔ ۷۸ — نام اتاق ویزیت: اول اتاق خود پذیرش (خدمتی)، وگرنه
                // اتاق برنامهٔ امروز پزشک؛ نام کامل مقدم بر شمارهٔ اتاق
                r.Room is null && r.DoctorId is not null && roomByDoctor.TryGetValue(r.DoctorId.Value, out var scheduledRoom)
                    ? scheduledRoom
                    : (r.Room is null
                        ? null
                        : (r.Room.RoomName ?? r.Room.RoomNumber)));

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
            // ═══ چرخهٔ ۷۵ (گزارش مالک: «بیمار ویزیت‌شده از لیست صف پاک نمیشه») ═══
            // پذیرشِ «ویزیت‌شده/ترخیص‌شده» هرگز به صف منتظر برنمی‌گردد — حتی اگر
            // ویزیتش پیش‌نویس باشد (ذخیرهٔ بدون پایان) یا رکورد ویزیتش گم شده
            // باشد؛ پیش‌تر چنین ردیفی دوباره در «در انتظار» می‌نشست.
            else if (r.Status is ReceptionStatus.Visited or ReceptionStatus.Discharged)
            {
                // پایان‌یافته — نه در انتظار، نه فراخوان‌پذیر؛ فقط در صورت وجود
                // ویزیتِ کامل‌شده در فهرست «ویزیت‌شده‌ها» دیده می‌شود
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

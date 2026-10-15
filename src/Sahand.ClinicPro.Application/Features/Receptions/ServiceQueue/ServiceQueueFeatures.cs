using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Receptions.ServiceQueue;

/// <summary>
/// ═══ چرخهٔ ۷۸ (گزارش مالک) — صف مستقل خدمات درمانی ═══
/// «در صفحه ویزیت و معاینه فقط صف پذیرش‌های پزشکان رو نشون بده و برای صف و
/// انجام خدمات درمانی یک صفحه جداگانه باشه» — پیش‌تر پذیرش‌های خدمتیِ بدون
/// پزشک هم داخل صف ویزیت می‌آمدند و فراخوانشان خطای «این ردیف پذیرش خدمتی
/// است و پزشکی ندارد» می‌داد (خود پیام هم می‌گفت «از صفحهٔ همان خدمت پیگیری
/// شود» ولی چنین صفحه‌ای وجود نداشت!). اکنون:
///   • GetVisitQueue فقط پذیرش‌های «پزشک‌دار» را برمی‌گرداند؛
///   • این فیچر صف کامل خدمات را می‌سازد: در انتظار / در حال انجام / انجام‌شده؛
///   • فراخوان به اتاق (InVisit) و «انجام شد» (Visited) با پخش بلادرنگ صف.
/// </summary>
public record ServiceQueueItemDto(
    long ReceptionId,
    long PatientId,
    string PatientName,
    string Mobile,
    int QueueNumber,
    string ServiceName,
    string? RoomName,
    string ReceptionTime,
    int WaitingMinutes,
    string? Notes);

/// <summary>ردیف انجام‌شده‌های امروز</summary>
public record ServiceDoneItemDto(
    long ReceptionId,
    string PatientName,
    string ServiceName,
    string? RoomName);

/// <summary>بخش‌های صف خدمات روز</summary>
public record ServiceQueueDto(
    List<ServiceQueueItemDto> Waiting,
    List<ServiceQueueItemDto> InProgress,
    List<ServiceDoneItemDto> Done,
    int WaitingCount,
    int InProgressCount,
    int DoneCount);

/// <summary>پرس‌وجوی صف خدمات امروز — پذیرش‌های خدمتیِ بدون پزشک</summary>
public record GetServiceQueueQuery : IRequest<Result<ServiceQueueDto>>;

/// <summary>گرداننده صف خدمات</summary>
public class GetServiceQueueQueryHandler : IRequestHandler<GetServiceQueueQuery, Result<ServiceQueueDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetServiceQueueQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>
    /// اجرا — پذیرش‌های امروزِ خدمتی (ServiceId پر، بدون پزشک) به سه بخش
    /// «در انتظار / در حال انجام / انجام‌شده» تقسیم می‌شوند. مرز روز بر پایهٔ
    /// UTC همانند GetVisitQueue (درس چرخهٔ ۲۱ — پذیرش تازه در مرز روز گم نشود).
    /// نام خدمت از فیلد غیرنرمال‌شدهٔ خود پذیرش خوانده می‌شود (پایدار در فهرست‌ها
    /// — درس چرخهٔ ۲۴) و نام اتاق از ناوبری Room؛ هر دو سمت SQL (الگوی امن چرخهٔ ۷۸).
    /// </summary>
    public async Task<Result<ServiceQueueDto>> Handle(GetServiceQueueQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // چرخهٔ ۷۸: پیش‌پردازش با Select سمت SQL — بدون dereference ناوبری
        // سمت کلاینت (درس NullReference لیست انتظار همین چرخه)
        var rows = await _unitOfWork.Repository<Reception>().QueryableNoTracking
            .Where(r => r.ReceptionDate >= today && r.ReceptionDate < tomorrow &&
                        r.Status != ReceptionStatus.Cancelled &&
                        r.ServiceId != null && r.DoctorId == null)
            .OrderBy(r => r.Id)
            .Select(r => new
            {
                r.Id, r.PatientId, r.Status,
                // گارد تهی: پذیرشِ بی‌بیمار نباید پرس‌وجو را بترکاند (همان درس
                // NRE لیست انتظار همین چرخه) — نام جایگزین «بیمار ناشناس»
                PatientName = r.Patient != null
                    ? r.Patient.FirstName + " " + r.Patient.LastName
                    : "بیمار ناشناس",
                Mobile = r.Patient != null ? r.Patient.Mobile : "",
                ServiceName = r.ServiceName != null ? r.ServiceName : "خدمت درمانی",
                RoomName = r.Room != null
                    ? (r.Room.RoomName != null ? r.Room.RoomName : r.Room.RoomNumber)
                    : null,
                r.ReceptionDate, r.Notes
            })
            .ToListAsync(cancellationToken);

        var waiting = new List<ServiceQueueItemDto>();
        var inProgress = new List<ServiceQueueItemDto>();
        var done = new List<ServiceDoneItemDto>();

        foreach (var r in rows)
        {
            var item = new ServiceQueueItemDto(
                r.Id, r.PatientId, r.PatientName.Trim(), r.Mobile,
                (int)(r.Id % 100), r.ServiceName, r.RoomName,
                r.ReceptionDate.ToString("HH:mm"),
                (int)(DateTime.UtcNow - r.ReceptionDate).TotalMinutes,
                r.Notes);

            switch (r.Status)
            {
                // روی میز خدمت — فراخوانده شده ولی هنوز «انجام شد» نشده
                case ReceptionStatus.InVisit:
                    inProgress.Add(item);
                    break;
                // پایان‌یافته — «انجام‌شده‌های امروز»
                case ReceptionStatus.Visited:
                case ReceptionStatus.Discharged:
                    done.Add(new ServiceDoneItemDto(r.Id, r.PatientName.Trim(), r.ServiceName, r.RoomName));
                    break;
                // Admitted / InQueue — در انتظار نوبت خدمت
                default:
                    waiting.Add(item);
                    break;
            }
        }

        return Result<ServiceQueueDto>.Success(new ServiceQueueDto(
            waiting, inProgress, done, waiting.Count, inProgress.Count, done.Count));
    }
}

/// <summary>
/// فراخوان بیمارِ خدمت به اتاق انجام — وضعیت پذیرش به InVisit می‌رود و
/// تغییر صف بلادرنگ پخش می‌شود (همان الگوی CallPatient ویزیت، بدون ساخت ویزیت).
/// </summary>
public record CallServicePatientCommand(long ReceptionId) : IRequest<Result>;

/// <summary>گرداننده فراخوان خدمت</summary>
public class CallServicePatientCommandHandler : IRequestHandler<CallServicePatientCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealtimeNotifier _realtime;

    /// <summary>سازنده — با اتصال بلادرنگ برای پخش فوری تغییر صف</summary>
    public CallServicePatientCommandHandler(IUnitOfWork unitOfWork, IRealtimeNotifier realtime)
    {
        _unitOfWork = unitOfWork;
        _realtime = realtime;
    }

    /// <summary>اجرا — فراخوان به اتاق انجام خدمت</summary>
    public async Task<Result> Handle(CallServicePatientCommand request,
        CancellationToken cancellationToken)
    {
        var reception = await _unitOfWork.Repository<Reception>().QueryableTracking
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && !r.IsDeleted, cancellationToken);
        if (reception is null)
            return Result.Failure(new Error("SERVICEQ.NOT_FOUND", "ردیف پذیرش یافت نشد — فهرست را تازه کنید"));

        // فقط پذیرش خدمتی معتبر است — ویزیت پزشک از صفحهٔ ویزیت فراخوان می‌شود
        if (reception.ServiceId is null || reception.DoctorId is not null)
            return Result.Failure(new Error("SERVICEQ.NOT_SERVICE",
                "این ردیف پذیرش پزشکی است — فراخوانش از صفحهٔ «صف ویزیت» انجام می‌شود"));

        if (reception.Status == ReceptionStatus.Cancelled)
            return Result.Failure(new Error("SERVICEQ.CANCELLED", "پذیرش این بیمار لغو شده است"));

        // انجام‌شده دیگر فراخوان نمی‌شود — پیام روشن به‌جای خطای مبهم
        if (reception.Status is ReceptionStatus.Visited or ReceptionStatus.Discharged)
            return Result.Failure(new Error("SERVICEQ.ALREADY_DONE", "این خدمت قبلاً انجام شده است"));

        // ایدمپوتنت — فراخوان دوبارهٔ ردیف روی‌میز خطا نمی‌دهد؛ همان وضعیت می‌ماند
        if (reception.Status != ReceptionStatus.InVisit)
            reception.Status = ReceptionStatus.InVisit;

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        await _realtime.QueueChangedAsync(reason: "call-service", cancellationToken: cancellationToken);
        return Result.Success();
    }
}

/// <summary>
/// اعلام انجام خدمت — وضعیت پذیرش به Visited می‌رود (پایان خدمت)، تغییر صف
/// بلادرنگ پخش می‌شود و ردیف به «انجام‌شده‌های امروز» منتقل می‌شود.
/// </summary>
public record CompleteServiceReceptionCommand(long ReceptionId) : IRequest<Result>;

/// <summary>گرداننده اعلام انجام خدمت</summary>
public class CompleteServiceReceptionCommandHandler : IRequestHandler<CompleteServiceReceptionCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealtimeNotifier _realtime;

    /// <summary>سازنده — با اتصال بلادرنگ برای پخش فوری تغییر صف</summary>
    public CompleteServiceReceptionCommandHandler(IUnitOfWork unitOfWork, IRealtimeNotifier realtime)
    {
        _unitOfWork = unitOfWork;
        _realtime = realtime;
    }

    /// <summary>اجرا — ثبت پایان خدمت</summary>
    public async Task<Result> Handle(CompleteServiceReceptionCommand request,
        CancellationToken cancellationToken)
    {
        var reception = await _unitOfWork.Repository<Reception>().QueryableTracking
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && !r.IsDeleted, cancellationToken);
        if (reception is null)
            return Result.Failure(new Error("SERVICEQ.NOT_FOUND", "ردیف پذیرش یافت نشد — فهرست را تازه کنید"));

        if (reception.ServiceId is null || reception.DoctorId is not null)
            return Result.Failure(new Error("SERVICEQ.NOT_SERVICE",
                "این ردیف پذیرش پزشکی است — پایانش از فرم ویزیت ثبت می‌شود"));

        if (reception.Status == ReceptionStatus.Cancelled)
            return Result.Failure(new Error("SERVICEQ.CANCELLED", "پذیرش این بیمار لغو شده است"));

        // ایدمپوتنت — «انجام شد» دوباره روی ردیفِ انجام‌شده خطا نمی‌دهد
        if (reception.Status != ReceptionStatus.Visited)
            reception.Status = ReceptionStatus.Visited;

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        await _realtime.QueueChangedAsync(reason: "service-done", cancellationToken: cancellationToken);
        return Result.Success();
    }
}

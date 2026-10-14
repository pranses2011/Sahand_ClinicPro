using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Visits.CallPatient;

/// <summary>خروجی فراخوان — شناسه ویزیت (تازه یا ازقبل در جریان)</summary>
public record CallPatientResult(long VisitId, long ReceptionId, long PatientId, bool AlreadyInProgress);

/// <summary>
/// ═══ فرمان فراخوان «همان بیمارِ انتخاب‌شده» — چرخهٔ ۷۵ (گزارش مالک) ═══
/// «بعد از ویزیت یک بیمار، وقتی از لیست بیماران در صف انتظار روی دکمه
/// فراخوان کلیک میکنم خطای 'بیماری در صف انتظار نیست' میده» — ریشه:
/// دکمهٔ فراخوانِ هر ردیف، فرمان کورِ «بعدی» (call-next) را صدا می‌زد؛ آن فرمان
/// همیشه «اولین پذیرشِ همان پزشک» را برمی‌دارد و بیمارِ ردیفِ کلیک‌شده را نمی‌دید؛
/// اگر اولین پذیرشِ واجدشرطی نبود (مثلاً همهٔ ردیف‌های صفحهٔ پزشک، پذیرش خدمتیِ
/// بدون پزشک‌اند یا وضعیت‌شان در ویزیت است) پیام گمراه‌کنندهٔ «بیماری در صف
/// انتظار نیست» می‌آمد — در حالی که کاربر دقیقاً روی یک بیمارِ درصف کلیک کرده بود.
/// این فرمان همان ردیف را هدف می‌گیرد:
///   • Admitted/InQueue → ویزیت تازهٔ در جریان ساخته می‌شود؛
///   • InVisit با ویزیتِ در جریان → همان ویزیت برمی‌گردد (فراخوان دوباره امن است)؛
///   • ویزیتِ انجام‌شده یا پذیرش لغوشده → پیام روشن فارسی.
/// </summary>
public record CallPatientCommand(long ReceptionId, long? DoctorId = null) : IRequest<Result<CallPatientResult>>;

/// <summary>گردانندهٔ فراخوان اختصاصی یک پذیرش</summary>
public class CallPatientCommandHandler : IRequestHandler<CallPatientCommand, Result<CallPatientResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealtimeNotifier _realtime;

    /// <summary>سازنده — با اتصال بلادرنگ برای پخش فوری تغییر صف</summary>
    public CallPatientCommandHandler(IUnitOfWork unitOfWork, IRealtimeNotifier realtime)
    {
        _unitOfWork = unitOfWork;
        _realtime = realtime;
    }

    /// <summary>اجرا — فراخوان همان پذیرشِ انتخابی</summary>
    public async Task<Result<CallPatientResult>> Handle(CallPatientCommand request,
        CancellationToken cancellationToken)
    {
        var reception = await _unitOfWork.Repository<Domain.Entities.Scheduling.Reception>().QueryableTracking
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && !r.IsDeleted, cancellationToken);
        if (reception is null)
            return Result<CallPatientResult>.Failure(
                new Error("RECEPTION.NOT_FOUND", "ردیف پذیرش یافت نشد — فهرست صف را تازه کنید"));

        // پذیرش خدمتیِ بدون پزشک موضوع «فراخوان ویزیت» نیست — پیام دقیق خودش
        if (reception.DoctorId is null)
            return Result<CallPatientResult>.Failure(new Error("QUEUE.NO_DOCTOR",
                "این ردیف پذیرش خدمتی است و پزشکی ندارد — از صفحهٔ همان خدمت پیگیری شود"));

        // هر ویزیتِ همین پذیرش امروز — در جریان؟ انجام‌شده؟
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var existingVisit = await _unitOfWork.Repository<Visit>().QueryableTracking
            .Where(v => v.ReceptionId == reception.Id &&
                        v.VisitDate >= today && v.VisitDate < tomorrow)
            .OrderByDescending(v => v.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingVisit is not null && existingVisit.Status == VisitStatus.InProgress)
        {
            // فراخوان دوبارهٔ همان بیمارِ روی میز — همان ویزیت برمی‌گردد (ایدمپوتنت)
            return Result<CallPatientResult>.Success(
                new CallPatientResult(existingVisit.Id, reception.Id, reception.PatientId, true));
        }

        if (existingVisit is not null && existingVisit.Status == VisitStatus.Completed)
            return Result<CallPatientResult>.Failure(new Error("QUEUE.ALREADY_VISITED",
                "این بیمار قبلاً ویزیت شده است — ردیفش در فهرست «ویزیت‌شده‌ها» است"));

        if (reception.Status == Domain.Enums.ReceptionStatus.Cancelled)
            return Result<CallPatientResult>.Failure(new Error("QUEUE.CANCELLED",
                "پذیرش این بیمار لغو شده است"));

        // ویزیت‌های در جریانِ همان پزشک پایان می‌یابند (بیمار بعدی — فرم ۱۱)
        var inProgress = await _unitOfWork.Repository<Visit>().QueryableTracking
            .Where(v => v.VisitDate >= today && v.VisitDate < tomorrow &&
                        v.Status == VisitStatus.InProgress &&
                        v.DoctorId == reception.DoctorId)
            .ToListAsync(cancellationToken);
        foreach (var v in inProgress)
        {
            v.Status = VisitStatus.Completed;
            v.EndTime = DateTime.UtcNow;
            v.Duration = v.StartTime.HasValue ? (int)(DateTime.UtcNow - v.StartTime.Value).TotalMinutes : null;
            _unitOfWork.Repository<Visit>().Update(v);
        }

        var visit = new Visit
        {
            PatientId = reception.PatientId,
            DoctorId = reception.DoctorId.Value,
            ReceptionId = reception.Id,
            VisitDate = DateTime.UtcNow,
            VisitDateShamsi = Common.Helpers.PersianDateHelper.ToShamsi(DateTime.UtcNow),
            VisitType = VisitType.Initial,
            Status = VisitStatus.InProgress,
            StartTime = DateTime.UtcNow,
            VisitFee = 0m,
            ChiefComplaint = reception.Notes
        };
        await _unitOfWork.Repository<Visit>().AddAsync(visit, cancellationToken);

        reception.Status = Domain.Enums.ReceptionStatus.InVisit;
        _unitOfWork.Repository<Domain.Entities.Scheduling.Reception>().Update(reception);

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        // پخش بلادرنگ تغییر صف — پذیرش و مانیتور سالن فوری تازه می‌شوند
        await _realtime.QueueChangedAsync(reason: "call-patient", cancellationToken: cancellationToken);

        return Result<CallPatientResult>.Success(
            new CallPatientResult(visit.Id, reception.Id, reception.PatientId, false));
    }
}

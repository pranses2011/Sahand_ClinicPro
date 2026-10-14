using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Visits.CallNextPatient;

/// <summary>خروجی فراخوان — شناسه ویزیت ساخته‌شده</summary>
public record CallNextResult(long VisitId, long ReceptionId, long PatientId);

/// <summary>فرمان فراخوان بیمار بعدی از صف — فرم ۱۳</summary>
public record CallNextPatientCommand(long? DoctorId) : IRequest<Result<CallNextResult>>;

/// <summary>گرداننده فراخوان</summary>
public class CallNextPatientCommandHandler : IRequestHandler<CallNextPatientCommand, Result<CallNextResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealtimeNotifier _realtime;

    /// <summary>سازنده — با اتصال بلادرنگ برای پخش فوری تغییر صف (گام R1)</summary>
    public CallNextPatientCommandHandler(IUnitOfWork unitOfWork, IRealtimeNotifier realtime)
    {
        _unitOfWork = unitOfWork;
        _realtime = realtime;
    }

    /// <summary>اجرا — اولین پذیرش در صف → ویزیت در حال انجام</summary>
    public async Task<Result<CallNextResult>> Handle(CallNextPatientCommand request,
        CancellationToken cancellationToken)
    {
        // ═══ چرخهٔ ۷۴ (گزارش مالک) — ریشهٔ «بیماری در صف انتظار نیست» ═══
        // «در صفحه ویزیت و معاینه، وقتی از لیست بیماران در صف انتظار روی دکمه
        // فراخوان کلیک میکنم خطای 'بیماری در صف انتظار نیست' میده» — ریشهٔ قطعی:
        // ثبت پذیرش (CreateReceptionCommand) وضعیت Admitted می‌گذارد، نمایش صف
        // (GetVisitQueue) همهٔ وضعیت‌های غیر لغو را «منتظر» نشان می‌دهد، اما
        // فراخوان فقط InQueue را برمی‌داشت و هیچ مسیری در برنامه InQueue
        // ست نمی‌کرد ⇒ هر فراخوانی همیشه «صف خالی» برمی‌گشت.
        // اصلاح: وضعیت‌های «منتظرِ فراخوان» = Admitted (تازه پذیرش‌شده) +
        // InQueue (در صف اطلاحی) — هم‌تراز با منطق نمایش صف؛ به‌علاوه پذیرش‌های
        // «خدمتی بدون پزشک» از فراخوان ویزیت پزشک کنار گذاشته می‌شوند
        // (Visit.DoctorId صفر نامعتبر است — گارد پیش‌تر فقط در کامنت بود).
        // مرز روز هم بر پایهٔ UTC همانند ثبت پذیرش و نمایش صف هم‌تراز شد.
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var next = await _unitOfWork.Repository<Reception>().QueryableTracking
            .Where(r => r.ReceptionDate >= today && r.ReceptionDate < tomorrow &&
                        (r.Status == ReceptionStatus.Admitted || r.Status == ReceptionStatus.InQueue) &&
                        r.DoctorId != null &&
                        (request.DoctorId == null || r.DoctorId == request.DoctorId))
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (next is null)
            return Result<CallNextResult>.Failure(new Error("QUEUE.EMPTY", "بیماری در صف انتظار نیست"));

        // هر ویزیت در جریان برای همین پزشک؟ پایان خودکار طبق «بیمار بعدی» فرم ۱۱
        var inProgress = await _unitOfWork.Repository<Visit>().QueryableTracking
            .Where(v => v.VisitDate >= today && v.VisitDate < tomorrow &&
                        v.Status == VisitStatus.InProgress &&
                        (request.DoctorId == null || v.DoctorId == request.DoctorId))
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
            PatientId = next.PatientId,
            // چرخه ۲۴ — پذیرش خدمتی ممکن است پزشک نداشته باشد؛ «فراخوان بیمار
            // بعدی» فقط برای پذیرش‌های ویزیت پزشک معنا دارد و در نگاشت، پزشک
            // اختیاری به مقدار معتبر می‌رسد (بیمار خدمتی در همین گارد قبل از
            // این نقطه رد شده است)
            DoctorId = next.DoctorId ?? 0,
            ReceptionId = next.Id,
            VisitDate = DateTime.UtcNow,
            VisitDateShamsi = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(DateTime.UtcNow),
            VisitType = VisitType.Initial,
            Status = VisitStatus.InProgress,
            StartTime = DateTime.UtcNow,
            VisitFee = 0m,
            ChiefComplaint = next.Notes
        };
        await _unitOfWork.Repository<Visit>().AddAsync(visit, cancellationToken);

        next.Status = ReceptionStatus.InVisit;
        _unitOfWork.Repository<Reception>().Update(next);

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        // پخش بلادرنگ تغییر صف — پذیرش و مانیتور صف فوری تازه می‌شوند (گام R1)
        await _realtime.QueueChangedAsync(reason: "call-next", cancellationToken: cancellationToken);

        return Result<CallNextResult>.Success(new CallNextResult(visit.Id, next.Id, next.PatientId));
    }
}

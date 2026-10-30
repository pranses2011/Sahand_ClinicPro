using System.Security.Cryptography;
using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Receptions.Commands.CreateReception;

/// <summary>
/// فرمان پذیرش بیمار — از نوبت یا مراجعه حضوری بدون نوبت
/// </summary>
/// <param name="Dto">داده پذیرش</param>
/// <param name="ReceptionistId">شناسه منشی پذیرش‌کننده — از کاربر فعلی پر می‌شود</param>
public record CreateReceptionCommand(CreateReceptionDto Dto, long? ReceptionistId = null)
    : IRequest<Result<long>>;

/// <summary>اعتبارسنج پذیرش</summary>
public class CreateReceptionCommandValidator : AbstractValidator<CreateReceptionCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateReceptionCommandValidator()
    {
        RuleFor(x => x.Dto.PatientId).GreaterThan(0).WithMessage("انتخاب بیمار الزامی است");
        RuleFor(x => x.Dto.DoctorId).GreaterThan(0).WithMessage("انتخاب پزشک الزامی است");
    }
}

/// <summary>گرداننده ثبت پذیرش — ورود بیمار به جریان ویزیت روز</summary>
public class CreateReceptionCommandHandler : IRequestHandler<CreateReceptionCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateReceptionCommandHandler> _logger;
    private readonly IRealtimeNotifier _realtime;

    /// <summary>سازنده با تزریق واحد کار — و اتصال بلادرنگ صف (گام R1)</summary>
    public CreateReceptionCommandHandler(IUnitOfWork unitOfWork,
        ILogger<CreateReceptionCommandHandler> logger, IRealtimeNotifier realtime)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _realtime = realtime;
    }

    /// <summary>اجرای فرمان پذیرش</summary>
    public async Task<Result<long>> Handle(CreateReceptionCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        if (dto.RequestId == Guid.Empty)
            return Result<long>.Failure(new Error("RECEPTION.REQUEST_INVALID", "شناسه درخواست پذیرش معتبر نیست."));

        // کوئری ردیابی‌شونده همچنان فیلتر شعبه/حذف را اعمال می‌کند؛ FindAsync
        // می‌تواند موجودیت موجود در حافظه از شعبه دیگر را بازگرداند.
        var patient = await _unitOfWork.Repository<Patient>().QueryableTracking
            .FirstOrDefaultAsync(p => p.Id == dto.PatientId && !p.IsDeleted, cancellationToken);
        if (patient is null)
            return Result<long>.Failure(Error.NotFound("بیمار", dto.PatientId));

        string? fingerprint = null;
        if (dto.RequestId.HasValue)
        {
            fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(dto with { RequestId = null })));
            var prior = await _unitOfWork.Repository<Reception>().QueryableNoTracking
                .FirstOrDefaultAsync(r => r.RequestId == dto.RequestId, cancellationToken);
            if (prior is not null)
                return prior.PatientId == dto.PatientId && prior.RequestFingerprint == fingerprint
                    ? Result<long>.Success(prior.Id)
                    : Result<long>.Failure(new Error("RECEPTION.REQUEST_CONFLICT", "شناسه درخواست پیش‌تر برای پذیرش دیگری استفاده شده است."));
        }

        // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — پذیرش خدمت درمانی ═══
        // «قسمت پذیرش فقط پزشک دارد — بیمارِ آمپول/سرم/پانسمان/آزمایش/عکس چه
        // می‌شود؟» — پذیرش دوگونه است: ویزیت پزشک (DoctorId) یا خدمت درمانی
        // (ServiceId — از تعرفه‌ها: تزریقات/پانسمان/آزمایش/تصویربرداری و…).
        // دقیقاً یکی از دو مسیر الزامی است؛ انجام‌دهندهٔ خدمت پرسنل است.
        if ((dto.DoctorId is null) == (dto.ServiceId is null))
            return Result<long>.Failure(new Domain.Common.Error(
                "RECEPTION.TARGET", "برای پذیرش دقیقاً یکی از پزشک یا خدمت درمانی را انتخاب کنید"));

        if (dto.DoctorId is not null)
        {
            var doctor = await _unitOfWork.Repository<Doctor>().QueryableNoTracking
                .FirstOrDefaultAsync(d => d.Id == dto.DoctorId.Value && !d.IsDeleted && d.IsActive, cancellationToken);
            if (doctor is null)
                return Result<long>.Failure(Error.NotFound("پزشک", dto.DoctorId.Value));
        }

        Domain.Entities.Billing.Service? service = null;
        if (dto.ServiceId is not null)
        {
            service = await _unitOfWork.Repository<Domain.Entities.Billing.Service>().QueryableNoTracking
                .FirstOrDefaultAsync(s => s.Id == dto.ServiceId.Value && !s.IsDeleted && s.IsActive, cancellationToken);
            if (service is null)
                return Result<long>.Failure(Error.NotFound("خدمت", dto.ServiceId.Value));
        }

        // انتخاب صریح قدیمی یا غیرفعال نباید قبل از رسیدن به محاسبه فاکتور
        // باعث ثبت پذیرش ناقص شود؛ null بیمه برای سازگاری کلاینت قدیمی است.
        if (dto.InsuranceCompanyId is < 0)
            return Result<long>.Failure(new Error("RECEPTION.INSURANCE_INVALID", "بیمه انتخاب‌شده معتبر نیست."));
        if (dto.InsuranceCompanyId is > 0 && !await _unitOfWork.Repository<InsuranceCompany>().QueryableNoTracking
            .AnyAsync(i => i.Id == dto.InsuranceCompanyId && !i.IsDeleted && i.IsActive && i.IsContracted, cancellationToken))
            return Result<long>.Failure(new Error("RECEPTION.INSURANCE_INVALID", "بیمه انتخاب‌شده فعال یا طرف قرارداد نیست؛ بیمه را دوباره انتخاب کنید."));
        if (dto.PerformedByEmployeeId is long performerId && !await _unitOfWork.Repository<Employee>().QueryableNoTracking
            .AnyAsync(e => e.Id == performerId && !e.IsDeleted && e.IsActive, cancellationToken))
            return Result<long>.Failure(new Error("RECEPTION.PERFORMER_INVALID", "پرسنل انجام‌دهنده معتبر یا فعال نیست."));

        // ═══ چرخهٔ سی‌ونه‌م (گزارش مالک) — اتاق انجام خدمت پذیرش خدمتی ═══
        // «اتاق معاینه برای پزشک در برنامهٔ کاری پزشک مشخص می‌شود ولی برای خدمات
        // درمانی نمی‌شود معین کرد» — اگر اتاقی ارسال شده باشد باید وجود و فعال
        // باشد؛ اتاق غیرفعال‌شده (حذف نرم) پذیرفته نمی‌شود تا بیمار به اتاق
        // بسته هدایت نشود.
        if (dto.RoomId is not null)
        {
            var room = await _unitOfWork.Repository<ExaminationRoom>().QueryableNoTracking
                .FirstOrDefaultAsync(r => r.Id == dto.RoomId.Value && !r.IsDeleted, cancellationToken);
            if (room is null || !room.IsActive)
                return Result<long>.Failure(Error.NotFound("اتاق", dto.RoomId.Value));
        }

        Appointment? appointment = null;
        if (dto.AppointmentId is long appointmentId)
        {
            appointment = await _unitOfWork.Repository<Appointment>().QueryableTracking
                .FirstOrDefaultAsync(a => a.Id == appointmentId && !a.IsDeleted, cancellationToken);
            if (appointment is null || appointment.IsDeleted)
                return Result<long>.Failure(Error.NotFound("نوبت", appointmentId));
            if (appointment.PatientId != dto.PatientId || appointment.DoctorId != dto.DoctorId || dto.ServiceId is not null)
                return Result<long>.Failure(new Error("RECEPTION.APPOINTMENT_MISMATCH", "بیمار یا پزشک پذیرش با نوبت مطابقت ندارد."));
            if (appointment.Status is not (AppointmentStatus.Booked or AppointmentStatus.Confirmed))
                return Result<long>.Failure(new Error("RECEPTION.APPOINTMENT_STATUS", "این نوبت برای پذیرش در وضعیت مجاز نیست."));
        }

        // ساعت پذیرش باید عین ساعت دیواری درمانگاه باشد (گزارش مالک: ساعت
        // پذیرش GMT نشان داده می‌شد) — مُهر زمانی به وقت رسمی ایران ثبت می‌شود
        var now = PersianDateHelper.IranNow();

        // ═══ اتاق پذیرش ویزیت از برنامهٔ کاری پزشک (گزارش مالک) ═══
        // «در پذیرش پزشک، اتاق رو نمیندازه در حالی که باید از برنامه کاری
        // پزشک برداره» — کلاینت برای حالت ویزیت اتاقی نمی‌فرستد؛ اگر اتاق
        // صریح نیامده و پزشک برای امروز (روز هفتهٔ ایرانی: شنبه=۰) برنامهٔ
        // فعالی با اتاق دارد، همان اتاق روی پذیرش می‌نشیند تا در صف و قبض
        // چاپ مجدد دیده شود. برنامهٔ بدون اتاق یا خارج از بازهٔ اعتبار،
        // اتاقی تحمیل نمی‌کند.
        if (dto.RoomId is null && dto.DoctorId is not null)
        {
            var dayIndex = now.DayOfWeek switch
            {
                DayOfWeek.Saturday => 0,
                DayOfWeek.Sunday => 1,
                DayOfWeek.Monday => 2,
                DayOfWeek.Tuesday => 3,
                DayOfWeek.Wednesday => 4,
                DayOfWeek.Thursday => 5,
                DayOfWeek.Friday => 6,
                _ => 0
            };
            var today = DateOnly.FromDateTime(now);
            var schedule = await _unitOfWork.Repository<DoctorSchedule>().QueryableNoTracking
                .Where(s => s.DoctorId == dto.DoctorId.Value && s.IsActive
                            && s.DayOfWeek == dayIndex && s.RoomId != null
                            && (s.ValidFrom == null || s.ValidFrom <= today)
                            && (s.ValidTo == null || s.ValidTo >= today))
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (schedule?.RoomId is not null)
                dto = dto with { RoomId = schedule.RoomId };
        }
        var reception = new Reception
        {
            RequestId = dto.RequestId,
            RequestFingerprint = fingerprint,
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            // چرخه ۲۴ — پذیرش خدمت درمانی (بدون پزشک)
            ServiceId = dto.ServiceId,
            ServiceName = dto.ServiceId is null ? null :
                (dto.ServiceName ?? service?.ServiceName),
            PerformedByEmployeeId = dto.PerformedByEmployeeId,
            // چرخهٔ ۳۹ — اتاق محل انجام خدمت (پذیرش خدمتی)
            RoomId = dto.RoomId,
            AppointmentId = dto.AppointmentId,
            // چرخه بیست‌ویکم (گزارش پانزدهم مالک): بیمه پذیرش — سه حالت:
            //   ▪ 0  → «آزاد» صریح از کشوی پذیرش → بدون بیمه ذخیره می‌شود (null)
            //   ▪ >0 → همان شرکت بیمهٔ انتخاب‌شدهٔ کاربر (مثلاً آسیا) ذخیره می‌شود
            //   ▪ null (نبود کلید در درخواست) → سازگاری قدیم: بیمهٔ پروندهٔ بیمار
            // ریشهٔ «آسیا انتخاب شد ولی آزاد ثبت شد» همین‌جا بود: پیش‌تر مقدار تهیِ
            // ارسالی بی‌شرط با بیمهٔ پرونده (که تهی بود) جایگزین می‌شد.
            InsuranceCompanyId = dto.InsuranceCompanyId switch
            {
                0 => null,
                > 0 => dto.InsuranceCompanyId,
                _ => patient.InsuranceCompanyId
            },
            ReferralCode = dto.ReferralCode,
            ReceptionDate = now,
            ReceptionDateShamsi = PersianDateHelper.ToShamsi(now),
            Status = ReceptionStatus.Admitted,
            ReceptionistId = request.ReceptionistId,
            EnterTime = now,
            Notes = dto.Notes
        };

        await _unitOfWork.Repository<Reception>().AddAsync(reception, cancellationToken);

        // چرخه بیست‌وسوم (گزارش شانزدهم مالک): پذیرش خودش یک «مراجعه» است —
        // ستون «آخرین مراجعه» فهرست بیماران باید با پذیرش هم تازه شود (نه فقط با ثبت ویزیت)
        if (patient.LastVisitDate is null || patient.LastVisitDate < now)
            patient.LastVisitDate = now;

        // نوبت فقط پس از تطبیق بیمار، پزشک و وضعیت، حاضر می‌شود.
        if (appointment is not null) appointment.Status = AppointmentStatus.Arrived;

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        // پخش بلادرنگ — ورود بیمار جدید به صف برای همه مانیتورهای شعبه (گام R1)
        await _realtime.QueueChangedAsync(reason: "enqueue", cancellationToken: cancellationToken);

        _logger.LogInformation("پذیرش {Number} برای بیمار {PatientId} ثبت شد",
            reception.ReceptionNumber, dto.PatientId);

        return Result<long>.Success(reception.Id);
    }
}

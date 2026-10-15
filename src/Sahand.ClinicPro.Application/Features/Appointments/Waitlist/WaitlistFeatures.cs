using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Appointments.Waitlist;

/// <summary>ثبت ردیف جدید لیست انتظار — چرخه ۱۳ (درخواست ۳-۴)</summary>
public record AddToWaitlistCommand(AddWaitlistDto Dto) : IRequest<Result<WaitlistRowDto>>;

/// <summary>ورودی ثبت در لیست انتظار</summary>
public record AddWaitlistDto(
    long PatientId,
    long? DoctorId,
    long? SpecialtyId,
    DateOnly PreferredFrom,
    DateOnly PreferredTo,
    string? PreferredTimeOfDay,
    int Priority,
    string? Notes);

/// <summary>ردیف نمایشی لیست انتظار</summary>
public record WaitlistRowDto(
    long Id,
    long PatientId,
    string PatientName,
    string PatientMobile,
    string DoctorName,
    string PreferredText,
    string PriorityText,
    string StatusText,
    string StatusColor,
    string NotifiedText,
    string? Notes);

/// <summary>پرس‌وجوی فهرست لیست انتظار — با فیلتر وضعیت</summary>
public record GetWaitlistQuery(WaitlistStatus? Status = null) : IRequest<Result<List<WaitlistRowDto>>>;

/// <summary>حذف/لغو ردیف لیست انتظار</summary>
public record CancelWaitlistEntryCommand(long Id) : IRequest<Result>;

/// <summary>
/// پیشنهاد خودکار نوبت آزاد به متناظرترین ردیف لیست انتظار —
/// با لغو هر نوبت، منتظران همان پزشک/تخصص در بازه تاریخ پیشنهاد می‌گیرند
/// </summary>
public record NotifyWaitlistForFreedSlotCommand(
    long CancelledAppointmentId,
    long DoctorId,
    DateOnly AppointmentDate,
    TimeOnly AppointmentTime) : IRequest<Result<long>>;

/// <summary>گرداننده ثبت در لیست انتظار</summary>
public class AddToWaitlistCommandHandler : IRequestHandler<AddToWaitlistCommand, Result<WaitlistRowDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public AddToWaitlistCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<WaitlistRowDto>> Handle(AddToWaitlistCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        if (dto.PreferredTo < dto.PreferredFrom)
            return Result<WaitlistRowDto>.Failure(
                new Error("WAITLIST_RANGE", "تاریخ پایان بازه قبل از شروع آن است"));

        var patient = await _unitOfWork.Repository<Domain.Entities.People.Patient>().QueryableNoTracking
            .FirstOrDefaultAsync(p => p.Id == dto.PatientId && !p.IsDeleted, cancellationToken);
        if (patient is null)
            return Result<WaitlistRowDto>.Failure(
                new Error("PATIENT_NOT_FOUND", "بیمار یافت نشد"));

        var entry = new WaitlistEntry
        {
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            SpecialtyId = dto.SpecialtyId,
            PreferredFrom = dto.PreferredFrom,
            PreferredTo = dto.PreferredTo,
            PreferredTimeOfDay = string.IsNullOrWhiteSpace(dto.PreferredTimeOfDay) ? null : dto.PreferredTimeOfDay.Trim(),
            Priority = dto.Priority is >= 1 and <= 3 ? (Priority)dto.Priority : Priority.Normal,
            Status = WaitlistStatus.Waiting,
            ContactMobile = patient.Mobile,
            Notes = dto.Notes?.Trim()
        };
        await _unitOfWork.Repository<WaitlistEntry>().AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<WaitlistRowDto>.Success(await ToRowAsync(entry, cancellationToken));
    }

    /// <summary>تبدیل به ردیف نمایشی</summary>
    private async Task<WaitlistRowDto> ToRowAsync(WaitlistEntry e, CancellationToken ct)
    {
        var patient = await _unitOfWork.Repository<Domain.Entities.People.Patient>().QueryableNoTracking
            .Where(p => p.Id == e.PatientId)
            .Select(p => new { p.FirstName, p.LastName, p.Mobile })
            .FirstOrDefaultAsync(ct);
        var doctorName = "هر پزشک";
        if (e.DoctorId is { } did)
        {
            var d = await _unitOfWork.Repository<Domain.Entities.People.Doctor>().QueryableNoTracking
                .Where(x => x.Id == did && x.Employee != null)
                .Select(x => x.Employee!.FirstName + " " + x.Employee.LastName)
                .FirstOrDefaultAsync(ct);
            if (d is not null) doctorName = $"دکتر {d}";
        }
        else if (e.SpecialtyId is { } sid)
        {
            var spec = await _unitOfWork.Repository<Domain.Entities.People.Specialty>().QueryableNoTracking
                .Where(s => s.Id == sid)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(ct);
            if (spec is not null) doctorName = $"تخصص {spec}";
        }

        return ToRow(e, patient is null
            ? "؟"
            : $"{patient.FirstName} {patient.LastName}".Trim(),
            patient?.Mobile ?? e.ContactMobile ?? "—", doctorName);
    }

    /// <summary>ساخت ردیف نمایشی</summary>
    internal static WaitlistRowDto ToRow(WaitlistEntry e, string patientName, string mobile, string doctorName)
    {
        var (statusText, statusColor) = e.Status switch
        {
            WaitlistStatus.Waiting => ("⏳ در انتظار", "#1565C0"),
            WaitlistStatus.Notified => ("📨 پیامک شد", "#F9A825"),
            WaitlistStatus.Confirmed => ("✅ تأیید بیمار", "#2E7D32"),
            WaitlistStatus.Expired => ("⌛ منقضی", "#9E9E9E"),
            WaitlistStatus.Cancelled => ("✖️ لغوشده", "#C62828"),
            _ => ("—", "#78909C")
        };
        return new WaitlistRowDto(
            e.Id,
            e.PatientId,
            patientName,
            mobile,
            doctorName,
            $"{PersianDateHelper.ToShamsi(e.PreferredFrom.ToDateTime(TimeOnly.MinValue))} تا " +
            $"{PersianDateHelper.ToShamsi(e.PreferredTo.ToDateTime(TimeOnly.MinValue))}" +
            (string.IsNullOrWhiteSpace(e.PreferredTimeOfDay) ? "" : $" ({e.PreferredTimeOfDay})"),
            e.Priority == Priority.Emergency ? "🚨 اورژانس" : e.Priority == Priority.High ? "🔥 فوری" : "معمولی",
            statusText, statusColor,
            e.NotifiedAt is { } n ? PersianDateHelper.ToShamsiWithTime(n) : "—",
            e.Notes);
    }
}

/// <summary>گرداننده فهرست لیست انتظار</summary>
public class GetWaitlistQueryHandler : IRequestHandler<GetWaitlistQuery, Result<List<WaitlistRowDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetWaitlistQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<WaitlistRowDto>>> Handle(GetWaitlistQuery request,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<WaitlistEntry>().QueryableNoTracking
            .Where(w => !w.IsDeleted);
        if (request.Status is { } st)
            query = query.Where(w => w.Status == st);

        var entries = await query
            .OrderBy(w => w.Priority) // اورژانس=۱ کوچک‌تر = اولویت بالاتر
            .ThenBy(w => w.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        // نام بیماران و پزشکان دسته‌ای
        var patientIds = entries.Select(e => e.PatientId).Distinct().ToList();
        var doctorIds = entries.Where(e => e.DoctorId is not null).Select(e => e.DoctorId!.Value).Distinct().ToList();
        var patients = await _unitOfWork.Repository<Domain.Entities.People.Patient>().QueryableNoTracking
            .Where(p => patientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => new { Name = $"{p.FirstName} {p.LastName}".Trim(), p.Mobile }, cancellationToken);

        // ═══ چرخهٔ ۷۸ — رفع NullReferenceException اندپوینت فهرست لیست انتظار ═══
        // گزارش مالک + لاگ سرور: «ثبت در لیست می‌گوید ثبت شد ولی در لیست نشان نمی‌دهد
        // و خطای NullReferenceException می‌دهد» — استک: GetWaitlistQueryHandler خط ۱۸۷
        // در lambda انتخاب عنصرِ ToDictionaryAsync. ریشه: «d.Employee!.FirstName» در
        // سلکتور عنصرِ ToDictionaryAsync «سمت کلاینت» روی موجودیت‌های متریالزه ارزیابی
        // می‌شود؛ شرط Where(d.Employee != null) فقط در SQL به EmployeeId NOT NULL
        // ترجمه می‌شود و ناوبری Employee روی موجودیت بارگذاری‌شده «تهی» است (نه
        // Include هست و نه lazy-loading فعال) ⇒ همان‌جا NRE و پاسخ ۵۰۰؛ در نتیجه
        // پنل لیست انتظار پس از هر ثبت موفق، خالی می‌ماند. رفع: پیش‌پردازش با
        // Select سمت SQL (ترجمه‌پذیر به کوئری واحد؛ همان الگوی امن ToRowAsync همین
        // فایل و GetVisitQueue چرخهٔ ۷۵) و سپس دیکشنری‌سازی در حافظه — بدون N+1.
        var doctors = (await _unitOfWork.Repository<Domain.Entities.People.Doctor>().QueryableNoTracking
                .Where(d => doctorIds.Contains(d.Id) && d.Employee != null)
                .Select(d => new { d.Id, Name = (d.Employee!.FirstName + " " + d.Employee.LastName).Trim() })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.Id, x => x.Name);

        var rows = entries.Select(e =>
        {
            var p = patients.GetValueOrDefault(e.PatientId);
            var doctorName = "هر پزشک";
            if (e.DoctorId is { } did && doctors.TryGetValue(did, out var dn))
                doctorName = $"دکتر {dn}";
            return AddToWaitlistCommandHandler.ToRow(e, p?.Name ?? "؟", p?.Mobile ?? e.ContactMobile ?? "—", doctorName);
        }).ToList();

        return Result<List<WaitlistRowDto>>.Success(rows);
    }
}

/// <summary>گرداننده لغو ردیف</summary>
public class CancelWaitlistEntryCommandHandler : IRequestHandler<CancelWaitlistEntryCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public CancelWaitlistEntryCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result> Handle(CancelWaitlistEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _unitOfWork.Repository<WaitlistEntry>().QueryableTracking
            .FirstOrDefaultAsync(w => w.Id == request.Id && !w.IsDeleted, cancellationToken);
        if (entry is null)
            return Result.Failure(Error.NotFound("ردیف لیست انتظار", request.Id));

        entry.Status = WaitlistStatus.Cancelled;
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>
/// گرداننده پیشنهاد خودکار — متناظرترین منتظر را پیدا می‌کند،
/// پیامک پیشنهاد در صف می‌گذارد و وضعیتش Notified می‌کند
/// </summary>
public class NotifyWaitlistForFreedSlotCommandHandler
    : IRequestHandler<NotifyWaitlistForFreedSlotCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly Interfaces.ISmsService _sms;

    /// <summary>سازنده</summary>
    public NotifyWaitlistForFreedSlotCommandHandler(IUnitOfWork unitOfWork, Interfaces.ISmsService sms)
    {
        _unitOfWork = unitOfWork;
        _sms = sms;
    }

    /// <summary>اجرا — شناسه ردیف برنده یا صفر</summary>
    public async Task<Result<long>> Handle(NotifyWaitlistForFreedSlotCommand request,
        CancellationToken cancellationToken)
    {
        var date = request.AppointmentDate;

        // منتظران واجد شرایط: همان پزشک یا تخصص مرتبط، بازه شامل تاریخ نوبت، وضعیت Waiting
        var candidates = await _unitOfWork.Repository<WaitlistEntry>().QueryableTracking
            .Where(w => !w.IsDeleted && w.Status == WaitlistStatus.Waiting &&
                        w.PreferredFrom <= date && w.PreferredTo >= date &&
                        (w.DoctorId == request.DoctorId || w.DoctorId == null))
            .OrderBy(w => w.Priority) // اورژانس=۱ < فوری=۲ < عادی=۳ — کوچک‌تر اولویت بالاتر
            .ThenBy(w => w.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return Result<long>.Success(0);

        var winner = candidates[0];
        winner.Status = WaitlistStatus.Notified;
        winner.NotifiedAt = DateTime.UtcNow;
        winner.OfferedAppointmentId = request.CancelledAppointmentId;
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        // پیامک پیشنهاد — با متن صریح؛ قالب اختصاصی در صورت وجود از کد waitlist-offer استفاده می‌شود
        var dateText = PersianDateHelper.ToShamsi(date.ToDateTime(TimeOnly.MinValue));
        var message =
            $"«درمانگاه سهند» سلام 🌿\n" +
            $"به دلیل لغو نوبت، جای خالی روز {dateText} ساعت {request.AppointmentTime:hh\\:mm} پیش شماست.\n" +
            $"برای تأیید و رزرو نهایی با شماره ۰۴۱-۳۳۳۳۴۴۴ تماس بگیرید.";
        try
        {
            var mobile = winner.ContactMobile ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(mobile))
                await _sms.SendAsync(mobile, message, cancellationToken);
        }
        catch
        {
            // خطای ارسال پیامک نباید تراکنش لغو نوبت را بلامحل کند — وضعیت Notified می‌ماند و اپراتور پیگیری تلفنی می‌کند
        }

        return Result<long>.Success(winner.Id);
    }
}

using Sahand.ClinicPro.Application.Features.Appointments.Waitlist;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// اطلاع‌رسان لیست انتظار — چرخه ۱۳ (درخواست ۳-۴)
/// لایه سبک برای فراخوانی از گرداننده‌های دیگر بدون وابستگی مستقیم به MediatR
/// </summary>
public interface IWaitlistNotifier
{
    /// <summary>
    /// پیشنهاد جای خالی نوبت لغوشده به متناظرترین منتظر —
    /// پیامک در صف می‌رود و وضعیت ردیف Notified می‌شود
    /// </summary>
    Task<long> NotifyFreedSlotAsync(long cancelledAppointmentId, long doctorId,
        DateOnly appointmentDate, TimeOnly appointmentTime, CancellationToken ct = default);
}

/// <summary>پیاده‌سازی پیش‌فرض — wrapper روی NotifyWaitlistForFreedSlotCommand</summary>
public class WaitlistNotifier : IWaitlistNotifier
{
    private readonly MediatR.ISender _sender;

    /// <summary>سازنده</summary>
    public WaitlistNotifier(MediatR.ISender sender) => _sender = sender;

    /// <summary>اجرا — شناسه ردیف برنده یا صفر</summary>
    public async Task<long> NotifyFreedSlotAsync(long cancelledAppointmentId, long doctorId,
        DateOnly appointmentDate, TimeOnly appointmentTime, CancellationToken ct = default)
    {
        var result = await _sender.Send(new NotifyWaitlistForFreedSlotCommand(
            cancelledAppointmentId, doctorId, appointmentDate, appointmentTime), ct);
        return result.IsSuccess ? result.Value : 0;
    }
}

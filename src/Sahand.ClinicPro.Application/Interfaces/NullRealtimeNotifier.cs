using Sahand.ClinicPro.Domain.Entities.Communication;

namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// پیاده‌سازی بی‌اثر قرارداد بلادرنگ — پیش‌فرض میزبان‌های بدون SignalR (مثل پورتال لایسنس)
/// در API با پیاده‌سازی واقعی SignalR بازنویسی (TryAdd ثبت می‌شود)
/// </summary>
public sealed class NullRealtimeNotifier : IRealtimeNotifier
{
    /// <summary>رویداد صف — بی‌اثر</summary>
    public Task QueueChangedAsync(long? branchId = null, string reason = "update",
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>اعلان کاربر — بی‌اثر</summary>
    public Task NotifyUserAsync(long userId, Notification notification,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>هشدار عمومی — بی‌اثر</summary>
    public Task BroadcastAlertAsync(string title, string message,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}

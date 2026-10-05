using Microsoft.AspNetCore.SignalR;
using Sahand.ClinicPro.API.Hubs;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Communication;

namespace Sahand.ClinicPro.API.Realtime;

/// <summary>
/// پیاده‌سازی SignalR قرارداد بلادرنگ — گام R1
/// صف به گروه شعبه، اعلان به گروه کاربر و هشدار به همه پخش می‌شود
/// </summary>
public sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
    /// <summary>زمینه Hub صف</summary>
    private readonly IHubContext<QueueHub> _queueHub;

    /// <summary>زمینه Hub اعلان</summary>
    private readonly IHubContext<NotificationHub> _notificationHub;

    /// <summary>کاربر جاری — تعیین شعبه پیش‌فرض رویداد صف</summary>
    private readonly ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق زمینه‌های Hub</summary>
    public SignalRRealtimeNotifier(IHubContext<QueueHub> queueHub,
        IHubContext<NotificationHub> notificationHub, ICurrentUserService currentUser)
    {
        _queueHub = queueHub;
        _notificationHub = notificationHub;
        _currentUser = currentUser;
    }

    /// <summary>
    /// پخش تغییر صف به گروه شعبه — کلاینت WPF صف پذیرش/ویزیت را فوری تازه می‌کند
    /// </summary>
    public async Task QueueChangedAsync(long? branchId = null, string reason = "update",
        CancellationToken cancellationToken = default)
    {
        var effectiveBranch = branchId ?? _currentUser.BranchId;
        var message = new QueueChangedMessage(reason, effectiveBranch, DateTime.UtcNow);
        await _queueHub.Clients.Group(QueueHub.GroupNameFor(effectiveBranch))
            .SendAsync("queue-changed", message, cancellationToken);
    }

    /// <summary>
    /// پخش اعلان ثبت‌شده به گروه اختصاصی کاربر مقصد
    /// </summary>
    public async Task NotifyUserAsync(long userId, Notification notification,
        CancellationToken cancellationToken = default)
    {
        var message = new UserNotificationMessage(
            notification.Id, notification.Title, notification.Message ?? string.Empty,
            notification.Type.ToString(), notification.CreatedAt == default
                ? DateTime.UtcNow : notification.CreatedAt);
        await _notificationHub.Clients.Group(NotificationHub.UserGroup(userId))
            .SendAsync("notification-received", message, cancellationToken);
    }

    /// <summary>
    /// پخش هشدار سراسری به همه کلاینت‌های متصل (هر دو Hub)
    /// </summary>
    public async Task BroadcastAlertAsync(string title, string message,
        CancellationToken cancellationToken = default)
    {
        var payload = new BroadcastAlertMessage(title, message, DateTime.UtcNow);
        await _queueHub.Clients.All.SendAsync("broadcast-alert", payload, cancellationToken);
        await _notificationHub.Clients.All.SendAsync("broadcast-alert", payload, cancellationToken);
    }
}

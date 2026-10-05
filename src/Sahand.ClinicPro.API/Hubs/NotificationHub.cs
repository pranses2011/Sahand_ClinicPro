using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.API.Hubs;

/// <summary>
/// Hub اعلان‌های کاربر — اتصال بلادرنگ گام R1
/// هر کاربر در گروه اختصاصی «user-{id}» قرار می‌گیرد؛ اعلان فقط به همان کاربر پخش می‌شود
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    /// <summary>کاربر جاری — برای گروه‌بندی فردی</summary>
    private readonly ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق سرویس کاربر جاری</summary>
    public NotificationHub(ICurrentUserService currentUser) => _currentUser = currentUser;

    /// <summary>
    /// هنگام اتصال — عضویت در گروه اختصاصی کاربر
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        if (_currentUser.UserId is { } userId)
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }

    /// <summary>نام گروه اختصاصی کاربر — قرارداد مشترک با SignalRRealtimeNotifier</summary>
    public static string UserGroup(long userId) => $"user-{userId}";
}

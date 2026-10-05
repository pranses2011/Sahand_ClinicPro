using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.API.Hubs;

/// <summary>
/// Hub صف پذیرش و ویزیت — اتصال بلادرنگ گام R1
/// کلاینت‌های هر شعبه در گروه «branch-{id}» قرار می‌گیرند و فقط تغییرات صف خود را می‌بینند
/// </summary>
[Authorize]
public sealed class QueueHub : Hub
{
    /// <summary>کاربر جاری — برای گروه‌بندی شعبه‌ای</summary>
    private readonly ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق سرویس کاربر جاری</summary>
    public QueueHub(ICurrentUserService currentUser) => _currentUser = currentUser;

    /// <summary>
    /// هنگام اتصال — عضویت در گروه شعبه (برای شعبه تهی، گروه سراسری)
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var branchId = _currentUser.BranchId;
        var group = branchId is null ? "queue-all" : $"branch-{branchId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// نام گروه صف برای شعبه — قرارداد مشترک با SignalRRealtimeNotifier
    /// </summary>
    public static string GroupNameFor(long? branchId)
        => branchId is null ? "queue-all" : $"branch-{branchId}";
}

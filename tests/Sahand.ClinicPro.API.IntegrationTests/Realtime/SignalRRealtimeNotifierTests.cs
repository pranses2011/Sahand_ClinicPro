using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Sahand.ClinicPro.API.Hubs;
using Sahand.ClinicPro.API.Realtime;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Enums;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests.Realtime;

/// <summary>
/// تست‌های واحد پیاده‌سازی SignalR اتصال بلادرنگ — گام R1
/// با نمایه‌سازی ساختگی زمینه Hub بررسی می‌شود که هر رویداد
/// به گروه درست (شعبه/کاربر) و با نام متد صحیح پخش می‌شود
/// </summary>
public class SignalRRealtimeNotifierTests
{
    /// <summary>ساخت تست با دو زمینه Hub نمایه‌شده</summary>
    private static (SignalRRealtimeNotifier Notifier,
        Mock<IClientProxy> QueueProxy, Mock<IClientProxy> NotificationProxy,
        Mock<ICurrentUserService> CurrentUser) Build(long? branchId = null)
    {
        var queueProxy = new Mock<IClientProxy>();
        queueProxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(),
                It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var notificationProxy = new Mock<IClientProxy>();
        notificationProxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(),
                It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var queueClients = new Mock<IHubClients>();
        queueClients.Setup(c => c.Group(It.IsAny<string>())).Returns(queueProxy.Object);
        queueClients.Setup(c => c.All).Returns(queueProxy.Object);

        var notificationClients = new Mock<IHubClients>();
        notificationClients.Setup(c => c.Group(It.IsAny<string>())).Returns(notificationProxy.Object);
        notificationClients.Setup(c => c.All).Returns(notificationProxy.Object);

        var queueContext = new Mock<IHubContext<QueueHub>>();
        queueContext.Setup(c => c.Clients).Returns(queueClients.Object);

        var notificationContext = new Mock<IHubContext<NotificationHub>>();
        notificationContext.Setup(c => c.Clients).Returns(notificationClients.Object);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.BranchId).Returns(branchId);

        return (new SignalRRealtimeNotifier(queueContext.Object, notificationContext.Object,
            currentUser.Object), queueProxy, notificationProxy, currentUser);
    }

    /// <summary>تغییر صف باید به گروه همان شعبه با متد «queue-changed» پخش شود</summary>
    [Fact]
    public async Task QueueChanged_SendsToBranchGroup_WithCorrectMethod()
    {
        var (notifier, queueProxy, _, _) = Build();

        await notifier.QueueChangedAsync(branchId: 12, reason: "enqueue");

        queueProxy.Verify(p => p.SendCoreAsync("queue-changed",
            It.Is<object?[]>(args => MatchesQueueMessage(args, "enqueue", 12)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>بدون شعبه صریح، شعبه کاربر جاری ملاک گروه‌بندی است</summary>
    [Fact]
    public async Task QueueChanged_WithoutBranch_FallsBackToCurrentUserBranch()
    {
        var (notifier, queueProxy, _, _) = Build(branchId: 30);

        await notifier.QueueChangedAsync();

        queueProxy.Verify(p => p.SendCoreAsync("queue-changed",
            It.Is<object?[]>(args => MatchesQueueMessage(args, "update", 30)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>اعلان کاربر باید به گروه «user-{id}» با متد «notification-received» پخش شود</summary>
    [Fact]
    public async Task NotifyUser_SendsToUserGroup_WithCorrectMethod()
    {
        var (notifier, _, notificationProxy, _) = Build();
        var notification = new Notification
        {
            Id = 45, UserId = 7, Title = "نوبت جدید", Message = "بیمار بعدی وارد شد",
            Type = NotificationType.Info, CreatedAt = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc)
        };

        await notifier.NotifyUserAsync(7, notification);

        notificationProxy.Verify(p => p.SendCoreAsync("notification-received",
            It.Is<object?[]>(args => MatchesNotificationMessage(args, 45, "نوبت جدید")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>هشدار سراسری باید از هر دو Hub به همه پخش شود</summary>
    [Fact]
    public async Task BroadcastAlert_SendsThroughBothHubs()
    {
        var (notifier, queueProxy, notificationProxy, _) = Build();

        await notifier.BroadcastAlertAsync("هشدار", "متن هشدار");

        queueProxy.Verify(p => p.SendCoreAsync("broadcast-alert",
            It.Is<object?[]>(_ => true), It.IsAny<CancellationToken>()), Times.Once);
        notificationProxy.Verify(p => p.SendCoreAsync("broadcast-alert",
            It.Is<object?[]>(_ => true), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>قرارداد نام گروه‌ها — مشترک بین Hub و پیاده‌سازی پخش</summary>
    [Fact]
    public void GroupNames_FollowDocumentedContract()
    {
        QueueHub.GroupNameFor(12).Should().Be("branch-12");
        QueueHub.GroupNameFor(null).Should().Be("queue-all");
        NotificationHub.UserGroup(7).Should().Be("user-7");
    }

    /// <summary>آزمون بار پیام تغییر صف — متد مجاز در درخت عبارت</summary>
    private static bool MatchesQueueMessage(object?[]? args, string reason, long? branchId)
        => args?[0] is QueueChangedMessage m && m.Reason == reason && m.BranchId == branchId;

    /// <summary>آزمون بار پیام اعلان — متد مجاز در درخت عبارت</summary>
    private static bool MatchesNotificationMessage(object?[]? args, long id, string title)
        => args?[0] is UserNotificationMessage m && m.Id == id && m.Title == title;
}

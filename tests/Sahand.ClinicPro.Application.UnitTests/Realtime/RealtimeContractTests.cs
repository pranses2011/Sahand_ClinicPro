using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Enums;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests.Realtime;

/// <summary>
/// تست‌های قرارداد پیام‌های بلادرنگ — گام R1
/// این رکوردها سیم مشترک سرور (SignalR) و کلاینت (WPF) هستند؛
/// تغییر ناخواسته نام فیلدها باید در تست‌ها آشکار شود
/// </summary>
public class RealtimeContractTests
{
    /// <summary>پیام تغییر صف باید با نام‌گذاری پیش‌فرض سریال و بازیابی شود</summary>
    [Fact]
    public void QueueChangedMessage_RoundTrips_WithExpectedFields()
    {
        // آماده‌سازی — پیام واقعی با مقادیر نمونه
        var original = new QueueChangedMessage("call-next", 12, new DateTime(2026, 10, 5, 8, 30, 0, DateTimeKind.Utc));

        // اجرا — سریال JSON و بازیابی
        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<QueueChangedMessage>(json);

        // تأیید — همه فیلدها حفظ شده‌اند
        roundTripped.Should().NotBeNull();
        roundTripped!.Reason.Should().Be("call-next");
        roundTripped.BranchId.Should().Be(12);
        roundTripped.RaisedAt.Should().Be(original.RaisedAt);
    }

    /// <summary>پیام اعلان کاربر باید بدون افت داده رفت‌وبرگشت کند</summary>
    [Fact]
    public void UserNotificationMessage_RoundTrips_WithExpectedFields()
    {
        var original = new UserNotificationMessage(45, "عنوان آزمایشی", "متن اعلان",
            "Success", new DateTime(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc));

        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<UserNotificationMessage>(json);

        roundTripped.Should().NotBeNull();
        roundTripped!.Id.Should().Be(45);
        roundTripped.Title.Should().Be("عنوان آزمایشی");
        roundTripped.Message.Should().Be("متن اعلان");
        roundTripped.Type.Should().Be("Success");
        roundTripped.RaisedAt.Should().Be(original.RaisedAt);
    }

    /// <summary>پیام هشدار سراسری باید بدون افت داده رفت‌وبرگشت کند</summary>
    [Fact]
    public void BroadcastAlertMessage_RoundTrips_WithExpectedFields()
    {
        var original = new BroadcastAlertMessage("قطع موقت", "سامانه سپاس تا ۱۰ دقیقه دیگر برمی‌گردد",
            new DateTime(2026, 10, 5, 10, 15, 0, DateTimeKind.Utc));

        var roundTripped = JsonSerializer.Deserialize<BroadcastAlertMessage>(
            JsonSerializer.Serialize(original));

        roundTripped.Should().NotBeNull();
        roundTripped!.Title.Should().Be("قطع موقت");
        roundTripped.Message.Should().Contain("سپاس");
    }

    /// <summary>پیاده‌سازی بی‌اثر نباید خطا بدهد — میزبان‌های بدون SignalR</summary>
    [Fact]
    public async Task NullRealtimeNotifier_AllMethods_AreNoOps()
    {
        var notifier = new NullRealtimeNotifier();

        // اجرا — هیچ‌یک نباید استثنا پرتاب کند
        await notifier.QueueChangedAsync(5, "enqueue");
        await notifier.NotifyUserAsync(7, new Notification
        {
            UserId = 7, Title = "آزمون", Type = NotificationType.Info
        });
        await notifier.BroadcastAlertAsync("عنوان", "متن");

        // اگر به اینجا رسیدیم، رفتار بی‌اثر درست است
        Assert.True(true);
    }
}

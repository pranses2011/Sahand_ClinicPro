using Sahand.ClinicPro.Domain.Entities.Communication;

namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// قرارداد اتصال بلادرنگ — انتزاع مستقل از فناوری (SignalR در API پیاده‌سازی می‌شود)
/// گرداننده‌های لایه کاربرد بدون وابستگی به SignalR رویدادهای بلادرنگ را پخش می‌کنند
/// </summary>
public interface IRealtimeNotifier
{
    /// <summary>
    /// پخش رویداد تغییر صف — همه کلاینت‌های همان شعبه صف را فوری تازه می‌کنند
    /// </summary>
    /// <param name="branchId">شناسه شعبه — تهی یعنی شعبه جاری کاربر فراخوان</param>
    /// <param name="reason">علت تغییر (enqueue / call-next / status / discharge)</param>
    /// <param name="cancellationToken">توکن لغو</param>
    Task QueueChangedAsync(long? branchId = null, string reason = "update",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// پخش اعلان فوری به یک کاربر مشخص — به‌محض ثبت اعلان در پایگاه داده فراخوانده می‌شود
    /// </summary>
    /// <param name="userId">شناسه کاربر مقصد</param>
    /// <param name="notification">موجودیت اعلان ثبت‌شده</param>
    /// <param name="cancellationToken">توکن لغو</param>
    Task NotifyUserAsync(long userId, Notification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// پخش هشدار عمومی به همه کاربران متصل — رویدادهای سراسری مانند قطع سامانه سپاس
    /// </summary>
    /// <param name="title">عنوان هشدار</param>
    /// <param name="message">متن هشدار</param>
    /// <param name="cancellationToken">توکن لغو</param>
    Task BroadcastAlertAsync(string title, string message,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// پیام رویداد تغییر صف — قرارداد سیم‌کشی سرور و کلاینت WPF
/// </summary>
/// <param name="Reason">علت تغییر صف (enqueue / call-next / status / discharge)</param>
/// <param name="BranchId">شناسه شعبه</param>
/// <param name="RaisedAt">زمان رویداد (UTC)</param>
public sealed record QueueChangedMessage(string Reason, long? BranchId, DateTime RaisedAt);

/// <summary>
/// پیام اعلان بلادرنگ کاربر — قرارداد سیم‌کشی سرور و کلاینت WPF
/// </summary>
/// <param name="Id">شناسه اعلان</param>
/// <param name="Title">عنوان</param>
/// <param name="Message">متن اعلان</param>
/// <param name="Type">نوع اعلان (Info / Success / Warning / Error / Queue)</param>
/// <param name="RaisedAt">زمان رویداد (UTC)</param>
public sealed record UserNotificationMessage(long Id, string Title, string Message,
    string Type, DateTime RaisedAt);

/// <summary>
/// پیام هشدار سراسری — همه کاربران متصل می‌بینند
/// </summary>
/// <param name="Title">عنوان هشدار</param>
/// <param name="Message">متن هشدار</param>
/// <param name="RaisedAt">زمان رویداد (UTC)</param>
public sealed record BroadcastAlertMessage(string Title, string Message, DateTime RaisedAt);

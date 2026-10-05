namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// قرارداد اتصال بلادرنگ کلاینت — دو اتصال SignalR به Hubهای صف و اعلان (گام R1)
/// </summary>
public interface IRealtimeClient
{
    /// <summary>آیا اتصال برقرار است</summary>
    bool IsConnected { get; }

    /// <summary>وضعیت اتصال برای StatusBar — «متصل» / «قطع» / «در حال اتصال»</summary>
    string StatusText { get; }

    /// <summary>رویداد تغییر صف — علت تغییر و شعبه</summary>
    event EventHandler<(string Reason, long? BranchId)>? QueueChanged;

    /// <summary>رویداد دریافت اعلان جدید — عنوان و متن و نوع</summary>
    event EventHandler<(string Title, string Message, string Type)>? NotificationReceived;

    /// <summary>رویداد هشدار سراسری</summary>
    event EventHandler<(string Title, string Message)>? BroadcastAlert;

    /// <summary>رویداد تغییر وضعیت اتصال — برای StatusBar</summary>
    event EventHandler? ConnectionStateChanged;

    /// <summary>
    /// برقراری اتصال به هر دو Hub با توکن کاربر جاری — پس از ورود موفق فراخوانده می‌شود
    /// اگر قبلاً متصل باشد بی‌اثر است
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// قطع اتصال — هنگام خروج از حساب یا بستن برنامه
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

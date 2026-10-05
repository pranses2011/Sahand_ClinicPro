namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// قرارداد سرویس پیام‌های شناور — موفقیت سبز و خطا قرمز طبق قوانین کلی UI
/// پوسته اصلی به رویداد گوش می‌دهد و Snackbar متریال را نمایش می‌دهد
/// </summary>
public interface ISnackbarService
{
    /// <summary>رویداد انتشار پیام — (متن، آیا موفق است)</summary>
    event Action<string, bool>? MessagePosted;

    /// <summary>نمایش پیام موفق</summary>
    void Success(string message);

    /// <summary>نمایش پیام خطا</summary>
    void Error(string message);

    /// <summary>نمایش پیام خبری</summary>
    void Info(string message);
}

/// <summary>
/// پیاده‌سازی سرویس پیام شناور — انتشار از طریق رویداد
/// </summary>
public sealed class SnackbarService : ISnackbarService
{
    /// <summary>رویداد انتشار پیام</summary>
    public event Action<string, bool>? MessagePosted;

    /// <summary>پیام موفق</summary>
    public void Success(string message) => Post(message, true);

    /// <summary>پیام خطا</summary>
    public void Error(string message) => Post(message, false);

    /// <summary>پیام خبری</summary>
    public void Info(string message) => Post(message, true);

    /// <summary>انتشار امن رویداد روی نخ رابط کاربری</summary>
    private void Post(string message, bool success)
    {
        var prefixed = success ? $"✅ {message}" : $"❌ {message}";
        var handler = MessagePosted;
        if (handler is null)
            return;

        // اطمینان از اجرا روی نخ رابط کاربری
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is { } d && !d.CheckAccess())
            d.BeginInvoke(() => handler(prefixed, success));
        else
            handler(prefixed, success);
    }
}

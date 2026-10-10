namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// نوع پیام شناور — رنگ دایرهٔ آیکون و علامت آن بر همین اساس تعیین می‌شود.
/// چرخهٔ سی‌وهشتم (گزارش مالک): «پیام‌های توست ظاهرشان مانند تصویر نمونه
/// (ToastNotif.png) با رنگبندی بر طبق نوع و پیام» — چهار نوع رنگی:
/// سبز موفقیت / قرمز خطا / کهربایی هشدار / آبی خبری.
/// </summary>
public enum ToastKind
{
    /// <summary>موفقیت — سبز #059669 با تیک سفید درشت (دقیقاً مطابق نمونهٔ مالک)</summary>
    Success,

    /// <summary>خطا — قرمز #DC2626 با علامت ضربدر سفید</summary>
    Error,

    /// <summary>هشدار — کهربایی #D97706 با علامت تعجب سفید</summary>
    Warning,

    /// <summary>خبری — آبی #2563EB با نشان «i» سفید</summary>
    Info,
}

/// <summary>
/// مدل یک پیام شناور — نوع + متن؛ میزبان پوسته (MainWindow) همین مدل‌ها را
/// در کپسول‌های گوشه‌گرد نمایش می‌دهد. رکورد است تا برابری مقداری داشته
/// باشد و حذف از فهرست پس از اتمام زمان یا کلیک ساده باشد.
/// </summary>
/// <param name="Kind">نوع پیام — تعیین‌کنندهٔ رنگ و آیکون کپسول</param>
/// <param name="Text">متن پیام — بدون پیشوند ایموجی (آیکون خود کپسول نقش را می‌زند)</param>
public sealed record ToastMessage(ToastKind Kind, string Text);

/// <summary>
/// قرارداد سرویس پیام‌های شناور — چهار نوع رنگی (چرخهٔ ۳۸)؛
/// پوسته اصلی به رویداد گوش می‌دهد و کپسول‌های نمونه‌وار نمایش می‌دهد.
/// پیش‌تر پیام با پیشوندهای ایموجی «✅/❌» مخلوط می‌شد و دو نوع بیشتر نداشت؛
/// اکنون نوع پیام ساخت‌یافته است و ظاهر (رنگ/آیکون) کار خود سرویس است.
/// </summary>
public interface ISnackbarService
{
    /// <summary>رویداد انتشار پیام — مدل کامل (نوع + متن)</summary>
    event Action<ToastMessage>? MessagePosted;

    /// <summary>نمایش پیام موفقیت — سبز با تیک</summary>
    void Success(string message);

    /// <summary>نمایش پیام خطا — قرمز با ضربدر</summary>
    void Error(string message);

    /// <summary>نمایش پیام هشدار — کهربایی با تعجب</summary>
    void Warning(string message);

    /// <summary>نمایش پیام خبری — آبی با «i»</summary>
    void Info(string message);
}

/// <summary>
/// پیاده‌سازی سرویس پیام شناور — انتشار ساخت‌یافته از طریق رویداد؛
/// متن پیام دیگر دستکاری نمی‌شود (پیشوندهای ایموجی حذف شدند — آیکونِ خودِ
/// کپسول نوع پیام را نشان می‌دهد و متن فارسی تمیز می‌ماند).
/// </summary>
public sealed class SnackbarService : ISnackbarService
{
    /// <summary>رویداد انتشار پیام</summary>
    public event Action<ToastMessage>? MessagePosted;

    /// <summary>پیام موفق — سبز</summary>
    public void Success(string message) => Post(message, ToastKind.Success);

    /// <summary>پیام خطا — قرمز</summary>
    public void Error(string message) => Post(message, ToastKind.Error);

    /// <summary>پیام هشدار — کهربایی</summary>
    public void Warning(string message) => Post(message, ToastKind.Warning);

    /// <summary>پیام خبری — آبی</summary>
    public void Info(string message) => Post(message, ToastKind.Info);

    /// <summary>
    /// انتشار امن رویداد روی نخ رابط کاربری — پیام‌ها ممکن است از نخ‌های
    /// پس‌زمینه (SignalR/تایمرها/async) هم فراخوانی شوند؛ میزبان نمایش در
    /// پوسته است و باید روی نخ UI اجرا شود.
    /// </summary>
    private void Post(string message, ToastKind kind)
    {
        if (string.IsNullOrWhiteSpace(message))
            return; // پیام خالی هیچ نمایش نمی‌دهد

        var toast = new ToastMessage(kind, message.Trim());
        var handler = MessagePosted;
        if (handler is null)
            return;

        // اطمینان از اجرا روی نخ رابط کاربری
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is { } d && !d.CheckAccess())
            d.BeginInvoke(() => handler(toast));
        else
            handler(toast);
    }
}

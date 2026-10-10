using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// ابزارهای مشترک عکس پروفایل پرسنل/پزشکان — چرخه هجدهم (گزارش دوازدهم مالک):
/// انتخاب فایل (JPG/PNG)، تبدیل بایت به ImageSource، بارگذاری/دریافت از API
/// و بزرگ‌نمایی در PhotoViewerWindow.
/// چرخهٔ سی‌وهفتم (گزارش مالک): «در لیست پزشکان و لیست پرسنل تصویرشان به‌صورت
/// بندانگشتیِ قابل‌کلیک برای بزرگ‌نمایی نمایش داده شود» — بندانگشتی‌ساز کم‌حافظه
/// (DecodePixelWidth) + کش ایستا برای پرهیز از دانلود تکراری بین صفحه‌ها/بازخوانی‌ها.
/// </summary>
public static class PhotoTools
{
    /// <summary>حداکثر حجم مجاز عکس — هماهنگ با PeoplePhotosController</summary>
    private const int MaxPhotoBytes = 5 * 1024 * 1024;

    /// <summary>
    /// عرض رمزگشایی بندانگشتی (پیکسل) — بندانگشتی در گرید تا ~۳۴px نمایش داده
    /// می‌شود؛ ۹۶ پیکسل هم حافظهٔ بسیار کمی می‌گیرد (چند ده کیلوبایت) و هم روی
    /// نمایشگرهای با مقیاس DPI بالا کاملاً شفاف می‌ماند.
    /// </summary>
    public const int ThumbnailDecodeWidth = 96;

    /// <summary>
    /// کش بندانگشتی‌های ایستا — کلید قراردادی: «doc:{شناسهٔ پزشک}» و
    /// «emp:{شناسهٔ پرسنل}». ImageSource‌ها Freeze می‌شوند و بین رشته‌ها
    /// امن‌اند؛ عمر کش تا پایان اجرای برنامه است و با تعویض/حذف عکس
    /// (InvalidateThumbnail) باطل می‌شود.
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ImageSource> ThumbnailCache = new();

    /// <summary>انتخاب فایل عکس از دیالوگ — خروجی: (بایت‌ها، نام فایل)</summary>
    public static (byte[] Bytes, string FileName)? PickPhoto(string title = "انتخاب عکس پروفایل")
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            Filter = "تصویر JPG|*.jpg;*.jpeg|تصویر PNG|*.png",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != true) return null;

        var bytes = File.ReadAllBytes(dialog.FileName);
        if (bytes.Length == 0)
        {
            Views.AppDialog.Warn("عکس نامعتبر", "فایل انتخاب‌شده خالی است.");
            return null;
        }
        if (bytes.Length > MaxPhotoBytes)
        {
            Views.AppDialog.Warn("عکس بزرگ", "حجم عکس نباید بیش از ۵ مگابایت باشد.");
            return null;
        }
        return (bytes, Path.GetFileName(dialog.FileName));
    }

    /// <summary>تبدیل بایت‌های عکس به ImageSource با کش کامل حافظه</summary>
    public static ImageSource? ToImage(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0) return null;
        var image = new BitmapImage();
        using var stream = new MemoryStream(bytes);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    // ═══ چرخهٔ سی‌وهفتم (گزارش مالک) — بندانگشتی لیست پزشکان/پرسنل ═══

    /// <summary>کلید کش بندانگشتی پزشک — عکس زیر رکورد پرسنل مرتبط است</summary>
    public static string DoctorThumbnailKey(long doctorId) => $"doc:{doctorId}";

    /// <summary>کلید کش بندانگشتی پرسنل</summary>
    public static string EmployeeThumbnailKey(long employeeId) => $"emp:{employeeId}";

    /// <summary>
    /// تبدیل بایت‌های عکس به بندانگشتی کم‌حافظه — رمزگشایی فقط در عرض داده‌شده
    /// (DecodePixelWidth)؛ برخلاف ToImage که تصویر کامل را در حافظه نگه می‌دارد،
    /// این متد فقط نسخهٔ کوچک‌شدهٔ نهایی را زنده نگه می‌دارد (چند ده کیلوبایت).
    /// نتیجه Freeze می‌شود تا در گرید/بین رشته‌ها بدون قفل امن باشد.
    /// </summary>
    /// <param name="bytes">بایت‌های عکس اصلی — تهی/خالی یا خراب یعنی خروجی تهی</param>
    /// <param name="decodePixelWidth">عرض رمزگشایی (پیش‌فرض استاندارد فهرست‌ها)</param>
    public static ImageSource? ToThumbnail(byte[]? bytes, int decodePixelWidth = ThumbnailDecodeWidth)
    {
        if (bytes is null || bytes.Length == 0) return null;
        try
        {
            var image = new BitmapImage();
            using var stream = new MemoryStream(bytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            // رمزگشایی فقط تا عرض داده‌شده — حافظهٔ مصرفی مستقل از اندازهٔ عکس اصلی
            image.DecodePixelWidth = Math.Max(16, decodePixelWidth);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null; // بایت‌های خراب/قالب ناشناخته — بندانگشتی ساخته نمی‌شود
        }
    }

    /// <summary>بندانگشتی کش‌شده — تهی یعنی هنوز در کش نیست</summary>
    public static ImageSource? GetCachedThumbnail(string key) =>
        ThumbnailCache.TryGetValue(key, out var cached) ? cached : null;

    /// <summary>ثبت بندانگشتی در کش (ایستا — تا پایان اجرای برنامه می‌ماند)</summary>
    public static void CacheThumbnail(string key, ImageSource thumbnail) =>
        ThumbnailCache[key] = thumbnail;

    /// <summary>
    /// باطل‌کردن کش بندانگشتی — پس از تعویض/حذف عکس در سرور صدا زده می‌شود تا
    /// بازخوانی بعدی فهرست، نسخهٔ تازه را دانلود کند نه بندانگشتی کهنه را.
    /// </summary>
    public static void InvalidateThumbnail(string key) => ThumbnailCache.TryRemove(key, out _);

    /// <summary>گرفتن عکس پروفایل پرسنل از API — null یعنی عکسی ثبت نشده</summary>
    public static async Task<byte[]?> DownloadEmployeePhotoAsync(IApiClient api, long employeeId,
        CancellationToken ct = default)
    {
        try
        {
            return await api.DownloadFileAsync($"people/employees/{employeeId}/photo", ct);
        }
        catch
        {
            return null; // عکسی نیست یا خطای گذرا — بی‌صدا
        }
    }

    /// <summary>گرفتن عکس پزشک از API (از پرسنل مرتبط) — null یعنی عکسی ثبت نشده</summary>
    public static async Task<byte[]?> DownloadDoctorPhotoAsync(IApiClient api, long doctorId,
        CancellationToken ct = default)
    {
        try
        {
            return await api.DownloadFileAsync($"people/doctors/{doctorId}/photo", ct);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>بارگذاری عکس پروفایل پرسنل در API — PostFileAsync چندبخشی</summary>
    public static async Task<bool> UploadEmployeePhotoAsync(IApiClient api, long employeeId,
        byte[] bytes, string fileName, CancellationToken ct = default)
    {
        var response = await api.PostFileAsync<UploadResponse>(
            $"people/employees/{employeeId}/photo", fileName, bytes, null, ct);
        return response?.IsSuccess == true;
    }

    /// <summary>
    /// بزرگ‌نمایی عکس در نمایشگر تمام‌صفحه — «با کلیک روی عکس اون رو بزرگ نشون بده»
    /// </summary>
    public static void ShowLarge(byte[]? bytes, string title)
    {
        var image = ToImage(bytes);
        if (image is null)
        {
            Views.AppDialog.Info("بدون عکس", "برای این شخص عکسی ثبت نشده است.");
            return;
        }
        var viewer = new PhotoViewerWindow(image, title)
        {
            Owner = System.Windows.Application.Current?.Windows.OfType<Window>()
                .FirstOrDefault(w => w.IsActive)
        };
        viewer.ShowDialog();
    }

    /// <summary>پاسخ بارگذاری عکس — پاکت استاندارد</summary>
    private sealed class UploadResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
    }
}

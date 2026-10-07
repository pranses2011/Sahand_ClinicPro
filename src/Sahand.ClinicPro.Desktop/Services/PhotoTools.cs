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
/// </summary>
public static class PhotoTools
{
    /// <summary>حداکثر حجم مجاز عکس — هماهنگ با PeoplePhotosController</summary>
    private const int MaxPhotoBytes = 5 * 1024 * 1024;

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

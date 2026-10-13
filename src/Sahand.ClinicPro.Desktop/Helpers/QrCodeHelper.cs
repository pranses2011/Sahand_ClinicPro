using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>
/// سازندهٔ تصویر QR برای اسناد چاپی — چرخهٔ ۶۹ (فاز ۳ / F-01+F-02 نقشه راه ai-review)
/// «بارکد/QR نسخه و برچسب نمونه»
///
/// از موتور بی‌وابستگیِ QRCoder (PngByteQRCode — بدون System.Drawing، سازگار
/// با net8.0 و لینوکس CI) استفاده می‌شود؛ خروجی BitmapSource برای درج مستقیم
/// در FlowDocument چاپی (System.Windows.Documents.Image) آماده است.
/// </summary>
public static class QrCodeHelper
{
    /// <summary>
    /// ساخت تصویر QR از متن — اندازهٔ منطقی به پیکسل (پیش‌فرض ۹۰px مربعی).
    /// خطای موتور، تهی برمی‌گرداند (فراخوان با تهی، بخش QR را حذف می‌کند —
    /// چاپ اصلی هرگز به‌خاطر QR نباید شکست بخورد).
    /// </summary>
    /// <param name="payload">متن کدگذاری‌شده در QR — مثلاً شناسهٔ راستی‌آزمایی نسخه</param>
    /// <param name="sizePx">اندازهٔ منطقی تصویر (پیکسل) — همان عرض و ارتفاع</param>
    public static BitmapSource? Build(string payload, int sizePx = 90)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        try
        {
            // موتور بایتی خالص — بدون وابستگی به GDI+ (سازگار با هر محیط اجرا)
            using var generator = new QRCoder.QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCoder.QRCodeGenerator.ECCLevel.M);
            var pngBytes = new QRCoder.PngByteQRCode(data).GetGraphic(8);

            // بایت PNG → BitmapSource قابل استفاده در سند چاپی
            var image = new BitmapImage();
            using var stream = new MemoryStream(pngBytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze(); // رندر نخ چاپ — freeze برای دسترسی چندنخی امن

            return image;
        }
        catch
        {
            // QR جانبیِ سند است — نبودش چاپ را نمی‌شکند
            return null;
        }
    }

    /// <summary>
    /// بار راستی‌آزمایی نسخهٔ الکترونیک — چرخهٔ ۶۹ (F-01):
    /// قالب کوتاه و قابل تفکیک «CLINICPRO-RX» که با هر اسکنر QR قابل خواندن
    /// است و شمارهٔ نسخه + بیمار + تاریخ را با هم حمل می‌کند (داروخانه/بیمار
    /// می‌تواند اصالت جفت را با برگهٔ کاغذی مقایسه کند).
    /// </summary>
    public static string RxPayload(string? prescriptionCode, string patientName, string dateShamsi)
        => $"CLINICPRO-RX|{prescriptionCode ?? "-"}|{patientName}|{dateShamsi}";

    /// <summary>
    /// بار برچسب نمونهٔ آزمایشگاه — چرخهٔ ۶۹ (F-02):
    /// شناسهٔ یکتای «درخواست + قلم» که روی ظرف نمونه چسبانده می‌شود؛ آزمایشگاه
    /// با اسکن، قلم درست را در فرم نتیجه پیدا می‌کند (خطای جابه‌جایی نمونه صفر).
    /// </summary>
    public static string SpecimenPayload(string orderNumber, long itemId, string testName)
        => $"CLINICPRO-LAB|{orderNumber}|I{itemId}|{testName}";
}

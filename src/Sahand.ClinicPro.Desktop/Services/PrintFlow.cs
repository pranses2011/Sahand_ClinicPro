using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// دروازهٔ یکپارچهٔ چاپ برنامه — چرخهٔ ۶۷ (گزارش مالک):
/// «در تنظیمات چاپ گزینه‌ای باشه برای پیش‌نمایش چاپ تا با فعال کردنش، تمامی
/// چاپ‌ها قبل از ارسال به پرینتر پیش‌نمایش نشون بدن و در صورت نیاز چاپشون
/// کنیم و اگه غیرفعال باشه تمامی چاپ‌ها مستقیم به پرینتر ارسال بشه».
/// </summary>
/// <remarks>
/// ═══ چرا این کلاس لازم شد ═══
/// تنظیم «نمایش پیش‌نمایش قبل از چاپ» (ShowPreview در دستهٔ Printer) از چرخهٔ ۲۴
/// موجود بود ولی «فقط» چاپ نسخهٔ الکترونیک از آن می‌گذشت؛ قبض نوبت، رسید شیفت،
/// کارت بیمار، فیش حقوقی و نتیجهٔ آزمایش همیشه بی‌پیش‌نمایش و مستقیم به چاپگر
/// می‌رفتند. اکنون همهٔ مسیرهای چاپ از همین دروازه می‌گذرند:
///
///   • Print(doc, ...) → تنظیم ShowPreview در ذخیرهٔ محلی خوانده می‌شود؛
///     روشن ⇒ پنجرهٔ «پیش‌نمایش چاپ» با دکمهٔ چاپ باز می‌شود (کاربر یا چاپ
///     می‌کند یا می‌بندد)؛ خاموش ⇒ ارسال بی‌واسطه به چاپگر مقصد.
///
/// ═══ هم‌زیستی با مسیر قدیمی نسخه ═══
/// PrescriptionPrintHelper.ShowPreview خودش پنجرهٔ پیش‌نمایش تخصصی (با انتخاب
/// طرح چاپ) دارد و دست نخورده باقی مانده — آن مسیر انتخاب‌گر طرح دارد و
/// جایگزینی‌اش از دست می‌رفت؛ سایر چاپ‌ها به همین دروازه می‌آیند.
///
/// ═══ نحوهٔ خواندن تنظیم ═══
/// همان الگوی آینهٔ محلی چرخهٔ ۲۴ (TicketPrinter): مقدار سمت سرور در
/// SettingsViewModel ذخیره و «آینه»ی محلی‌اش هم برای دسترسی بی‌درنگ (بدون
/// رفت‌وبرگشت شبکه) نوشته می‌شود — کلید Printer.ShowPreview.
/// </remarks>
public static class PrintFlow
{
    /// <summary>کلید ذخیرهٔ محلی پرچم پیش‌نمایش — آینهٔ تنظیم سرورِ Printer.ShowPreview</summary>
    public const string PreviewSettingKey = "Printer.ShowPreview";

    /// <summary>
    /// پرچم «پیش‌نمایش قبل از چاپ» — خوانده‌شده از ذخیرهٔ محلی؛
    /// پیش‌فرض روشن است (با متن فارسی «true»/«false» ذخیره می‌شود).
    /// </summary>
    public static bool PreviewEnabled =>
        LocalSettingsStore.Get(PreviewSettingKey) != "false";

    /// <summary>
    /// چاپ یک سند جریان با احترام به تنظیم پیش‌نمایش.
    /// </summary>
    /// <param name="document">سند آمادهٔ چاپ (عرض/ارتفاع صفحه باید از قبل تنظیم شده باشد)</param>
    /// <param name="title">عنوان چاپ — در پنجرهٔ پیش‌نمایش و jobname چاپگر</param>
    /// <param name="printerName">چاپگر مقصد از تنظیمات — تهی یعنی پیش‌فرض ویندوز</param>
    /// <param name="pageWidth">عرض کاغذ (پیکسل ۹۶dpi) — فقط برای مسیر چاپ مستقیم؛ تهی = خودکار چاپگر</param>
    /// <param name="pageHeight">ارتفاع کاغذ (پیکسل ۹۶dpi) — تهی = خودکار چاپگر</param>
    /// <returns>true اگر چاپ (یا پیش‌نمایشِ منتهی به چاپ) انجام شد</returns>
    public static bool Print(FlowDocument document, string title, string? printerName = null,
        double? pageWidth = null, double? pageHeight = null)
    {
        if (PreviewEnabled)
        {
            // پیش‌نمایش — مالکیت پنجره با پنجرهٔ فعال برنامه؛ کاربر از همان‌جا چاپ می‌کند
            var preview = new PrintPreviewWindow(document, title, printerName)
            {
                Owner = System.Windows.Application.Current?.Windows
                    .Cast<Window>().FirstOrDefault(w => w.IsActive)
                    ?? System.Windows.Application.Current?.MainWindow
            };
            preview.ShowDialog();
            return preview.Printed;
        }

        // چاپ مستقیم — بدون هیچ پنجره‌ای (رفتار پیشین)
        return PrintDirect(document, title, printerName, pageWidth, pageHeight);
    }

    /// <summary>
    /// ارسال مستقیم به چاپگر — بدون پیش‌نمایش؛ چاپگر مقصد از تنظیمات یا
    /// پیش‌فرض ویندوز. خطای چاپ با دیالوگ فارسی برنامه (نه کرش) اعلام می‌شود.
    /// </summary>
    /// <param name="document">سند آماده</param>
    /// <param name="title">نام کار چاپ</param>
    /// <param name="printerName">چاپگر مقصد — تهی یعنی پیش‌فرض</param>
    /// <param name="pageWidth">عرض کاغذ (پیکسل ۹۶dpi) — تهی = خودکار چاپگر</param>
    /// <param name="pageHeight">ارتفاع کاغذ (پیکسل ۹۶dpi) — تهی = خودکار چاپگر</param>
    /// <returns>true اگر ارسال موفق بود</returns>
    public static bool PrintDirect(FlowDocument document, string title, string? printerName = null,
        double? pageWidth = null, double? pageHeight = null)
    {
        try
        {
            var dialog = new PrintDialog();

            // چاپگر مقصد — همان الگوی AppointmentTicketPrinter (خطای فارسی اگر نبود)
            if (!string.IsNullOrWhiteSpace(printerName))
            {
                var server = new LocalPrintServer();
                var queue = server.GetPrintQueues()
                    .FirstOrDefault(q => q.FullName == printerName);
                if (queue is null)
                {
                    AppDialog.Error("چاپ " + title,
                        $"چاپگر «{printerName}» در دسترس نیست.{Environment.NewLine}در تنظیمات → چاپگر، چاپگر مقصد را بررسی کنید.");
                    return false;
                }
                dialog.PrintQueue = queue;
            }

            // اندازهٔ کاغذ سفارشی — فیش‌پرینترهای حرارتی (۸۰ میلی) و سایر غیر-A4
            if (pageWidth is { } w && pageHeight is { } h)
                dialog.PrintTicket.PageMediaSize = new PageMediaSize(w, h);

            if (document is IDocumentPaginatorSource src)
                dialog.PrintDocument(src.DocumentPaginator, title);
            return true;
        }
        catch (Exception)
        {
            // خطای درایور/دسترسی — پیام فارسی برنامه به‌جای کرش (قانون دیالوگ فارسی)
            AppDialog.Error("چاپ " + title,
                "ارسال به چاپگر ناموفق بود." + Environment.NewLine +
                "اتصال چاپگر را بررسی یا از پیش‌نمایش چاپ استفاده کنید.");
            return false;
        }
    }
}

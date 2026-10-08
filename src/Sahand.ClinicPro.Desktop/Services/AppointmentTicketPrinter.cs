using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// چاپ قبض نوبت — چرخه بیست‌وچهارم (گزارش هفدهم مالک):
/// «یک چاپگر پیش‌فرض هم برای نوبت‌دهی باشد تا قبض‌های نوبت با آن چاپ شود».
/// ═══════════════════════════════════════════════════════════════════
/// • چاپ مستقیم به «چاپگر نوبت‌دهی» انتخاب‌شده در تنظیمات (بدون دیالوگ)؛
///   در نبود انتخاب، چاپگر پیش‌فرض ویندوز
/// • چیدمان فشردهٔ مناسب فیش‌پرینترهای حرارتی: عرض ۸۰ میلی‌متر (۳۰۲px در
///   ۹۶dpi) با متن مرکزچین و خط جداکننده — روی کاغذ A4 هم بی‌نقص چاپ می‌شود
/// • اگر پرینتر در دسترس نباشد، پیام خطای فارسی (نه کرش) نمایش داده می‌شود
/// </summary>
public static class AppointmentTicketPrinter
{
    /// <summary>عرض قبض — ۸۰ میلی‌متر کاغذ حرارتی رایج (پیکسل در ۹۶dpi)</summary>
    private const double TicketWidth = 302;

    /// <summary>ارتفاع صفحهٔ قبض — بلند؛ خودکار به اندازهٔ محتوا کوتاه می‌شود</summary>
    private const double TicketHeight = 600;

    /// <summary>
    /// چاپ مستقیم قبض نوبت — بدون نمایش دیالوگ؛ چاپگر مقصد از تنظیمات.
    /// </summary>
    /// <param name="printerName">نام چاپگر مقصد — تهی یعنی پیش‌فرض ویندوز</param>
    /// <param name="clinicName">نام درمانگاه — سربرگ قبض</param>
    /// <param name="code">کد پیگیری نوبت</param>
    /// <param name="patientName">نام بیمار</param>
    /// <param name="doctorName">نام پزشک</param>
    /// <param name="dateShamsi">تاریخ نوبت (شمسی)</param>
    /// <param name="time">ساعت نوبت</param>
    /// <param name="turnText">شمارهٔ نوبت/نوع ویزیت</param>
    /// <returns>true اگر چاپ با موفقیت ارسال شد</returns>
    public static bool Print(string? printerName, string clinicName, string code,
        string patientName, string doctorName, string dateShamsi, string time, string turnText)
    {
        try
        {
            var dialog = new PrintDialog();

            // چاپگر مقصد — انتخاب تنظیمات یا پیش‌فرض ویندوز
            if (!string.IsNullOrWhiteSpace(printerName))
            {
                var server = new LocalPrintServer();
                var queue = server.GetPrintQueues()
                    .FirstOrDefault(q => q.FullName == printerName);
                if (queue is null)
                {
                    Views.AppDialog.Error("چاپ قبض نوبت",
                        $"چاپگر «{printerName}» در دسترس نیست.{Environment.NewLine}در تنظیمات → چاپگر، چاپگر نوبت‌دهی را بررسی کنید.");
                    return false;
                }
                dialog.PrintQueue = queue;
            }

            dialog.PrintTicket.PageMediaSize =
                new PageMediaSize(PageMediaSizeWidth, TicketHeight);

            var doc = BuildTicket(clinicName, code, patientName, doctorName, dateShamsi, time, turnText);
            doc.PageWidth = TicketWidth;
            doc.PageHeight = TicketHeight;

            if (doc is IDocumentPaginatorSource src)
                dialog.PrintDocument(src.DocumentPaginator, $"قبض نوبت {code}");

            return true;
        }
        catch (Exception)
        {
            // خطای درایور/دسترسی — پیام فارسی برنامه به‌جای کرش (قانون دیالوگ فارسی)
            Views.AppDialog.Error("چاپ قبض نوبت",
                "ارسال قبض به چاپگر ناموفق بود." + Environment.NewLine +
                "اتصال چاپگر را بررسی یا از پیش‌نمایش چاپ استفاده کنید.");
            return false;
        }
    }

    /// <summary>عرض کاغذ فیش حرارتی ۸۰ میلی‌متری بر حسب پیکسل (۹۶dpi)</summary>
    private static double PageMediaSizeWidth => TicketWidth;

    /// <summary>
    /// ساخت سند قبض — متن مرکزچین با فونت وزیرمتن (جاسازی‌شده)؛
    /// ارقام فارسی در همهٔ مقادیر؛ خط‌چین جداکننده بین بخش‌ها.
    /// </summary>
    private static FlowDocument BuildTicket(string clinicName, string code,
        string patientName, string doctorName, string dateShamsi, string time, string turnText)
    {
        var doc = new FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = Helpers.FontAssets.PrintFont,
            FontSize = 11,
            PagePadding = new Thickness(10, 6, 10, 6),
            ColumnWidth = double.PositiveInfinity
        };

        // سربرگ — نام درمانگاه بزرگ
        doc.Blocks.Add(Center(clinicName, 15, FontWeights.Bold));
        doc.Blocks.Add(Center("قبض نوبت‌دهی", 11, FontWeights.SemiBold, muted: true));
        doc.Blocks.Add(Dashed());

        // کد پیگیری — درشت و برجسته (مهم‌ترین دادهٔ قبض)
        doc.Blocks.Add(Center($"کد پیگیری: {PersianDateHelper.ToPersianDigits(code)}",
            13, FontWeights.Bold));

        doc.Blocks.Add(Dashed());

        // مشخصات نوبت — هر ردیف یک خط
        doc.Blocks.Add(Row("بیمار", patientName));
        doc.Blocks.Add(Row("پزشک", doctorName));
        doc.Blocks.Add(Row("تاریخ", PersianDateHelper.ToPersianDigits(dateShamsi)));
        doc.Blocks.Add(Row("ساعت", PersianDateHelper.ToPersianDigits(time)));
        if (!string.IsNullOrWhiteSpace(turnText))
            doc.Blocks.Add(Row("نوبت", PersianDateHelper.ToPersianDigits(turnText)));

        doc.Blocks.Add(Dashed());
        doc.Blocks.Add(Center("با سلامت همراهتان هستیم", 9.5, FontWeights.Normal, muted: true));
        doc.Blocks.Add(Center(PersianDateHelper.ToShamsiFa(DateTime.Now), 9, FontWeights.Normal, muted: true));

        return doc;
    }

    /// <summary>پاراگراف مرکزچین — muted=true یعنی متن کم‌رنگ راهنما
    /// (Run از نوع TextElement است و Opacity ندارد؛ رنگ خاکستری جایگزین شد)</summary>
    private static Paragraph Center(string text, double size, FontWeight weight, bool muted = false)
    {
        var p = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 2, 0, 2)
        };
        p.Inlines.Add(new Run(text)
        {
            FontSize = size,
            FontWeight = weight,
            Foreground = muted
                ? new System.Windows.Media.SolidColorBrush(
                      System.Windows.Media.Color.FromRgb(0x78, 0x82, 0x8C))
                : System.Windows.Media.Brushes.Black
        });
        return p;
    }

    /// <summary>پاراگراف دوستونهٔ «برچسب: مقدار» — برچسب توپر</summary>
    private static Paragraph Row(string label, string value)
    {
        var p = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
        p.Inlines.Add(new Run($"{label}: ") { FontWeight = FontWeights.Bold });
        p.Inlines.Add(new Run(value));
        return p;
    }

    /// <summary>خط‌چین جداکننده — سجا رایج قبض‌های حرارتی</summary>
    private static Paragraph Dashed()
    {
        var p = new Paragraph
        {
            Margin = new Thickness(0, 4, 0, 4),
            Foreground = System.Windows.Media.Brushes.Gray
        };
        p.Inlines.Add(new Run(new string('-', 34)) { FontSize = 9 });
        return p;
    }
}

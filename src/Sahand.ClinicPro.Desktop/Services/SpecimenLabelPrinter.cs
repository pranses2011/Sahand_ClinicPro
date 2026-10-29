using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// چاپگر برچسب نمونه‌های آزمایشگاه — چرخهٔ ۶۹ (فاز ۳ / F-02 نقشه راه ai-review)
/// «برچسب نمونه»: برای هر قلم درخواست، یک برچسب کوچک با QR یکتای
/// «درخواست + قلم» + نام بیمار + نام آزمایش چاپ می‌شود که روی ظرف نمونه
/// (لوله/ظرف ادرار و…) چسبانده می‌شود؛ اسکن آن در آزمایشگاه، قلم درست را
/// در فرم نتیجه پیدا می‌کند — خطای جابه‌جایی نمونه به صفر می‌رسد.
///
/// ساختار برچسب (سطر به سطر):
/// [QR یکتا ۷۰px] | «درمانگاه سهند — برچسب نمونه»
///                | شماره درخواست (درشت) · نام بیمار
///                | نام آزمایش (برجسته) + تاریخ درخواست
/// همهٔ برچسب‌ها در یک سند (یک برچسب در هر صفحه) چاپ می‌شوند تا برش با
/// قیچی/برچسب‌خور در مرز صفحه انجام شود.
/// </summary>
public static class SpecimenLabelPrinter
{
    /// <summary>
    /// ساخت سند و چاپ برچسب‌های همهٔ اقلام یک درخواست — با احترام به تنظیم
    /// پیش‌نمایش (PrintFlow) و چاپگر پیش‌فرض تنظیمات.
    /// </summary>
    /// <param name="detail">جزئیات درخواست از GET lab/orders/{id}</param>
    /// <param name="orderId">شناسه درخواست — برای عنوان کار چاپ</param>
    /// <returns>true اگر چاپ (یا پیش‌نمایش منتهی به چاپ) انجام شد</returns>
    public static bool Print(LabResultViewModel.LabOrderDetail detail, long orderId)
    {
        var doc = BuildLabels(detail);

        // برچسب نیم‌صفحه‌ای — ارتفاع کوتاه‌تر از A4 تا برش آسان باشد
        doc.PageWidth = 793.7;
        doc.PageHeight = 340; // ~۹۰ میلی‌متر — اندازهٔ رایج برچسب‌خور آزمایشگاه

        var orderNo = detail.OrderNumber ?? orderId.ToString();
        return PrintFlow.Print(doc, $"برچسب نمونه‌های آزمایش {orderNo}",
            PrintJobSettings.PrinterFor("SpecimenLabel"), jobKey: "SpecimenLabel");
    }

    /// <summary>ساخت سند برچسب‌ها — یک برچسب در هر صفحه (مرز برش)</summary>
    private static FlowDocument BuildLabels(LabResultViewModel.LabOrderDetail d)
    {
        var doc = new FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = PrintFont(),
            FontSize = 12,
            PagePadding = new Thickness(28),
            ColumnWidth = double.PositiveInfinity
        };

        var orderNo = d.OrderNumber ?? d.Id.ToString();
        foreach (var item in d.Items ?? new List<LabResultViewModel.LabOrderItemDto>())
        {
            doc.Blocks.Add(BuildOneLabel(orderNo, d.PatientName, d.DateShamsi,
                item.TestName, item.Id));
        }

        return doc;
    }

    /// <summary>یک برچسب — جدول دوسطونه: ستون راست QR / ستون چپ شناسنامهٔ نمونه</summary>
    private static Block BuildOneLabel(string orderNo, string patientName,
        string dateShamsi, string testName, long itemId)
    {
        var table = new Table { CellSpacing = 0 };
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Auto) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        table.RowGroups.Add(new TableRowGroup());
        var row = new TableRow();

        // ─── ستون QR — بار یکتای «درخواست + قلم» (F-02) ───
        var qrCell = new TableCell
        {
            Padding = new Thickness(0, 4, 0, 4),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xB0, 0xBE, 0xC5)),
            BorderThickness = new Thickness(1),
        };
        var qrPar = new Paragraph { Margin = new Thickness(0), TextAlignment = TextAlignment.Center };
        var qrSource = QrCodeHelper.Build(
            QrCodeHelper.SpecimenPayload(orderNo, itemId, testName), sizePx: 70);
        if (qrSource is not null)
        {
            var qr = new System.Windows.Controls.Image
            {
                Source = qrSource,
                Width = 70,
                Height = 70
            };
            qrPar.Inlines.Add(new InlineUIContainer(qr));
        }
        qrCell.Blocks.Add(qrPar);
        row.Cells.Add(qrCell);

        // ─── ستون شناسنامه — درمانگاه/درخواست/بیمار/آزمایش ───
        var infoCell = new TableCell
        {
            Padding = new Thickness(10, 4, 10, 4),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xB0, 0xBE, 0xC5)),
            BorderThickness = new Thickness(0, 1, 1, 1),
        };

        var clinic = LocalSettingsStore.Get("Printer.ClinicName");
        var line1 = new Paragraph { Margin = new Thickness(0) };
        line1.Inlines.Add(new Run(string.IsNullOrWhiteSpace(clinic) ? "درمانگاه سهند" : clinic)
        {
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0x54, 0x6E, 0x7A))
        });
        infoCell.Blocks.Add(line1);

        var line2 = new Paragraph { Margin = new Thickness(0, 2, 0, 0) };
        line2.Inlines.Add(new Run("درخواست: ")
        {
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0x54, 0x6E, 0x7A))
        });
        line2.Inlines.Add(new Run(Application.Common.Helpers.PersianDateHelper.ToPersianDigits(orderNo))
        {
            FontSize = 13,
            FontWeight = FontWeights.Bold
        });
        line2.Inlines.Add(new Run("   بیمار: ")
        {
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0x54, 0x6E, 0x7A))
        });
        line2.Inlines.Add(new Run(patientName) { FontSize = 12, FontWeight = FontWeights.SemiBold });
        infoCell.Blocks.Add(line2);

        var line3 = new Paragraph { Margin = new Thickness(0, 2, 0, 0) };
        line3.Inlines.Add(new Run("آزمایش: ") { FontSize = 10 });
        line3.Inlines.Add(new Run(testName)
        {
            FontSize = 13.5,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x69, 0x5C))
        });
        if (!string.IsNullOrWhiteSpace(dateShamsi))
        {
            line3.Inlines.Add(new Run($"   ({Application.Common.Helpers.PersianDateHelper.ToPersianDigits(dateShamsi)})")
            {
                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0x54, 0x6E, 0x7A))
            });
        }
        infoCell.Blocks.Add(line3);

        row.Cells.Add(infoCell);
        table.RowGroups[0].Rows.Add(row);

        // فاصلهٔ تنفس بعد از هر برچسب — مرز برش بین برچسب‌ها
        table.Margin = new Thickness(0, 0, 0, 10);
        return table;
    }

    /// <summary>فونت چاپی برنامه — وزیرمتن جاسازی‌شده (هم‌الگوی سایر چاپگرها)</summary>
    private static System.Windows.Media.FontFamily PrintFont() =>
        new("pack://application:,,,/Assets/Fonts/#Vazirmatn");
}

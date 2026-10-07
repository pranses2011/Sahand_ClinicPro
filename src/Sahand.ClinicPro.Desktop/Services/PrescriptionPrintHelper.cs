using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// سازندهٔ سند چاپ نسخهٔ دارویی — چرخه بیست‌ویکم (گزارش پانزدهم مالک:
/// «دکمه چاپ کار نمی‌کند»). سند FlowDocument راست‌چین فارسی با سربرگ
/// درمانگاه، مشخصات بیمار/پزشک، جدول اقلام و محل امضا می‌سازد و پنجرهٔ
/// پیش‌نمایش چاپ (DocumentViewer با دکمهٔ چاپ ویندوز) باز می‌کند.
/// </summary>
public static class PrescriptionPrintHelper
{
    /// <summary>رنگ سازمانی سربرگ — هماهنگ با تم آبی محصول</summary>
    private static readonly SolidColorBrush HeaderBrush = new(Color.FromRgb(0x15, 0x65, 0xC0));

    /// <summary>رنگ متن کم‌رنگ راهنما</summary>
    private static readonly SolidColorBrush MutedBrush = new(Color.FromRgb(0x60, 0x64, 0x68));

    /// <summary>
    /// ساخت سند نسخه — اقلام به‌صورت (نام، زیرعنوان، مصرف، مسیر، توضیح)
    /// </summary>
    public static FlowDocument BuildDocument(
        string patientName,
        string patientNationalCode,
        string doctorName,
        string dateShamsi,
        string? trackingCode,
        IReadOnlyList<(string Name, string Subtitle, string Usage, string Route, string Instructions)> items)
    {
        var doc = new FlowDocument
        {
            // راست‌چین و قلم فارسی — مستقل از فونت تنظیمات تا چاپ همیشه خوانا باشد
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = new FontFamily("Tahoma"),
            FontSize = 12,
            PagePadding = new Thickness(40),
            ColumnWidth = double.PositiveInfinity
        };

        // ═══ سربرگ ═══
        var header = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        };
        header.Inlines.Add(new Run("نسخهٔ دارویی")
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = HeaderBrush
        });
        doc.Blocks.Add(header);

        var sub = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
        sub.Inlines.Add(new Run($"تاریخ: {dateShamsi}")
        {
            FontSize = 11,
            Foreground = MutedBrush
        });
        if (!string.IsNullOrWhiteSpace(trackingCode))
            sub.Inlines.Add(new Run($"    |    کد رهگیری الکترونیک: {trackingCode}")
            {
                FontSize = 11,
                Foreground = MutedBrush
            });
        doc.Blocks.Add(sub);

        // ═══ مشخصات بیمار و پزشک ═══
        var info = new Paragraph { Margin = new Thickness(0, 0, 0, 10) };
        info.Inlines.Add(new Run("بیمار: ") { FontWeight = FontWeights.Bold });
        info.Inlines.Add(new Run($"{patientName}   "));
        info.Inlines.Add(new Run("کد ملی: ") { FontWeight = FontWeights.Bold });
        info.Inlines.Add(new Run($"{patientNationalCode}   "));
        info.Inlines.Add(new Run("پزشک: ") { FontWeight = FontWeights.Bold });
        info.Inlines.Add(new Run(doctorName));
        doc.Blocks.Add(info);

        // ═══ جدول اقلام ═══
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5),
            Margin = new Thickness(0, 0, 0, 14)
        };
        table.Columns.Add(new TableColumn { Width = new GridLength(34) });    // ردیف
        table.Columns.Add(new TableColumn { Width = new GridLength(2.4, GridUnitType.Star) }); // دارو
        table.Columns.Add(new TableColumn { Width = new GridLength(2.2, GridUnitType.Star) }); // مصرف
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });   // مسیر
        table.RowGroups.Add(new TableRowGroup());

        // سرستون‌ها
        var head = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD)) };
        foreach (var title in new[] { "#", "دارو", "مصرف", "مسیر" })
        {
            head.Cells.Add(new TableCell(new Paragraph(new Run(title)
            {
                FontWeight = FontWeights.Bold
            })));
        }
        table.RowGroups[0].Rows.Add(head);

        // ردیف اقلام
        for (var i = 0; i < items.Count; i++)
        {
            var it = items[i];
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph(new Run((i + 1).ToString()))));
            // نام دارو + زیرعنوان (فرم/دوز/سازنده) در یک پاراگراف دومین‌خط
            var medPar = new Paragraph(new Run(it.Name)) { Margin = new Thickness(0) };
            if (!string.IsNullOrWhiteSpace(it.Subtitle))
            {
                medPar.Inlines.Add(new Run($"\n{it.Subtitle}")
                {
                    FontSize = 10,
                    Foreground = MutedBrush
                });
            }
            row.Cells.Add(new TableCell(medPar));
            row.Cells.Add(new TableCell(new Paragraph(new Run(it.Usage))));
            row.Cells.Add(new TableCell(new Paragraph(new Run(it.Route))));
            // توضیح اختیاری — پررنگ زیر مصرف
            if (!string.IsNullOrWhiteSpace(it.Instructions))
                row.Cells[2].Blocks.Add(new Paragraph(new Run($"({it.Instructions})")
                {
                    FontSize = 10,
                    Foreground = MutedBrush
                }) { Margin = new Thickness(0) });
            table.RowGroups[0].Rows.Add(row);
        }
        doc.Blocks.Add(table);

        // ═══ پانویس و امضا ═══
        var footer = new Paragraph { Margin = new Thickness(0, 18, 0, 0) };
        footer.Inlines.Add(new Run("مهر و امضای پزشک: ")
        {
            Foreground = MutedBrush,
            FontSize = 11
        });
        footer.Inlines.Add(new Run("......................................") { Foreground = MutedBrush });
        doc.Blocks.Add(footer);

        var note = new Paragraph { Margin = new Thickness(0, 8, 0, 0) };
        note.Inlines.Add(new Run("این نسخه توسط نرم‌افزار مدیریت درمانگاه سهند صادر شده است.")
        {
            FontSize = 9.5,
            Foreground = MutedBrush,
            Italic = true
        });
        doc.Blocks.Add(note);

        return doc;
    }

    /// <summary>
    /// باز کردن پنجرهٔ پیش‌نمایش چاپ — DocumentViewer دکمهٔ چاپ استاندارد ویندوز
    /// و تغییر اندازهٔ صفحه (A4/A5) را در اختیار کاربر می‌گذارد.
    /// </summary>
    public static void ShowPreview(FlowDocument document, string title)
    {
        document.PageWidth = 793.7;  // A4 - 210mm با ۹۶dpi
        document.PageHeight = 1122.5; // A4 - 297mm

        var viewer = new FlowDocumentPageViewer
        {
            Document = document,
            // نوار ابزار پیش‌نمایش (زوم/جستجو/چاپ) خودکار ظاهر می‌شود
            MinZoom = 50,
            MaxZoom = 300
        };

        var window = new Window
        {
            Title = title,
            Content = viewer,
            Width = 860,
            Height = 640,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive),
            FlowDirection = FlowDirection.RightToLeft
        };
        window.ShowDialog();
    }
}

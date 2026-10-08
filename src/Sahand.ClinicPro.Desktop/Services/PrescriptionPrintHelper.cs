using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// سازندهٔ سند چاپ نسخهٔ دارویی — چرخه بیست‌ویکم (گزارش پانزدهم مالک:
/// «دکمه چاپ کار نمی‌کند»). سند FlowDocument راست‌چین فارسی با سربرگ
/// درمانگاه، مشخصات بیمار/پزشک، جدول اقلام و محل امضا می‌سازد.
/// ═══ چرخه بیست‌وسوم (گزارش شانزدهم مالک) ═══
/// «صفحه چاپ نسخه دکمه‌ای برای ارسال به پرینتر ندارد و قالب‌بندی بهم‌ریخته است»:
///   • FlowDocumentPageViewer پیش‌تر هیچ دکمهٔ چاپی نداشت (فقط زوم) → پنجرهٔ
///     اختصاصی با نوار ابزار فارسی شامل دکمهٔ بزرگ «چاپ» ساخته شد که
///     PrintDialog ویندوز را باز می‌کند و سند را مستقیم به پرینتر می‌فرستد.
///   • ریشهٔ بهم‌ریختگی: «\n» داخل Run در FlowDocument خط جدید نمی‌سازد
///     (نادیده گرفته می‌شود) → عنصر LineBreak استاندارد به‌کار رفت؛ سلول‌های
///     جدول خط‌کشی سراسری گرفتند؛ شماره ردیف‌ها و تاریخ با ارقام فارسی
///     (PersianDateHelper.n) شد؛ ستون «توضیح» جدا شد تا متن‌های طولانی
///     چندسطری و خوانا بمانند.
/// </summary>
public static class PrescriptionPrintHelper
{
    /// <summary>رنگ سازمانی سربرگ — هماهنگ با تم آبی محصول</summary>
    private static readonly SolidColorBrush HeaderBrush = new(Color.FromRgb(0x15, 0x65, 0xC0));

    /// <summary>رنگ متن کم‌رنگ راهنما</summary>
    private static readonly SolidColorBrush MutedBrush = new(Color.FromRgb(0x60, 0x64, 0x68));

    /// <summary>قلم فارسی پایدار برای چاپ — روی همهٔ ویندوزها موجود است</summary>
    private const string FontName = "Tahoma";

    /// <summary>
    /// ساخت سند نسخه — اقلام به‌صورت (نام، زیرعنوان، مصرف، مسیر، توضیح)
    /// </summary>
    public static FlowDocument BuildDocument(
        string patientName,
        string patientNationalCode,
        string doctorName,
        string dateShamsi,
        string? trackingCode,
        IReadOnlyList<(string Name, string? Subtitle, string Usage, string? Route, string? Instructions)> items)
    {
        var doc = new FlowDocument
        {
            // راست‌چین و قلم فارسی — مستقل از فونت تنظیمات تا چاپ همیشه خوانا باشد
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = new FontFamily(FontName),
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
        // تاریخ و کد رهگیری با ارقام فارسی (چرخه ۲۳ — همهٔ اعداد برنامه فارسی)
        sub.Inlines.Add(new Run($"تاریخ: {PersianDateHelper.ToPersianDigits(dateShamsi)}")
        {
            FontSize = 11,
            Foreground = MutedBrush
        });
        if (!string.IsNullOrWhiteSpace(trackingCode))
            sub.Inlines.Add(new Run($"    |    کد رهگیری الکترونیک: {PersianDateHelper.ToPersianDigits(trackingCode)}")
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
        info.Inlines.Add(new Run($"{PersianDateHelper.ToPersianDigits(patientNationalCode)}   "));
        info.Inlines.Add(new Run("پزشک: ") { FontWeight = FontWeights.Bold });
        info.Inlines.Add(new Run(doctorName));
        doc.Blocks.Add(info);

        // ═══ جدول اقلام — با خط‌کشی کامل سلول‌ها (چرخه ۲۳: خوانایی) ═══
        var border = new Thickness(0.5);
        var borderBrush = Brushes.Gray;
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = borderBrush,
            BorderThickness = border,
            Margin = new Thickness(0, 0, 0, 14)
        };
        table.Columns.Add(new TableColumn { Width = new GridLength(34) });            // ردیف
        table.Columns.Add(new TableColumn { Width = new GridLength(2.4, GridUnitType.Star) }); // دارو
        table.Columns.Add(new TableColumn { Width = new GridLength(2.2, GridUnitType.Star) }); // مصرف
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });   // مسیر
        table.Columns.Add(new TableColumn { Width = new GridLength(1.6, GridUnitType.Star) }); // توضیح
        table.RowGroups.Add(new TableRowGroup());

        // سرستون‌ها — با خط‌کشی سلول (چرخه ۲۳)
        var head = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD)) };
        foreach (var title in new[] { "#", "دارو", "مصرف", "مسیر", "توضیح" })
        {
            var c = new TableCell(new Paragraph(new Run(title)
            {
                FontWeight = FontWeights.Bold
            }))
            {
                BorderBrush = borderBrush,
                BorderThickness = border,
                Padding = new Thickness(4, 3, 4, 3)
            };
            head.Cells.Add(c);
        }
        table.RowGroups[0].Rows.Add(head);

        // ردیف اقلام
        for (var i = 0; i < items.Count; i++)
        {
            var it = items[i];
            var row = new TableRow();

            // شمارهٔ ردیف — ارقام فارسی (چرخه ۲۳)
            row.Cells.Add(MakeCell(new Paragraph(new Run(PersianDateHelper.ToPersianDigits((i + 1).ToString())))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0)
            }, border, borderBrush));

            // نام دارو + زیرعنوان (فرم/دوز/سازنده) — چرخه ۲۳: LineBreak واقعی
            // به‌جای «\n» که در FlowDocument نادیده گرفته می‌شد و باعث چسبیدن
            // زیرعنوان به نام دارو (بهم‌ریختگی) می‌شد
            var medPar = new Paragraph(new Run(it.Name)) { Margin = new Thickness(0) };
            if (!string.IsNullOrWhiteSpace(it.Subtitle))
            {
                medPar.Inlines.Add(new LineBreak());
                medPar.Inlines.Add(new Run(it.Subtitle)
                {
                    FontSize = 10,
                    Foreground = MutedBrush
                });
            }
            row.Cells.Add(MakeCell(medPar, border, borderBrush));

            // مصرف (دوز + بازه) — چندسطری با LineBreak
            var usagePar = new Paragraph(new Run(it.Usage)) { Margin = new Thickness(0) };
            if (!string.IsNullOrWhiteSpace(it.Instructions))
            {
                usagePar.Inlines.Add(new LineBreak());
                usagePar.Inlines.Add(new Run($"({it.Instructions})")
                {
                    FontSize = 10,
                    Foreground = MutedBrush
                });
            }
            row.Cells.Add(MakeCell(usagePar, border, borderBrush));

            row.Cells.Add(MakeCell(new Paragraph(new Run(it.Route)) { Margin = new Thickness(0) }, border, borderBrush));

            // ستون توضیح خالی برای یادداشت دستی داروخانه/بیمار
            row.Cells.Add(MakeCell(new Paragraph(new Run(string.Empty)) { Margin = new Thickness(0) }, border, borderBrush));

            // یک‌درمیان رنگ پس‌زمینهٔ ردیف‌ها برای خوانایی (چرخه ۲۳)
            if (i % 2 == 1)
                row.Background = new SolidColorBrush(Color.FromRgb(0xF7, 0xFA, 0xFD));

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
            FontStyle = FontStyles.Italic
        });
        doc.Blocks.Add(note);

        return doc;
    }

    /// <summary>
    /// ساخت سلول جدول با خط‌کشی و فاصلهٔ داخلی یکسان — چرخه ۲۳:
    /// پیش‌تر سلول‌ها Border نداشتند و جدول روی کاغذ ناخوانا/بهم‌ریخته دیده می‌شد.
    /// </summary>
    private static TableCell MakeCell(Paragraph content, Thickness border, Brush borderBrush) =>
        new(content)
        {
            BorderBrush = borderBrush,
            BorderThickness = border,
            Padding = new Thickness(4, 3, 4, 3)
        };

    /// <summary>
    /// باز کردن پنجرهٔ پیش‌نمایش چاپ — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
    /// «صفحه چاپ دکمه‌ای برای ارسال به پرینتر ندارد» — FlowDocumentPageViewer
    /// هیچ دکمهٔ چاپی ندارد؛ پنجرهٔ اختصاصی با نوار ابزار فارسی ساخته شد:
    ///   • دکمهٔ بزرگ «چاپ» → PrintDialog ویندوز (انتخاب پرینتر + تنظیمات) و
    ///     ارسال مستقیم سند به پرینتر
    ///   • زوم +/− و نمایش درصد زوم + دکمهٔ بستن — همهٔ برچسب‌ها فارسی
    /// </summary>
    public static void ShowPreview(FlowDocument document, string title)
    {
        document.PageWidth = 793.7;   // A4 - 210mm با ۹۶dpi
        document.PageHeight = 1122.5; // A4 - 297mm

        // ─── نمایشگر سند (بدون نوار ابزار پیش‌فرض — نوار اختصاصی فارسی داریم) ───
        var viewer = new FlowDocumentPageViewer
        {
            Document = document,
            MinZoom = 50,
            MaxZoom = 300
        };

        // ─── دکمهٔ چاپ — ارسال مستقیم به پرینتر از طریق PrintDialog ───
        var printBtn = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new TextBlock { Text = "🖨", FontSize = 16, VerticalAlignment = VerticalAlignment.Center },
                    new TextBlock
                    {
                        Text = "چاپ", FontWeight = FontWeights.Bold, FontSize = 13,
                        Margin = new Thickness(7, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center
                    }
                }
            },
            Background = new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0)),
            Foreground = Brushes.White,
            Padding = new Thickness(18, 8, 18, 8),
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontFamily = new FontFamily(FontName)
        };
        // رویداد کلیک چاپ —.paginator استاندارد سند به پرینتر فرستاده می‌شود؛
        // اگر کاربر پرینتر را لغو کند یا پرینتری نصب نباشد، بی‌صدا بازمی‌گردد
        printBtn.Click += (_, _) =>
        {
            try
            {
                var dlg = new System.Windows.Controls.PrintDialog();
                if (dlg.ShowDialog() == true)
                {
                    // سند را مستقیم به پرینتر انتخابی ارسال می‌کنیم —
                    // IDocumentPaginatorSource صفحه‌بندی استاندارد WPF را می‌سازد
                    if (document is IDocumentPaginatorSource src)
                        dlg.PrintDocument(src.DocumentPaginator, title);
                }
            }
            catch (Exception ex)
            {
                // خطای درایور پرینتر نباید برنامه را بندازد — دیالوگ فارسی برنامه
                Views.AppDialog.Error("خطای چاپ", $"ارسال به پرینتر ناموفق بود:{Environment.NewLine}{ex.Message}");
            }
        };

        // ─── زوم +/− و درصد زوم ───
        var zoomText = new TextBlock
        {
            Text = $"{PersianDateHelper.ToPersianDigits(((int)viewer.Zoom).ToString())}٪",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 8, 0),
            FontFamily = new FontFamily(FontName),
            MinWidth = 44,
            TextAlignment = TextAlignment.Center
        };
        var zoomIn = new Button { Content = "+", Width = 34, Height = 30, FontSize = 15, FontFamily = new FontFamily(FontName) };
        var zoomOut = new Button { Content = "−", Width = 34, Height = 30, FontSize = 15, FontFamily = new FontFamily(FontName) };
        // زوم با به‌روزرسانی درصد فارسی — FlowDocumentPageViewer رویداد ZoomChanged
        // ندارد؛ متن درصد در همان کلیک‌ها تازه می‌شود
        zoomIn.Click += (_, _) =>
        {
            viewer.Zoom = Math.Min(viewer.MaxZoom, viewer.Zoom + 10);
            zoomText.Text = $"{PersianDateHelper.ToPersianDigits(((int)viewer.Zoom).ToString())}٪";
        };
        zoomOut.Click += (_, _) =>
        {
            viewer.Zoom = Math.Max(viewer.MinZoom, viewer.Zoom - 10);
            zoomText.Text = $"{PersianDateHelper.ToPersianDigits(((int)viewer.Zoom).ToString())}٪";
        };

        // ─── دکمهٔ بستن ───
        var closeBtn = new Button
        {
            Content = "بستن",
            Padding = new Thickness(14, 6, 14, 6),
            FontFamily = new FontFamily(FontName)
        };

        // ─── نوار ابزار فارسی بالای پنجره ───
        var toolbar = new DockPanel { Background = new SolidColorBrush(Color.FromRgb(0xF2, 0xF6, 0xFB)), LastChildFill = true };
        var toolbarBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD5, 0xDF, 0xEB)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(10, 6, 10, 6),
            Child = toolbar
        };

        // راست: دکمهٔ چاپ (چون RTL است اولین فرزند سمت راست می‌افتد)
        var rightTools = new StackPanel { Orientation = Orientation.Horizontal };
        rightTools.Children.Add(printBtn);
        DockPanel.SetDock(rightTools, Dock.Right);
        toolbar.Children.Add(rightTools);

        // چپ: زوم و بستن
        var leftTools = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        leftTools.Children.Add(zoomIn);
        leftTools.Children.Add(zoomText);
        leftTools.Children.Add(zoomOut);
        leftTools.Children.Add(new Border { Width = 10 });
        leftTools.Children.Add(closeBtn);
        DockPanel.SetDock(leftTools, Dock.Left);
        toolbar.Children.Add(leftTools);

        // عنوان وسط
        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontFamily = new FontFamily(FontName)
        };
        toolbar.Children.Add(titleText);

        // ─── چیدمان نهایی پنجره ───
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(toolbarBorder, 0);
        Grid.SetRow(viewer, 1);
        grid.Children.Add(toolbarBorder);
        grid.Children.Add(viewer);

        var window = new Window
        {
            Title = title,
            Content = grid,
            Width = 880,
            Height = 680,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive),
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = new FontFamily(FontName)
        };
        closeBtn.Click += (_, _) => window.Close();
        window.ShowDialog();
    }
}

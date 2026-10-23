using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Sahand.ClinicPro.Desktop.Controls.Charts;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت گزارش‌ساز — گام U16:
/// ساخت ستون‌های داینامیک جدول نتایج بر اساس کاتالوگ هر گزارش
/// </summary>
public partial class ReportBrowserView : UserControl
{
    /// <summary>ViewModel نگاشت‌شده</summary>
    private ReportBrowserViewModel? _vm;

    /// <summary>سازنده</summary>
    public ReportBrowserView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BindViewModel();
        Loaded += (_, _) => BindViewModel();
    }

    /// <summary>اتصال VM و ساخت ستون‌ها هنگام تغییر داده‌ها</summary>
    private void BindViewModel()
    {
        if (_vm == DataContext || DataContext is not ReportBrowserViewModel vm) return;
        _vm = vm;

        if (vm.Columns is INotifyCollectionChanged cols)
            cols.CollectionChanged += (_, _) => RebuildColumns(vm);

        if (vm.Rows is INotifyCollectionChanged rows)
            rows.CollectionChanged += (_, _) => RebuildColumns(vm);

        // چرخهٔ ۴۸ — خروجی تصویری: رندر عناصر دیداری در ویو انجام می‌شود
        vm.ExportImageRequested += (_, _) => ExportImage(vm);

        RebuildColumns(vm);
    }

    /// <summary>ساخت مجدد ستون‌های جدول بر اساس فهرست ستون‌های VM</summary>
    private void RebuildColumns(ReportBrowserViewModel vm)
    {
        ResultsGrid.Columns.Clear();
        if (vm.Columns.Count == 0) return;
        for (var i = 0; i < vm.Columns.Count; i++)
        {
            var index = i;

            // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — رفع کرش گزارش‌ها ═══
            // «ArgumentException: '' is not a valid value for property 'FontFamily'»
            // در همین متد رخ می‌داد. دو ریشه داشت:
            //   ۱) مقداردهی شرطی «i == 0 ? null : …» روی DependencyProperty
            //      فونت — گذاشتن null صریح در مقداردهندهٔ شیء، زنجیرهٔ اعتبارسنجی
            //      WPF را با مقدار رشته‌ای خالی («») روبرو می‌کرد و کرش می‌کرد؛
            //      فونت حالت فقط در صورت نیاز و پس از ساخت ستون تنظیم می‌شود.
            //   ۲) فونت هاردکد «Consolas» تضاد با خط فارسی/ارقام فارسی و تم
            //      برنامه داشت؛ ستون‌ها اکنون فونت را از منبع پویای تم
            //      (App.FontFamily — همان وزیرمتن یا انتخاب کاربر) به ارث
            //      می‌برند تا تغییر فونت تنظیمات روی گزارش‌ها هم بنشیند.
            var column = new DataGridTextColumn
            {
                Header = vm.Columns[i],
                Width = DataGridLength.Auto,
                MaxWidth = 420,
                ElementStyle = (Style)FindResource("App.TableCellTextBlock"),
                // سلول‌ها با نمای‌ساز سفارشی از ردیف استخراج می‌شوند
                Binding = new Binding
                {
                    Mode = BindingMode.OneWay,
                    Converter = new Converters.CellValueConverter(),
                    ConverterParameter = index.ToString()
                }
            };

            ResultsGrid.Columns.Add(column);
        }
    }

    /* ═══════════════════════════════════════════════════════════
       چرخهٔ ۴۸ — خروجی تصویری نتایج (گزارش مالک: «برای گزارشات خروجی
       تصویر هم بزار با فرمت‌های مختلف قابل انتخاب توسط کاربر»)
       ═══════════════════════════════════════════════════════════ */

    /// <summary>عرض بوم خروجی تصویر — پیکسل منطقی؛ نمودار و جدول در همین عرض می‌نشینند</summary>
    private const double ExportCanvasWidth = 1040;

    /// <summary>فرمت‌های قابل انتخاب کاربر — ترتیب با نمایهٔ فیلتر پنجرهٔ ذخیره یکی است</summary>
    private static readonly (string Label, string Extension)[] ImageFormats =
    {
        ("تصویر PNG (بدون افت کیفیت)", "png"),
        ("تصویر JPEG (حجم کم)", "jpg"),
        ("تصویر JPEG (کیفیت بالا)", "jpeg"),
        ("تصویر BMP (بدون فشرده‌سازی)", "bmp"),
        ("تصویر GIF", "gif"),
        ("تصویر TIFF (چاپ حرفه‌ای)", "tiff"),
    };

    /// <summary>
    /// خروجی تصویری نتایج — ابتدا فرمت از کاربر پرسیده می‌شود (فهرست
    /// «نوع فایل» پنجرهٔ ذخیره)، سپس یک بوم تمیز سفید با سربرگ (عنوان گزارش +
    /// بازه + تاریخ شمسی)، کارت‌های خلاصه، نمودار (در صورت نموداری بودن گزارش)
    /// و جدول کامل ردیف‌ها ساخته و رندر می‌شود. جدول با TextBlock‌های واقعی
    /// ساخته می‌شود (نه DataGrid) تا مجازی‌سازی، ردیف‌های خارج از دید را حذف نکند.
    /// </summary>
    private void ExportImage(ReportBrowserViewModel vm)
    {
        // ۱) انتخاب مسیر + فرمت — کاربر از فهرست «نوع فایل» فرمت را برمی‌گزیند
        var filter = string.Join("|", ImageFormats.Select((f, i) => $"{f.Label}|*.{f.Extension}"));
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = filter,
            FilterIndex = 1, // پیش‌فرض PNG — بدون افت کیفیت
            FileName = $"{vm.SelectedReport?.Id ?? "report"}_{DateTime.Now:yyyyMMdd_HHmm}.{ImageFormats[0].Extension}"
        };
        if (dialog.ShowDialog() != true) return; // انصراف کاربر

        var format = ImageFormats[Math.Clamp(dialog.FilterIndex - 1, 0, ImageFormats.Length - 1)];

        // ۲) ساخت بوم خروجی — خارج از درخت دیداری؛ اندازه‌گذاری دستی در پایان
        var canvas = BuildExportVisual(vm);

        // ۳) اندازه‌گذاری دستی (بوم در پنجره نیست — Measure/Arrange صریح لازم است)
        var size = new Size(ExportCanvasWidth, double.PositiveInfinity);
        canvas.Measure(size);
        canvas.Arrange(new Rect(new Size(ExportCanvasWidth, canvas.DesiredSize.Height)));
        canvas.UpdateLayout();

        // ۴) رندر با مقیاس ۲ برابری (DPI ۱۹۲) تا متن فارسی و نمودار در زوم تیز بمانند
        var width = (int)Math.Ceiling(ExportCanvasWidth * 2);
        var height = (int)Math.Ceiling(canvas.DesiredSize.Height * 2);
        var bitmap = new RenderTargetBitmap(width, height, 192, 192, PixelFormats.Pbgra32);
        bitmap.Render(canvas);

        // ۵) کدگذاری با فرمت منتخب — JPEG پس‌زمینهٔ شفاف را سیاه می‌کند؛ بوم از
        //    ابتدا سفید است پس همهٔ فرمت‌ها سالم از آب درمی‌آیند
        BitmapEncoder encoder = format.Extension switch
        {
            "jpg" or "jpeg" => new JpegBitmapEncoder { QualityLevel = format.Extension == "jpeg" ? 95 : 85 },
            "bmp" => new BmpBitmapEncoder(),
            "gif" => new GifBitmapEncoder(),
            "tiff" => new TiffBitmapEncoder { Compression = TiffCompressOption.Lzw },
            _ => new PngBitmapEncoder(),
        };
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using (var stream = System.IO.File.Create(dialog.FileName))
            encoder.Save(stream);

        vm.NotifyImageExported(System.IO.Path.GetFileName(dialog.FileName));
    }

    /// <summary>
    /// ساخت بوم خروجی — سربرگ + خلاصه‌ها + نمودار + جدول؛ هم‌چیدمان بصری گزارش
    /// چاپی برنامه (سربرگ درشت، خط بازهٔ شمسی، خط خلاصه‌ها) تا خروجی تصویری نیز
    /// سند رسمی باشد نه عکس خام از صفحه.
    /// </summary>
    private StackPanel BuildExportVisual(ReportBrowserViewModel vm)
    {
        var app = System.Windows.Application.Current;
        var faFont = app?.TryFindResource("App.FontFamily") as FontFamily ?? new FontFamily("Tahoma");
        var codeFont = app?.TryFindResource("App.CodeFont") as FontFamily ?? faFont;

        var panel = new StackPanel
        {
            Width = ExportCanvasWidth,
            Background = Brushes.White, // JPEG شفافیت ندارد — پس‌زمینهٔ صریح سفید
            FlowDirection = FlowDirection.RightToLeft,
        };

        // ── سربرگ: عنوان گزارش ──
        panel.Children.Add(new TextBlock
        {
            Text = vm.ReportTitle,
            FontFamily = faFont,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x29, 0x37)),
            Margin = new Thickness(36, 28, 36, 4),
        });

        // ── خط بازه و تاریخ ──
        var rangeText = vm.HasDateRange
            ? $"بازه: {PersianDateHelperToShamsiFa(vm.FromDate)} تا {PersianDateHelperToShamsiFa(vm.ToDate)}"
            : "بازه: کل داده‌ها";
        panel.Children.Add(new TextBlock
        {
            Text = $"{rangeText}    |    تاریخ خروجی: {PersianDateHelperToShamsiFa(DateTime.Now)} - {Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToClockFa(DateTime.Now)}",
            FontFamily = faFont,
            FontSize = 11.5,
            Foreground = Brushes.Gray,
            Margin = new Thickness(36, 0, 36, 12),
        });

        // ── خط کارت‌های خلاصه — «برچسب: مقدار» با جداکنندهٔ میانی ──
        if (vm.SummaryCards.Count > 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = string.Join("    |    ", vm.SummaryCards.Select(c => $"{c.Label}: {c.Value}")),
                FontFamily = faFont,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x37, 0x47, 0x4F)),
                Margin = new Thickness(36, 0, 36, 14),
                TextWrapping = TextWrapping.Wrap,
            });
        }

        // ── نمودار (فقط گزارش‌های نموداری) — همان کنترل زندهٔ صفحه با دادهٔ فعلی ──
        if (vm.HasChart && vm.ChartPoints is { Count: > 0 })
        {
            FrameworkElement chart = vm.ChartKind switch
            {
                "doughnut" => new DoughnutChart
                {
                    Width = 520,
                    Height = 300,
                    CenterText = vm.ChartCenterText,
                    CenterSubtext = vm.ChartUnit,
                    Points = vm.ChartPoints,
                },
                "hbar" => new HorizontalBarChart
                {
                    Width = ExportCanvasWidth - 72,
                    Height = Math.Min(430, 46.0 * vm.ChartPoints.Count + 40),
                    Points = vm.ChartPoints,
                },
                "column" => new ColumnChart
                {
                    Width = ExportCanvasWidth - 72,
                    Height = 300,
                    Points = vm.ChartPoints,
                },
                _ => new AreaChart
                {
                    Width = ExportCanvasWidth - 72,
                    Height = 300,
                    Points = vm.ChartPoints,
                },
            };
            var chartHost = new Border
            {
                Child = chart,
                Margin = new Thickness(36, 0, 36, 14),
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = Brushes.White,
            };
            panel.Children.Add(chartHost);
        }

        // ── جدول کامل — WPF Grid واقعی بدون مجازی‌سازی؛ همهٔ ردیف‌ها رندر می‌شوند ──
        var colCount = Math.Max(1, vm.Columns.Count);
        var usableWidth = ExportCanvasWidth - 72;
        var colWidth = Math.Floor(usableWidth / colCount);

        var table = new Grid { Width = usableWidth };
        for (var i = 0; i < colCount; i++)
            table.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(colWidth, GridUnitType.Pixel),
            });

        // سلول ساز مشترک — قاب + پس‌زمینه + متن؛ سرستون درشت‌تر از داده‌ها
        UIElement MakeCell(string text, bool isHeader, Brush? background)
        {
            return new Border
            {
                Background = background,
                BorderBrush = new SolidColorBrush(
                    isHeader ? Color.FromRgb(0xB0, 0xBE, 0xC5) : Color.FromRgb(0xE0, 0xE0, 0xE0)),
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(7, isHeader ? 6 : 5, 7, isHeader ? 6 : 5),
                Child = new TextBlock
                {
                    Text = text,
                    FontFamily = faFont,
                    FontSize = isHeader ? 12 : 11.5,
                    FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x32, 0x38)),
                    TextWrapping = TextWrapping.Wrap,
                },
            };
        }

        // سرستون‌ها — ردیف صفر با پس‌زمینهٔ آبی روشن
        var headerBg = new SolidColorBrush(Color.FromRgb(0xE8, 0xEF, 0xF6));
        headerBg.Freeze();
        table.RowDefinitions.Add(new RowDefinition());
        for (var c = 0; c < colCount; c++)
        {
            var cell = MakeCell(c < vm.Columns.Count ? vm.Columns[c] : "", isHeader: true, headerBg);
            Grid.SetRow(cell, 0);
            Grid.SetColumn(cell, c);
            table.Children.Add(cell);
        }

        // ردیف‌های داده — یک‌درمیان سایهٔ بسیار ملایم برای خوانایی
        var zebra = new SolidColorBrush(Color.FromArgb(0x0D, 0x00, 0x00, 0x00));
        zebra.Freeze();
        for (var r = 0; r < vm.Rows.Count; r++)
        {
            table.RowDefinitions.Add(new RowDefinition());
            var row = vm.Rows[r];
            for (var c = 0; c < colCount; c++)
            {
                var text = c < row.Cells.Count ? row.Cells[c].Text : "";
                var cell = MakeCell(text, isHeader: false, r % 2 == 1 ? zebra : Brushes.White);
                Grid.SetRow(cell, r + 1);
                Grid.SetColumn(cell, c);
                table.Children.Add(cell);
            }
        }

        // جدول مستقیم روی بوم می‌نشیند — رندر خارج از پنجره با Measure/Arrange دستی
        panel.Children.Add(new Border
        {
            Child = table,
            Margin = new Thickness(36, 0, 36, 28),
        });

        return panel;
    }

    /// <summary>تاریخ شمسی فارسی‌رقم — میان‌بر خوانا برای سربرگ خروجی تصویری</summary>
    private static string PersianDateHelperToShamsiFa(DateTime date)
        => Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsiFa(date);
}

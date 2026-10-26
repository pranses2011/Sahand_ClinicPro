using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت صفحهٔ مستقل «تصویربرداری» — ماژول حرفه‌ای (طرح تأییدشدهٔ مالک، نسخهٔ ۲ پیش‌نمایش):
/// رندر نرم‌افزاری پنجره/سطح (Window/Level) روی تصاویر معمولی مطالعه، اندازه‌گیری
/// دوبار کلیک روی تصویر، پخش برش‌ها (Cine) و چاپ گزارش ساخت‌یافته. فایل‌های DICOM
/// در نمایشگر اختصاصی DICOM باز می‌شوند (فرمان OpenDicomViewer در ViewModel).
/// </summary>
public partial class ImagingView : UserControl
{
    private ImagingViewModel? _vm;
    private BitmapSource? _source;
    private byte[] _sourcePixels = Array.Empty<byte>();
    private int _srcW;
    private int _srcH;
    private WriteableBitmap? _rendered;
    private (Point Element, Point Image)? _measureFirst;
    private readonly DispatcherTimer _cine;

    /// <summary>سازنده — مقداردهی اولیهٔ اجزا و تایمر پخش برش‌ها</summary>
    public ImagingView()
    {
        InitializeComponent();
        _cine = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(260) };
        _cine.Tick += CineTick;
        DataContextChanged += OnDataContextChanged;
        Unloaded += (_, _) => _cine.Stop();
    }

    /// <summary>اتصال به رویدادهای ViewModel هنگام تغییر بافت داده</summary>
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
        {
            _vm.ViewerStateChanged -= OnViewerStateChanged;
            _vm.ImageSourceChanged -= OnImageSourceChanged;
        }
        _vm = e.NewValue as ImagingViewModel;
        if (_vm is not null)
        {
            _vm.ViewerStateChanged += OnViewerStateChanged;
            _vm.ImageSourceChanged += OnImageSourceChanged;
            LoadImage();
        }
    }

    private void OnViewerStateChanged() => Dispatcher.Invoke(RenderViewer);
    private void OnImageSourceChanged() => Dispatcher.Invoke(LoadImage);

    /// <summary>بارگذاری تصویر جاری مطالعه از بایت‌های دریافتی و آماده‌سازی پیکسل‌ها</summary>
    private void LoadImage()
    {
        _source = null;
        _rendered = null;
        _measureFirst = null;
        MeasureCanvas.Children.Clear();
        var bytes = _vm?.CurrentImageBytes;
        if (bytes is { Length: > 0 })
        {
            try
            {
                var bmp = new BitmapImage();
                using (var ms = new MemoryStream(bytes))
                {
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                }
                bmp.Freeze();
                BitmapSource src = bmp;
                if (src.PixelWidth > 1000)
                {
                    var scale = 1000.0 / src.PixelWidth;
                    src = new TransformedBitmap(src, new ScaleTransform(scale, scale));
                }
                var converted = new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
                _srcW = converted.PixelWidth;
                _srcH = converted.PixelHeight;
                _sourcePixels = new byte[_srcW * _srcH * 4];
                converted.CopyPixels(_sourcePixels, _srcW * 4, 0);
                _source = converted;
            }
            catch
            {
                _source = null;
            }
        }
        RenderViewer();
    }

    /// <summary>
    /// رندر تصویر با پنجره/سطح فعلی — همان منطق پیش‌نمایش تأییدشده:
    /// روشنایی از سطح (L) و کنتراست از پهنا (W) ساخته می‌شود و جابه‌جایی
    /// برش روشنایی جزئی می‌دهد؛ معکوس، زوم، چرخش و برگردان هم همین‌جا اعمال می‌شوند.
    /// </summary>
    private void RenderViewer()
    {
        if (_vm is null) return;
        if (_source is null || _sourcePixels.Length == 0)
        {
            ViewerImage.Source = null;
            NoImageText.Visibility = Visibility.Visible;
            return;
        }
        NoImageText.Visibility = Visibility.Collapsed;

        var w = Math.Max(10, _vm.WindowWidth);
        var contrast = 0.6 + w / 160.0;
        var bright = 0.55 + _vm.WindowLevel / 100.0;
        var sliceAdj = (_vm.SliceIndex - 14) * 0.004;
        var invert = _vm.IsInverted;

        if (_rendered is null || _rendered.PixelWidth != _srcW || _rendered.PixelHeight != _srcH)
            _rendered = new WriteableBitmap(_srcW, _srcH, 96, 96, PixelFormats.Bgra32, null);

        var output = new byte[_sourcePixels.Length];
        for (var i = 0; i < _sourcePixels.Length; i += 4)
        {
            var gray = (_sourcePixels[i] + _sourcePixels[i + 1] + _sourcePixels[i + 2]) / 3.0;
            var v = (gray / 255.0 - 0.5) * contrast + 0.5;
            v = v * bright + sliceAdj;
            if (invert) v = 1 - v;
            var b = (byte)Math.Clamp(v * 255, 0, 255);
            output[i] = b;
            output[i + 1] = b;
            output[i + 2] = b;
            output[i + 3] = 255;
        }
        _rendered.WritePixels(new Int32Rect(0, 0, _srcW, _srcH), output, _srcW * 4, 0);
        ViewerImage.Source = _rendered;

        var zoom = Math.Max(0.1, _vm.ZoomPercent / 100.0);
        var group = new TransformGroup();
        group.Children.Add(new ScaleTransform(_vm.IsFlipped ? -zoom : zoom, zoom));
        group.Children.Add(new RotateTransform(_vm.Rotation));
        ViewerImage.RenderTransform = group;
    }

    /// <summary>کلیک روی نمایشگر — در حالت اندازه‌گیری، دو نقطه ضایعه را مشخص می‌کند</summary>
    private void ViewerHost_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_vm is null || !_vm.MeasureMode || _source is null) return;
        var pos = e.GetPosition(ViewerImage);
        var aw = ViewerImage.ActualWidth;
        var ah = ViewerImage.ActualHeight;
        if (aw <= 0 || ah <= 0) return;

        var scale = Math.Min(aw / _srcW, ah / _srcH);
        var dw = _srcW * scale;
        var dh = _srcH * scale;
        var ox = (aw - dw) / 2;
        var oy = (ah - dh) / 2;
        if (pos.X < ox || pos.X > ox + dw || pos.Y < oy || pos.Y > oy + dh) return;

        var imgPoint = new Point((pos.X - ox) / scale, (pos.Y - oy) / scale);
        DrawMeasureDot(pos);
        if (_measureFirst is null)
        {
            _measureFirst = (pos, imgPoint);
            return;
        }

        var first = _measureFirst.Value;
        var dx = imgPoint.X - first.Image.X;
        var dy = imgPoint.Y - first.Image.Y;
        var mm = Math.Sqrt(dx * dx + dy * dy) * 0.3;
        var line = new Line
        {
            X1 = first.Element.X,
            Y1 = first.Element.Y,
            X2 = pos.X,
            Y2 = pos.Y,
            Stroke = new SolidColorBrush(Color.FromRgb(0x7C, 0xFC, 0x9A)),
            StrokeThickness = 1.6
        };
        MeasureCanvas.Children.Add(line);
        _measureFirst = null;
        _vm.AddMeasurement(mm);
    }

    /// <summary>نقطهٔ راهنمای اندازه‌گیری روی تصویر</summary>
    private void DrawMeasureDot(Point pos)
    {
        var dot = new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = new SolidColorBrush(Color.FromRgb(0x7C, 0xFC, 0x9A)),
            Stroke = Brushes.Black,
            StrokeThickness = 0.8
        };
        Canvas.SetLeft(dot, pos.X - 4);
        Canvas.SetTop(dot, pos.Y - 4);
        MeasureCanvas.Children.Add(dot);
    }

    /// <summary>شروع/توقف پخش برش‌ها</summary>
    private void CineButton_Click(object sender, RoutedEventArgs e)
    {
        if (_cine.IsEnabled)
        {
            _cine.Stop();
            CineButton.Content = "▶ پخش برش‌ها (Cine)";
        }
        else
        {
            _cine.Start();
            CineButton.Content = "⏸ توقف پخش";
        }
    }

    /// <summary>گام پخش — برش بعدی با چرخهٔ ۱ تا سقف برش‌ها</summary>
    private void CineTick(object? sender, EventArgs e)
    {
        if (_vm is null) return;
        _vm.SliceIndex = _vm.SliceIndex >= _vm.SliceTotal ? 1 : _vm.SliceIndex + 1;
    }

    /// <summary>چاپ گزارش ساخت‌یافتهٔ مطالعهٔ انتخابی با سربرگ درمانگاه</summary>
    private void PrintReport_Click(object sender, RoutedEventArgs e)
    {
        if (_vm?.SelectedImagingOrder is null) return;
        var o = _vm.SelectedImagingOrder;
        var doc = new FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = new FontFamily("Tahoma"),
            FontSize = 12
        };

        Paragraph Para(string text, double size = 12, bool bold = false)
        {
            return new Paragraph(new Run(text))
            {
                FontSize = size,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(0, 3, 0, 3)
            };
        }

        doc.Blocks.Add(Para("گزارش تصویربرداری — درمانگاه سهند (ClinicPro)", 16, true));
        doc.Blocks.Add(Para($"بیمار: {o.PatientName}   ·   مطالعه: {o.ModalityFa} {o.BodyPart}   ·   وضعیت: {o.StatusText}"));
        if (!string.IsNullOrWhiteSpace(_vm.ReportClinicalInfo))
            doc.Blocks.Add(Para("شرح حال: " + _vm.ReportClinicalInfo));
        if (!string.IsNullOrWhiteSpace(_vm.ReportTechnique))
            doc.Blocks.Add(Para("تکنیک: " + _vm.ReportTechnique));
        doc.Blocks.Add(Para("یافته‌ها:", 13, true));
        doc.Blocks.Add(Para(_vm.ReportFindings ?? "—"));
        doc.Blocks.Add(Para("نتیجه‌گیری:", 13, true));
        doc.Blocks.Add(Para(_vm.ReportImpression ?? "—"));
        if (!string.IsNullOrWhiteSpace(_vm.ReportRecommendation))
            doc.Blocks.Add(Para("توصیه: " + _vm.ReportRecommendation));
        if (!string.IsNullOrWhiteSpace(_vm.DoseCtdi) || !string.IsNullOrWhiteSpace(_vm.DoseDlp))
            doc.Blocks.Add(Para($"دوز تابش: CTDIvol = {_vm.DoseCtdi ?? "—"} mGy · DLP = {_vm.DoseDlp ?? "—"} mGy·cm"));
        if (_vm.CriticalFinding)
            doc.Blocks.Add(Para($"یافتهٔ بحرانی — اطلاع داده شد به: {_vm.CriticalNotifiedTo} ({_vm.CriticalMethod})", 12, true));
        doc.Blocks.Add(Para("امضای رادیولوژیست: ______________________", 12, true));

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == true)
            dialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "گزارش تصویربرداری");
    }
}

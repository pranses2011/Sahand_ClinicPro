using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Sahand.ClinicPro.Shared.Dicom;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// نمایشگر DICOM — گام R4 (چرخه چهارم)
/// تجزیه محلی فایل با DicomFileReader خالص + رندر WPF با پنجره/سطح
/// تعاملی و بزرگ‌نمایی — بدون هیچ کتابخانه خارجی
/// </summary>
public partial class DicomViewerWindow : Window
{
    /// <summary>تصویر تجزیه‌شده فعلی</summary>
    private DicomImage? _image;

    /// <summary>سازنده — می‌توان مسیر فایل را مستقیم داد</summary>
    public DicomViewerWindow(string? filePath = null)
    {
        InitializeComponent();

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            LoadFile(filePath);
    }

    /// <summary>گشودن فایل محلی با گفت‌وگوی انتخاب</summary>
    private void OnOpenLocalClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "انتخاب فایل DICOM",
            Filter = "فایل DICOM (*.dcm;*.dicom)|*.dcm;*.dicom|همه فایل‌ها (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;
        LoadFile(dialog.FileName);
    }

    /// <summary>بارگذاری و تجزیه فایل DICOM</summary>
    public void LoadFile(string path) => LoadImage(() => DicomFileReader.Load(path), Path.GetFileName(path));

    /// <summary>بارگذاری دادهٔ دریافتی در حافظه، بدون ایجاد فایل موقت حاوی اطلاعات بیمار.</summary>
    public void LoadBytes(byte[] bytes, string displayName) => LoadImage(() =>
    {
        using var stream = new MemoryStream(bytes, writable: false);
        return DicomFileReader.Load(stream);
    }, displayName);

    private void LoadImage(Func<DicomImage> readImage, string displayName)
    {
        try
        {
            _image = readImage();
            FileTitle.Text = displayName;
            DimensionsText.Text = $"{_image.Width}×{_image.Height}";
            DepthText.Text = $"{_image.BitsAllocated} بیت{(_image.IsSigned ? " (علامت‌دار)" : "")}";
            KindText.Text = _image.Rgb24 is not null ? "رنگی RGB" : "خاکستری";

            // پنجره پیش‌فرض از فایل وگرنه بازه خودکار
            var (min, max) = _image.RawRange();
            var center = _image.WindowCenter ?? (min + max) / 2.0;
            var width = _image.WindowWidth ?? Math.Max(1, max - min);

            CenterSlider.Value = Math.Clamp(center, CenterSlider.Minimum, CenterSlider.Maximum);
            WidthSlider.Value = Math.Clamp(width, WidthSlider.Minimum, WidthSlider.Maximum);

            RenderMetadata();
            RenderImage();
            StatusText.Text = $"بارگذاری موفق — {_image.Width}×{_image.Height} پیکسل، " +
                              $"{_image.BitsAllocated} بیت، تفسیر {_image.PhotometricInterpretation}";
        }
        catch (DicomParseException ex)
        {
            MessageBox.Show(this, ex.Message, "خطای تجزیه DICOM",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            StatusText.Text = $"خطا: {ex.Message}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "خواندن فایل ناموفق بود: " + ex.Message,
                "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>رسم مجدد با پنجره/سطح فعلی</summary>
    private void OnWindowChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        CenterValue.Text = CenterSlider.Value.ToString("0");
        WidthValue.Text = WidthSlider.Value.ToString("0");
        if (_image is not null && e.OriginalSource is Slider)
            RenderImage();
    }

    /// <summary>پنجره خودکار — مرکز و عرض از بازه واقعی مقادیر</summary>
    private void OnAutoWindowClick(object sender, RoutedEventArgs e)
    {
        if (_image is null) return;
        var (min, max) = _image.RawRange();
        CenterSlider.Value = Math.Clamp((min + max) / 2.0, CenterSlider.Minimum, CenterSlider.Maximum);
        WidthSlider.Value = Math.Clamp(Math.Max(1, max - min), WidthSlider.Minimum, WidthSlider.Maximum);
    }

    /// <summary>بزرگ‌نمایی — تغییر مقیاس رندر</summary>
    private void OnZoomChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        var percent = (int)e.NewValue;
        ZoomValue.Text = $"{percent}٪";
        if (ImageHost.Source is not null)
        {
            var scale = percent / 100.0;
            ImageHost.LayoutTransform = new ScaleTransform(scale, scale);
        }
    }

    /// <summary>رندر پیکسل‌ها به BitmapSource — خاکستری یا رنگی</summary>
    private void RenderImage()
    {
        if (_image is null) return;

        if (_image.Rgb24 is not null)
        {
            ImageHost.Source = BitmapSource.Create(
                _image.Width, _image.Height, 96, 96, PixelFormats.Rgb24, null,
                _image.Rgb24, _image.Width * 3);
        }
        else
        {
            var gray = _image.RenderGray8(CenterSlider.Value, WidthSlider.Value);
            ImageHost.Source = BitmapSource.Create(
                _image.Width, _image.Height, 96, 96, PixelFormats.Gray8, null,
                gray, _image.Width);
        }

        // اعمال بزرگ‌نمایی فعلی مجدد
        OnZoomChanged(this, new RoutedPropertyChangedEventArgs<double>(
            ZoomSlider.Value, ZoomSlider.Value));
    }

    /// <summary>نمایش متادیتای فارسی در پنل کنار</summary>
    private void RenderMetadata()
    {
        if (_image is null) return;
        MetadataPanel.Children.Clear();

        var index = 0;
        var total = _image.Metadata.Count;
        foreach (var (name, value) in _image.Metadata)
        {
            var row = new StackPanel { Margin = new Thickness(4, 5, 4, 5) };
            row.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 10.5,
                Opacity = 0.65
            });
            row.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            });
            MetadataPanel.Children.Add(row);

            if (++index < total)
                MetadataPanel.Children.Add(new Separator());
        }
    }
}

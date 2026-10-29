using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت صفحهٔ «فیزیوتراپی» — ماژول حرفه‌ای (طرح تأییدشدهٔ مالک):
/// نقشهٔ بدن (تصویر پایهٔ مرد/زن + ماسک رنگی ناحیه‌ها + چندضلعی‌های کلیک)
/// به‌صورت پویا روی BodyMapHost ساخته می‌شود تا انتخاب دقیقاً روی خودِ
/// ناحیهٔ تصویر بنشیند؛ چاپ تمرین خانگی نیز همین‌جا انجام می‌شود.
/// </summary>
public partial class PhysiotherapyView : UserControl
{
    private readonly Dictionary<string, Rectangle> _zoneRects = new();
    private PhysiotherapyViewModel? _vm;
    private bool _builtFemale;

    /// <summary>سازنده — مقداردهی اولیهٔ اجزا و اشتراک رویداد نقشهٔ بدن</summary>
    public PhysiotherapyView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => BuildBodyMap();
    }

    /// <summary>با تغییر ویومدل، اشتراک رویداد رنگ‌آمیزی نقشه تازه می‌شود</summary>
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null) _vm.BodyMapChanged -= RepaintBodyMap;
        _vm = e.NewValue as PhysiotherapyViewModel;
        if (_vm is not null) _vm.BodyMapChanged += RepaintBodyMap;
        BuildBodyMap();
    }

    /// <summary>
    /// ساخت نقشهٔ بدن برای جنسیت جاری: تصویر پایه، مستطیل رنگی هر ناحیه با
    /// ماسک همان ناحیه (Assets/Physio) و چندضلعی‌های کلیک روی آن.
    /// </summary>
    private void BuildBodyMap()
    {
        if (_vm is null || BodyMapHost is null) return;
        _builtFemale = _vm.IsFemale;
        _zoneRects.Clear();
        BodyMapHost.Children.Clear();

        var canvas = new Canvas { Width = PhysioBodyMapData.MapWidth, Height = PhysioBodyMapData.MapHeight };
        var gender = _builtFemale ? "female" : "male";
        var baseImage = new Image
        {
            Width = PhysioBodyMapData.MapWidth,
            Height = PhysioBodyMapData.MapHeight,
            Source = LoadImage($"bodymap-{gender}.png"),
            Stretch = Stretch.Fill
        };
        canvas.Children.Add(baseImage);

        var masks = _builtFemale ? PhysioBodyMapData.FemaleMasks : PhysioBodyMapData.MaleMasks;
        foreach (var (zone, file) in masks)
        {
            var rect = new Rectangle
            {
                Width = PhysioBodyMapData.MapWidth,
                Height = PhysioBodyMapData.MapHeight,
                Fill = Brushes.Transparent,
                OpacityMask = new ImageBrush(LoadImage(file)) { Stretch = Stretch.Fill },
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            _zoneRects[zone] = rect;
            canvas.Children.Add(rect);
        }

        var hits = _builtFemale ? PhysioBodyMapData.FemaleHits : PhysioBodyMapData.MaleHits;
        foreach (var (zone, pointsText) in hits)
        {
            var points = new PointCollection();
            foreach (var pair in pointsText.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var xy = pair.Split(',');
                if (xy.Length == 2 && double.TryParse(xy[0], out var x) && double.TryParse(xy[1], out var y))
                    points.Add(new Point(x, y));
            }
            if (points.Count < 3) continue;
            var polygon = new Polygon
            {
                Points = points,
                Fill = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Tag = zone
            };
            polygon.MouseLeftButtonDown += OnZoneClicked;
            canvas.Children.Add(polygon);
        }

        BodyMapHost.Children.Add(canvas);
        RepaintBodyMap();
    }

    /// <summary>کلیک روی ناحیهٔ نقشه — انتخاب همان گروه در ویومدل</summary>
    private void OnZoneClicked(object sender, MouseButtonEventArgs e)
    {
        if (_vm is null || sender is not Polygon polygon || polygon.Tag is not string zone) return;
        _vm.SelectZone(zone);
    }

    /// <summary>
    /// رنگ‌آمیزی ناحیه‌ها: انتخابی سبزآبی؛ دارای درد بر اساس شدت
    /// (۰-۲ سبز، ۳-۵ نارنجی، ۶-۱۰ قرمز) — همان منطق پیش‌نمایش تأییدشده.
    /// </summary>
    private void RepaintBodyMap()
    {
        if (_vm is null) return;
        if (_vm.IsFemale != _builtFemale) { BuildBodyMap(); return; }
        foreach (var (zone, rect) in _zoneRects)
        {
            var measured = _vm.ZonePain.TryGetValue(zone, out var pain);
            var selected = zone == _vm.SelectedZone;
            if (!measured && !selected) { rect.Visibility = Visibility.Collapsed; continue; }
            rect.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                measured ? (pain <= 2 ? "#66BB6A" : pain <= 5 ? "#FFA726" : "#EF5350") : "#00897B"));
            rect.Opacity = measured ? 0.60 : 0.55;
            rect.Visibility = Visibility.Visible;
        }
    }

    /// <summary>بارگذاری تصویر جاسازی‌شدهٔ نقشهٔ بدن</summary>
    private static BitmapImage LoadImage(string file)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri($"pack://application:,,,/Assets/Physio/{file}", UriKind.Absolute);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }

    /// <summary>چاپ تمرین خانگی بیمار با نام و متن برنامه</summary>
    private void PrintHomeProgram_Click(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        if (!_vm.IsPhysioSessionEnabled || _vm.PhysioPatient is null)
        { AppDialog.Error("چاپ تمرین خانگی", "ابتدا بیمار و سابقهٔ او را بارگذاری کنید."); return; }
        if (string.IsNullOrWhiteSpace(_vm.HomeProgram))
        { AppDialog.Error("چاپ تمرین خانگی", "ابتدا متن برنامهٔ تمرین خانگی را وارد کنید."); return; }
        var doc = new FlowDocument
        {
            PagePadding = new Thickness(48),
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = new FontFamily("pack://application:,,,/Assets/Fonts/#Vazirmatn")
        };
        doc.Blocks.Add(new Paragraph(new Run("برنامهٔ تمرین خانگی فیزیوتراپی"))
        { FontSize = 20, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
        doc.Blocks.Add(new Paragraph(new Run($"بیمار: {_vm.PhysioPatient?.FullName ?? "—"}    پروتکل: {_vm.ActiveProtocolName ?? "—"}"))
        { FontSize = 13 });
        doc.Blocks.Add(new Paragraph(new Run(_vm.HomeProgram ?? string.Empty)) { FontSize = 13 });
        doc.Blocks.Add(new Paragraph(new Run("لطفاً تمرین‌ها را روزانه طبق برنامه انجام دهید و در صورت تشدید درد با درمانگاه تماس بگیرید."))
        { FontSize = 11 });
        PrintFlow.Print(doc, "تمرین خانگی فیزیوتراپی");
    }
}

using System.Windows;
using System.Windows.Controls;
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
    private readonly HashSet<ValidationError> _numericErrors = new();
    private PhysiotherapyViewModel? _vm;
    private bool _builtFemale;

    /// <summary>سازنده — مقداردهی اولیهٔ اجزا و اشتراک رویداد نقشهٔ بدن</summary>
    public PhysiotherapyView()
    {
        InitializeComponent();
        Validation.AddErrorHandler(this, OnNumericValidationError);
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => BuildBodyMap();
    }

    /// <summary>با تغییر ویومدل، اشتراک رویداد رنگ‌آمیزی نقشه تازه می‌شود</summary>
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null) { _vm.BodyMapChanged -= RepaintBodyMap; _vm.HasInvalidPhysioInput = false; }
        _vm = e.NewValue as PhysiotherapyViewModel;
        if (_vm is not null) { _vm.BodyMapChanged += RepaintBodyMap; _vm.HasInvalidPhysioInput = _numericErrors.Count != 0; }
        BuildBodyMap();
    }

    private void OnNumericValidationError(object sender, ValidationErrorEventArgs e)
    {
        if (e.Action == ValidationErrorEventAction.Added) _numericErrors.Add(e.Error);
        else _numericErrors.Remove(e.Error);
        if (_vm is not null) _vm.HasInvalidPhysioInput = _numericErrors.Count != 0;
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
        PrintPhysioDocument(_vm, _vm.HomeProgram, false);
    }

    private void PrintReferralReport_Click(object sender, RoutedEventArgs e)
    {
        if (_vm is null || !_vm.IsPhysioSessionEnabled || _vm.PhysioPatient is null)
        { AppDialog.Error("چاپ گزارش", "ابتدا بیمار و سابقهٔ او را بارگذاری کنید."); return; }
        // گزارش بلافاصله از مقادیر فعلی ساخته می‌شود و پس از خطای اعتبار چاپ نمی‌شود.
        _vm.GenerateReportCommand.Execute(null);
        if (string.IsNullOrWhiteSpace(_vm.ReferralReport)) return;
        PrintPhysioDocument(_vm, _vm.ReferralReport, true);
    }

    private static void PrintPhysioDocument(PhysiotherapyViewModel vm, string? text, bool referral)
    {
        if (!PhysioPrintService.TryCreate(vm.PhysioPatient, text, out var data))
        { AppDialog.Error("چاپ فیزیوتراپی", "مشخصات بیمار یا متن گزارش معتبر نیست."); return; }
        var key = referral ? "PhysioReferralReport" : "PhysioHomeProgram";
        var title = referral ? "پیش‌نویس گزارش فیزیوتراپی" : "پیش‌نویس تمرین خانگی فیزیوتراپی";
        PrintFlow.Print(PhysioPrintService.Build(data!, IranClock.Now, referral), title,
            PrintJobSettings.PrinterFor(key), jobKey: key);
    }
}

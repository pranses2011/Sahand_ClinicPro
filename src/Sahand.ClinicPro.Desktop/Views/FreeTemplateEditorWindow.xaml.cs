using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>ویرایش نسخه محلی بوم؛ تغییر سرور تنها با ذخیره صفحه اصلی انجام می‌شود.</summary>
public partial class FreeTemplateEditorWindow : Window
{
    private const string DragFormat = "SahandClinicPro.FreeTemplate.Kind";
    private readonly FreeTemplateDocument _document;
    private readonly IReadOnlyList<ComplementaryViewModel.TemplateVariable> _variables;
    private FreeTemplateElement? _selected;
    private Point _toolStart, _dragStart;
    private double _startX, _startY;
    public string? ResultHtml { get; private set; }
    public string? ResultPaperSize { get; private set; }
    public string? ResultOrientation { get; private set; }

    public FreeTemplateEditorWindow(FreeTemplateDocument document, IEnumerable<ComplementaryViewModel.TemplateVariable> variables, string paper, string orientation)
    {
        _document = JsonSerializer.Deserialize<FreeTemplateDocument>(JsonSerializer.Serialize(document))!;
        _variables = variables.GroupBy(v => v.Key).Select(g => g.First()).ToArray();
        InitializeComponent();
        VariableBox.ItemsSource = _variables;
        PaperBox.Text = paper;
        OrientationBox.SelectedIndex = string.Equals(orientation, "Landscape", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        RefreshCanvas();
    }
    private static string Number(double n) => n.ToString("0.##", CultureInfo.InvariantCulture);
    private static double ReadNumber(TextBox box)
    {
        var text = box.Text.Trim();
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value)) return value;
        throw new ArgumentException("عدد معتبر وارد کنید (برای اعشار از نقطه استفاده کنید).");
    }
    private int? ReadPadding()
    {
        var text = SheetPaddingBox.Text.Trim();
        if (text.Length == 0) return null;
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value is >= 0 and <= 100) return value;
        throw new ArgumentException("حاشیه داخلی باید خالی یا عدد صحیح بین ۰ و ۱۰۰ باشد.");
    }
    private static BitmapSource DecodeImage(byte[] data)
    {
        using var input = new MemoryStream(data, writable: false);
        var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        if (decoder is not (PngBitmapDecoder or JpegBitmapDecoder) || decoder.Frames.Count != 1)
            throw new ArgumentException("تصویر PNG یا JPEG تک‌فریم انتخاب کنید.");
        var frame = decoder.Frames[0];
        if (frame.PixelWidth < 1 || frame.PixelHeight < 1 || frame.PixelWidth > 2048 || frame.PixelHeight > 2048)
            throw new ArgumentException("ابعاد تصویر باید حداکثر ۲۰۴۸ در ۲۰۴۸ پیکسل باشد.");
        frame.Freeze();
        return frame;
    }
    private static UIElement ImagePreview(string? data)
    {
        try { return new Image { Source = DecodeImage(Convert.FromBase64String(data ?? "")), Stretch = Stretch.Fill }; }
        catch (Exception ex) when (ex is ArgumentException or FormatException or IOException or NotSupportedException or InvalidOperationException)
        { return new TextBlock { Text = "تصویر قابل نمایش نیست", Foreground = Brushes.DarkRed, TextWrapping = TextWrapping.Wrap }; }
    }
    private void RefreshCanvas()
    {
        PageCanvas.Width = _document.Width; PageCanvas.Height = _document.Height;
        CanvasWidthBox.Text = Number(_document.Width); CanvasHeightBox.Text = Number(_document.Height);
        SheetPaddingBox.Text = _document.SheetPadding?.ToString(CultureInfo.InvariantCulture) ?? "";
        PageCanvas.Children.Clear();
        foreach (var item in _document.Elements)
        {
            var label = item.Kind switch { "variable" => item.Text + " «" + (_variables.FirstOrDefault(v => v.Key == item.Key)?.Label ?? item.Key) + "»", "text" => item.Text, "line" => "────────", _ => "" };
            var ink = (Brush)new BrushConverter().ConvertFromString(item.Color)!;
            var content = new Border { BorderBrush = ink,
                BorderThickness = item.Kind == "rectangle" ? new Thickness(1) : item.Kind == "line" ? new Thickness(0, 1, 0, 0) : new Thickness(0),
                Child = item.Kind == "image" ? ImagePreview(item.ImageData) : new TextBlock { Text = item.Kind == "line" ? "" : label, FontSize = item.FontSize, FontWeight = item.Bold ? FontWeights.Bold : FontWeights.Normal,
                    TextAlignment = item.Alignment == "center" ? TextAlignment.Center : item.Alignment == "left" ? TextAlignment.Left : TextAlignment.Right,
                    Foreground = ink, FlowDirection = FlowDirection.RightToLeft, TextWrapping = TextWrapping.Wrap } };
            var border = new Border { Width = item.Width, Height = item.Height, Tag = item, Cursor = Cursors.SizeAll,
                BorderThickness = new Thickness(ReferenceEquals(item, _selected) ? 1 : 0),
                BorderBrush = ReferenceEquals(item, _selected) ? Brushes.DodgerBlue : Brushes.Transparent,
                Background = Brushes.Transparent, ClipToBounds = true, Child = content };
            Canvas.SetLeft(border, item.X); Canvas.SetTop(border, item.Y);
            border.MouseLeftButtonDown += ItemDown; border.MouseMove += ItemMove; border.MouseLeftButtonUp += ItemUp;
            PageCanvas.Children.Add(border);
        }
    }
    private void Select(FreeTemplateElement item)
    {
        _selected = item; TextBoxValue.Text = item.Text; VariableBox.SelectedValue = item.Key;
        VariableBox.IsEnabled = item.Kind == "variable";
        XBox.Text = Number(item.X); YBox.Text = Number(item.Y); WidthBox.Text = Number(item.Width); HeightBox.Text = Number(item.Height);
        FontBox.Text = item.FontSize.ToString(CultureInfo.InvariantCulture); ColorBox.Text = item.Color;
        BoldBox.IsChecked = item.Bold; AlignmentBox.SelectedIndex = item.Alignment == "center" ? 1 : item.Alignment == "left" ? 2 : 0;
    }
    private string? ReadAlignment() => AlignmentBox.SelectedIndex == 1 ? "center" : AlignmentBox.SelectedIndex == 2 ? "left" : null;
    private bool CanDiscardPending()
    {
        var dirty = false;
        try
        {
            dirty = ReadNumber(CanvasWidthBox) != _document.Width || ReadNumber(CanvasHeightBox) != _document.Height || ReadPadding() != _document.SheetPadding;
            if (_selected is { } item)
                dirty |= TextBoxValue.Text != item.Text || (VariableBox.SelectedValue as string ?? "") != item.Key ||
                    ReadNumber(XBox) != item.X || ReadNumber(YBox) != item.Y || ReadNumber(WidthBox) != item.Width || ReadNumber(HeightBox) != item.Height ||
                    !int.TryParse(FontBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var font) || font != item.FontSize || ColorBox.Text.Trim() != item.Color || (BoldBox.IsChecked == true) != item.Bold || ReadAlignment() != item.Alignment;
        }
        catch (ArgumentException) { dirty = true; }
        return !dirty || AppDialog.Confirm("تغییرات اعمال‌نشده", "تغییرات ویژگی یا ابعاد هنوز اعمال نشده‌اند و با این کار کنار گذاشته می‌شوند. ادامه می‌دهید؟", AppDialogIcon.Warning) == true;
    }
    private void ItemDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { Tag: FreeTemplateElement item } border) return;
        if (!CanDiscardPending()) { e.Handled = true; return; }
        Select(item); _dragStart = e.GetPosition(PageCanvas); _startX = item.X; _startY = item.Y; border.CaptureMouse(); e.Handled = true;
    }
    private void ItemMove(object sender, MouseEventArgs e)
    {
        if (sender is not Border { IsMouseCaptured: true, Tag: FreeTemplateElement item } border || e.LeftButton != MouseButtonState.Pressed) return;
        var p = e.GetPosition(PageCanvas);
        item.X = Math.Round(Math.Clamp(_startX + p.X - _dragStart.X, 0, Math.Max(0, _document.Width - item.Width)), 2);
        item.Y = Math.Round(Math.Clamp(_startY + p.Y - _dragStart.Y, 0, Math.Max(0, _document.Height - item.Height)), 2);
        Canvas.SetLeft(border, item.X); Canvas.SetTop(border, item.Y); XBox.Text = Number(item.X); YBox.Text = Number(item.Y);
    }
    private void ItemUp(object sender, MouseButtonEventArgs e)
    { if (sender is Border border) border.ReleaseMouseCapture(); RefreshCanvas(); e.Handled = true; }
    private void ToolDown(object sender, MouseButtonEventArgs e) => _toolStart = e.GetPosition(this);
    private void ToolMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || sender is not Button { Tag: string kind } button) return;
        var p = e.GetPosition(this);
        if (Math.Abs(p.X - _toolStart.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(p.Y - _toolStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        var data = new DataObject(); data.SetData(DragFormat, kind); DragDrop.DoDragDrop(button, data, DragDropEffects.Copy); e.Handled = true;
    }
    private void Canvas_DragOver(object sender, DragEventArgs e) { e.Effects = e.Data.GetDataPresent(DragFormat) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; }
    private void Canvas_Drop(object sender, DragEventArgs e)
    { if (e.Data.GetData(DragFormat) is string kind) { Add(kind, e.GetPosition(PageCanvas)); e.Handled = true; } }
    private void Add_Click(object sender, RoutedEventArgs e) { if (sender is Button { Tag: string kind }) Add(kind, new Point(20, 20)); }
    private void Add(string kind, Point point)
    {
        if (kind is not ("text" or "variable" or "rectangle" or "line" or "image")) return;
        if (!CanDiscardPending()) return;
        if (kind == "variable" && _variables.Count == 0) { AppDialog.Error("طراحی قالب", "برای این سند متغیری در دسترس نیست."); return; }
        string? imageData = null;
        if (kind == "image")
        {
            if (_document.Elements.Count(x => x.Kind == "image") >= 4) { AppDialog.Error("تصویر", "حداکثر چهار تصویر مجاز است."); return; }
            var dialog = new OpenFileDialog { Filter = "تصویر PNG یا JPEG|*.png;*.jpg;*.jpeg", CheckFileExists = true, Multiselect = false };
            if (dialog.ShowDialog(this) != true) return;
            try
            {
                using var file = File.OpenRead(dialog.FileName);
                if (file.Length is < 8 or > 32768) throw new ArgumentException("حجم تصویر باید حداکثر ۳۲ کیلوبایت باشد.");
                var bytes = new byte[(int)file.Length]; file.ReadExactly(bytes);
                imageData = Convert.ToBase64String(bytes);
                if (FreeTemplateDesign.ImageMimeType(imageData) is null) throw new ArgumentException("فقط فایل PNG یا JPEG مجاز است.");
                _ = DecodeImage(bytes);
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException or IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException)
            { AppDialog.Error("تصویر", "خواندن تصویر ناموفق بود: " + ex.Message); return; }
        }
        var width = Math.Min(200, _document.Width); var height = kind == "line" ? 8 : kind == "image" ? 100 : 40;
        var item = new FreeTemplateElement { Kind = kind, Text = kind == "text" ? "متن جدید" : kind == "variable" ? _variables[0].Label + ":" : "",
            Key = kind == "variable" ? _variables[0].Key : "", ImageData = imageData, Width = width, Height = height,
            X = Math.Clamp(point.X, 0, _document.Width - width), Y = Math.Clamp(point.Y, 0, _document.Height - height) };
        _document.Elements.Add(item); Select(item); RefreshCanvas();
    }
    private void Update_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        try
        {
            if (ReadNumber(CanvasWidthBox) != _document.Width || ReadNumber(CanvasHeightBox) != _document.Height || ReadPadding() != _document.SheetPadding)
                throw new ArgumentException("ابتدا ابعاد بوم را اعمال کنید تا تغییرات آن کنار گذاشته نشود.");
            var previous = _selected;
            var updated = new FreeTemplateElement { Kind = _selected.Kind, Text = TextBoxValue.Text, Key = VariableBox.SelectedValue as string ?? "",
                X = ReadNumber(XBox), Y = ReadNumber(YBox), Width = ReadNumber(WidthBox), Height = ReadNumber(HeightBox),
                FontSize = int.Parse(FontBox.Text, CultureInfo.InvariantCulture), Color = ColorBox.Text.Trim(), ImageData = _selected.ImageData, Bold = BoldBox.IsChecked == true, Alignment = ReadAlignment() };
            var index = _document.Elements.IndexOf(_selected); _document.Elements[index] = updated;
            try { FreeTemplateDesign.Build(_document, _variables.Select(v => v.Key).ToArray()); }
            catch { _document.Elements[index] = previous; throw; }
            Select(updated); RefreshCanvas();
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException) { AppDialog.Error("ویژگی عنصر", ex.Message); }
    }
    private void ResizeCanvas_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var width = ReadNumber(CanvasWidthBox); var height = ReadNumber(CanvasHeightBox);
            var padding = ReadPadding();
            var oldW = _document.Width; var oldH = _document.Height; var oldPadding = _document.SheetPadding;
            _document.Width = width; _document.Height = height; _document.SheetPadding = padding;
            try { FreeTemplateDesign.Build(_document, _variables.Select(v => v.Key).ToArray()); }
            catch { _document.Width = oldW; _document.Height = oldH; _document.SheetPadding = oldPadding; throw; }
            RefreshCanvas();
        }
        catch (ArgumentException ex) { AppDialog.Error("اندازه بوم", ex.Message); }
    }
    private void Remove_Click(object sender, RoutedEventArgs e) { if (_selected is not null && CanDiscardPending()) { _document.Elements.Remove(_selected); _selected = null; RefreshCanvas(); } }
    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ReadNumber(CanvasWidthBox) != _document.Width || ReadNumber(CanvasHeightBox) != _document.Height || ReadPadding() != _document.SheetPadding)
                throw new ArgumentException("ابتدا دکمه اعمال ابعاد بوم را بزنید.");
            if (_selected is { } item && (TextBoxValue.Text != item.Text || (VariableBox.SelectedValue as string ?? "") != item.Key ||
                ReadNumber(XBox) != item.X || ReadNumber(YBox) != item.Y || ReadNumber(WidthBox) != item.Width || ReadNumber(HeightBox) != item.Height ||
                !int.TryParse(FontBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var font) || font != item.FontSize || ColorBox.Text.Trim() != item.Color || (BoldBox.IsChecked == true) != item.Bold || ReadAlignment() != item.Alignment))
                throw new ArgumentException("ابتدا دکمه اعمال ویژگی‌ها را بزنید.");
            var paper = PaperBox.Text.Trim();
            if (!VisualTemplateDesign.IsValidPaperSize(paper)) throw new ArgumentException("اندازه کاغذ معتبر نیست.");
            var orientation = OrientationBox.SelectedIndex == 1 ? "Landscape" : "Portrait";
            var availableWidth = FreeTemplateDesign.AvailableBodyWidth(paper, orientation, _document.SheetPadding);
            if (availableWidth == 0) throw new ArgumentException("جهت افقی برای این اندازه کاغذ پشتیبانی نمی‌شود.");
            if (_document.Width > availableWidth)
                throw new ArgumentException("عرض بوم از فضای بدنه کاغذ بیشتر است و بخشی از سند بریده می‌شود؛ ابعاد بوم یا کاغذ را اصلاح کنید.");
            var html = FreeTemplateDesign.Build(_document, _variables.Select(v => v.Key).ToArray());
            if (_document.Elements.Any(x => x.Kind == "variable") && AppDialog.Confirm("بررسی محتوای چاپ",
                "کادرهای بوم اندازه ثابت دارند و اطلاعات طولانی یا ردیف‌های جدول ممکن است بریده شوند. پیش از استفاده، تمام داده‌ها و صفحه‌ها را با داده واقعی و چاپ آزمایشی بررسی کنید. ادامه می‌دهید؟", AppDialogIcon.Warning) != true) return;
            ResultHtml = html; ResultPaperSize = paper; ResultOrientation = orientation; DialogResult = true;
        }
        catch (ArgumentException ex) { AppDialog.Error("طراحی قالب", ex.Message); }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

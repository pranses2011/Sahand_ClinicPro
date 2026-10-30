using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

    public FreeTemplateEditorWindow(FreeTemplateDocument document, IEnumerable<ComplementaryViewModel.TemplateVariable> variables, string paper)
    {
        _document = JsonSerializer.Deserialize<FreeTemplateDocument>(JsonSerializer.Serialize(document))!;
        _variables = variables.GroupBy(v => v.Key).Select(g => g.First()).ToArray();
        InitializeComponent();
        VariableBox.ItemsSource = _variables;
        PaperBox.Text = paper;
        RefreshCanvas();
    }
    private static string Number(double n) => n.ToString("0.##", CultureInfo.InvariantCulture);
    private static double ReadNumber(TextBox box)
    {
        var text = box.Text.Trim();
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value)) return value;
        throw new ArgumentException("عدد معتبر وارد کنید (برای اعشار از نقطه استفاده کنید).");
    }
    private void RefreshCanvas()
    {
        PageCanvas.Width = _document.Width; PageCanvas.Height = _document.Height;
        CanvasWidthBox.Text = Number(_document.Width); CanvasHeightBox.Text = Number(_document.Height);
        PageCanvas.Children.Clear();
        foreach (var item in _document.Elements)
        {
            var label = item.Kind switch { "variable" => item.Text + " «" + (_variables.FirstOrDefault(v => v.Key == item.Key)?.Label ?? item.Key) + "»", "text" => item.Text, "line" => "────────", _ => "" };
            var border = new Border { Width = item.Width, Height = item.Height, Tag = item, Cursor = Cursors.SizeAll,
                BorderThickness = new Thickness(ReferenceEquals(item, _selected) ? 2 : 1),
                BorderBrush = ReferenceEquals(item, _selected) ? Brushes.DodgerBlue : item.Kind is "rectangle" or "line" ? Brushes.SlateGray : Brushes.Transparent,
                Background = Brushes.Transparent, ClipToBounds = true,
                Child = new TextBlock { Text = label, FontSize = item.FontSize, Foreground = Brushes.DarkSlateGray, FlowDirection = FlowDirection.RightToLeft, TextWrapping = TextWrapping.Wrap } };
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
    }
    private void ItemDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { Tag: FreeTemplateElement item } border) return;
        Select(item); _dragStart = e.GetPosition(PageCanvas); _startX = item.X; _startY = item.Y; border.CaptureMouse(); e.Handled = true;
    }
    private void ItemMove(object sender, MouseEventArgs e)
    {
        if (sender is not Border { IsMouseCaptured: true, Tag: FreeTemplateElement item } border || e.LeftButton != MouseButtonState.Pressed) return;
        var p = e.GetPosition(PageCanvas);
        item.X = Math.Clamp(_startX + p.X - _dragStart.X, 0, Math.Max(0, _document.Width - item.Width));
        item.Y = Math.Clamp(_startY + p.Y - _dragStart.Y, 0, Math.Max(0, _document.Height - item.Height));
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
        if (kind is not ("text" or "variable" or "rectangle" or "line")) return;
        if (kind == "variable" && _variables.Count == 0) { AppDialog.Error("طراحی قالب", "برای این سند متغیری در دسترس نیست."); return; }
        var width = Math.Min(200, _document.Width); var height = kind == "line" ? 8 : 40;
        var item = new FreeTemplateElement { Kind = kind, Text = kind == "text" ? "متن جدید" : kind == "variable" ? _variables[0].Label + ":" : "",
            Key = kind == "variable" ? _variables[0].Key : "", Width = width, Height = height,
            X = Math.Clamp(point.X, 0, _document.Width - width), Y = Math.Clamp(point.Y, 0, _document.Height - height) };
        _document.Elements.Add(item); Select(item); RefreshCanvas();
    }
    private void Update_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        try
        {
            var previous = _selected;
            var updated = new FreeTemplateElement { Kind = _selected.Kind, Text = TextBoxValue.Text, Key = VariableBox.SelectedValue as string ?? "",
                X = ReadNumber(XBox), Y = ReadNumber(YBox), Width = ReadNumber(WidthBox), Height = ReadNumber(HeightBox),
                FontSize = int.Parse(FontBox.Text, CultureInfo.InvariantCulture), Color = ColorBox.Text.Trim() };
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
            var oldW = _document.Width; var oldH = _document.Height;
            _document.Width = width; _document.Height = height;
            try { FreeTemplateDesign.Build(_document, _variables.Select(v => v.Key).ToArray()); }
            catch { _document.Width = oldW; _document.Height = oldH; throw; }
            RefreshCanvas();
        }
        catch (ArgumentException ex) { AppDialog.Error("اندازه بوم", ex.Message); }
    }
    private void Remove_Click(object sender, RoutedEventArgs e) { if (_selected is not null) { _document.Elements.Remove(_selected); _selected = null; RefreshCanvas(); } }
    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var paper = PaperBox.Text.Trim();
            if (!VisualTemplateDesign.IsValidPaperSize(paper)) throw new ArgumentException("اندازه کاغذ معتبر نیست.");
            ResultHtml = FreeTemplateDesign.Build(_document, _variables.Select(v => v.Key).ToArray()); ResultPaperSize = paper; DialogResult = true;
        }
        catch (ArgumentException ex) { AppDialog.Error("طراحی قالب", ex.Message); }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

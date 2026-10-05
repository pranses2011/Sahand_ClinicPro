using System.Windows.Media;

namespace Sahand.ClinicPro.Desktop.Controls.Charts;

/// <summary>
/// مدل داده نقطه نمودار — مشترک بین همه کنترل‌های نمودار
/// Label برچسب فارسی، Value مقدار عددی، Tooltip متن راهنمای شناور
/// </summary>
public record ChartPointData(string Label, double Value, string? Tooltip = null);

/// <summary>
/// پالت رنگی ثابت نمودارها — ۱۰ رنگ متریال با تفکیک‌پذیری بالا؛
/// ترتیب چرخشی تا هر تعداد نقطه پوشش داده شود.
/// </summary>
public static class ChartPalette
{
    private static readonly string[] Hex =
    {
        "#1565C0", "#00897B", "#F4511E", "#6A1B9A", "#FDD835",
        "#2E7D32", "#D81B60", "#00838F", "#5D4037", "#546E7A",
    };

    /// <summary>رنگ شاخص i از پالت (چرخشی)</summary>
    public static Brush Brush(int i) => (Brush)new BrushConverter().ConvertFromString(Hex[i % Hex.Length])!;

    /// <summary>رنگ اصلی نمودار روند</summary>
    public static Brush Primary => (Brush)new BrushConverter().ConvertFromString("#1565C0")!;
}

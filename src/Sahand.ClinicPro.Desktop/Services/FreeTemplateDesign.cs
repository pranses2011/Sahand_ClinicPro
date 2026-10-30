using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>عنصر مستقل روی بوم قالب؛ اندازه و مکان بر حسب پیکسل ۹۶ نقطه بر اینچ است.</summary>
public sealed class FreeTemplateElement
{
    public string Kind { get; set; } = "text";
    public string Text { get; set; } = "";
    public string Key { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 200;
    public double Height { get; set; } = 40;
    public int FontSize { get; set; } = 14;
    public string Color { get; set; } = "#173657";
}

/// <summary>مدل نسخه‌دار بوم که فقط پس از بازخوانی دقیق HTML دوباره ویرایش می‌شود.</summary>
public sealed class FreeTemplateDocument
{
    public double Width { get; set; } = 600;
    public double Height { get; set; } = 800;
    public List<FreeTemplateElement> Elements { get; set; } = new();
}

/// <summary>تولید و بازخوانی ایمن بدنه بوم مستقل از HTMLهای قدیمی.</summary>
public static class FreeTemplateDesign
{
    private const string Prefix = "<!--clinicpro-canvas-v1:";
    private static string Num(double n) => n.ToString("0.##", CultureInfo.InvariantCulture);
    private static string Safe(string text) => WebUtility.HtmlEncode(text).Replace("{", "&#123;").Replace("}", "&#125;");
    public static bool HasMarker(string? html) => html?.StartsWith(Prefix, StringComparison.Ordinal) == true;

    /// <summary>عرض تقریبی قابل استفاده بدنه با کسر حاشیه ثابت قالب سرور؛ تضمین چاپگر نیست.</summary>
    public static double AvailableBodyWidth(string paper, string orientation = "Portrait")
    {
        if (!VisualTemplateDesign.IsValidPaperSize(paper)) return 0;
        if (string.Equals(orientation, "Landscape", StringComparison.OrdinalIgnoreCase))
            return Sahand.ClinicPro.Application.Features.Reporting.PrintTemplates.PrintTemplateRenderer.LandscapePaper(paper) is { } page ? Math.Max(0, page.WidthPx - 52) : 0;
        if (!string.Equals(orientation, "Portrait", StringComparison.OrdinalIgnoreCase)) return 0;
        var key = paper.Trim().ToUpperInvariant();
        var width = key switch { "A3" => 1123, "A4" => 794, "A5" => 559, "A6" => 397, "B5" => 691,
            "LETTER" => 816, "LEGAL" => 1056, "THERMAL80" => 302, "THERMAL58" => 219, _ => 0 };
        if (width == 0)
        {
            var match = Regex.Match(key, @"^CUSTOM:\s*(\d{1,4})\s*[X×]", RegexOptions.CultureInvariant);
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var mm)) return 0;
            width = (int)Math.Round(mm * 96.0 / 25.4);
        }
        return Math.Max(0, width - 52);
    }

    public static string Build(FreeTemplateDocument document, IReadOnlyCollection<string> allowed)
    {
        if (document is null || !double.IsFinite(document.Width) || !double.IsFinite(document.Height) ||
            document.Width is < 100 or > 2500 || document.Height is < 100 or > 4000 || document.Elements is null || document.Elements.Count is < 1 or > 200)
            throw new ArgumentException("اندازه بوم یا تعداد عناصر معتبر نیست.");
        var body = new StringBuilder().Append("<div style='position:relative;direction:ltr;width:").Append(Num(document.Width))
            .Append("px;height:").Append(Num(document.Height)).Append("px'>");
        foreach (var item in document.Elements)
        {
            if (item is null || item.Text is null || item.Text.Length > 2000 || !double.IsFinite(item.X) || !double.IsFinite(item.Y) ||
                !double.IsFinite(item.Width) || !double.IsFinite(item.Height) || item.X < 0 || item.Y < 0 || item.Width < 5 || item.Height < 5 ||
                item.X + item.Width > document.Width || item.Y + item.Height > document.Height || item.FontSize is < 8 or > 72 ||
                item.Color is null || !Regex.IsMatch(item.Color, @"^#[0-9a-fA-F]{6}$"))
                throw new ArgumentException("مکان، اندازه یا رنگ عنصر معتبر نیست یا از بوم بیرون است.");
            if (item.Kind != "text" && item.Kind != "variable" && item.Kind != "rectangle" && item.Kind != "line")
                throw new ArgumentException("نوع عنصر معتبر نیست.");
            if (item.Kind == "variable" && (string.IsNullOrEmpty(item.Key) || !allowed.Contains(item.Key) || !Regex.IsMatch(item.Key, "^[a-z0-9_]+$")))
                throw new ArgumentException("متغیر این سند معتبر نیست.");
            body.Append("<div style='position:absolute;box-sizing:border-box;overflow:hidden;direction:rtl;white-space:pre-wrap;left:")
                .Append(Num(item.X)).Append("px;top:").Append(Num(item.Y)).Append("px;width:").Append(Num(item.Width))
                .Append("px;height:").Append(Num(item.Height)).Append("px;font-size:").Append(item.FontSize)
                .Append("px;color:").Append(item.Color);
            if (item.Kind == "rectangle") body.Append(";border:1px solid ").Append(item.Color);
            if (item.Kind == "line") body.Append(";border-top:1px solid ").Append(item.Color);
            body.Append("'>");
            if (item.Kind is "text" or "variable") body.Append(Safe(item.Text));
            if (item.Kind == "variable") body.Append(" {{").Append(item.Key).Append("}}");
            body.Append("</div>");
        }
        body.Append("</div>");
        return Prefix + Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(document)) + "-->" + body;
    }

    public static bool TryRead(string? html, IReadOnlyCollection<string> allowed, out FreeTemplateDocument? document)
    {
        document = null;
        if (!HasMarker(html)) return false;
        var end = html!.IndexOf("-->", Prefix.Length, StringComparison.Ordinal);
        if (end < 0 || end - Prefix.Length > 1000000) return false;
        try
        {
            var value = JsonSerializer.Deserialize<FreeTemplateDocument>(Convert.FromBase64String(html[Prefix.Length..end]));
            if (value is null || Build(value, allowed) != html) return false;
            document = value;
            return true;
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException or OverflowException) { return false; }
    }
}

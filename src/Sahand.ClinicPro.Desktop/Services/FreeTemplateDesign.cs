using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    /// <summary>متن ضخیم؛ مقدار پیش‌فرض در قالب قدیمی اضافه نمی‌شود.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Bold { get; set; }
    /// <summary>متن مورب؛ در داده قالب قدیمی پیش‌فرض خاموش می‌ماند.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Italic { get; set; }
    /// <summary>خط زیر متن؛ در داده قالب قدیمی پیش‌فرض خاموش می‌ماند.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Underline { get; set; }
    /// <summary>چیدمان متن؛ null همان راست‌چین پیش‌فرض است.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Alignment { get; set; }
    /// <summary>تصویر محلی محدود؛ قالب قدیمی بدون این فیلد حفظ می‌شود.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageData { get; set; }
}

/// <summary>مدل نسخه‌دار بوم که فقط پس از بازخوانی دقیق HTML دوباره ویرایش می‌شود.</summary>
public sealed class FreeTemplateDocument
{
    public double Width { get; set; } = 600;
    public double Height { get; set; } = 800;
    /// <summary>حاشیه داخلی یکسان؛ null به معنی حاشیه پیش‌فرض سرور است.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SheetPadding { get; set; }
    public List<FreeTemplateElement> Elements { get; set; } = new();
}

/// <summary>جهت دستگیره تغییر اندازه عنصر.</summary>
public enum FreeTemplateResizeHandle { TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left }

/// <summary>تولید و بازخوانی ایمن بدنه بوم مستقل از HTMLهای قدیمی.</summary>
public static class FreeTemplateDesign
{
    private const string Prefix = "<!--clinicpro-canvas-v1:";
    private static string Num(double n) => n.ToString("0.##", CultureInfo.InvariantCulture);
    private static string Safe(string text) => WebUtility.HtmlEncode(text).Replace("{", "&#123;").Replace("}", "&#125;");
    public static bool HasMarker(string? html) => html?.StartsWith(Prefix, StringComparison.Ordinal) == true;

    /// <summary>تغییر ترتیب یک لایه به اندازه یک پله؛ انتهای فهرست روی سایر عناصر رندر می‌شود.</summary>
    public static bool MoveLayer(FreeTemplateDocument document, FreeTemplateElement element, int direction)
    {
        if (direction is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(direction));
        var index = document.Elements.IndexOf(element);
        var next = index + direction;
        if (index < 0 || next < 0 || next >= document.Elements.Count) return false;
        (document.Elements[index], document.Elements[next]) = (document.Elements[next], document.Elements[index]);
        return true;
    }

    /// <summary>محاسبه مکان عنصر بدون خروج از بوم، حتی با ابعاد اعشاری.</summary>
    public static (double X, double Y) MoveWithinCanvas(FreeTemplateDocument document, FreeTemplateElement element, double x, double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(document.Width) || !double.IsFinite(document.Height) ||
            !double.IsFinite(element.Width) || !double.IsFinite(element.Height) || element.Width < 5 || element.Height < 5 ||
            element.Width > document.Width || element.Height > document.Height)
            throw new ArgumentException("محدوده جابه‌جایی معتبر نیست.");
        var maxX = Math.Floor((document.Width - element.Width) * 100) / 100;
        var maxY = Math.Floor((document.Height - element.Height) * 100) / 100;
        return (Math.Clamp(Math.Round(x, 2), 0, maxX), Math.Clamp(Math.Round(y, 2), 0, maxY));
    }

    /// <summary>تغییر اندازه از هشت دستگیره با حفظ لبه مقابل و محدوده بوم.</summary>
    public static (double X, double Y, double Width, double Height) ResizeFromHandle(
        FreeTemplateDocument document, FreeTemplateElement element, FreeTemplateResizeHandle handle, double dx, double dy)
    {
        if (!Enum.IsDefined(handle) || !double.IsFinite(dx) || !double.IsFinite(dy) ||
            !double.IsFinite(element.X) || !double.IsFinite(element.Y) || !double.IsFinite(element.Width) || !double.IsFinite(element.Height) ||
            !double.IsFinite(document.Width) || !double.IsFinite(document.Height) || element.X < 0 || element.Y < 0 ||
            element.Width < 5 || element.Height < 5 || element.X + element.Width > document.Width || element.Y + element.Height > document.Height)
            throw new ArgumentException("محدوده تغییر اندازه معتبر نیست.");
        var left = handle is FreeTemplateResizeHandle.Left or FreeTemplateResizeHandle.TopLeft or FreeTemplateResizeHandle.BottomLeft;
        var right = handle is FreeTemplateResizeHandle.Right or FreeTemplateResizeHandle.TopRight or FreeTemplateResizeHandle.BottomRight;
        var top = handle is FreeTemplateResizeHandle.Top or FreeTemplateResizeHandle.TopLeft or FreeTemplateResizeHandle.TopRight;
        var bottom = handle is FreeTemplateResizeHandle.Bottom or FreeTemplateResizeHandle.BottomLeft or FreeTemplateResizeHandle.BottomRight;
        var x = element.X; var y = element.Y; var width = element.Width; var height = element.Height;
        if (left)
        {
            var fixedRight = element.X + element.Width;
            x = Math.Clamp(Math.Round(element.X + dx, 2), 0, Math.Floor((fixedRight - 5) * 100) / 100);
            width = Math.Clamp(Math.Round(fixedRight - x, 2), 5, Math.Floor((document.Width - x) * 100) / 100);
        }
        else if (right)
            width = Math.Clamp(Math.Round(element.Width + dx, 2), 5, Math.Floor((document.Width - element.X) * 100) / 100);
        if (top)
        {
            var fixedBottom = element.Y + element.Height;
            y = Math.Clamp(Math.Round(element.Y + dy, 2), 0, Math.Floor((fixedBottom - 5) * 100) / 100);
            height = Math.Clamp(Math.Round(fixedBottom - y, 2), 5, Math.Floor((document.Height - y) * 100) / 100);
        }
        else if (bottom)
            height = Math.Clamp(Math.Round(element.Height + dy, 2), 5, Math.Floor((document.Height - element.Y) * 100) / 100);
        return (x, y, width, height);
    }

    /// <summary>محاسبه اندازه عنصر در کشیدن گوشه، محدود به محدوده بوم.</summary>
    public static (double Width, double Height) ResizeWithinCanvas(FreeTemplateDocument document, FreeTemplateElement element, double width, double height)
    {
        if (!double.IsFinite(width) || !double.IsFinite(height) || !double.IsFinite(element.X) || !double.IsFinite(element.Y) ||
            !double.IsFinite(document.Width) || !double.IsFinite(document.Height) || element.X < 0 || element.Y < 0 ||
            document.Width - element.X < 5 || document.Height - element.Y < 5)
            throw new ArgumentException("محدوده تغییر اندازه معتبر نیست.");
        // گرد کردن پس از Clamp می‌تواند در ابعاد اعشاری، لبه را چند
        // هزارم پیکسل از بوم خارج کند. سقف را به سمت داخل گرد می‌کنیم.
        var maxWidth = Math.Floor((document.Width - element.X) * 100) / 100;
        var maxHeight = Math.Floor((document.Height - element.Y) * 100) / 100;
        return (Math.Clamp(Math.Round(width, 2), 5, maxWidth),
                Math.Clamp(Math.Round(height, 2), 5, maxHeight));
    }

    /// <summary>عرض تقریبی قابل استفاده بدنه با کسر حاشیه ثابت قالب سرور؛ تضمین چاپگر نیست.</summary>
    public static double AvailableBodyWidth(string paper, string orientation = "Portrait", int? sheetPadding = null)
    {
        if (!VisualTemplateDesign.IsValidPaperSize(paper) || sheetPadding is < 0 or > 100) return 0;
        var padding = 2 * (sheetPadding ?? 26);
        if (string.Equals(orientation, "Landscape", StringComparison.OrdinalIgnoreCase))
            return Sahand.ClinicPro.Application.Features.Reporting.PrintTemplates.PrintTemplateRenderer.LandscapePaper(paper) is { } page ? Math.Max(0, page.WidthPx - padding) : 0;
        if (!string.Equals(orientation, "Portrait", StringComparison.OrdinalIgnoreCase)) return 0;
        var key = paper.Trim().ToUpperInvariant();
        var width = key switch { "A3" => 1123, "A4" => 794, "A5" => 559, "A6" => 397, "B5" => 691,
            "LETTER" => 816, "LEGAL" => 816, "THERMAL80" => 302, "THERMAL58" => 219, _ => 0 };
        if (width == 0)
        {
            var match = Regex.Match(key, @"^CUSTOM:\s*(\d{1,4})\s*[X×]", RegexOptions.CultureInvariant);
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var mm)) return 0;
            width = (int)Math.Round(mm * 96.0 / 25.4);
        }
        return Math.Max(0, width - padding);
    }

    /// <summary>تنها داده تصویری داخلی PNG یا JPEG، بدون URL یا SVG؛ هر تصویر حداکثر ۳۲ کیلوبایت.</summary>
    public static string? ImageMimeType(string? data)
    {
        if (string.IsNullOrEmpty(data) || data.Length > 43692) return null;
        try
        {
            var bytes = Convert.FromBase64String(data);
            if (bytes.Length is < 8 or > 32768 || Convert.ToBase64String(bytes) != data) return null;
            if (bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
            if (bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255) return "image/jpeg";
            return null;
        }
        catch (FormatException) { return null; }
    }

    /// <summary>اعتبار بوم در حین ویرایش؛ سند خالی می‌تواند اندازه داشته باشد ولی قابل ذخیره نیست.</summary>
    public static void ValidateCanvas(FreeTemplateDocument document)
    {
        if (document is null || !double.IsFinite(document.Width) || !double.IsFinite(document.Height) ||
            document.Width is < 100 or > 2500 || document.Height is < 100 or > 4000 || document.SheetPadding is < 0 or > 100 || document.Elements is null || document.Elements.Count > 200)
            throw new ArgumentException("اندازه بوم یا تعداد عناصر معتبر نیست.");
    }

    public static string Build(FreeTemplateDocument document, IReadOnlyCollection<string> allowed)
    {
        ValidateCanvas(document);
        if (document.Elements.Count == 0) throw new ArgumentException("برای ذخیره حداقل یک عنصر به بوم اضافه کنید.");
        if (document.Elements.Count(x => x?.Kind == "image") > 4) throw new ArgumentException("حداکثر چهار تصویر در بوم مجاز است.");
        var body = new StringBuilder();
        if (document.SheetPadding is { } padding) body.Append("<style>.sheet{padding:").Append(padding).Append("px !important;}</style>");
        body.Append("<div style='position:relative;direction:ltr;width:").Append(Num(document.Width))
            .Append("px;height:").Append(Num(document.Height)).Append("px'>");
        foreach (var item in document.Elements)
        {
            if (item is null || item.Text is null || item.Text.Length > 2000 || !double.IsFinite(item.X) || !double.IsFinite(item.Y) ||
                !double.IsFinite(item.Width) || !double.IsFinite(item.Height) || item.X < 0 || item.Y < 0 || item.Width < 5 || item.Height < 5 ||
                item.X + item.Width > document.Width || item.Y + item.Height > document.Height || item.FontSize is < 8 or > 72 ||
                item.Color is null || !Regex.IsMatch(item.Color, @"^#[0-9a-fA-F]{6}$"))
                throw new ArgumentException("مکان، اندازه یا رنگ عنصر معتبر نیست یا از بوم بیرون است.");
            if (item.Alignment is not (null or "center" or "left")) throw new ArgumentException("چیدمان متن معتبر نیست.");
            if (item.Kind != "text" && item.Kind != "variable" && item.Kind != "rectangle" && item.Kind != "line" && item.Kind != "image")
                throw new ArgumentException("نوع عنصر معتبر نیست.");
            var imageType = item.Kind == "image" ? ImageMimeType(item.ImageData) : null;
            if (item.Kind == "image" && imageType is null) throw new ArgumentException("تصویر باید PNG یا JPEG داخلی و حداکثر ۳۲ کیلوبایت باشد.");
            if (item.Kind != "image" && item.ImageData is not null) throw new ArgumentException("داده تصویر برای این عنصر مجاز نیست.");
            if (item.Kind == "variable" && (string.IsNullOrEmpty(item.Key) || !allowed.Contains(item.Key) || !Regex.IsMatch(item.Key, "^[a-z0-9_]+$")))
                throw new ArgumentException("متغیر این سند معتبر نیست.");
            body.Append("<div style='position:absolute;box-sizing:border-box;overflow:hidden;direction:rtl;white-space:pre-wrap;left:")
                .Append(Num(item.X)).Append("px;top:").Append(Num(item.Y)).Append("px;width:").Append(Num(item.Width))
                .Append("px;height:").Append(Num(item.Height)).Append("px;font-size:").Append(item.FontSize)
                .Append("px;color:").Append(item.Color);
            if (item.Bold) body.Append(";font-weight:bold");
            if (item.Italic) body.Append(";font-style:italic");
            if (item.Underline) body.Append(";text-decoration:underline");
            if (item.Alignment is not null) body.Append(";text-align:").Append(item.Alignment);
            if (item.Kind == "rectangle") body.Append(";border:1px solid ").Append(item.Color);
            if (item.Kind == "line") body.Append(";border-top:1px solid ").Append(item.Color);
            body.Append("'>");
            if (item.Kind is "text" or "variable") body.Append(Safe(item.Text));
            if (item.Kind == "variable") body.Append(" {{").Append(item.Key).Append("}}");
            if (item.Kind == "image") body.Append("<img alt='' style='width:100%;height:100%' src='data:").Append(imageType).Append(";base64,").Append(item.ImageData).Append("'>");
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

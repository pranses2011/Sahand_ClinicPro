using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>یک ردیف بصری: متن ثابت یا برچسب و متغیر سند.</summary>
public sealed class VisualTemplateBlock
{
    public string Kind { get; set; } = "text";
    public string Text { get; set; } = "";
    public string Key { get; set; } = "";
    public bool IsVariable => Kind == "variable";
}

/// <summary>قالب‌های ساخته‌شده در طراح بصری؛ HTMLهای دیگر بدون تبدیل دست‌نخورده می‌مانند.</summary>
public static class VisualTemplateDesign
{
    private const string Prefix = "<!--clinicpro-visual-v1:";
    /// <summary>بدنه با نشان طراح بصری آغاز می‌شود، حتی اگر دستکاری شده باشد.</summary>
    public static bool HasVisualMarker(string? html) => html?.StartsWith(Prefix, StringComparison.Ordinal) == true;
    private static string SafeText(string text) => WebUtility.HtmlEncode(text).Replace("{", "&#123;").Replace("}", "&#125;");

    /// <summary>اندازه‌های کاغذ استاندارد یا سفارشی با ابعاد معقول بر حسب میلی‌متر.</summary>
    public static bool IsValidPaperSize(string? paper)
    {
        if (string.IsNullOrWhiteSpace(paper)) return false;
        if (new[] { "A3", "A4", "A5", "A6", "B5", "Letter", "Legal", "Thermal80", "Thermal58" }.Contains(paper, StringComparer.OrdinalIgnoreCase)) return true;
        var match = Regex.Match(paper, @"^Custom:\s*(\d{1,4})\s*[x×]\s*(\d{1,4})\s*(mm)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success && int.TryParse(match.Groups[1].Value, out var width) && int.TryParse(match.Groups[2].Value, out var height) && width is >= 20 and <= 1000 && height is >= 20 and <= 1000;
    }

    public static string Build(IReadOnlyList<VisualTemplateBlock> blocks, IReadOnlyCollection<string> allowed)
    {
        if (blocks.Count is < 1 or > 100) throw new ArgumentException("تعداد ردیف‌ها باید بین ۱ و ۱۰۰ باشد.");
        var body = new StringBuilder();
        foreach (var block in blocks)
        {
            if (block is null || block.Text is null || block.Text.Length > 1000) throw new ArgumentException("متن ردیف نامعتبر است.");
            if (block.Kind == "text") body.Append("<div class='visual-text'>").Append(SafeText(block.Text)).Append("</div>");
            else if (block.Kind == "variable" && !string.IsNullOrEmpty(block.Key) && allowed.Contains(block.Key) && Regex.IsMatch(block.Key, "^[a-z0-9_]+$"))
                body.Append("<div class='visual-field'>").Append(SafeText(block.Text)).Append(" {{").Append(block.Key).Append("}}</div>");
            else throw new ArgumentException("متغیر یا نوع ردیف معتبر نیست.");
        }
        var data = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(blocks));
        return Prefix + data + "-->" + body;
    }

    public static bool TryRead(string? html, IReadOnlyCollection<string> allowed, out List<VisualTemplateBlock> blocks)
    {
        blocks = new();
        if (string.IsNullOrEmpty(html) || !html.StartsWith(Prefix, StringComparison.Ordinal)) return false;
        var end = html.IndexOf("-->", Prefix.Length, StringComparison.Ordinal);
        if (end < 0 || end - Prefix.Length > 200000) return false;
        try
        {
            var loaded = JsonSerializer.Deserialize<List<VisualTemplateBlock>>(Convert.FromBase64String(html[Prefix.Length..end]));
            if (loaded is null || Build(loaded, allowed) != html) return false;
            blocks = loaded;
            return true;
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException) { return false; }
    }
}

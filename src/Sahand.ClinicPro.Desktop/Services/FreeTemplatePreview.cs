using System.IO;
using System.Xml;
using System.Xml.Linq;
using Sahand.ClinicPro.Application.Features.Reporting.PrintTemplates;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>پیش‌نمایش نمونه محلی فقط برای بوم تأییدشده، بدون ذخیره یا تماس با سرور.</summary>
public static class FreeTemplatePreview
{
    /// <summary>فقط مقدار متنی نمونه را روی بوم نشان می‌دهد؛ جدول HTML به‌صورت برچسب باقی می‌ماند.</summary>
    public static bool TrySampleText(IReadOnlyDictionary<string, object?> sample, string key, out string text)
    {
        text = "";
        if (key.EndsWith("_html", StringComparison.OrdinalIgnoreCase) || !sample.TryGetValue(key, out var value) || value is not string s || string.IsNullOrWhiteSpace(s)) return false;
        text = s;
        return true;
    }

    /// <summary>استخراج محدود متن جدول نمونه؛ HTML روی بوم اجرا نمی‌شود.</summary>
    public static bool TrySampleTable(IReadOnlyDictionary<string, object?> sample, string key, out string[][] rows)
    {
        rows = Array.Empty<string[]>();
        if (!key.EndsWith("_html", StringComparison.OrdinalIgnoreCase) ||
            !sample.TryGetValue(key, out var value) || value is not string html || html.Length > 32768) return false;
        try
        {
            using var input = new StringReader(html);
            using var reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 32768 });
            var root = XDocument.Load(reader).Root;
            if (root?.Name.LocalName != "table") return false;
            var result = root.Descendants().Where(x => x.Name.LocalName == "tr").Take(21)
                .Select(row => row.Elements().Where(x => x.Name.LocalName is "th" or "td").Take(11).Select(x => x.Value).ToArray()).ToArray();
            if (result.Length is < 1 or > 20 || result.Any(row => row.Length is < 1 or > 10 || row.Any(cell => cell.Length > 1000))) return false;
            rows = result;
            return true;
        }
        catch (XmlException) { return false; }
    }

    public static string Create(string html, string type, string paper, string? accent, int fontSize, bool showLogo, IReadOnlyCollection<string> allowed, string orientation = "Portrait", string? fontFamily = null)
    {
        if (!FreeTemplateDesign.TryRead(html, allowed, out _)) throw new ArgumentException("بدنه بوم قابل تأیید نیست.");
        var sample = PrintTemplateRenderer.SampleData(type);
        sample.TryAdd("clinic_name", "درمانگاه نمونه");
        sample.TryAdd("clinic_address", "نشانی نمونه");
        sample.TryAdd("clinic_phone", "شماره نمونه");
        sample.TryAdd("print_date", "نمونه");
        var body = "<div style='border:2px solid #b00020;color:#b00020;padding:8px'>پیش‌نویس — داده نمونه، فاقد اعتبار و ذخیره‌نشده</div>" + PrintTemplateRenderer.Render(html, sample);
        return PrintTemplateRenderer.WrapDocument(body, "درمانگاه نمونه", "نشانی نمونه", "شماره نمونه", null,
            accent, fontSize, fontFamily, showLogo, paper, orientation).Replace("__PRINT_DATE__", "نمونه");
    }
}

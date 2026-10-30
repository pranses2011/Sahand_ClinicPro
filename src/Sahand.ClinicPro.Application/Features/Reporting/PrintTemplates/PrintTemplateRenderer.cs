using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Ganss.Xss;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Sahand.ClinicPro.Application.Features.Reporting.PrintTemplates;

/// <summary>
/// موتور رندر قالب چاپ — جایگزینی متغیرهای {{...}} + بسته‌بندی سند کامل (چرخه ۱۳، درخواست ۱-۶)
/// </summary>
public static class PrintTemplateRenderer
{
    /// <summary>الگوی متغیر {{نام_متغیر}}</summary>
    private static readonly Regex VarPattern = new(@"\{\{\s*([\w\.آ-ی]+)\s*\}\}",
        RegexOptions.Compiled);

    // فقط این سه متغیر قطعه HTML هستند؛ بقیه حتی با پسوند _html متن ساده‌اند.
    // قطعات جدول از کلاینت می‌آیند و نباید بتوانند اسکریپت، رویداد، تصویر
    // خارجی یا CSS قابل اجرا را به پیش‌نمایش حاوی اطلاعات بیمار تزریق کنند.
    private static readonly HashSet<string> TableVariables = new(StringComparer.Ordinal)
        { "items_html", "tests_html", "results_html" };
    private static readonly HtmlSanitizer TableSanitizer = CreateTableSanitizer();

    private static HtmlSanitizer CreateTableSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[] { "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption", "colgroup", "col", "span", "p", "br", "b", "strong", "i", "em" })
            sanitizer.AllowedTags.Add(tag);
        sanitizer.AllowedAttributes.Clear();
        foreach (var attribute in new[] { "class", "colspan", "rowspan", "border", "cellspacing", "cellpadding" })
            sanitizer.AllowedAttributes.Add(attribute);
        return sanitizer;
    }

    /// <summary>رنگ پیش‌فرض تأکیدی</summary>
    private const string DefaultAccent = "#16355E";

    /// <summary>
    /// متغیرها فقط در متن قالب مجازند؛ در صفت HTML، توضیح HTML، اسکریپت،
    /// استایل و محتوای template نباید جایگذاری شوند (زمینهٔ متفاوت کدگذاری).
    /// </summary>
    public static bool HasUnsafeVariablePlacement(string html)
    {
        var document = new HtmlParser().ParseDocument(html);
        return HasUnsafeNode(document);
    }

    private static bool HasUnsafeNode(INode node)
    {
        if (node is IComment comment && VarPattern.IsMatch(comment.Data)) return true;
        if (node is IElement element)
        {
            if (element.Attributes.Any(a => VarPattern.IsMatch(a.Name) || VarPattern.IsMatch(a.Value))) return true;
            if ((element.LocalName is "script" or "style" or "template") && VarPattern.IsMatch(element.InnerHtml)) return true;
        }
        return node.ChildNodes.Any(HasUnsafeNode);
    }

    /// <summary>جایگزینی متغیرها در بدنه HTML</summary>
    /// <param name="html">بدنه قالب</param>
    /// <param name="variables">مقادیر متغیرها</param>
    public static string Render(string html, IDictionary<string, object?> variables)
    {
        if (HasUnsafeVariablePlacement(html))
            throw new ArgumentException("متغیرهای قالب فقط در متن مجازند، نه در صفت HTML، استایل یا اسکریپت.", nameof(html));
        return VarPattern.Replace(html, m =>
        {
            var key = m.Groups[1].Value;
            if (!variables.TryGetValue(key, out var value) || value is null)
                return $"«{key}»"; // متغیر بی‌مقدار — در پیش‌نمایش مشخص بماند
            var text = value.ToString() ?? string.Empty;
            return TableVariables.Contains(key)
                ? TableSanitizer.Sanitize(text)
                : WebUtility.HtmlEncode(text);
        });
    }

    /// <summary>بسته‌بندی بدنه در سند HTML کامل با استایل چاپ RTL و سربرگ درمانگاه</summary>
    /// <param name="bodyHtml">بدنه رندرشده</param>
    /// <param name="clinicName">نام درمانگاه برای سربرگ</param>
    /// <param name="clinicAddress">نشانی</param>
    /// <param name="clinicPhone">تلفن</param>
    /// <param name="logoUrl">نشانی لوگو — اختیاری</param>
    /// <param name="accent">رنگ تأکیدی</param>
    /// <param name="fontSize">اندازه فونت پایه</param>
    /// <param name="fontFamily">خانواده فونت</param>
    /// <param name="showLogo">نمایش لوگو</param>
    /// <param name="paper">اندازه کاغذ</param>
    /// <param name="orientation">جهت صفحه؛ مرورگر و درایور ممکن است آن را نادیده بگیرند.</param>
    public static string WrapDocument(
        string bodyHtml,
        string clinicName,
        string? clinicAddress,
        string? clinicPhone,
        string? logoUrl,
        string? accent,
        int fontSize,
        string? fontFamily,
        bool showLogo,
        string paper = "A5",
        string orientation = "Portrait")
    {
        // این مقادیر داخل CSS قرار می‌گیرند و HTML encoding برای آن کافی نیست.
        // مقدار نامعتبر یا قدیمی به تنظیم امن پیش‌فرض برمی‌گردد.
        var accentColor = accent is not null && Regex.IsMatch(accent, @"^(#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})|[a-zA-Z]{1,32})$")
            ? accent : DefaultAccent;
        var safeFont = fontFamily is not null && Regex.IsMatch(fontFamily, @"^[\p{L}\p{N} _,\-]{1,100}$")
            ? fontFamily : "Vazirmatn";
        var font = $"{safeFont}, Tahoma, sans-serif";
        var safeLogo = Uri.TryCreate(logoUrl, UriKind.Absolute, out var logoUri) &&
            (logoUri.Scheme == Uri.UriSchemeHttp || logoUri.Scheme == Uri.UriSchemeHttps)
            ? WebUtility.HtmlEncode(logoUri.AbsoluteUri) : null;
        // ═══ چرخه سی‌ویکم — پشتیبانی کامل اندازه‌های کاغذ در پیش‌نمایش/چاپ:
        // استانداردها (۹۶dpi) + فیش حرارتی ۸۰/۵۸ میلی‌متری + قالب سفارشی
        // «Custom:عرضxارتفاع» (میلی‌متر). مقیاس: ۹۶px ≈ ۲۵.۴mm → ۳.۷۸px بر mm
        // (سازگار با رفتار پیشین: مقادیر مجهول همچنان به A5 می‌روند) ═══
        var landscape = string.Equals(orientation, "Landscape", StringComparison.OrdinalIgnoreCase) ? LandscapePaper(paper) : null;
        var width = landscape is { } spec ? $"{spec.WidthPx}px" : ResolvePaperWidthPx(paper);
        var pageRule = landscape is { } page ? $"@page {{ size: {page.CssSize}; }}" : "";

        var sb = new StringBuilder(@"<!DOCTYPE html>
<html lang=""fa"" dir=""rtl"">
<head>
<meta charset=""utf-8"" />
<style>
").Append(pageRule).Append(@"
  * { margin:0; padding:0; box-sizing:border-box; }
  body { font-family:").Append(font).Append(@"; font-size:").Append(fontSize).Append(@"px; color:#1F2A3D;
         background:#F0F2F5; padding:16px; }
  .sheet { width:").Append(width).Append(@"; margin:0 auto; background:#fff; padding:28px 26px;
            border-radius:6px; box-shadow:0 2px 14px rgba(22,53,94,.12); position:relative; overflow:hidden; }
  .sheet::before { content:''; position:absolute; inset:0 0 auto 0; height:5px; background:").Append(accentColor).Append(@"; }
  .hdr { display:flex; align-items:center; gap:12px; padding-bottom:12px; border-bottom:2px solid ").Append(accentColor).Append(@"; margin-bottom:16px; }
  .hdr .logo { width:54px; height:54px; border-radius:12px; object-fit:contain; }
  .hdr .logo-ph { width:54px; height:54px; border-radius:12px; background:").Append(accentColor).Append(@";
      color:#fff; display:flex; align-items:center; justify-content:center; font-size:26px; font-weight:800; }
  .hdr .nm { font-size:17px; font-weight:800; color:").Append(accentColor).Append(@"; }
  .hdr .ad { font-size:11px; color:#5B708C; margin-top:3px; }
  .ftr { margin-top:22px; padding-top:10px; border-top:1px dashed #C7D2E0; font-size:10.5px;
         color:#8296B3; display:flex; justify-content:space-between; }
  table.pt { width:100%; border-collapse:collapse; }
  table.pt th { background:").Append(accentColor).Append(@"; color:#fff; font-size:11.5px; padding:7px 9px; text-align:right; }
  table.pt td { border-bottom:1px solid #E5EAF2; font-size:12px; padding:6px 9px; text-align:right; }
  .sig { margin-top:26px; display:flex; justify-content:space-between; }
  .sig .box { text-align:center; font-size:11px; color:#5B708C; }
  .sig .line { width:150px; border-bottom:1px solid #9DB0C9; height:26px; margin:0 auto 4px; }
  @media print { body { background:#fff; padding:0; } .sheet { box-shadow:none; border-radius:0; width:auto; } }
</style>
</head>
<body>
<div class=""sheet"">
  <div class=""hdr"">");
        if (showLogo)
        {
            sb.Append(safeLogo is null
                ? @"<div class=""logo-ph"">س</div>"
                : $@"<img class=""logo"" src=""{safeLogo}"" alt="""" />");
        }
        sb.Append(@"<div>
      <div class=""nm"">").Append(WebUtility.HtmlEncode(clinicName)).Append(@"</div>");
        if (!string.IsNullOrWhiteSpace(clinicAddress) || !string.IsNullOrWhiteSpace(clinicPhone))
            sb.Append(@"      <div class=""ad"">")
                .Append(WebUtility.HtmlEncode(string.Join(" • ", new[] { clinicAddress, clinicPhone }.Where(s => !string.IsNullOrWhiteSpace(s)))))
                .Append("</div>");
        sb.Append(@"
    </div>
  </div>
  <!-- ═══ بدنه قالب ═══ -->
").Append(bodyHtml).Append(@"
  <div class=""ftr"">
    <span>سامانه جامع مدیریت درمانگاه سهند</span>
    <span>تاریخ چاپ: __PRINT_DATE__</span>
  </div>
</div>
</body>
</html>");
        return sb.ToString();
    }

    /// <summary>
    /// تبدیل اندازهٔ کاغذ به عرض پیکسلی برگه در رزولوشن ۹۶dpi — چرخه سی‌ویکم.
    /// استانداردها: A3=1123 / A4=794 / A5=559 / A6=397 / B5=691 / Letter=816 / Legal=816
    /// فیش حرارتی: Thermal80=302 / Thermal58=219 (عرض مفید کاغذ ۸۰ و ۵۸ میلی‌متری)
    /// سفارشی: «Custom:عرضxارتفاع» بر حسب میلی‌متر با مقیاس ۹۶/۲۵.۴ ≈ ۳.۷۸ پیکسل بر میلی‌متر
    /// </summary>
    /// <param name="paper">کلید اندازهٔ کاغذ</param>
    private static string ResolvePaperWidthPx(string? paper)
    {
        // مطابقت بدون توجه به بزرگی/کوچکی حروف و فضاهای اضافی
        var key = paper?.Trim();
        if (string.IsNullOrEmpty(key)) return "559px"; // پیش‌فرض A5

        switch (key.ToUpperInvariant())
        {
            case "A3": return "1123px";
            case "A4": return "794px";
            case "A6": return "397px";
            case "B5": return "691px";
            case "LETTER": return "816px";
            case "LEGAL": return "816px";
            case "THERMAL80": return "302px"; // ۸۰mm × ۳.۷۸ ≈ ۳۰۲px — پرینترهای کاسه‌ای متداول
            case "THERMAL58": return "219px"; // ۵۸mm × ۳.۷۸ ≈ ۲۱۹px — فیش‌خوان‌های کوچک
            default:
                // قالب سفارشی — Custom:100x180 یا Custom:100×180 (میلی‌متر)
                var m = Regex.Match(key, @"^Custom:\s*(\d{1,4})\s*[x×]\s*(\d{1,4})", RegexOptions.IgnoreCase);
                if (m.Success && int.TryParse(m.Groups[1].Value, out var mm))
                {
                    // محدودهٔ منطقی ۲۰ تا ۱۰۰۰ میلی‌متر (خارج از آن → A5)
                    var px = (int)Math.Round(mm * 96.0 / 25.4);
                    if (px is >= 75 and <= 3779) return $"{px}px";
                }
                return "559px"; // نام آشنا → A5 (رفتار سازگار با قبل)
        }
    }

    /// <summary>مشخصات کاغذ افقی محدود به اندازه‌های شناخته‌شده؛ فیش پیوسته جهت ثابت ندارد.</summary>
    public static (int WidthPx, string CssSize)? LandscapePaper(string? paper)
    {
        var key = paper?.Trim().ToUpperInvariant();
        var standard = key switch
        {
            "A3" => (1587, "A3 landscape"), "A4" => (1123, "A4 landscape"),
            "A5" => (794, "A5 landscape"), "A6" => (559, "A6 landscape"),
            "B5" => (971, "B5 landscape"), "LETTER" => (1056, "letter landscape"),
            "LEGAL" => (1344, "legal landscape"), _ => (0, "")
        };
        if (standard.Item1 > 0) return standard;
        if (key is null) return null;
        var match = Regex.Match(key, @"^CUSTOM:\s*(\d{1,4})\s*[X×]\s*(\d{1,4})\s*(MM)?$", RegexOptions.CultureInvariant);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var w) || !int.TryParse(match.Groups[2].Value, out var h) ||
            w is < 20 or > 1000 || h is < 20 or > 1000) return null;
        return ((int)Math.Round(h * 96.0 / 25.4), $"{h}mm {w}mm");
    }

    /// <summary>داده نمونه برای پیش‌نمایش زنده هر نوع سند</summary>
    public static Dictionary<string, object?> SampleData(string typeKey) => typeKey switch
    {
        "Prescription" => new Dictionary<string, object?>
        {
            ["patient_name"] = "سارا محمدی",
            ["patient_age"] = "۳۴ سال",
            ["patient_national_code"] = "۴۵۸۰۰۲۳۴۱",
            ["doctor_name"] = "دکتر رضا کریمی — متخصص داخلی",
            ["visit_date"] = "۱۴۰۵/۰۷/۱۴",
            ["prescription_code"] = "RX-10245",
            ["items_html"] = "<table class='pt'><tr><th>ردیف</th><th>دارو</th><th>دوز</th><th>مدت</th></tr>" +
                              "<tr><td>۱</td><td>استامینوفن ۵۰۰</td><td>هر ۸ ساعت</td><td>۵ روز</td></tr>" +
                              "<tr><td>۲</td><td>اموکسی‌سیلین ۵۰۰</td><td>هر ۸ ساعت</td><td>۷ روز</td></tr></table>",
            ["notes"] = "در صورت ادامه تب بعد از ۴۸ ساعت مراجعه شود"
        },
        "LabOrder" => new Dictionary<string, object?>
        {
            ["patient_name"] = "علی رضایی",
            ["doctor_name"] = "دکتر مریم احمدی",
            ["order_number"] = "LAB-8821",
            ["order_date"] = "۱۴۰۵/۰۷/۱۴",
            ["tests_html"] = "<table class='pt'><tr><th>#</th><th>آزمون</th></tr><tr><td>۱</td><td>CBC کامل</td></tr><tr><td>۲</td><td>قند خون ناشتا</td></tr></table>",
            ["clinical_info"] = "خستگی مزمن و کم‌خونی"
        },
        "LabResult" => new Dictionary<string, object?>
        {
            ["patient_name"] = "علی رضایی",
            ["order_number"] = "LAB-8821",
            ["result_date"] = "۱۴۰۵/۰۷/۱۵",
            ["results_html"] = "<table class='pt'><tr><th>آزمون</th><th>نتیجه</th><th>محدوده</th></tr>" +
                               "<tr><td>Hb</td><td>10.2</td><td>13-17</td></tr><tr><td>FBS</td><td>95</td><td>70-100</td></tr></table>",
            ["verified_by"] = "کارشناس آزمایشگاه — تأیید متخصص"
        },
        "ImagingOrder" => new Dictionary<string, object?>
        {
            ["patient_name"] = "حسن نوری",
            ["modality"] = "رادیوگرافی قفسه سینه",
            ["order_number"] = "IMG-1204",
            ["order_date"] = "۱۴۰۵/۰۷/۱۴",
            ["clinical_info"] = "سرفه درد و سرفه مزمن"
        },
        "AppointmentTicket" => new Dictionary<string, object?>
        {
            ["patient_name"] = "زهرا موسوی",
            ["doctor_name"] = "دکتر رضا کریمی",
            ["appointment_date"] = "۱۴۰۵/۰۷/۱۶",
            ["appointment_time"] = "۱۰:۳۰",
            ["queue_number"] = "۱۲",
            ["tracking_code"] = "APT-556677"
        },
        "ReceptionReceipt" => new Dictionary<string, object?>
        {
            ["patient_name"] = "بیمار نمونه",
            ["patient_national_code"] = "۰۰۰۰۰۰۰۰۰۰", ["patient_mobile"] = "۰۹۰۰۰۰۰۰۰۰۰",
            ["reception_date"] = "۱۴۰۵/۰۷/۱۴", ["reception_time"] = "۱۰:۳۰",
            ["reception_code"] = "RCP-3344", ["queue_number"] = "RCP-3344",
            ["service_name"] = "ویزیت نمونه", ["performer_name"] = "پزشک نمونه",
            ["room_name"] = "اتاق نمونه", ["insurance_name"] = "بیمه نمونه",
            ["items_html"] = "<table class='pt'><tr><th>شرح</th><th>تعداد</th><th>مبلغ</th></tr><tr><td>ویزیت نمونه</td><td>۱</td><td>۵۰۰٬۰۰۰ ریال</td></tr></table>",
            ["items_total"] = "۵۰۰٬۰۰۰ ریال", ["discount"] = "۰ ریال", ["tax"] = "۰ ریال",
            ["insurance_share"] = "۵۰٬۰۰۰ ریال", ["patient_share"] = "۴۵۰٬۰۰۰ ریال",
            ["amount"] = "۴۵۰٬۰۰۰ ریال", ["payable"] = "۴۵۰٬۰۰۰ ریال", ["paid_amount"] = "۴۵۰٬۰۰۰ ریال",
            ["invoice_number"] = "INV-123", ["payment_method"] = "نقدی"
        },
        "MedicalCertificate" => new Dictionary<string, object?>
        {
            ["patient_name"] = "مریم احمدی",
            ["doctor_name"] = "دکتر رضا کریمی",
            ["issue_date"] = "۱۴۰۵/۰۷/۱۴",
            ["content"] = "احتراماً سلامت کامل جناب‌ها/سرکارم جهت ادامه فعالیت تأیید می‌گردد.",
            ["duration"] = "سه روز استراحت"
        },
        "PayrollSlip" => new Dictionary<string, object?>
        {
            ["employee_name"] = "سمیه رحیمی",
            ["period"] = "مهر ۱۴۰۵",
            ["base_salary"] = "۲۱۰٬۰۰۰٬۰۰۰ ریال",
            ["overtime_pay"] = "۸٬۴۰۰٬۰۰۰ ریال",
            ["insurance"] = "۱۵٬۳۳۰٬۰۰۰ ریال",
            ["tax"] = "۳٬۶۰۰٬۰۰۰ ریال",
            ["net_pay"] = "۱۹۹٬۴۷۰٬۰۰۰ ریال"
        },
        _ => new Dictionary<string, object?> // Invoice و پیش‌فرض
        {
            ["invoice_code"] = "INV-2026-1188",
            ["invoice_date"] = "۱۴۰۵/۰۷/۱۴",
            ["patient_name"] = "سارا محمدی",
            ["insurance_name"] = "تأمین اجتماعی",
            ["items_html"] = "<table class='pt'><tr><th>خدمت</th><th>تعداد</th><th>مبلغ (ریال)</th></tr>" +
                             "<tr><td>ویزیت متخصص</td><td>۱</td><td>۴۵۰٬۰۰۰</td></tr>" +
                             "<tr><td>آزمایش CBC</td><td>۱</td><td>۳۲۰٬۰۰۰</td></tr></table>",
            ["subtotal"] = "۷۷۰٬۰۰۰ ریال",
            ["discount"] = "۵۰٬۰۰۰ ریال",
            ["insurance_share"] = "۴۰۰٬۰۰۰ ریال",
            ["patient_share"] = "۳۲۰٬۰۰۰ ریال",
            ["total"] = "۷۲۰٬۰۰۰ ریال", ["tax"] = "۰ ریال", ["paid"] = "۱۰۰٬۰۰۰ ریال",
            ["remaining"] = "۲۲۰٬۰۰۰ ریال", ["payment_status"] = "پرداخت جزئی"
        }
    };

    /// <summary>فهرست متغیرهای هر نوع سند برای پالت طراح</summary>
    public static Dictionary<string, string[]> VariablesOf() => new()
    {
        ["Invoice"] = new[] { "invoice_code", "invoice_date", "patient_name", "insurance_name", "items_html", "subtotal", "discount", "insurance_share", "patient_share", "total", "tax", "paid", "remaining", "payment_status" },
        ["Prescription"] = new[] { "patient_name", "patient_age", "patient_national_code", "doctor_name", "visit_date", "prescription_code", "items_html", "notes" },
        ["LabOrder"] = new[] { "patient_name", "doctor_name", "order_number", "order_date", "tests_html", "clinical_info" },
        ["LabResult"] = new[] { "patient_name", "order_number", "result_date", "results_html", "verified_by" },
        ["ImagingOrder"] = new[] { "patient_name", "modality", "order_number", "order_date", "clinical_info" },
        ["AppointmentTicket"] = new[] { "patient_name", "doctor_name", "appointment_date", "appointment_time", "queue_number", "tracking_code" },
        ["ReceptionReceipt"] = new[] { "patient_name", "patient_national_code", "patient_mobile", "queue_number", "reception_code", "reception_date", "reception_time", "service_name", "performer_name", "room_name", "insurance_name", "items_html", "items_total", "insurance_share", "discount", "tax", "patient_share", "payable", "amount", "paid_amount", "payment_method", "invoice_number", "clinic_name", "clinic_phone", "clinic_address", "print_date" },
        ["MedicalCertificate"] = new[] { "patient_name", "doctor_name", "issue_date", "content", "duration" },
        ["PayrollSlip"] = new[] { "employee_name", "period", "base_salary", "overtime_pay", "insurance", "tax", "net_pay" }
    };
}

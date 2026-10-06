using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Reporting;

/// <summary>
/// قالب چاپ سفارشی — طراح آنلاین سربرگ/فاکتور/نسخه با متغیرهای جایگاه‌دار
/// هر درمانگاه می‌تواند چند قالب برای هر نوع سند داشته باشد (چرخه ۱۳، درخواست ۱-۶)
/// </summary>
public class PrintTemplate : AuditableEntity, ISoftDeletable
{
    /// <summary>نام نمایشی قالب — مثل «فاکتور رسمی با لوگو»</summary>
    public string TemplateName { get; set; } = null!;

    /// <summary>نوع سند — ۹ نوع</summary>
    public PrintTemplateType Type { get; set; } = PrintTemplateType.Invoice;

    /// <summary>توضیح اختیاری</summary>
    public string? Description { get; set; }

    /// <summary>بدنه HTML با متغیرهای {{نام_متغیر}}</summary>
    public string HtmlBody { get; set; } = null!;

    /// <summary>اندازه کاغذ — A4/A5/A6</summary>
    public string PaperSize { get; set; } = "A5";

    /// <summary>جهت صفحه — Portrait یا Landscape</summary>
    public string Orientation { get; set; } = "Portrait";

    /// <summary>اندازه فونت پایه (پیکسل)</summary>
    public int FontSize { get; set; } = 13;

    /// <summary>خانواده فونت — مثل Vazirmatn</summary>
    public string? FontFamily { get; set; }

    /// <summary>نمایش لوگوی درمانگاه در سربرگ</summary>
    public bool ShowClinicLogo { get; set; } = true;

    /// <summary>رنگ تأکیدی — کد هگز مثل #16355E</summary>
    public string? AccentColor { get; set; }

    /// <summary>فعال — قالب‌های غیرفعال فهرست چاپ نمی‌شوند</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>پیش‌فرض این نوع — هنگام چاپ سند همان نوع استفاده می‌شود</summary>
    public bool IsDefault { get; set; } = false;
}

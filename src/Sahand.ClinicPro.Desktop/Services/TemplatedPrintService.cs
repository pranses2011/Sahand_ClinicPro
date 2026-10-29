using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>نتیجه قابل تشخیص مسیر قالب؛ مرورگر نتیجه دیالوگ چاپ را گزارش نمی‌کند.</summary>
public enum TemplatePrintOutcome { Fallback, Printed, NotPrinted, HtmlWindowClosed }

/// <summary>
/// اعمال «واقعی» قالب‌های چاپ در سراسر برنامه — بازطراحی تأییدشدهٔ مالک:
/// «اعمال شدن این قالب‌ها رو هم در سراسر برنامه بررسی بکن چون اعمال نمیشن».
/// قرارداد (html_first): هنگام چاپ هر سند، اول قالب پیش‌فرضِ فعال همان
/// نوع از سرور گرفته و با متغیرهای واقعی همان سند رندر می‌شود؛ اگر قالبی
/// نبود یا سرور در دسترس نبود، false برمی‌گردد تا مسیر چاپ داخلی فعلی
/// (چیدمان FlowDocument هر چاپگر) مثل قبل اجرا شود — هیچ چاپی از دست نمی‌رود.
/// </summary>
public static class TemplatedPrintService
{
    /// <summary>پاکت پاسخ رندر قالب از سرور</summary>
    public sealed class RenderEnvelope
    {
        public bool IsSuccess { get; set; }
        public RenderData? Data { get; set; }
    }

    /// <summary>دادهٔ رندر — HTML آمادهٔ چاپ + مشخصات قالب استفاده‌شده</summary>
    public sealed class RenderData
    {
        public string? Html { get; set; }
        public long TemplateId { get; set; }
        public string? TemplateName { get; set; }
        /// <summary>استفاده از چاپگر داخلی قبض اصلی پذیرش.</summary>
        public bool UseOriginalReceptionReceipt { get; set; }
    }

    /// <summary>
    /// تلاش برای چاپ سند با قالب پیش‌فرض. نتیجه، مسیر انتخابی و امکان
    /// تشخیص ارسال به چاپگر را از انصراف و بسته شدن مرورگر جدا می‌کند.
    /// </summary>
    /// <param name="api">کلاینت API (از ویومدل صفحهٔ چاپ‌کننده)</param>
    /// <param name="templateType">نوع سند — مثل ReceptionReceipt یا LabResult</param>
    /// <param name="variables">متغیرهای واقعی سند (کلیدهای پالت قالب)</param>
    /// <param name="title">عنوان پنجرهٔ چاپ</param>
    /// <param name="printOriginalReceipt">چاپگر داخلی با نتیجهٔ ارسال به چاپگر</param>
    public static async Task<TemplatePrintOutcome> TryPrintAsync(IApiClient api, string templateType,
        Dictionary<string, object?> variables, string title, Func<bool>? printOriginalReceipt = null)
    {
        RenderEnvelope? response;
        try
        {
            response = await api.PostAsync<RenderEnvelope>(
                $"print-templates/render/{templateType}", new { variables });
        }
        catch
        {
            return TemplatePrintOutcome.Fallback;
        }

        if (response?.Data?.UseOriginalReceptionReceipt == true && printOriginalReceipt is not null)
            return printOriginalReceipt() ? TemplatePrintOutcome.Printed : TemplatePrintOutcome.NotPrinted;
        var html = response?.Data?.Html;
        if (string.IsNullOrWhiteSpace(html)) return TemplatePrintOutcome.Fallback;

        // پس از باز شدن مسیر انتخابی، خطا یا انصراف نباید باعث چاپ دوم شود.
        var window = new HtmlPrintWindow(html, title)
        {
            Owner = System.Windows.Application.Current?.Windows
                .Cast<System.Windows.Window>().FirstOrDefault(w => w.IsActive)
                ?? System.Windows.Application.Current?.MainWindow
        };
        window.ShowDialog();
        return TemplatePrintOutcome.HtmlWindowClosed;
    }

    /// <summary>ساخت جدول HTML اقلام — برای متغیر items_html قالب‌ها</summary>
    /// <param name="rows">ردیف‌های (شرح، تعداد، مبلغ) با متن آمادهٔ نمایش</param>
    public static string ItemsTableHtml(IEnumerable<(string Name, string Quantity, string Amount)> rows)
    {
        var body = string.Join(string.Empty, rows.Select(r =>
            $"<tr><td>{System.Net.WebUtility.HtmlEncode(r.Name)}</td><td>{System.Net.WebUtility.HtmlEncode(r.Quantity)}</td><td>{System.Net.WebUtility.HtmlEncode(r.Amount)}</td></tr>"));
        return "<table class='pt' border='1' cellspacing='0' cellpadding='5'>"
            + "<tr><th>شرح</th><th>تعداد</th><th>مبلغ</th></tr>" + body + "</table>";
    }
}

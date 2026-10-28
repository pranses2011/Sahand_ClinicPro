using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.Services;

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
    /// تلاش برای چاپ سند با قالب پیش‌فرض سرور. true یعنی چاپ قالبی انجام
    /// شد و مسیر داخلی نباید دوباره چاپ کند.
    /// </summary>
    /// <param name="api">کلاینت API (از ویومدل صفحهٔ چاپ‌کننده)</param>
    /// <param name="templateType">نوع سند — مثل ReceptionReceipt یا LabResult</param>
    /// <param name="variables">متغیرهای واقعی سند (کلیدهای پالت قالب)</param>
    /// <param name="title">عنوان پنجرهٔ چاپ</param>
    public static async Task<bool> TryPrintAsync(IApiClient api, string templateType,
        Dictionary<string, object?> variables, string title, Action? printOriginalReceipt = null)
    {
        try
        {
            var response = await api.PostAsync<RenderEnvelope>(
                $"print-templates/render/{templateType}", new { variables });
            if (response?.Data?.UseOriginalReceptionReceipt == true && printOriginalReceipt is not null)
            {
                // حتی انصراف از پیش‌نمایش هم نباید به چاپ دوم منجر شود.
                printOriginalReceipt();
                return true;
            }
            var html = response?.Data?.Html;
            if (string.IsNullOrWhiteSpace(html)) return false;

            var window = new HtmlPrintWindow(html, title)
            {
                Owner = System.Windows.Application.Current?.Windows
                    .Cast<System.Windows.Window>().FirstOrDefault(w => w.IsActive)
                    ?? System.Windows.Application.Current?.MainWindow
            };
            window.ShowDialog();
            return true;
        }
        catch
        {
            // هر خطایی در مسیر قالبی → بازگشت آرام به چاپ داخلی (قانون «چاپ از دست نرود»)
            return false;
        }
    }

    /// <summary>ساخت جدول HTML اقلام — برای متغیر items_html قالب‌ها</summary>
    /// <param name="rows">ردیف‌های (شرح، تعداد، مبلغ) با متن آمادهٔ نمایش</param>
    public static string ItemsTableHtml(IEnumerable<(string Name, string Quantity, string Amount)> rows)
    {
        var body = string.Join(string.Empty, rows.Select(r =>
            $"<tr><td>{r.Name}</td><td>{r.Quantity}</td><td>{r.Amount}</td></tr>"));
        return "<table style='width:100%;border-collapse:collapse' border='1' cellspacing='0' cellpadding='5'>"
            + "<tr><th>شرح</th><th>تعداد</th><th>مبلغ</th></tr>" + body + "</table>";
    }
}

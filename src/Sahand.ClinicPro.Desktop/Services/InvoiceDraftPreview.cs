using System.Globalization;
using System.Net;
using System.Text;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>پیش‌نویس محاسبه‌شده، نه فاکتور صادرشده یا رسید وجه.</summary>
public static class InvoiceDraftPreview
{
    private static string Escape(string? value) => WebUtility.HtmlEncode(value ?? "");
    private static string Number(decimal value) => Money.ToPersianDigits(value.ToString("#,0.##", CultureInfo.InvariantCulture));

    /// <summary>فقط داده‌های درخواست و جمع محاسبه‌شده سرور؛ واحد ثابت ریال است.</summary>
    public static string Build(CreateInvoiceDto request, InvoicePreviewDto quote, string patientName)
    {
        var rows = new StringBuilder();
        foreach (var item in request.Items)
            rows.Append("<tr><td>").Append(Escape(item.Description)).Append("</td><td>")
                .Append(Number(item.Quantity)).Append("</td><td>").Append(Number(item.UnitPrice))
                .Append("</td><td>").Append(Number(item.DiscountPercent)).Append("٪</td></tr>");
        return "<!doctype html><html lang='fa' dir='rtl'><head><meta charset='utf-8'><style>" +
            "body{font-family:Tahoma,sans-serif;margin:28px;color:#172b3c;font-size:13px}h1,.warning{color:#a51919}" +
            "h1{font-size:22px}table{width:100%;border-collapse:collapse;margin-top:20px}" +
            "td,th{border:1px solid #777;padding:7px;text-align:right}th{background:#eee}.totals{margin-top:18px;line-height:2}" +
            ".warning{font-weight:bold;border:2px solid #a51919;padding:10px}</style></head><body>" +
            "<h1>پیش‌نویس محاسبه فاکتور — صادر نشده</h1>" +
            "<div class='warning'>این برگه فاکتور صادرشده یا رسید پرداخت نیست و ثبت یا دریافت وجه را تأیید نمی‌کند.</div>" +
            "<p>بیمار: " + Escape(patientName) + " — شناسه: " + Number(request.PatientId) + "</p>" +
            "<p>مبالغ به ریال؛ محاسبه سرور، معتبر فقط تا زمان تغییر داده‌ها یا تعرفه.</p>" +
            "<table><thead><tr><th>شرح</th><th>تعداد</th><th>قیمت واحد (ریال)</th><th>درصد تخفیف</th></tr></thead><tbody>" + rows + "</tbody></table>" +
            "<div class='totals'>جمع: " + Number(quote.SubTotal) + " ریال<br>تخفیف: " + Number(quote.DiscountAmount) +
            " ریال<br>سهم بیمه: " + Number(quote.InsuranceShare) + " ریال<br>مالیات: " + Number(quote.Tax) +
            " ریال<br><strong>قابل پرداخت بیمار: " + Number(quote.PatientPayable) + " ریال</strong></div>" +
            "<p class='warning'>پیش‌نویس است — فاکتور و پرداخت هنوز تأیید نشده‌اند.</p></body></html>";
    }
}

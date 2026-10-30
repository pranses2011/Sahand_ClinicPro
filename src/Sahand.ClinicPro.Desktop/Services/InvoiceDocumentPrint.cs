using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>چاپ داده ثبت‌شده فاکتور، بدون ادعای رسید بودن یا تسویه.</summary>
public static class InvoiceDocumentPrint
{
    private static string Escape(string? s) => WebUtility.HtmlEncode(s ?? "");
    private static string Number(decimal value) => Money.ToPersianDigits(value.ToString("#,0.##", CultureInfo.InvariantCulture));
    private static string Amount(decimal value) => Number(value) + " ریال";
    private static string PaymentText(PaymentStatus status) => status switch
    {
        PaymentStatus.Unpaid => "پرداخت‌نشده", PaymentStatus.PartiallyPaid => "پرداخت جزئی",
        PaymentStatus.Paid => "پرداخت‌شده", PaymentStatus.Overpaid => "پرداخت اضافه", _ => "نامشخص"
    };
    public static bool Printable(InvoiceDetailDto? data, long id) => data is not null && data.Id == id && id > 0 &&
        data.PatientId > 0 && !string.IsNullOrWhiteSpace(data.PatientName) && data.Items is { Count: > 0 } &&
        data.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid or InvoiceStatus.Paid && Enum.IsDefined(data.PaymentStatus);

    /// <summary>متغیرها فقط از جزئیات دریافت‌شده و اقلام آن ساخته می‌شوند.</summary>
    public static Dictionary<string, object?> TemplateVariables(InvoiceDetailDto data)
    {
        var insurance = data.Items.Sum(i => i.InsuranceShare);
        return new Dictionary<string, object?>
        {
            ["invoice_code"] = data.InvoiceNumber ?? data.Id.ToString(CultureInfo.InvariantCulture),
            ["invoice_date"] = data.DateShamsi, ["patient_name"] = data.PatientName,
            ["insurance_name"] = data.InsuranceCompanyId is null ? "بدون بیمه اعمال‌شده" :
                string.IsNullOrWhiteSpace(data.InsuranceName) ? $"شناسه بیمه {data.InsuranceCompanyId} (نام ناموجود)" : data.InsuranceName,
            ["items_html"] = TemplatedPrintService.ItemsTableHtml(data.Items.Select(i =>
                (i.Description ?? "", Number(i.Quantity), Amount(i.TotalPrice)))),
            ["subtotal"] = Amount(data.SubTotal), ["discount"] = Amount(data.DiscountAmount),
            ["insurance_share"] = Amount(insurance), ["patient_share"] = Amount(data.TotalAmount),
            ["total"] = Amount(data.SubTotal - data.DiscountAmount + data.Tax),
            ["tax"] = Amount(data.Tax), ["paid"] = Amount(data.PaidAmount),
            ["remaining"] = Amount(data.RemainingAmount), ["payment_status"] = PaymentText(data.PaymentStatus)
        };
    }

    private static string Header(InvoiceDetailDto data) =>
        "<div style='border:2px solid #222;padding:10px;margin:10px 0;font:14px Tahoma,sans-serif;direction:rtl;color:#111;background:#fff'>" +
        "<strong>فاکتور ثبت‌شده — این برگه رسید پرداخت نیست</strong><br>" +
        "شماره: " + Escape(data.InvoiceNumber ?? data.Id.ToString(CultureInfo.InvariantCulture)) +
        " — شناسه: " + Number(data.Id) + " — بیمار: " + Escape(data.PatientName) +
        " (شناسه " + Number(data.PatientId) + ")<br>وضعیت پرداخت در زمان دریافت: " + Escape(PaymentText(data.PaymentStatus)) +
        " — قابل پرداخت بیمار: " + Amount(data.TotalAmount) + " — پرداخت‌شده: " + Amount(data.PaidAmount) +
        " — مانده: " + Amount(data.RemainingAmount) + "</div>";

    /// <summary>افزودن اطلاعات حیاتی به قالب؛ قالب فاقد body قابل استفاده نیست.</summary>
    public static string? WithRequiredHeader(string? html, InvoiceDetailDto data)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var body = Regex.Match(html, @"<body\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return body.Success ? html.Insert(body.Index + body.Length, Header(data)) : null;
    }

    /// <summary>جایگزین خوانا در نبود قالب فعال؛ ارقام واقعی فاکتور به ریال‌اند.</summary>
    public static string Fallback(InvoiceDetailDto data)
    {
        var v = TemplateVariables(data);
        return "<!doctype html><html lang='fa' dir='rtl'><head><meta charset='utf-8'></head><body style='font-family:Tahoma'>" +
            Header(data) + "<h2>جزئیات فاکتور</h2><p>تاریخ: " + Escape(data.DateShamsi) + "</p>" + v["items_html"] +
            "<p>جمع: " + v["subtotal"] + " — تخفیف: " + v["discount"] + " — بیمه: " + v["insurance_share"] +
            " — مالیات: " + v["tax"] + "</p></body></html>";
    }
}

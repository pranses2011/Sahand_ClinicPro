using System.Net;
using System.Text;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>متغیرهای واقعی برگهٔ نتیجه؛ اطلاعات ناموجود به‌صورت حدسی پر نمی‌شوند.</summary>
public static class LabResultTemplatePrint
{
    public static Dictionary<string, object?> Variables(LabResultViewModel.LabOrderDetail detail, long orderId)
    {
        static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
        var table = new StringBuilder("<table class='pt'><tr><th>آزمایش</th><th>نتیجه</th><th>واحد</th><th>محدوده مرجع</th><th>وضعیت</th><th>یادداشت</th></tr>");
        foreach (var item in detail.Items ?? new())
        {
            var value = !string.IsNullOrWhiteSpace(item.Result) ? item.Result
                : item.ResultNumeric is { } number ? LabNumericResult.Format(number) : "در انتظار";
            var status = item.IsAbnormal ? "غیرعادی" :
                string.IsNullOrWhiteSpace(item.Result) && item.ResultNumeric is null ? "—" : "بدون هشدار ثبت‌شده";
            table.Append("<tr><td>").Append(E(item.TestName)).Append("</td><td>").Append(E(value))
                .Append("</td><td>").Append(E(item.Unit)).Append("</td><td>").Append(E(item.PatientRangeText ?? item.NormalRange))
                .Append("</td><td>").Append(E(status)).Append("</td><td>").Append(E(item.Comment)).Append("</td></tr>");
        }
        table.Append("</table>");
        return new Dictionary<string, object?>
        {
            ["patient_name"] = detail.PatientName,
            ["doctor_name"] = detail.DoctorName,
            ["order_number"] = detail.OrderNumber ?? orderId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["order_date"] = detail.DateShamsi,
            // زمان نتیجه و نام تأییدکننده در قرارداد فعلی برگردانده نمی‌شوند.
            ["result_date"] = string.Empty,
            ["verified_by"] = string.Empty,
            ["results_html"] = table.ToString()
        };
    }

    public static async Task<(TemplatePrintOutcome Outcome, bool UsedFallback)> PrintAsync(
        LabResultViewModel.LabOrderDetail detail, long orderId,
        Func<Dictionary<string, object?>, Task<TemplatePrintOutcome>> render, Func<bool> printInternal)
    {
        var outcome = await render(Variables(detail, orderId));
        if (outcome != TemplatePrintOutcome.Fallback) return (outcome, false);
        return (printInternal() ? TemplatePrintOutcome.Printed : TemplatePrintOutcome.NotPrinted, true);
    }
}

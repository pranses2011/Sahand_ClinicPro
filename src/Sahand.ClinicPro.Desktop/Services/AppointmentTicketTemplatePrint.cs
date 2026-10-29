using System.Globalization;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>داده ثبت‌شدهٔ نوبت برای قالب چاپ؛ شماره صف اختیاری است.</summary>
public sealed record AppointmentTicketData(string PatientName, string DoctorName, string Date,
    string Time, string TrackingCode, int? QueueNumber)
{
    public Dictionary<string, object?> Variables() => new()
    {
        ["patient_name"] = PatientName,
        ["doctor_name"] = DoctorName,
        ["appointment_date"] = Date,
        ["appointment_time"] = Time,
        ["tracking_code"] = TrackingCode,
        ["queue_number"] = QueueNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty
    };
}

/// <summary>انتخاب قالب نوبت و جلوگیری از چاپ دوم پس از انصراف یا بستن پنجره.</summary>
public static class AppointmentTicketTemplatePrint
{
    public static async Task<(TemplatePrintOutcome Outcome, bool UsedFallback)> PrintAsync(
        AppointmentTicketData data,
        Func<Dictionary<string, object?>, Task<TemplatePrintOutcome>> render,
        Func<bool> printInternal)
    {
        var outcome = await render(data.Variables());
        if (outcome != TemplatePrintOutcome.Fallback) return (outcome, false);
        return (printInternal() ? TemplatePrintOutcome.Printed : TemplatePrintOutcome.NotPrinted, true);
    }
}

namespace Sahand.ClinicPro.Domain.Entities.Reporting;

/// <summary>شناسهٔ پایدار قالب داخلی قبض پذیرش؛ بدنه به موتور چاپ قدیمی ارجاع می‌دهد.</summary>
public static class BuiltInPrintTemplates
{
    /// <summary>نشانگر قالب قبض حرارتی اصلی.</summary>
    public const string ReceptionReceiptMarker = "clinicpro:reception-receipt:original:v1";
    /// <summary>نام نمایشی قبض اصلی.</summary>
    public const string ReceptionReceiptName = "قبض پذیرش اصلی — حرارتی";
}

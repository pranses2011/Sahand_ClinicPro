using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Sahand.ClinicPro.Application.Features.Paraclinical;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>دادهٔ معتبر برای چاپ پیش‌نویس نسخه عینک؛ امضا و ذخیرهٔ الکترونیک نیست.</summary>
public sealed record GlassesPrintData(long PatientId, string PatientName, string NationalCode,
    decimal? RightSphere, decimal? RightCylinder, int? RightAxis, decimal? RightAdd,
    decimal? LeftSphere, decimal? LeftCylinder, int? LeftAxis, decimal? LeftAdd, decimal? Pd);

public static class GlassesPrescriptionPrint
{
    private static bool Number(string? text, out decimal? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!PlainNumber.TryParse(text, out var parsed)) return false;
        value = parsed;
        return true;
    }

    private static bool Axis(string? text, out int? value)
    {
        value = null;
        if (!Number(text, out var number)) return false;
        if (number is null) return true;
        if (number < 0 || number > 180 || number != decimal.Truncate(number.Value)) return false;
        value = (int)number.Value;
        return true;
    }

    /// <summary>اعتبار اعداد و هویت پیش از چاپ؛ نبود مقدار با صفر اشتباه نمی‌شود.</summary>
    public static bool TryCreate(long patientId, string? patientName, string? nationalCode,
        string? rightSphere, string? rightCylinder, string? rightAxis, string? rightAdd,
        string? leftSphere, string? leftCylinder, string? leftAxis, string? leftAdd, string? pd,
        out GlassesPrintData? data)
    {
        data = null;
        if (patientId <= 0 || string.IsNullOrWhiteSpace(patientName) ||
            !Number(rightSphere, out var rs) || !Number(rightCylinder, out var rc) || !Axis(rightAxis, out var ra) || !Number(rightAdd, out var rd) ||
            !Number(leftSphere, out var ls) || !Number(leftCylinder, out var lc) || !Axis(leftAxis, out var la) || !Number(leftAdd, out var ld) ||
            !Number(pd, out var distance)) return false;
        if (rs is null && rc is null && rd is null && ls is null && lc is null && ld is null) return false;
        if (!EyeExamValueSafety.IsValid(rs, rc, ra, ls, lc, la, null, null, distance) ||
            !EyeExamValueSafety.IsValid(rd, null, null, ld, null, null, null, null, null)) return false;
        data = new GlassesPrintData(patientId, patientName.Trim(), nationalCode?.Trim() ?? string.Empty, rs, rc, ra, rd, ls, lc, la, ld, distance);
        return true;
    }

    private static string Power(decimal? number) => number is null ? "—" :
        (number > 0 ? "+" : string.Empty) + PlainNumber.Format(number.Value);
    private static string AxisText(int? number) => number is null ? "—" : Money.ToPersianDigits(number.Value.ToString());

    /// <summary>سند چاپی شامل هویت بیمار، مقادیر فرم و جای امضای دستی.</summary>
    public static FlowDocument BuildDocument(GlassesPrintData data, DateTime iranTime)
    {
        var doc = new FlowDocument { FlowDirection = FlowDirection.RightToLeft, FontFamily = new FontFamily("Tahoma"), FontSize = 13, PagePadding = new Thickness(42) };
        void Add(string text, double size = 13, bool bold = false) => doc.Blocks.Add(new Paragraph(new Run(text))
        { FontSize = size, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, Margin = new Thickness(0, 5, 0, 5) });
        Add("پیش‌نویس نسخه عینک — نیازمند بررسی و امضای تجویزکننده", 16, true);
        Add($"بیمار: {data.PatientName}    شناسه داخلی: {Money.ToPersianDigits(data.PatientId.ToString())}    کد ملی: {(string.IsNullOrWhiteSpace(data.NationalCode) ? "—" : Money.ToPersianDigits(data.NationalCode))}");
        Add("تاریخ چاپ: " + Money.ToPersianDigits(Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(iranTime)));
        void AddEye(string eye, decimal? sphere, decimal? cylinder, int? axis, decimal? add)
            => doc.Blocks.Add(new Paragraph(new Run($"{eye}   SPH {Power(sphere)}    CYL {Power(cylinder)}    AXIS {AxisText(axis)}    ADD {Power(add)}"))
            { FlowDirection = FlowDirection.LeftToRight, FontWeight = FontWeights.Bold, FontSize = 13, Margin = new Thickness(0, 5, 0, 5) });
        AddEye("OD", data.RightSphere, data.RightCylinder, data.RightAxis, data.RightAdd);
        AddEye("OS", data.LeftSphere, data.LeftCylinder, data.LeftAxis, data.LeftAdd);
        Add($"فاصله مردمک (PD): {(data.Pd is null ? "—" : PlainNumber.Format(data.Pd.Value))} میلی‌متر");
        Add("متن از مقادیر فعلی فرم تهیه شده و ممکن است ذخیره نشده باشد. امضای دیجیتال ندارد.", 11);
        Add("نام، مهر و امضای تجویزکننده: ______________________________", 13, true);
        return doc;
    }
}

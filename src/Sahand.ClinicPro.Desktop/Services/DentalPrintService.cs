using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.ViewModels;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>عکس مستقل از فرم فعلی؛ لزوماً معادل دادهٔ ذخیره‌شده نیست.</summary>
public sealed record DentalPrintSnapshot(long PatientId, string PatientName, string? NationalCode,
    IReadOnlyDictionary<int, int> Teeth, IReadOnlyList<DentalPlanRow> Plan);

/// <summary>ساخت گزارش قابل چاپ از چارت و طرح فعلی بدون رزرو نوبت یا صدور فاکتور.</summary>
public static class DentalPrintService
{
    public static string ConditionText(int condition) => condition switch
    {
        0 => "سالم", 1 => "پوسیدگی", 2 => "ترمیم‌شده", 3 => "درمان‌شده",
        4 => "روکش", 5 => "کشیده‌شده", 6 => "ایمپلنت", _ => "ثبت نشده"
    };

    /// <summary>فقط دندان‌های ثبت‌شده نشان داده می‌شوند، از هر دو دندان‌بندی.</summary>
    public static IReadOnlyList<string> ChartLines(DentalPrintSnapshot snapshot) => snapshot.Teeth
        .OrderBy(x => x.Key)
        .Select(x => $"دندان {Money.ToPersianDigits(x.Key.ToString())}: {ConditionText(x.Value)}")
        .ToArray();

    private static FlowDocument CreateDocument(DentalPrintSnapshot data, DateTime iranTime, string title)
    {
        var document = new FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft, FontFamily = new FontFamily("Tahoma"), FontSize = 12,
            PagePadding = new Thickness(42), ColumnWidth = double.PositiveInfinity
        };
        Add(document, title, true, 16);
        var id = Money.ToPersianDigits(data.PatientId.ToString());
        var code = string.IsNullOrWhiteSpace(data.NationalCode) ? "—" : Money.ToPersianDigits(data.NationalCode);
        Add(document, $"بیمار: {data.PatientName}    شناسه داخلی: {id}    کد ملی: {code}");
        Add(document, "تاریخ چاپ: " + Money.ToPersianDigits(PersianDateHelper.ToShamsi(iranTime)));
        Add(document, "پیش‌نویس از مقادیر فعلی فرم تهیه شده و ممکن است ذخیره نشده باشد.");
        return document;
    }

    private static void Add(FlowDocument document, string text, bool bold = false, double size = 12)
        => document.Blocks.Add(new Paragraph(new Run(text))
        { FontSize = size, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, Margin = new Thickness(0, 4, 0, 4) });

    public static FlowDocument BuildChart(DentalPrintSnapshot data, DateTime iranTime)
    {
        var doc = CreateDocument(data, iranTime, "پیش‌نویس چارت دندان");
        Add(doc, "دندان‌هایی که در فهرست نیستند ثبت نشده‌اند و سالم فرض نمی‌شوند.");
        var lines = ChartLines(data);
        if (lines.Count == 0) Add(doc, "هیچ وضعیت دندانی ثبت نشده است.");
        foreach (var line in lines) Add(doc, line);
        return doc;
    }

    public static FlowDocument BuildPlan(DentalPrintSnapshot data, DateTime iranTime)
    {
        var doc = CreateDocument(data, iranTime, "پیش‌نویس طرح درمان دندان");
        Add(doc, "این سند فاکتور یا تأیید پرداخت نیست. زمان‌های درج‌شده صرفاً پیشنهادند و نوبتی رزرو نمی‌کنند.");
        if (data.Plan.Count == 0) Add(doc, "قلمی در طرح وجود ندارد.");
        foreach (var row in data.Plan)
        {
            Add(doc, $"دندان {Money.ToPersianDigits(row.Tooth.ToString())} — {row.Procedure} — {Money.FormatWithUnit(row.Price)}", true);
            Add(doc, $"سطح: {row.Surface}    وضعیت: {row.StatusText}    زمان پیشنهادی: {row.Schedule}");
        }
        Add(doc, "جمع اقلام: " + Money.FormatWithUnit(data.Plan.Sum(row => row.Price)), true);
        Add(doc, "نام و امضای درمانگر: ______________________________");
        return doc;
    }
}

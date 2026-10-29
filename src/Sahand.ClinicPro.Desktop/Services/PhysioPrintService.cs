using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>عکس متن فعلی فرم برای چاپ، بدون ادعای ذخیره یا تأیید درمانگر.</summary>
public sealed record PhysioPrintData(long PatientId, string PatientName, string? NationalCode, string Text);

public static class PhysioPrintService
{
    public static bool TryCreate(QuickPatient? patient, string? text, out PhysioPrintData? data)
    {
        data = null;
        if (patient is null || patient.Id <= 0 || string.IsNullOrWhiteSpace(patient.FullName) || string.IsNullOrWhiteSpace(text)) return false;
        data = new(patient.Id, patient.FullName.Trim(), patient.NationalCode, text.Trim());
        return true;
    }

    public static FlowDocument Build(PhysioPrintData data, DateTime iranTime, bool referral)
    {
        var doc = new FlowDocument { PagePadding = new Thickness(42), ColumnWidth = double.PositiveInfinity,
            FlowDirection = FlowDirection.RightToLeft, FontFamily = new FontFamily("Tahoma"), FontSize = 13 };
        void Add(string text, bool bold = false, int size = 13) => doc.Blocks.Add(new Paragraph(new Run(text))
        { FontSize = size, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, Margin = new Thickness(0, 5, 0, 5) });
        Add(referral ? "پیش‌نویس گزارش فیزیوتراپی برای ارجاع‌دهنده" : "پیش‌نویس برنامهٔ تمرین خانگی فیزیوتراپی", true, 16);
        var code = string.IsNullOrWhiteSpace(data.NationalCode) ? "—" : Money.ToPersianDigits(data.NationalCode);
        Add($"بیمار: {data.PatientName}    شناسه داخلی: {Money.ToPersianDigits(data.PatientId.ToString())}    کد ملی: {code}");
        Add("تاریخ چاپ: " + Money.ToPersianDigits(PersianDateHelper.ToShamsi(iranTime)));
        Add("اطلاعات از فرم فعلی تهیه شده و ممکن است ذخیره نشده باشد؛ نیازمند بررسی و تأیید درمانگر است.", size: 11);
        Add(data.Text);
        if (!referral) Add("تمرین‌ها را فقط مطابق توصیهٔ درمانگر انجام دهید و در صورت تشدید علائم با درمانگر تماس بگیرید.", size: 11);
        Add("نام و امضای درمانگر: ______________________________", true);
        return doc;
    }
}

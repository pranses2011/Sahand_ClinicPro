using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف سوابق ویزیت در پرونده بیمار</summary>
public record PatientVisitRow(string DateShamsi, string DoctorName, string Diagnosis, string Status);

/// <summary>ردیف نسخه‌های بیمار</summary>
public record PatientPrescriptionRow(string DateShamsi, string DoctorName, int ItemsCount, string Status);

/// <summary>ردیف نوار نتایج غیرعادی پاراکلینیک — چرخه ۱۳ (درخواست ۱-۴)</summary>
public record AbnormalResultRow(
    string Title,
    string Value,
    string Range,
    string FlagText,
    string FlagColor,
    string DateShamsi,
    string Kind);

/// <summary>
/// ViewModel پرونده بیمار — گام U2 (فرم ۵): کارت سربرگ + هشدار آلرژی برجسته +
/// تب‌های سوابق (ویزیت/نسخه از endpointهای موجود + پیام گام‌های بعدی برای بقیه)
/// </summary>
public partial class PatientDetailViewModel : ObservableObject
{
    private readonly IApiClient _api;

    /* ── کارت سربرگ ── */
    [ObservableProperty] private string _fullName = "بارگذاری…";
    [ObservableProperty] private string _medicalRecordNo = string.Empty;
    [ObservableProperty] private string _nationalCode = string.Empty;
    [ObservableProperty] private string _mobile = string.Empty;
    [ObservableProperty] private string _ageText = "—";
    [ObservableProperty] private string _insuranceName = "—";
    [ObservableProperty] private string _bloodType = "—";
    [ObservableProperty] private string _genderText = "—";
    [ObservableProperty] private string _phone = "—";
    [ObservableProperty] private string _email = "—";
    [ObservableProperty] private string _city = "—";
    [ObservableProperty] private string _address = "—";
    [ObservableProperty] private string _insuranceNumber = "—";

    /// <summary>تگ‌های آلرژی — هشدار قرمز</summary>
    public ObservableCollection<string> Allergies { get; } = new();

    /// <summary>تگ‌های بیماری مزمن — نارنجی</summary>
    public ObservableCollection<string> ChronicDiseases { get; } = new();

    /// <summary>تگ‌های داروهای فعلی — آبی</summary>
    public ObservableCollection<string> CurrentMedications { get; } = new();

    /// <summary>سوابق ویزیت — از endpoint visits/patient/{id}</summary>
    public ObservableCollection<PatientVisitRow> Visits { get; } = new();

    /// <summary>نسخه‌های بیمار — از endpoint prescriptions?patientId=</summary>
    public ObservableCollection<PatientPrescriptionRow> Prescriptions { get; } = new();

    /// <summary>نوار نتایج غیرعادی پاراکلینیک — چرخه ۱۳</summary>
    public ObservableCollection<AbnormalResultRow> AbnormalResults { get; } = new();

    /// <summary>تعداد نتایج غیرعادی — برای نمایش نوار</summary>
    [ObservableProperty]
    private int _abnormalCount;

    /// <summary>متن خلاصه نوار — مثل «۳ آزمایش غیرطبیعی + ۱ یافته تصویری»</summary>
    [ObservableProperty]
    private string _abnormalSummaryText = string.Empty;

    /// <summary>سازنده با شناسه بیمار</summary>
    public PatientDetailViewModel(IApiClient api, long patientId)
    {
        _api = api;
        _ = LoadAsync(patientId);
    }

    /// <summary>بارگذاری پرونده + سوابق</summary>
    private async Task LoadAsync(long patientId)
    {
        // ۱) پرونده اصلی
        var patient = await _api.GetAsync<PatientDetailDto>($"patients/{patientId}");
        if (patient is null)
            return;

        FullName = $"{patient.FirstName} {patient.LastName}".Trim();
        MedicalRecordNo = patient.MedicalRecordNo;
        NationalCode = patient.NationalCode;
        Mobile = patient.Mobile;
        AgeText = patient.Age is { } age ? $"{age} سال" : "—";
        InsuranceName = patient.InsuranceName ?? "آزاد";
        BloodType = patient.BloodType ?? "نامشخص";
        GenderText = patient.Gender == 2 ? "زن" : "مرد";
        Phone = patient.Phone ?? "—";
        Email = patient.Email ?? "—";
        City = string.IsNullOrWhiteSpace(patient.City) ? "—" : patient.City;
        Address = string.IsNullOrWhiteSpace(patient.Address) ? "—" : patient.Address;
        InsuranceNumber = patient.InsuranceNumber ?? "—";

        FillTags(Allergies, patient.Allergies);
        FillTags(ChronicDiseases, patient.ChronicDiseases);
        FillTags(CurrentMedications, patient.CurrentMedications);

        // ۲) سوابق ویزیت
        var visits = await _api.GetAsync<VisitsEnvelope>($"visits/patient/{patientId}");
        if (visits?.Data is not null)
        {
            foreach (var v in visits.Data)
            {
                Visits.Add(new PatientVisitRow(
                    v.VisitDateShamsi ?? "—",
                    v.DoctorName ?? "—",
                    v.ChiefComplaint ?? "—",
                    v.Status ?? "—"));
            }
        }

        // ۳) نسخه‌ها
        var prescriptions = await _api.GetAsync<PrescriptionsEnvelope>($"prescriptions?patientId={patientId}&pageSize=50");
        if (prescriptions?.Data is not null)
        {
            foreach (var p in prescriptions.Data)
            {
                Prescriptions.Add(new PatientPrescriptionRow(
                    p.PrescriptionDateShamsi ?? "—",
                    p.DoctorName ?? "—",
                    p.ItemsCount,
                    p.Status ?? "—"));
            }
        }

        // ۴) نوار نتایج غیرعادی پاراکلینیک — چرخه ۱۳ (درخواست ۱-۴)
        var abnormal = await _api.GetAsync<AbnormalEnvelope>($"patients/{patientId}/abnormal-summary");
        if (abnormal?.Data is { HasAbnormal: true })
        {
            AbnormalCount = abnormal.Data.TotalCount;
            var parts = new List<string>();
            if (abnormal.Data.LabCount > 0)
                parts.Add($"{abnormal.Data.LabCount} آزمایش غیرطبیعی");
            if (abnormal.Data.ImagingCount > 0)
                parts.Add($"{abnormal.Data.ImagingCount} یافته تصویری هشدار");
            AbnormalSummaryText = string.Join(" + ", parts);
            foreach (var a in abnormal.Data.Items ?? new List<AbnormalItemApi>())
            {
                var value = a.Value is not null
                    ? $"{a.Value}{(string.IsNullOrWhiteSpace(a.Unit) ? "" : " " + a.Unit)}"
                    : (a.ResultText is { Length: > 46 } ? a.ResultText[..46] + "…" : a.ResultText ?? "—");
                AbnormalResults.Add(new AbnormalResultRow(
                    a.Title ?? "—", value,
                    string.IsNullOrWhiteSpace(a.NormalRange) ? "—" : $"نرمال: {a.NormalRange}",
                    string.IsNullOrWhiteSpace(a.FlagText) ? "خارج از محدوده" : a.FlagText,
                    string.IsNullOrWhiteSpace(a.FlagColor) ? "#FFF3E0" : a.FlagColor,
                    a.DateShamsi ?? "—",
                    a.Kind == "imaging" ? "🩻 تصویر" : "🧪 آزمایش"));
            }
        }
        else
        {
            AbnormalCount = 0;
        }
    }

    /// <summary>پر کردن مجموعه تگ از رشته JSON</summary>
    private static void FillTags(ObservableCollection<string> target, string? json)
    {
        target.Clear();
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            var items = JsonSerializer.Deserialize<List<string>>(json);
            if (items is not null)
                foreach (var item in items)
                    target.Add(item);
        }
        catch
        {
            target.Add(json);
        }
    }

    /* ── DTOهای سبک ── */

    /// <summary>پاسخ نوار نتایج غیرعادی — چرخه ۱۳</summary>
    public sealed class AbnormalEnvelope
    {
        public bool IsSuccess { get; set; }
        public AbnormalData? Data { get; set; }
    }

    /// <summary>داده نوار نتایج غیرعادی</summary>
    public sealed class AbnormalData
    {
        public int TotalCount { get; set; }
        public int LabCount { get; set; }
        public int ImagingCount { get; set; }
        public bool HasAbnormal { get; set; }
        public List<AbnormalItemApi>? Items { get; set; }
    }

    /// <summary>ردیف نوار نتیجه غیرعادی</summary>
    public sealed class AbnormalItemApi
    {
        public string? Title { get; set; }
        public decimal? Value { get; set; }
        public string? ResultText { get; set; }
        public string? NormalRange { get; set; }
        public string? Unit { get; set; }
        public string? FlagText { get; set; }
        public string? FlagColor { get; set; }
        public string? DateShamsi { get; set; }
        public string? Kind { get; set; }
    }

    /// <summary>پاسخ فهرست ویزیت‌های بیمار</summary>
    public sealed class VisitsEnvelope
    {
        public bool IsSuccess { get; set; }
        public List<VisitRow>? Data { get; set; }
    }

    /// <summary>ردیف ویزیت</summary>
    public sealed class VisitRow
    {
        public string? VisitDateShamsi { get; set; }
        public string? DoctorName { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>پاسخ فهرست نسخه‌های بیمار</summary>
    public sealed class PrescriptionsEnvelope
    {
        public bool IsSuccess { get; set; }
        public List<PrescriptionRowDto>? Data { get; set; }
    }

    /// <summary>ردیف نسخه</summary>
    public sealed class PrescriptionRowDto
    {
        public string? PrescriptionDateShamsi { get; set; }
        public string? DoctorName { get; set; }
        public int ItemsCount { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>DTO پرونده بیمار</summary>
    public sealed class PatientDetailDto
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MedicalRecordNo { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public int? Age { get; set; }
        public string? BloodType { get; set; }
        public int Gender { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? InsuranceName { get; set; }
        public string? InsuranceNumber { get; set; }
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? CurrentMedications { get; set; }
    }
}

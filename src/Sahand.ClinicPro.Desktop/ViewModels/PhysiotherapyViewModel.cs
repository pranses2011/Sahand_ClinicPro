using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel صفحهٔ مستقل «فیزیوتراپی» — چرخهٔ چهل‌وسوم (گزارش مالک)
/// </summary>
/// <remarks>
/// «برای هر کدوم یک صفحه مستقل طراحی بکن تا با همدیگه تداخلی نداشته باشن» —
/// تکنیک‌ها، نمرهٔ درد و پروندهٔ بیمار این تب از پاراکلینیکِ تب‌دار جدا و به
/// صفحهٔ اختصاصی منتقل شد؛ نام پراپرتی‌ها عیناً حفظ شد تا قالب XAML بدون
/// تغییر بایند شود. جستجوی بیمار از هلپر مشترک PatientQuickSearch.
/// </remarks>
public partial class PhysiotherapyViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه در هدر و منو</summary>
    public override string Title => "فیزیوتراپی";

    /// <summary>فهرست تکنیک‌های درمانی قابل انتخاب</summary>
    public ObservableCollection<string> Techniques { get; } = new()
        { "الکتروتراپی (TENS)", "اولتراسوند", "لیزر درمانی", "تمرینات درمانی", "ماساژ", "هیدروتراپی", "طب سوزنی" };

    /// <summary>تکنیک دیگر (متن آزاد)</summary>
    [ObservableProperty] private string? _physioOtherTechnique;

    /// <summary>نمرهٔ درد ۰-۱۰</summary>
    [ObservableProperty] private int _painScore;

    /// <summary>یادداشت جلسه</summary>
    [ObservableProperty] private string? _physioNotes;

    /// <summary>عبارت جستجوی بیمار فیزیوتراپی</summary>
    [ObservableProperty] private string _physioPatientSearch = string.Empty;

    /// <summary>بیمار منتخب</summary>
    [ObservableProperty] private QuickPatient? _physioPatient;

    /// <summary>تکنیک‌های انتخاب‌شدهٔ این جلسه</summary>
    public ObservableCollection<string> SelectedTechniques { get; } = new();

    /// <summary>رنگ نمرهٔ درد — سبز/زرد/قرمز</summary>
    public string PainColor => PainScore <= 3 ? "#43A047" : PainScore <= 6 ? "#FDD835" : "#E53935";

    partial void OnPainScoreChanged(int value) => OnPropertyChanged(nameof(PainColor));

    /// <summary>سازنده با تزریق سرویس‌ها</summary>
    public PhysiotherapyViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
    }

    /// <summary>جستجوی بیمار فیزیوتراپی</summary>
    [RelayCommand]
    private async Task SearchPhysioPatientAsync()
    {
        var patient = await Helpers.PatientQuickSearch.FindAsync(_api, _snackbar, PhysioPatientSearch);
        if (patient is not null) PhysioPatient = patient;
    }

    /// <summary>ذخیرهٔ جلسهٔ فیزیوتراپی</summary>
    [RelayCommand]
    private async Task SavePhysioSessionAsync()
    {
        if (PhysioPatient is null) { _snackbar.Error("ابتدا بیمار را جستجو کنید"); return; }
        // ═══ چرخهٔ ۴۸ — سازگاری کامل با قرارداد سرور RecordPhysioSessionCommand ═══
        // بدنهٔ قبلی فیلدهای ناهم‌جنس داشت (techniques/notes/sessionDateShamsi) که
        // سرور نمی‌شناخت ⇒ «تکنیک‌های درمان» هرگز ثبت نمی‌شد (Treatments تهی) و
        // یادداشت‌ها نیز دور ریخته می‌شدند — جلسهٔ بی‌محتوا ثبت می‌شد.
        var ok = await _api.PostAsync("physiotherapy/sessions", new
        {
            patientId = PhysioPatient.Id,
            therapistId = (long?)null,
            sessionNumber = 1,
            // تکنیک‌های منتخب + سایر — به فیلد Treatments سرور می‌نشیند؛
            // یادداشت آزاد کاردرمان هم (در نبود ستون مستقل در قرارداد فعلی سرور)
            // در انتهای همان فیلد با برچسب واضح حفظ می‌شود تا هیچ داده‌ای دور ریخته نشود.
            treatments = BuildSessionSummary(),
            duration = (int?)null,
            painScore = PainScore
        });
        if (ok)
        {
            _snackbar.Success("جلسه فیزیوتراپی ثبت شد");
            PhysioNotes = null;
            PhysioOtherTechnique = null;
            PainScore = 0;
        }
        else _snackbar.Error(_api.LastError ?? "ثبت جلسه ناموفق بود");
    }

    /// <summary>
    /// متن جمع‌بندی جلسه — تکنیک‌های منتخب + تکنیک دیگر + یادداشت درمانگر؛
    /// چرخهٔ ۴۸: قرارداد فعلی سرور فقط فیلد Treatments دارد؛ یادداشت با برچسب
    /// «یادداشت:» به انتهای آن می‌چسبد تا در گزارش‌ها و پرونده قابل خواندن بماند.
    /// </summary>
    private string BuildSessionSummary()
    {
        var parts = SelectedTechniques
            .Concat(string.IsNullOrWhiteSpace(PhysioOtherTechnique)
                ? System.Linq.Enumerable.Empty<string>()
                : new[] { PhysioOtherTechnique.Trim() })
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
        var summary = string.Join("، ", parts);
        if (!string.IsNullOrWhiteSpace(PhysioNotes))
            summary = string.IsNullOrWhiteSpace(summary)
                ? $"یادداشت: {PhysioNotes.Trim()}"
                : $"{summary} — یادداشت: {PhysioNotes.Trim()}";
        return summary;
    }
}

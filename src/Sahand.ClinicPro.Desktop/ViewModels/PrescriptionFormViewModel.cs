using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>سطر قلم دارویی در ویرایشگر اقلام</summary>
public partial class PrescriptionItemRow : ObservableObject
{
    /// <summary>شناسه دارو</summary>
    public long MedicationId { get; init; }

    /// <summary>نام دارو</summary>
    [ObservableProperty]
    private string _medicationName = string.Empty;

    /// <summary>دوز (مثال ۵۰۰ میلی‌گرم)</summary>
    [ObservableProperty]
    private string? _dosage;

    /// <summary>تکرار (مثال هر ۸ ساعت)</summary>
    [ObservableProperty]
    private string? _frequency;

    /// <summary>مدت (مثال ۷ روز)</summary>
    [ObservableProperty]
    private string? _duration;

    /// <summary>تعداد</summary>
    [ObservableProperty]
    private decimal _quantity = 1;

    /// <summary>واحد</summary>
    [ObservableProperty]
    private string? _unit;

    /// <summary>دستور مصرف</summary>
    [ObservableProperty]
    private string? _instructions;

    /// <summary>در صورت نیاز (SOS)</summary>
    [ObservableProperty]
    private bool _isSos;
}

/// <summary>گزینه دارو در جستجو</summary>
public record MedicationOption(long Id, string Name, string? Form, string? Strength);

/// <summary>
/// ViewModel فرم صدور نسخه — جستجوی بیمار + اقلام دارویی
/// </summary>
public partial class PrescriptionFormViewModel : ObservableObject
{
    private readonly IApiClient _api;

    /// <summary>عبارت جستجوی بیمار</summary>
    [ObservableProperty]
    private string _patientSearchTerm = string.Empty;

    /// <summary>نتایج جستجوی بیمار</summary>
    [ObservableProperty]
    private ObservableCollection<PatientOption> _patientResults = new();

    /// <summary>بیمار انتخاب‌شده</summary>
    [ObservableProperty]
    private PatientOption? _selectedPatient;

    /// <summary>پزشکان</summary>
    public ObservableCollection<DoctorOption> Doctors { get; } = new();

    /// <summary>نمایه پزشک</summary>
    [ObservableProperty]
    private int _doctorIndex = -1;

    /// <summary>شناسه ویزیت مرتبط — اختیاری</summary>
    [ObservableProperty]
    private string? _visitId;

    /// <summary>نمایه نوع نسخه</summary>
    [ObservableProperty]
    private int _typeIndex;

    /// <summary>نسخه الکترونیک است</summary>
    [ObservableProperty]
    private bool _isElectronic;

    /// <summary>داروی مخدر</summary>
    [ObservableProperty]
    private bool _isNarcotic;

    /// <summary>یادداشت</summary>
    [ObservableProperty]
    private string? _notes;

    /// <summary>عبارت جستجوی دارو</summary>
    [ObservableProperty]
    private string _medicationSearchTerm = string.Empty;

    /// <summary>نتایج جستجوی دارو</summary>
    [ObservableProperty]
    private ObservableCollection<MedicationOption> _medicationResults = new();

    /// <summary>داروی انتخاب‌شده از نتایج</summary>
    [ObservableProperty]
    private MedicationOption? _selectedMedication;

    /// <summary>اقلام نسخه</summary>
    public ObservableCollection<PrescriptionItemRow> Items { get; } = new();

    /// <summary>قلم انتخاب‌شده برای حذف</summary>
    [ObservableProperty]
    private PrescriptionItemRow? _selectedItem;

    /// <summary>پیام خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>در حال ذخیره</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>رویداد بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>رویداد ذخیره موفق</summary>
    public event EventHandler? SaveSucceeded;

    /// <summary>سازنده با کلاینت API و پزشکان</summary>
    public PrescriptionFormViewModel(IApiClient api, ObservableCollection<DoctorOption> doctors)
    {
        _api = api;
        foreach (var d in doctors)
            Doctors.Add(d);
    }

    /// <summary>جستجوی بیمار</summary>
    [RelayCommand]
    private async Task SearchPatientAsync()
    {
        if (string.IsNullOrWhiteSpace(PatientSearchTerm) || PatientSearchTerm.Trim().Length < 2)
            return;

        var response = await _api.GetAsync<AppointmentFormViewModel.PatientsSearchResponse>(
            $"patients/search?term={Uri.EscapeDataString(PatientSearchTerm.Trim())}");

        PatientResults = response?.Data is not null
            ? new ObservableCollection<PatientOption>(response.Data.Select(p =>
                new PatientOption(p.Id,
                    $"{p.FirstName} {p.LastName} — پرونده {p.MedicalRecordNo}",
                    $"کد ملی {p.NationalCode} — موبایل {p.Mobile}")))
            : new ObservableCollection<PatientOption>();
    }

    /// <summary>جستجوی دارو از داده مرجع</summary>
    [RelayCommand]
    private async Task SearchMedicationAsync()
    {
        if (string.IsNullOrWhiteSpace(MedicationSearchTerm) || MedicationSearchTerm.Trim().Length < 2)
            return;

        var response = await _api.GetAsync<MedicationsResponse>(
            $"lookups/medications?search={Uri.EscapeDataString(MedicationSearchTerm.Trim())}&take=25");

        MedicationResults = response?.Data is not null
            ? new ObservableCollection<MedicationOption>(response.Data.Select(m =>
                new MedicationOption(m.Id, m.Name, m.Form, m.Strength)))
            : new ObservableCollection<MedicationOption>();
    }

    /// <summary>افزودن داروی انتخاب‌شده به اقلام</summary>
    [RelayCommand]
    private void AddMedication()
    {
        if (SelectedMedication is null) return;

        Items.Add(new PrescriptionItemRow
        {
            MedicationId = SelectedMedication.Id,
            MedicationName = SelectedMedication.Name +
                (string.IsNullOrEmpty(SelectedMedication.Form) ? string.Empty : $" ({SelectedMedication.Form})"),
            Dosage = SelectedMedication.Strength,
            Quantity = 1,
            Unit = "عدد"
        });

        MedicationSearchTerm = string.Empty;
        MedicationResults.Clear();
    }

    /// <summary>حذف قلم انتخاب‌شده</summary>
    [RelayCommand]
    private void RemoveItem()
    {
        if (SelectedItem is not null)
            Items.Remove(SelectedItem);
    }

    /// <summary>صدور نسخه</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (SelectedPatient is null)
        {
            ErrorMessage = "انتخاب بیمار الزامی است";
            return;
        }
        if (DoctorIndex < 0 || DoctorIndex >= Doctors.Count)
        {
            ErrorMessage = "انتخاب پزشک الزامی است";
            return;
        }
        if (Items.Count == 0)
        {
            ErrorMessage = "افزودن حداقل یک قلم دارویی الزامی است";
            return;
        }

        long? visitId = null;
        if (!string.IsNullOrWhiteSpace(VisitId))
        {
            if (!long.TryParse(VisitId.Trim(), out var parsed) || parsed <= 0)
            {
                ErrorMessage = "شناسه ویزیت باید عدد باشد";
                return;
            }
            visitId = parsed;
        }

        IsBusy = true;
        var body = new
        {
            patientId = SelectedPatient.Id,
            doctorId = Doctors[DoctorIndex].Id,
            visitId,
            type = TypeIndex + 1,
            isElectronic = IsElectronic,
            isNarcotic = IsNarcotic,
            notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            items = Items.Select((it, index) => new
            {
                medicationId = it.MedicationId,
                orderIndex = index + 1,
                dosage = it.Dosage,
                frequency = it.Frequency,
                duration = it.Duration,
                quantity = it.Quantity,
                unit = it.Unit,
                instructions = it.Instructions,
                isSos = it.IsSos
            }).ToList()
        };

        var ok = await _api.PostAsync("prescriptions", body);
        IsBusy = false;

        if (ok) SaveSucceeded?.Invoke(this, EventArgs.Empty);
        else ErrorMessage = _api.LastError ?? "صدور نسخه ناموفق بود";
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>پاسخ داروهای مرجع</summary>
    public sealed class MedicationsResponse
    {
        public bool IsSuccess { get; set; }
        public List<MedicationDto>? Data { get; set; }
    }

    /// <summary>DTO دارو</summary>
    public sealed class MedicationDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Form { get; set; }
        public string? Strength { get; set; }
        public bool IsNarcotic { get; set; }
    }
}
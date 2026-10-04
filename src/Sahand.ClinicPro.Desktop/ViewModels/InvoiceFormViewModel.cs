using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>سطر قلم فاکتور در ویرایشگر</summary>
public partial class InvoiceItemRow : ObservableObject
{
    /// <summary>شرح خدمت</summary>
    [ObservableProperty]
    private string? _description;

    /// <summary>تعداد</summary>
    [ObservableProperty]
    private decimal _quantity = 1;

    /// <summary>مبلغ واحد به ریال</summary>
    [ObservableProperty]
    private decimal _unitPrice;

    /// <summary>درصد تخفیف</summary>
    [ObservableProperty]
    private decimal _discountPercent;

    /// <summary>جمع این قلم</summary>
    public decimal LineTotal => Quantity * UnitPrice * (1 - DiscountPercent / 100);
}

/// <summary>
/// ViewModel فرم صدور فاکتور — بیمار + بیمه + اقلام خدمت
/// </summary>
public partial class InvoiceFormViewModel : ObservableObject
{
    private readonly IApiClient _api;

    /// <summary>عبارت جستجوی بیمار</summary>
    [ObservableProperty]
    private string _patientSearchTerm = string.Empty;

    /// <summary>نتایج جستجو</summary>
    [ObservableProperty]
    private ObservableCollection<PatientOption> _patientResults = new();

    /// <summary>بیمار انتخاب‌شده</summary>
    [ObservableProperty]
    private PatientOption? _selectedPatient;

    /// <summary>بیمه‌ها</summary>
    public ObservableCollection<InsuranceOption> InsuranceCompanies { get; } = new();

    /// <summary>نمایه بیمه</summary>
    [ObservableProperty]
    private int _insuranceIndex = -1;

    /// <summary>نمایه نوع فاکتور</summary>
    [ObservableProperty]
    private int _typeIndex;

    /// <summary>درصد مالیات</summary>
    [ObservableProperty]
    private decimal _taxPercent;

    /// <summary>شناسه ویزیت مرتبط — اختیاری</summary>
    [ObservableProperty]
    private string? _visitId;

    /// <summary>توضیحات</summary>
    [ObservableProperty]
    private string? _description;

    /// <summary>اقلام فاکتور</summary>
    public ObservableCollection<InvoiceItemRow> Items { get; } = new();

    /// <summary>قلم انتخاب‌شده</summary>
    [ObservableProperty]
    private InvoiceItemRow? _selectedItem;

    /// <summary>جمع کل اقلام</summary>
    public decimal ItemsTotal => Items.Sum(i => i.LineTotal);

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

    /// <summary>سازنده با کلاینت API</summary>
    public InvoiceFormViewModel(IApiClient api)
    {
        _api = api;
        Items.Add(new InvoiceItemRow());
        HookItemsNotifications();
        _ = LoadInsuranceAsync();
    }

    /// <summary>اشتراک در تغییر اقلام — به‌روزرسانی جمع کل</summary>
    private void HookItemsNotifications()
    {
        Items.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ItemsTotal));
        foreach (var row in Items)
            row.PropertyChanged += (_, _) => OnPropertyChanged(nameof(ItemsTotal));
    }

    /// <summary>بارگذاری بیمه‌ها</summary>
    private async Task LoadInsuranceAsync()
    {
        var response = await _api.GetAsync<ReceptionFormViewModel.InsuranceResponse>(
            "lookups/insurance-companies");
        if (response?.Data is not null)
            foreach (var c in response.Data)
                InsuranceCompanies.Add(new InsuranceOption(c.Id, c.Name));
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

    /// <summary>افزودن قلم جدید</summary>
    [RelayCommand]
    private void AddItem()
    {
        var row = new InvoiceItemRow();
        row.PropertyChanged += (_, _) => OnPropertyChanged(nameof(ItemsTotal));
        Items.Add(row);
    }

    /// <summary>حذف قلم انتخاب‌شده</summary>
    [RelayCommand]
    private void RemoveItem()
    {
        if (SelectedItem is not null && Items.Count > 1)
            Items.Remove(SelectedItem);
    }

    /// <summary>صدور فاکتور</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (SelectedPatient is null)
        {
            ErrorMessage = "انتخاب بیمار الزامی است";
            return;
        }
        if (Items.All(i => i.UnitPrice <= 0 || string.IsNullOrWhiteSpace(i.Description)))
        {
            ErrorMessage = "برای هر قلم شرح خدمت و مبلغ واحد الزامی است";
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
            receptionId = (long?)null,
            visitId,
            type = TypeIndex + 1,
            insuranceCompanyId = InsuranceIndex >= 0 && InsuranceIndex < InsuranceCompanies.Count
                ? InsuranceCompanies[InsuranceIndex].Id
                : (long?)null,
            taxPercent = TaxPercent,
            description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            items = Items
                .Where(i => i.UnitPrice > 0 && !string.IsNullOrWhiteSpace(i.Description))
                .Select(i => new
                {
                    serviceId = (long?)null,
                    description = i.Description!.Trim(),
                    quantity = i.Quantity,
                    unitPrice = i.UnitPrice,
                    discountPercent = i.DiscountPercent,
                    doctorId = (long?)null,
                    doctorSharePercent = 0m
                }).ToList()
        };

        var ok = await _api.PostAsync("invoices", body);
        IsBusy = false;

        if (ok) SaveSucceeded?.Invoke(this, EventArgs.Empty);
        else ErrorMessage = _api.LastError ?? "صدور فاکتور ناموفق بود";
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel فرم ورود موجودی — بچ دارو با تاریخ انقضا
/// </summary>
public partial class StockInFormViewModel : ObservableObject
{
    private readonly IApiClient _api;

    /// <summary>جستجوی دارو</summary>
    [ObservableProperty]
    private string _medicationSearchTerm = string.Empty;

    /// <summary>نتایج جستجو</summary>
    [ObservableProperty]
    private ObservableCollection<MedicationOption> _medicationResults = new();

    /// <summary>داروی انتخاب‌شده</summary>
    [ObservableProperty]
    private MedicationOption? _selectedMedication;

    /// <summary>شماره بچ</summary>
    [ObservableProperty]
    private string? _batchNumber;

    /// <summary>تعداد</summary>
    [ObservableProperty]
    private decimal _quantity = 1;

    /// <summary>قیمت خرید به ریال</summary>
    [ObservableProperty]
    private decimal _purchasePrice;

    /// <summary>قیمت فروش به ریال</summary>
    [ObservableProperty]
    private decimal _sellPrice;

    /// <summary>تاریخ انقضا شمسی</summary>
    [ObservableProperty]
    private string? _expiryDateShamsi;

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
    public StockInFormViewModel(IApiClient api) => _api = api;

    /// <summary>جستجوی دارو</summary>
    [RelayCommand]
    private async Task SearchMedicationAsync()
    {
        if (string.IsNullOrWhiteSpace(MedicationSearchTerm) || MedicationSearchTerm.Trim().Length < 2)
            return;

        var response = await _api.GetAsync<PrescriptionFormViewModel.MedicationsResponse>(
            $"lookups/medications?search={Uri.EscapeDataString(MedicationSearchTerm.Trim())}&take=25");

        MedicationResults = response?.Data is not null
            ? new ObservableCollection<MedicationOption>(response.Data.Select(m =>
                new MedicationOption(m.Id, m.Name, m.Form, m.Strength)))
            : new ObservableCollection<MedicationOption>();
    }

    /// <summary>ثبت ورود موجودی</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (SelectedMedication is null)
        {
            ErrorMessage = "انتخاب دارو الزامی است";
            return;
        }
        if (Quantity <= 0)
        {
            ErrorMessage = "تعداد باید بزرگ‌تر از صفر باشد";
            return;
        }
        if (SellPrice < 0 || PurchasePrice < 0)
        {
            ErrorMessage = "قیمت‌ها نمی‌توانند منفی باشند";
            return;
        }
        if (string.IsNullOrWhiteSpace(ExpiryDateShamsi) || ExpiryDateShamsi.Trim().Length != 10)
        {
            ErrorMessage = "تاریخ انقضا شمسی الزامی است (مثال 1405/06/31)";
            return;
        }

        IsBusy = true;
        var body = new
        {
            medicationId = SelectedMedication.Id,
            batchNumber = string.IsNullOrWhiteSpace(BatchNumber) ? null : BatchNumber.Trim(),
            quantity = Quantity,
            purchasePrice = PurchasePrice,
            sellPrice = SellPrice,
            expiryDateShamsi = ExpiryDateShamsi.Trim(),
            supplierId = (long?)null
        };

        var ok = await _api.PostAsync("pharmacy/inventory", body);
        IsBusy = false;

        if (ok) SaveSucceeded?.Invoke(this, EventArgs.Empty);
        else ErrorMessage = _api.LastError ?? "ثبت موجودی ناموفق بود";
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
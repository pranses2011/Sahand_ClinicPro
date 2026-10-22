using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف قابل‌مدیریت خدمت در فهرست خدمات درمانگاه.</summary>
public sealed record ServiceCatalogRow(
    long Id, string? ServiceCode, string ServiceName, decimal Price,
    decimal DefaultCost, bool IsActive)
{
    /// <summary>متن وضعیت خدمت.</summary>
    public string StatusText => IsActive ? "فعال" : "غیرفعال";

    /// <summary>رنگ نشان وضعیت خدمت.</summary>
    public string StatusColor => IsActive ? "#2E7D32" : "#757575";
}

/// <summary>
/// صفحهٔ مدیریت فهرست خدمات — ساخت، ویرایش و حذف نرم خدمت‌ها.
/// قیمت و هزینهٔ تمام‌شده هم‌زمان ثبت می‌شوند تا محاسبهٔ سود خدمت و درصد حقوق
/// پرسنل بر مبنای اطلاعات واقعی انجام شود.
/// </summary>
public partial class ServiceCatalogViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private long? _editingId;
    private List<ServiceCatalogRow> _allRows = new();

    /// <inheritdoc />
    public override string Title => "مدیریت خدمات";

    /// <summary>فهرست فیلترشدهٔ خدمات برای جدول.</summary>
    public ObservableCollection<ServiceCatalogRow> Rows { get; } = new();

    /// <summary>تعداد خدمت‌های فعال.</summary>
    [ObservableProperty] private int _activeCount;

    /// <summary>تعداد خدمت‌های غیرفعال.</summary>
    [ObservableProperty] private int _inactiveCount;

    /// <summary>تعداد نتایج منطبق با جستجو و فیلتر.</summary>
    [ObservableProperty] private int _resultCount;

    /// <summary>وضعیت بارگذاری/ذخیره.</summary>
    [ObservableProperty] private bool _isBusy;

    /// <summary>متن جستجو.</summary>
    [ObservableProperty] private string _searchText = string.Empty;

    /// <summary>نمایش خدمت‌های غیرفعال.</summary>
    [ObservableProperty] private bool _showInactive;

    /// <summary>نام خدمت در فرم.</summary>
    [ObservableProperty] private string _serviceName = string.Empty;

    /// <summary>کد خدمت در فرم.</summary>
    [ObservableProperty] private string _serviceCode = string.Empty;

    /// <summary>قیمت فروش پایه به ریالِ ذخیره‌شده؛ کنترل ورودی واحد نمایشی را اعمال می‌کند.</summary>
    [ObservableProperty] private decimal _price;

    /// <summary>هزینهٔ تمام‌شدهٔ پایه به ریالِ ذخیره‌شده.</summary>
    [ObservableProperty] private decimal _defaultCost;

    /// <summary>فعال‌بودن خدمت.</summary>
    [ObservableProperty] private bool _isActive = true;

    /// <summary>آیا فرم در حالت ویرایش است؟</summary>
    [ObservableProperty] private bool _isEditing;

    /// <summary>متن دکمهٔ ذخیره.</summary>
    public string SaveButtonText => IsEditing ? "ذخیرهٔ تغییرات" : "ثبت خدمت";

    /// <summary>پیام خطای فرم/بارگذاری.</summary>
    [ObservableProperty] private string _errorMessage = string.Empty;

    /// <summary>ساخت ViewModel و بارگذاری اولیهٔ خدمت‌ها.</summary>
    public ServiceCatalogViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnShowInactiveChanged(bool value) => ApplyFilter();
    partial void OnIsEditingChanged(bool value) => OnPropertyChanged(nameof(SaveButtonText));

    /// <summary>خواندن همهٔ خدمت‌ها، شامل موارد غیرفعال.</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<ServicesResponse>("lookups/services/manage");
            if (response?.Data is null)
            {
                ErrorMessage = _api.LastError ?? "دریافت فهرست خدمات ناموفق بود";
                return;
            }

            _allRows = response.Data.Select(x => new ServiceCatalogRow(
                x.Id, x.ServiceCode, x.ServiceName, x.Price, x.DefaultCost, x.IsActive)).ToList();
            ActiveCount = _allRows.Count(x => x.IsActive);
            InactiveCount = _allRows.Count - ActiveCount;
            ErrorMessage = string.Empty;
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"دریافت فهرست خدمات ناموفق بود: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>شروع ثبت خدمت جدید.</summary>
    [RelayCommand]
    private void NewService() => ResetForm();

    /// <summary>انتقال اطلاعات ردیف انتخاب‌شده به فرم ویرایش.</summary>
    [RelayCommand]
    private void EditService(ServiceCatalogRow? row)
    {
        if (row is null) return;
        _editingId = row.Id;
        ServiceName = row.ServiceName;
        ServiceCode = row.ServiceCode ?? string.Empty;
        Price = row.Price;
        DefaultCost = row.DefaultCost;
        IsActive = row.IsActive;
        IsEditing = true;
        ErrorMessage = string.Empty;
    }

    /// <summary>ثبت خدمت جدید یا ذخیرهٔ تغییرات تعرفهٔ آزاد.</summary>
    [RelayCommand]
    private async Task SaveServiceAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(ServiceName))
        {
            ErrorMessage = "نام خدمت الزامی است";
            return;
        }
        if (Price < 0m || DefaultCost < 0m)
        {
            ErrorMessage = "قیمت و هزینهٔ تمام‌شده نمی‌توانند منفی باشند";
            return;
        }

        IsBusy = true;
        try
        {
            var body = new
            {
                serviceName = ServiceName.Trim(),
                serviceCode = string.IsNullOrWhiteSpace(ServiceCode) ? null : ServiceCode.Trim(),
                price = Price,
                defaultCost = DefaultCost,
                insuranceShare = 0m,
                patientShare = 100m,
                isActive = IsActive,
                insuranceCompanyId = (long?)null
            };

            var saved = _editingId is long id
                ? await _api.PutAsync($"lookups/services/{id}", body)
                : await _api.PostAsync("lookups/services", body);

            if (!saved)
            {
                ErrorMessage = _api.LastError ?? "ذخیرهٔ خدمت ناموفق بود";
                return;
            }

            _snackbar.Success(_editingId is null ? "خدمت جدید ثبت شد" : "تغییرات خدمت ذخیره شد");
            ResetForm();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ذخیرهٔ خدمت ناموفق بود: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>حذف نرم خدمت؛ سوابق فاکتور حفظ و خدمت غیرفعال می‌شود.</summary>
    [RelayCommand]
    private async Task DeleteServiceAsync(ServiceCatalogRow? row)
    {
        if (row is null || IsBusy) return;
        if (!Views.AppDialog.Confirm("حذف خدمت",
                $"خدمت «{row.ServiceName}» غیرفعال شود؟ سوابق فاکتورهای قبلی حفظ می‌شوند."))
            return;

        IsBusy = true;
        try
        {
            if (!await _api.DeleteAsync($"lookups/services/{row.Id}"))
            {
                _snackbar.Error(_api.LastError ?? "حذف خدمت ناموفق بود");
                return;
            }

            if (_editingId == row.Id) ResetForm();
            _snackbar.Success("خدمت غیرفعال شد و سوابق قبلی حفظ شدند");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _snackbar.Error($"حذف خدمت ناموفق بود: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>پاک‌کردن فرم و بازگشت به حالت ثبت.</summary>
    [RelayCommand]
    private void ResetForm()
    {
        _editingId = null;
        ServiceName = string.Empty;
        ServiceCode = string.Empty;
        Price = 0m;
        DefaultCost = 0m;
        IsActive = true;
        IsEditing = false;
        ErrorMessage = string.Empty;
    }

    private void ApplyFilter()
    {
        var term = SearchText.Trim();
        var filtered = _allRows.Where(x => ShowInactive || x.IsActive);
        if (!string.IsNullOrWhiteSpace(term))
            filtered = filtered.Where(x => x.ServiceName.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                || (x.ServiceCode?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false));

        var filteredRows = filtered.ToList();
        Rows.Clear();
        foreach (var row in filteredRows)
            Rows.Add(row);
        ResultCount = filteredRows.Count;
    }

    /// <summary>پاکت پاسخ فهرست خدمت‌ها.</summary>
    public sealed class ServicesResponse
    {
        public bool IsSuccess { get; set; }
        public List<ServiceDto>? Data { get; set; }
    }

    /// <summary>خدمت تعرفه‌دار از endpoint مدیریت.</summary>
    public sealed class ServiceDto
    {
        public long Id { get; set; }
        public string? ServiceCode { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal DefaultCost { get; set; }
        public bool IsActive { get; set; }
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف شعبه — گام U17</summary>
public partial class BranchRow : ObservableObject
{
    /// <summary>شناسه</summary>
    public long Id { get; init; }

    /// <summary>نام شعبه</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>کد</summary>
    public string? Code { get; init; }

    /// <summary>شهر</summary>
    public string? City { get; init; }

    /// <summary>استان</summary>
    public string? Province { get; init; }

    /// <summary>تلفن</summary>
    public string? Phone { get; init; }

    /// <summary>شعبه اصلی</summary>
    [ObservableProperty]
    private bool _isMain;

    /// <summary>فعال</summary>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>متن نشان اصلی</summary>
    public string MainText => IsMain ? "⭐ اصلی" : "—";
}

/// <summary>
/// ViewModel مدیریت شعبه‌ها — گام U17 (بخش ۵ پرامپت):
/// فهرست + ثبت/ویرایش + تعیین اصلی + حذف
/// </summary>
public partial class BranchesViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "شعبه‌ها";

    /// <summary>فهرست شعبه‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<BranchRow> _branches = new();

    /// <summary>شعبه انتخاب‌شده</summary>
    [ObservableProperty]
    private BranchRow? _selectedBranch;

    /// <summary>نام شعبه جدید</summary>
    [ObservableProperty]
    private string? _newBranchName;

    /// <summary>شهر شعبه جدید</summary>
    [ObservableProperty]
    private string? _newBranchCity;

    /// <summary>سازنده</summary>
    public BranchesViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadAsync();
    }

    /// <summary>بارگذاری شعبه‌ها</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<BranchesResponse>("branches");
            Branches = new ObservableCollection<BranchRow>(
                (response?.Data ?? new List<BranchDto>()).Select(b => new BranchRow
                {
                    Id = b.Id,
                    Name = b.Name,
                    Code = b.Code,
                    City = b.City,
                    Province = b.Province,
                    Phone = b.Phone,
                    IsMain = b.IsMain,
                    IsActive = b.IsActive
                }));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>ثبت شعبه جدید</summary>
    [RelayCommand]
    private async Task AddBranchAsync()
    {
        if (string.IsNullOrWhiteSpace(NewBranchName) || NewBranchName.Trim().Length < 2)
        { _snackbar.Error("نام شعبه حداقل ۲ کاراکتر است"); return; }

        IsBusy = true;
        var ok = await _api.PostAsync("branches", new
        {
            name = NewBranchName.Trim(),
            code = (string?)null,
            address = (string?)null,
            city = NewBranchCity,
            province = (string?)null,
            phone = (string?)null,
            isActive = true
        });
        IsBusy = false;

        if (ok) { _snackbar.Success("شعبه ایجاد شد"); NewBranchName = null; NewBranchCity = null; await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "ایجاد شعبه ناموفق بود");
    }

    /// <summary>تعیین شعبه اصلی</summary>
    [RelayCommand]
    private async Task SetMainAsync(BranchRow? row)
    {
        row ??= SelectedBranch;
        if (row is null) return;
        IsBusy = true;
        var ok = await _api.PostAsync($"branches/{row.Id}/set-main", null);
        IsBusy = false;
        if (ok) { _snackbar.Success($"«{row.Name}» شعبه اصلی شد"); await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "عملیات ناموفق بود");
    }

    /// <summary>حذف شعبه غیراصلی</summary>
    [RelayCommand]
    private async Task DeleteBranchAsync(BranchRow? row)
    {
        row ??= SelectedBranch;
        if (row is null) return;
        if (row.IsMain) { _snackbar.Error("شعبه اصلی قابل حذف نیست"); return; }

        IsBusy = true;
        var ok = await _api.DeleteAsync($"branches/{row.Id}");
        IsBusy = false;
        if (ok) { _snackbar.Success("شعبه حذف شد"); await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "حذف ناموفق بود");
    }

    /* ─── پاسخ‌ها ─── */

    private sealed class BranchesResponse
    {
        public bool IsSuccess { get; set; }
        public List<BranchDto>? Data { get; set; }
    }

    private sealed class BranchDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? Phone { get; set; }
        public bool IsMain { get; set; }
        public bool IsActive { get; set; }
    }
}

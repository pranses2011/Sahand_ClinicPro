using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف نسخه — منطبق با PrescriptionListDto سرور</summary>
public record PrescriptionRow(
    long Id,
    string? Code,
    string PatientName,
    string DoctorName,
    string Date,
    string TypeText,
    string StatusText,
    string MediaText);

/// <summary>
/// ViewModel نسخه‌نویسی — فهرست صفحه‌بندی‌شده با فیلتر وضعیت و جستجو
/// </summary>
public partial class PrescriptionsViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private const int PageSize = 20;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "نسخه‌نویسی";

    /// <summary>نسخه‌های صفحه فعلی</summary>
    [ObservableProperty]
    private ObservableCollection<PrescriptionRow> _prescriptions = new();

    /// <summary>نسخه انتخاب‌شده</summary>
    [ObservableProperty]
    private PrescriptionRow? _selectedPrescription;

    /// <summary>جستجو — کد نسخه یا نام بیمار</summary>
    [ObservableProperty]
    private string _searchTerm = string.Empty;

    /// <summary>شماره صفحه</summary>
    [ObservableProperty]
    private int _pageNumber = 1;

    /// <summary>تعداد صفحات</summary>
    [ObservableProperty]
    private int _totalPages = 1;

    /// <summary>تعداد کل</summary>
    [ObservableProperty]
    private int _totalCount;

    /// <summary>نمایه وضعیت فیلتر — صفر یعنی همه</summary>
    [ObservableProperty]
    private int _statusFilterIndex;

    /// <summary>برچسب وضعیت‌ها</summary>
    public string[] StatusLabels { get; } =
        { "صادر شده", "ارسال به بیمه", "تأیید بیمه", "تحویل شده", "ابطال شده" };

    /// <summary>پزشکان — برای فرم صدور</summary>
    public ObservableCollection<DoctorOption> Doctors { get; } = new();

    /// <summary>سازنده با تزریق کلاینت API</summary>
    public PrescriptionsViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = InitializeAsync();
    }

    /// <summary>بارگذاری پزشکان و فهرست</summary>
    private async Task InitializeAsync()
    {
        var page = await _api.GetAsync<AppointmentsViewModel.DoctorsPage>("doctors?page=1&pageSize=200");
        if (page?.Data is not null)
            foreach (var d in page.Data)
                Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName));

        await LoadAsync();
    }

    /// <summary>بارگذاری صفحه فعلی</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var endpoint = $"prescriptions?page={PageNumber}&pageSize={PageSize}" +
                       (StatusFilterIndex > 0 ? $"&status={StatusFilterIndex}" : string.Empty) +
                       (!string.IsNullOrWhiteSpace(SearchTerm)
                           ? $"&search={Uri.EscapeDataString(SearchTerm.Trim())}"
                           : string.Empty);

        var response = await _api.GetAsync<PrescriptionsPage>(endpoint);

        if (response?.Data is not null)
        {
            Prescriptions = new ObservableCollection<PrescriptionRow>(
                response.Data.Select(MapRow));
            PageNumber = response.PageNumber;
            TotalPages = response.TotalPages;
            TotalCount = response.TotalCount;
        }
        else
        {
            ErrorMessage = _api.LastError ?? "دریافت نسخه‌ها ناموفق بود";
        }

        IsBusy = false;
    }

    /// <summary>نگاشت ردیف</summary>
    private static PrescriptionRow MapRow(PrescriptionDto p) => new(
        p.Id, p.PrescriptionCode, p.PatientName, p.DoctorName, p.DateShamsi,
        TypeTextOf(p.Type), StatusTextOf(p.Status),
        p.IsElectronic ? "الکترونیک" : "کاغذی");

    /// <summary>متن فارسی نوع</summary>
    private static string TypeTextOf(int type) => type switch
    {
        1 => "دارویی",
        2 => "پاراکلینیکی",
        3 => "ترکیبی",
        _ => "سایر"
    };

    /// <summary>متن فارسی وضعیت</summary>
    private static string StatusTextOf(int status) => status is >= 1 and <= 5
        ? new[] { "صادر شده", "ارسال به بیمه", "تأیید بیمه", "تحویل شده", "ابطال شده" }[status - 1]
        : "نامشخص";

    /// <summary>جستجو — بازگشت به صفحه اول</summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadAsync();
    }

    /// <summary>صفحه بعد</summary>
    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (PageNumber < TotalPages)
        {
            PageNumber++;
            await LoadAsync();
        }
    }

    /// <summary>صفحه قبل</summary>
    [RelayCommand]
    private async Task PrevPageAsync()
    {
        if (PageNumber > 1)
        {
            PageNumber--;
            await LoadAsync();
        }
    }

    /// <summary>صدور نسخه جدید</summary>
    [RelayCommand]
    private void NewPrescription()
    {
        var vm = new PrescriptionFormViewModel(_api, _snackbar, Doctors);
        var window = new PrescriptionFormWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>مشاهده جزئیات نسخه انتخاب‌شده</summary>
    [RelayCommand]
    private void ViewDetails()
    {
        if (SelectedPrescription is null) return;

        var vm = new PrescriptionDetailViewModel(_api, SelectedPrescription.Id);
        var window = new PrescriptionDetailWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>پاسخ صفحه نسخه‌ها</summary>
    public sealed class PrescriptionsPage
    {
        public bool IsSuccess { get; set; }
        public List<PrescriptionDto>? Data { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>DTO نسخه</summary>
    public sealed class PrescriptionDto
    {
        public long Id { get; set; }
        public string? PrescriptionCode { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string DateShamsi { get; set; } = string.Empty;
        public int Type { get; set; }
        public int Status { get; set; }
        public bool IsElectronic { get; set; }
    }
}
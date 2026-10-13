using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ردیف نتیجهٔ جستجوی یکپارچه — چرخهٔ ۶۹ (فاز ۳ / F-06)
/// </summary>
/// <param name="Type">نوع نتیجه: patient / appointment / prescription — مقصد ناوبری</param>
/// <param name="Id">شناسه رکورد</param>
/// <param name="Title">عنوان اصلی (نام بیمار / شماره نوبت / شماره نسخه)</param>
/// <param name="Subtitle">توضیح دوم (کد ملی·موبایل / ساعت·پزشک / بیمار·تاریخ)</param>
/// <param name="GroupTitle">سرگروه فارسی حوزه (بیماران / نوبت‌های امروز / نسخه‌های اخیر)</param>
/// <param name="IconKind">آیکون PackIcon ردیف</param>
public record QuickSearchRow(
    string Type, long Id, string Title, string Subtitle, string GroupTitle, string IconKind);

/// <summary>
/// ViewModel جستجوی یکپارچه — چرخهٔ ۶۹ (فاز ۳ / F-06 «جستجوی یکپارچه Ctrl+K»)
///
/// پنجرهٔ شناورCtrl+K با تایپِ زنده (مکث ۲۵۰ms) سه حوزه را با هم می‌گردد:
/// بیماران (باز کردن پرونده)، نوبت‌های امروز (رفتن به نوبت‌دهی)، نسخه‌های
/// اخیر (رفتن به نسخه‌ها). کلیدهای بالا/پایین جابه‌جایی، Enter اجرا، Esc بستن.
/// </summary>
public partial class QuickSearchViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly INavigationService _navigation;
    private readonly Action _closeHost;
    private System.Windows.Threading.DispatcherTimer? _debounce;

    /// <summary>عبارت جستجو</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>نتایج گروه‌بندی‌شده</summary>
    public ObservableCollection<QuickSearchRow> Results { get; } = new();

    /// <summary>نتیجهٔ انتخاب‌شده (با کلید یا موس)</summary>
    [ObservableProperty]
    private QuickSearchRow? _selectedResult;

    /// <summary>در حال جستجو</summary>
    [ObservableProperty]
    private bool _isSearching;

    /// <summary>هیچ نتیجه‌ای نیست؟ (حالت خالی)</summary>
    [ObservableProperty]
    private bool _hasNoResults;

    /// <summary>راهنمای کوتاه پایین پنجره</summary>
    public string HintText { get; } = "↑↓ جابه‌جایی · Enter باز کردن · Esc بستن";

    /// <summary>
    /// سازنده — closeHost بستن پنجرهٔ میزبان را بر عهده دارد (View)
    /// </summary>
    public QuickSearchViewModel(IApiClient api, INavigationService navigation, Action closeHost)
    {
        _api = api;
        _navigation = navigation;
        _closeHost = closeHost;
    }

    /// <summary>تغییر متن — جستجوی زنده با مکث ۲۵۰ms (نه با هر کلید)</summary>
    partial void OnSearchTextChanged(string value)
    {
        _debounce ??= new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _debounce.Stop();
        _debounce.Tick -= DebounceTick;
        _debounce.Tick += DebounceTick;
        _debounce.Start();
    }

    /// <summary>انقضای مکث — اجرای جستجو روی نخ UI</summary>
    private void DebounceTick(object? sender, EventArgs e)
    {
        _debounce?.Stop();
        _ = SearchAsync();
    }

    /// <summary>اجرای جستجوی یکپارچه از سرور و پر کردن فهرست گروه‌بندی‌شده</summary>
    private async Task SearchAsync()
    {
        var term = SearchText?.Trim() ?? string.Empty;
        if (term.Length < 2)
        {
            Results.Clear();
            HasNoResults = false;
            return;
        }

        IsSearching = true;
        try
        {
            // پاکت استاندارد ApiResponse — داده در ویژگی data (درس چرخهٔ ۴۸)
            var root = await _api.GetAsync<JsonElement>($"search?q={Uri.EscapeDataString(term)}");
            var data = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d)
                ? d : root;

            Results.Clear();
            if (data.ValueKind == JsonValueKind.Object)
            {
                AddGroup(data, "patients", "بیماران", "Account");
                AddGroup(data, "appointments", "نوبت‌های امروز", "CalendarClock");
                AddGroup(data, "prescriptions", "نسخه‌های اخیر", "Prescription");
            }

            HasNoResults = Results.Count == 0;
            SelectedResult = Results.FirstOrDefault();
        }
        catch
        {
            // خطای شبکه — فهرست خالی + پیام حالت خالی (جستجوی بعدی با تایپ دوباره)
            Results.Clear();
            HasNoResults = true;
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>افزودن یک حوزه از پاسخ سرور به فهرست با سرگروه فارسی</summary>
    private void AddGroup(JsonElement data, string property, string groupTitle, string icon)
    {
        if (!data.TryGetProperty(property, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return;

        foreach (var item in arr.EnumerateArray())
        {
            Results.Add(new QuickSearchRow(
                item.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
                item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number ? id.GetInt64() : 0,
                item.TryGetProperty("title", out var ti) ? ti.GetString() ?? "" : "",
                item.TryGetProperty("subtitle", out var su) ? su.GetString() ?? "" : "",
                groupTitle, icon));
        }
    }

    /// <summary>
    /// اجرای نتیجهٔ انتخاب‌شده — باز کردن پرونده بیمار یا ناوبری به صفحهٔ مقصد
    /// </summary>
    [RelayCommand]
    private void OpenResult(QuickSearchRow? row)
    {
        row ??= SelectedResult;
        if (row is null)
            return;

        switch (row.Type)
        {
            case "patient":
                // پروندهٔ بیمار مستقیم باز می‌شود — همان تجربهٔ دابل‌کلیک فهرست بیماران
                var window = new Views.PatientDetailWindow(
                    new PatientDetailViewModel(_api, row.Id))
                {
                    Owner = System.Windows.Application.Current?.MainWindow
                };
                _closeHost();
                window.ShowDialog();
                break;

            case "appointment":
                _navigation.NavigateTo("appointments");
                _closeHost();
                break;

            case "prescription":
                _navigation.NavigateTo("prescriptions");
                _closeHost();
                break;
        }
    }

    /// <summary>بستن با Esc</summary>
    [RelayCommand]
    private void Close() => _closeHost();
}

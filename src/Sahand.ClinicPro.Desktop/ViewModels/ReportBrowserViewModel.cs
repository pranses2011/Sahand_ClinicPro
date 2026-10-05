using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>دسته گزارش در سایدبار — فرم ۲۷</summary>
public record ReportCategory(string Icon, string Title, int Count, List<ReportCatalogItem> Reports);

/// <summary>ردیف گزارش در کاتالوگ</summary>
public record ReportCatalogItem(string Id, string Title, string Description, List<string> ParamsList);

/// <summary>سلول جدول نتیجه گزارش</summary>
public record ReportResultCell(string Text);

/// <summary>ردیف نتیجه گزارش</summary>
public record ReportResultRow(List<ReportResultCell> Cells);

/// <summary>کارت خلاصه نتیجه</summary>
public record ReportSummaryCard(string Label, string Value, string Color);

/// <summary>
/// ViewModel گزارش‌ساز — گام U16 طبق فرم ۲۷:
/// سایدبار دسته‌ها + کاتالوگ ۴۰+ گزارش + پارامترهای داینامیک + اجرا و نتایج
/// </summary>
public partial class ReportBrowserViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "گزارش‌ها";

    /// <summary>دسته‌های سایدبار</summary>
    [ObservableProperty]
    private ObservableCollection<ReportCategory> _categories = new();

    /// <summary>دسته انتخاب‌شده</summary>
    [ObservableProperty]
    private ReportCategory? _selectedCategory;

    /// <summary>گزارش انتخاب‌شده در کاتالوگ</summary>
    [ObservableProperty]
    private ReportCatalogItem? _selectedReport;

    /// <summary>جستجوی گزارش</summary>
    [ObservableProperty]
    private string? _search;

    /// <summary>عنوان و توضیح گزارش در پنل نتیجه</summary>
    [ObservableProperty]
    private string _reportTitle = "گزارشی انتخاب نشده";

    /// <summary>توضیح گزارش</summary>
    [ObservableProperty]
    private string _reportDescription = "از دسته‌های سمت راست یک گزارش انتخاب کنید";

    /// <summary>نمایش پارامتر تاریخ از</summary>
    [ObservableProperty]
    private bool _hasDateRange;

    /// <summary>از تاریخ</summary>
    [ObservableProperty]
    private DateTime _fromDate = DateTime.Today.AddDays(-30);

    /// <summary>تا تاریخ</summary>
    [ObservableProperty]
    private DateTime _toDate = DateTime.Today;

    /// <summary>متن دکمه اجرا</summary>
    [ObservableProperty]
    private bool _isReportAvailable;

    /// <summary>پیام گزارش در دسترس نیست</summary>
    [ObservableProperty]
    private string? _unavailableMessage;

    /// <summary>کارت‌های خلاصه</summary>
    public ObservableCollection<ReportSummaryCard> SummaryCards { get; } = new();

    /// <summary>ستون‌های جدول</summary>
    public ObservableCollection<string> Columns { get; } = new();

    /// <summary>ردیف‌های نتیجه</summary>
    public ObservableCollection<ReportResultRow> Rows { get; } = new();

    /// <summary>سازنده</summary>
    public ReportBrowserViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadCatalogAsync();
    }

    /// <summary>بارگذاری کاتالوگ گزارش‌ها</summary>
    [RelayCommand]
    private async Task LoadCatalogAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<CatalogResponse>("reports/catalog");
            Categories = new ObservableCollection<ReportCategory>(
                (response?.Data ?? new List<CategoryDto>()).Select(c => new ReportCategory(
                    c.Icon, c.Category,
                    c.Reports.Count,
                    c.Reports.Select(r => new ReportCatalogItem(
                        r.Id, r.Title, r.Description, r.ParamsList)).ToList())));
            SelectedCategory = Categories.FirstOrDefault();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>انتخاب گزارش — بارگذاری پارامترهای داینامیک</summary>
    partial void OnSelectedReportChanged(ReportCatalogItem? value)
    {
        if (value is null) return;
        ReportTitle = value.Title;
        ReportDescription = value.Description;
        HasDateRange = value.ParamsList.Any(p => p.Contains("Date"));
        SummaryCards.Clear();
        Rows.Clear();
        Columns.Clear();
        UnavailableMessage = null;
        IsReportAvailable = true;
    }

    /// <summary>اجرای گزارش انتخاب‌شده</summary>
    [RelayCommand]
    private async Task ExecuteAsync()
    {
        if (SelectedReport is null) { _snackbar.Info("ابتدا یک گزارش انتخاب کنید"); return; }

        IsBusy = true;
        try
        {
            var from = FromDate.ToString("yyyy-MM-dd");
            var to = ToDate.ToString("yyyy-MM-dd");
            var response = await _api.GetAsync<ExecuteResponse>(
                $"reports/execute/{SelectedReport.Id}?fromDate={from}&toDate={to}");
            var data = response?.Data;
            if (data is null)
            {
                _snackbar.Error(_api.LastError ?? "اجرای گزارش ناموفق بود");
                return;
            }

            IsReportAvailable = data.Available;
            UnavailableMessage = data.Available ? null : data.Message;

            SummaryCards.Clear();
            if (data.Summary is not null)
                foreach (var prop in data.Summary.Value.EnumerateObject())
                {
                    var value = prop.Value.ValueKind switch
                    {
                        JsonValueKind.Number => FormatNumber(prop.Value.GetDecimal()),
                        JsonValueKind.String => prop.Value.GetString() ?? "—",
                        _ => prop.Value.ToString()
                    };
                    SummaryCards.Add(new ReportSummaryCard(prop.Name, value, "#1E88E5"));
                }

            Columns.Clear();
            foreach (var col in data.Columns ?? new List<string>())
                Columns.Add(col);

            Rows.Clear();
            if (data.Rows is not null)
            {
                foreach (var row in data.Rows)
                {
                    var cells = new List<ReportResultCell>();
                    foreach (var cell in row.EnumerateObject())
                    {
                        var text = cell.Value.ValueKind switch
                        {
                            JsonValueKind.Number => cell.Value.TryGetInt64(out var l)
                                ? l.ToString("N0")
                                : FormatNumber(cell.Value.GetDecimal()),
                            JsonValueKind.String => cell.Value.GetString() ?? "—",
                            _ => cell.Value.ToString()
                        };
                        cells.Add(new ReportResultCell(text));
                    }
                    Rows.Add(new ReportResultRow(cells));
                }
            }

            if (data.Available)
                _snackbar.Success($"گزارش «{SelectedReport.Title}» اجرا شد — {Rows.Count} ردیف");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>خروجی اکسل از نتایج فعلی — CSV با BOM برای سازگاری فارسی</summary>
    [RelayCommand]
    private void ExportExcel()
    {
        if (Rows.Count == 0) { _snackbar.Info("ابتدا گزارش را اجرا کنید"); return; }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV UTF-8|*.csv",
            FileName = $"{SelectedReport?.Id ?? "report"}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };
        if (dialog.ShowDialog() != true) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(string.Join(",", Columns.Select(c => $"\"{c}\"")));
        foreach (var row in Rows)
            sb.AppendLine(string.Join(",", row.Cells.Select(c => $"\"{c.Text}\"")));
        System.IO.File.WriteAllText(dialog.FileName, sb.ToString(), new System.Text.UTF8Encoding(true));
        _snackbar.Success("خروجی CSV ذخیره شد");
    }

    /// <summary>قالب‌بندی عدد با جداکننده</summary>
    private static string FormatNumber(decimal value)
        => value == Math.Truncate(value) ? ((long)value).ToString("N0") : value.ToString("N2");

    /* ─── پاسخ‌ها ─── */

    private sealed class CatalogResponse
    {
        public bool IsSuccess { get; set; }
        public List<CategoryDto>? Data { get; set; }
    }

    private sealed class CategoryDto
    {
        public string Icon { get; set; } = "";
        public string Category { get; set; } = "";
        public List<ReportDto> Reports { get; set; } = new();
    }

    private sealed class ReportDto
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> ParamsList { get; set; } = new();
    }

    private sealed class ExecuteResponse
    {
        public bool IsSuccess { get; set; }
        public ExecuteData? Data { get; set; }
    }

    private sealed class ExecuteData
    {
        public bool Available { get; set; }
        public string? Message { get; set; }
        public JsonElement? Summary { get; set; }
        public List<string>? Columns { get; set; }
        public List<JsonElement>? Rows { get; set; }
    }
}

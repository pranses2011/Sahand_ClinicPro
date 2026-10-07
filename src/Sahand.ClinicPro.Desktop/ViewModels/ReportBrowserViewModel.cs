using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Desktop.Controls.Charts;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;
using Sahand.ClinicPro.Desktop.Helpers;

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

    /// <summary>نمایش انتخابگر بیمار — گام P1</summary>
    [ObservableProperty]
    private bool _hasPatientParam;

    /// <summary>نمایش فیلد تعداد ماه — گام P1</summary>
    [ObservableProperty]
    private bool _hasMonthsParam;

    /// <summary>نمایش فیلد کد ICD — گام P1</summary>
    [ObservableProperty]
    private bool _hasIcdParam;

    /// <summary>نمایش فیلد حداقل مبلغ — گام P1</summary>
    [ObservableProperty]
    private bool _hasMinAmountParam;

    /// <summary>نمایش فیلد تعداد نتایج — گام P1</summary>
    [ObservableProperty]
    private bool _hasTopParam;

    /// <summary>عبارت جستجوی بیمار (نام/کد ملی/موبایل/پرونده) — گام P1</summary>
    [ObservableProperty]
    private string? _patientSearchTerm;

    /// <summary>نتایج جستجوی بیمار — گام P1</summary>
    public ObservableCollection<PatientFormViewModel.PatientDetail> PatientResults { get; } = new();

    /// <summary>بیمار انتخاب‌شده برای گزارش‌های محاسبه‌مرکز بیمار — گام P1</summary>
    [ObservableProperty]
    private PatientFormViewModel.PatientDetail? _selectedPatient;

    /// <summary>تعداد ماه برای گزارش‌های بازه ماهانه — گام P1</summary>
    [ObservableProperty]
    private int _months = 6;

    /// <summary>کد ICD اختیاری — پیشوند تشخیص (خالی = مجموعه مزمن پیش‌فرض) — گام P1</summary>
    [ObservableProperty]
    private string? _icdCode;

    /// <summary>حداقل مبلغ (تومان) برای فیلتر بدهکاران — گام P1</summary>
    [ObservableProperty]
    private decimal _minAmount;

    /// <summary>سقف تعداد ردیف‌های نتایج — گام P1</summary>
    [ObservableProperty]
    private int _topCount = 20;

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

    /* ── بخش نمودار — چرخه یازدهم: سرور بلاک chart برمی‌گرداند و کلاینت نمودارِ متناسب رندر می‌کند ── */

    /// <summary>گزارش جاری بلاک نمودار دارد؟</summary>
    [ObservableProperty]
    private bool _hasChart;

    /// <summary>نوع نمودار سروری: area | column | doughnut | hbar</summary>
    [ObservableProperty]
    private string _chartKind = "";

    /// <summary>عنوان نمودار</summary>
    [ObservableProperty]
    private string _chartTitle = "";

    /// <summary>واحد محور مقادیر — زیرنویس عنوان</summary>
    [ObservableProperty]
    private string _chartUnit = "";

    /// <summary>متن مرکز دونات — جمع کل قالب‌بندی‌شده</summary>
    [ObservableProperty]
    private string _chartCenterText = "";

    /// <summary>نقاط نمودار</summary>
    [ObservableProperty]
    private IReadOnlyList<ChartPointData>? _chartPoints;

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
        // پارامترهای تخصصی گام P1 — بر اساس فهرست paramsList سرور
        HasPatientParam = value.ParamsList.Contains("patientId");
        HasMonthsParam = value.ParamsList.Contains("months");
        HasIcdParam = value.ParamsList.Contains("icd10");
        HasMinAmountParam = value.ParamsList.Contains("minAmount");
        HasTopParam = value.ParamsList.Contains("top");
        PatientResults.Clear();
        SelectedPatient = null;
        PatientSearchTerm = null;
        Months = 6;
        IcdCode = null;
        MinAmount = 0;
        TopCount = 20;
        SummaryCards.Clear();
        Rows.Clear();
        Columns.Clear();
        HasChart = false;
        ChartPoints = null;
        UnavailableMessage = null;
        IsReportAvailable = true;
    }

    /// <summary>جستجوی بیمار برای گزارش‌های بیمارمرکز — گام P1</summary>
    [RelayCommand]
    private async Task SearchPatientsAsync()
    {
        var term = PatientSearchTerm?.Trim();
        if (string.IsNullOrEmpty(term)) { _snackbar.Info("نام، کد ملی، موبایل یا شماره پرونده را وارد کنید"); return; }

        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<PatientFormViewModel.SearchResponse>(
                $"patients/search?term={Uri.EscapeDataString(term)}");
            PatientResults.Clear();
            foreach (var p in (response?.Data ?? new List<PatientFormViewModel.PatientDetail>()).Take(15))
                PatientResults.Add(p);
            if (PatientResults.Count == 0) _snackbar.Info("بیماری یافت نشد");
        }
        finally { IsBusy = false; }
    }

    /// <summary>انتخاب بیمار از نتایج — ثبت شناسه برای اجرای گزارش</summary>
    partial void OnSelectedPatientChanged(PatientFormViewModel.PatientDetail? value)
    {
        if (value is not null)
            _snackbar.Info($"بیمار انتخاب شد: {value.FirstName} {value.LastName} — پرونده {value.MedicalRecordNo}");
    }

    /// <summary>اجرای گزارش انتخاب‌شده — با پارامترهای داینامیک گام P1</summary>
    [RelayCommand]
    private async Task ExecuteAsync()
    {
        if (SelectedReport is null) { _snackbar.Info("ابتدا یک گزارش انتخاب کنید"); return; }
        if (HasPatientParam && SelectedPatient is null)
        { _snackbar.Error("این گزارش به بیمار نیاز دارد — ابتدا بیمار را جستجو و انتخاب کنید"); return; }

        IsBusy = true;
        try
        {
            // ساخت رشته پرس‌وجو — فقط پارامترهای مرتبط با گزارش جاری ارسال می‌شوند
            var qs = new List<string>();
            if (HasDateRange)
            {
                qs.Add($"fromDate={FromDate:yyyy-MM-dd}");
                qs.Add($"toDate={ToDate:yyyy-MM-dd}");
            }
            if (HasPatientParam && SelectedPatient is not null) qs.Add($"patientId={SelectedPatient.Id}");
            if (HasMonthsParam) qs.Add($"months={Months}");
            if (HasIcdParam && !string.IsNullOrWhiteSpace(IcdCode)) qs.Add($"icd10={Uri.EscapeDataString(IcdCode.Trim())}");
            if (HasMinAmountParam && MinAmount > 0) qs.Add($"minAmount={MinAmount}");
            if (HasTopParam) qs.Add($"top={TopCount}");
            var response = await _api.GetAsync<ExecuteResponse>(
                $"reports/execute/{SelectedReport.Id}" + (qs.Count > 0 ? "?" + string.Join("&", qs) : ""));
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
                                ? Money.Format(l)
                                : FormatNumber(cell.Value.GetDecimal()),
                            JsonValueKind.String => cell.Value.GetString() ?? "—",
                            _ => cell.Value.ToString()
                        };
                        cells.Add(new ReportResultCell(text));
                    }
                    Rows.Add(new ReportResultRow(cells));
                }
            }

            // بلاک نمودار — چرخه یازدهم: رندر بر اساس نوع اعلام‌شده سرور
            HasChart = data.Chart is not null;
            ChartPoints = null;
            if (data.Chart is not null)
            {
                var c = data.Chart.Value;
                ChartKind = c.TryGetProperty("type", out var t) ? t.GetString() ?? "column" : "column";
                ChartTitle = c.TryGetProperty("title", out var ti) ? ti.GetString() ?? "" : "";
                ChartUnit = c.TryGetProperty("unit", out var u) ? u.GetString() ?? "" : "";
                var pts = new List<ChartPointData>();
                if (c.TryGetProperty("points", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in arr.EnumerateArray())
                    {
                        var label = p.TryGetProperty("label", out var l) ? l.GetString() ?? "—" : "—";
                        var value = p.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.Number
                            ? v.GetDouble() : 0;
                        pts.Add(new ChartPointData(label, value, $"{label}: {value:N0} {ChartUnit}"));
                    }
                }
                ChartPoints = pts;
                ChartCenterText = pts.Count > 0 ? Money.Format((decimal)pts.Sum(x => x.Value)) : "";
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

    /// <summary>خروجی PDF با سربرگ درمانگاه — چرخه سیزدهم (درخواست ۴-۶)</summary>
    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        if (Rows.Count == 0) { _snackbar.Info("ابتدا گزارش را اجرا کنید"); return; }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "فایل PDF|*.pdf",
            FileName = $"{SelectedReport?.Id ?? "report"}_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
        };
        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        try
        {
            var body = new
            {
                title = ReportTitle,
                summary = SummaryCards.ToDictionary(s => s.Label, s => s.Value),
                columns = Columns.ToList(),
                rows = Rows.Select(r => r.Cells.Select(c => c.Text).ToList()).ToList(),
            };
            var pdf = await _api.PostDownloadAsync("reports/pdf", body);
            if (pdf is null || pdf.Length == 0)
            {
                _snackbar.Error(_api.LastError ?? "تولید PDF ناموفق بود");
                return;
            }
            await System.IO.File.WriteAllBytesAsync(dialog.FileName, pdf);
            _snackbar.Success($"PDF با سربرگ درمانگاه ذخیره شد ({pdf.Length / 1024.0:F0} KB)");
        }
        finally { IsBusy = false; }
    }

    /// <summary>گشودن پنجره گزارش‌ساز رایگانه (کوئری‌ساز گرافیکی) — چرخه سیزدهم (درخواست ۴-۳)</summary>
    [RelayCommand]
    private void OpenAdhocBuilder()
    {
        var vm = App.Services.GetRequiredService<AdhocReportViewModel>();
        var window = new AdhocReportWindow(vm) { Owner = System.Windows.Application.Current.MainWindow };
        window.Show();
    }

    /// <summary>قالب‌بندی عدد با جداکننده</summary>
    private static string FormatNumber(decimal value)
        => value == Math.Truncate(value) ? Money.Format(value) : value.ToString("N2");

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
        public JsonElement? Chart { get; set; }
    }
}

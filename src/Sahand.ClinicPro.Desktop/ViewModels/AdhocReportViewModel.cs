using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Controls.Charts;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>فیلد لیست سفید با وضعیت انتخاب — گام ۱ گزارش‌ساز</summary>
public partial class AdhocFieldItem : ObservableObject
{
    /// <summary>نام فنی فیلد</summary>
    public string Name { get; init; } = "";

    /// <summary>عنوان فارسی</summary>
    public string Title { get; init; } = "";

    /// <summary>نوع نمایشی</summary>
    public string Type { get; init; } = "";

    /// <summary>گزینه‌های شمارشی — برای ورودی فیلتر</summary>
    public List<string>? Options { get; init; }

    /// <summary>انتخاب‌شده؟</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>قابل فیلتر؟ (فیلدهای محاسبه‌شده نه)</summary>
    public bool Filterable { get; init; } = true;
}

/// <summary>یک ردیف فیلتر — گام ۲ گزارش‌ساز</summary>
public partial class AdhocFilterRow : ObservableObject
{
    /// <summary>نام فیلد انتخابی</summary>
    [ObservableProperty]
    private AdhocFieldItem? _field;

    /// <summary>کلید عملگر</summary>
    [ObservableProperty]
    private string _op = "eq";

    /// <summary>مقدار اول</summary>
    [ObservableProperty]
    private string? _value1;

    /// <summary>مقدار دوم (مابین)</summary>
    [ObservableProperty]
    private string? _value2;

    /// <summary>عملگر جاری مابین/خالی است؟ (مقدار دوم لازم یا هیچ مقدار لازم نیست)</summary>
    public bool NeedsSecondValue => Op is "between";

    /// <summary>عملگر جاری مقدار نمی‌خواهد؟</summary>
    public bool NeedsNoValue => Op is "empty" or "notEmpty";

    /// <summary>بازخورد تغییر عملگر — تازه‌سازی قابلیت ورودی‌ها</summary>
    partial void OnOpChanged(string value)
    {
        OnPropertyChanged(nameof(NeedsSecondValue));
        OnPropertyChanged(nameof(NeedsNoValue));
    }
}

/// <summary>تعریف ذخیره‌شده در فهرست</summary>
public record SavedAdhocReport(long Id, string ReportName, string? Code, string? Parameters);

/// <summary>
/// ViewModel گزارش‌ساز رایگانه (کوئری‌ساز گرافیکی) — چرخه سیزدهم (درخواست ۴-۳).
/// سه گام: انتخاب موجودیت و ستون‌ها ← فیلترها ← گروه‌بندی/تجمیع؛
/// اجرای امن سمت سرور + ذخیره/اجرا مجدد تعریف + خروجی جدول/نمودار/CSV.
/// </summary>
public partial class AdhocReportViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان پنجره</summary>
    public override string Title => "گزارش‌ساز رایگانه";

    /* ── گام ۱: موجودیت و ستون‌ها ── */

    /// <summary>موجودیت‌های لیست سفید</summary>
    public ObservableCollection<string> Entities { get; } = new();

    /// <summary>موجودیت انتخابی</summary>
    [ObservableProperty]
    private string? _selectedEntity;

    /// <summary>فیلدهای موجودیت انتخابی</summary>
    public ObservableCollection<AdhocFieldItem> Fields { get; } = new();

    /* ── گام ۲: فیلترها ── */

    /// <summary>ردیف‌های فیلتر</summary>
    public ObservableCollection<AdhocFilterRow> Filters { get; } = new();

    /// <summary>فیلترهای قابل انتخاب (بدون فیلدهای محاسبه‌شده)</summary>
    public ObservableCollection<AdhocFieldItem> FilterableFields { get; } = new();

    /* ── گام ۳: گروه/تجمیع ── */

    /// <summary>فیلد گروه‌بندی انتخابی (خالی = بدون گروه)</summary>
    [ObservableProperty]
    private AdhocFieldItem? _groupByField;

    /// <summary>عمل تجمیع انتخابی</summary>
    [ObservableProperty]
    private string _aggregate = "count";

    /// <summary>فیلد عددی تجمیع</summary>
    [ObservableProperty]
    private AdhocFieldItem? _aggregateField;

    /// <summary>عمل‌های تجمیع قابل نمایش</summary>
    public List<string> AggregateOptions { get; } = new()
    { "بدون تجمیع (فهرست)", "شمارش", "جمع", "میانگین", "کمینه", "بیشینه" };

    /// <summary>فیلدهای عددی/مبلغی قابل تجمیع</summary>
    public ObservableCollection<AdhocFieldItem> NumericFields { get; } = new();

    /* ── پارامترهای عمومی ── */

    /// <summary>سقف ردیف</summary>
    [ObservableProperty]
    private int _limit = 100;

    /// <summary>نوع نمودار</summary>
    [ObservableProperty]
    private string _chartType = "ستونی";

    /// <summary>گزینه‌های نمودار</summary>
    public List<string> ChartOptions { get; } = new() { "ستونی", "دونات", "میله‌ای", "بدون نمودار" };

    /// <summary>نام گزارش برای ذخیره</summary>
    [ObservableProperty]
    private string? _reportName;

    /// <summary>تعریف‌های ذخیره‌شده</summary>
    public ObservableCollection<SavedAdhocReport> SavedReports { get; } = new();

    /// <summary>تعریف ذخیره‌شده انتخابی</summary>
    [ObservableProperty]
    private SavedAdhocReport? _selectedSavedReport;

    /* ── نتایج ── */

    /// <summary>کارت‌های خلاصه</summary>
    public ObservableCollection<ReportSummaryCard> SummaryCards { get; } = new();

    /// <summary>عنوان‌های ستون نتیجه</summary>
    public ObservableCollection<string> Columns { get; } = new();

    /// <summary>ردیف‌های نتیجه</summary>
    public ObservableCollection<ReportResultRow> Rows { get; } = new();

    /// <summary>نتیجه‌ای برای نمایش هست؟</summary>
    [ObservableProperty]
    private bool _hasResult;

    /// <summary>نتیجه در دسترس است؟ (خطای اعتبارسنجی نبود)</summary>
    [ObservableProperty]
    private bool _isAvailable = true;

    /// <summary>پیام خطای اعتبارسنجی سرور</summary>
    [ObservableProperty]
    private string? _unavailableMessage;

    /// <summary>گزارش جاری نمودار دارد؟</summary>
    [ObservableProperty]
    private bool _hasChart;

    /// <summary>نوع نمودار نتیجه: column/doughnut/hbar</summary>
    [ObservableProperty]
    private string _chartKind = "column";

    /// <summary>عنوان نمودار</summary>
    [ObservableProperty]
    private string _chartTitle = "";

    /// <summary>واحد نمودار</summary>
    [ObservableProperty]
    private string _chartUnit = "";

    /// <summary>متن مرکز دونات</summary>
    [ObservableProperty]
    private string _chartCenterText = "";

    /// <summary>نقاط نمودار</summary>
    [ObservableProperty]
    private IReadOnlyList<ChartPointData>? _chartPoints;

    /// <summary>عملگرهای قابل انتخاب — از طرح سرور</summary>
    public ObservableCollection<KeyValuePair<string, string>> Operators { get; } = new();

    /// <summary>سازنده — بارگذاری طرح و فهرست ذخیره‌شده‌ها</summary>
    public AdhocReportViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadSchemaAsync();
        _ = LoadSavedAsync();
    }

    /// <summary>بارگذاری طرح لیست سفید از سرور</summary>
    [RelayCommand]
    private async Task LoadSchemaAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<SchemaResponse>("reports/adhoc/schema");
            var schema = response?.Data;
            if (schema is null)
            {
                _snackbar.Error(_api.LastError ?? "بارگذاری طرح گزارش‌ساز ناموفق بود");
                return;
            }

            _cachedSchema = schema;
            Entities.Clear();
            foreach (var e in schema.Entities) Entities.Add(e.Title);
            foreach (var op in schema.Operators)
                Operators.Add(new KeyValuePair<string, string>(op.Key, op.Title));

            SelectedEntity = Entities.FirstOrDefault();
        }
        finally { IsBusy = false; }
    }

    /// <summary>انتخاب موجودیت — پرکردن فیلدها و فیلدهای قابل فیلتر/تجمیع</summary>
    partial void OnSelectedEntityChanged(string? value)
    {
        if (value is null) return;
        var response = _cachedSchema;
        var entity = response?.Entities.FirstOrDefault(e => e.Title == value);
        if (entity is null) return;

        Fields.Clear();
        FilterableFields.Clear();
        NumericFields.Clear();
        foreach (var f in entity.Fields)
        {
            var item = new AdhocFieldItem
            {
                Name = f.Name, Title = f.Title, Type = f.Type, Options = f.Options, Filterable = f.Filterable,
            };
            Fields.Add(item);
            if (f.Filterable) FilterableFields.Add(item);
            if (f.Type is "number" or "money") NumericFields.Add(item);
        }

        Filters.Clear();
        GroupByField = null;
        AggregateField = NumericFields.FirstOrDefault();
        Aggregate = "شمارش";
        ClearResults();
    }

    /// <summary>طرح کش‌شده — برای یافتن کلید موجودیت از عنوان فارسی</summary>
    private AdhocSchemaData? _cachedSchema;

    /// <summary>افزودن یک ردیف فیلتر خالی</summary>
    [RelayCommand]
    private void AddFilter()
    {
        if (FilterableFields.Count == 0) { _snackbar.Info("این موجودیت فیلد قابل فیلتری ندارد"); return; }
        if (Filters.Count >= 5) { _snackbar.Info("حداکثر ۵ فیلتر هم‌زمان"); return; }
        Filters.Add(new AdhocFilterRow { Field = FilterableFields[0], Op = "eq" });
    }

    /// <summary>حذف ردیف فیلتر</summary>
    [RelayCommand]
    private void RemoveFilter(AdhocFilterRow? row)
    {
        if (row is not null) Filters.Remove(row);
    }

    /// <summary>پاک‌سازی نتایج</summary>
    private void ClearResults()
    {
        SummaryCards.Clear();
        Columns.Clear();
        Rows.Clear();
        HasResult = false;
        HasChart = false;
        ChartPoints = null;
        IsAvailable = true;
        UnavailableMessage = null;
    }

    /// <summary>ساخت DTO تعریف از وضعیت فرم — کلیدهای لاتین برای سرور</summary>
    private object BuildDefinition()
    {
        var entity = _cachedSchema?.Entities.FirstOrDefault(e => e.Title == SelectedEntity);
        var chart = ChartType switch
        {
            "ستونی" => "column", "دونات" => "doughnut", "میله‌ای" => "hbar", _ => "none",
        };
        var agg = Aggregate switch
        {
            "شمارش" => "count", "جمع" => "sum", "میانگین" => "avg",
            "کمینه" => "min", "بیشینه" => "max", _ => "none",
        };
        return new
        {
            entity = entity?.Key ?? "patient",
            fields = Fields.Where(f => f.IsSelected).Select(f => f.Name).ToList(),
            filters = Filters.Select(f => new
            {
                field = f.Field?.Name,
                op = f.Op,
                value1 = f.Value1,
                value2 = f.Value2,
            }).ToList(),
            groupBy = GroupByField?.Name,
            aggregate = agg,
            aggregateField = AggregateField?.Name,
            sortField = (string?)null,
            sortDesc = false,
            limit = Limit,
            chart,
        };
    }

    /// <summary>اجرای گزارش ساخته‌شده</summary>
    [RelayCommand]
    private async Task RunAsync()
    {
        var selected = Fields.Where(f => f.IsSelected).ToList();
        if (selected.Count == 0 && GroupByField is null)
        { _snackbar.Error("حداقل یک ستون انتخاب کنید"); return; }
        if (GroupByField is null && selected.Count == 0) return;

        IsBusy = true;
        try
        {
            var response = await _api.PostAsync<ExecuteResponse>("reports/adhoc/run",
                new { definition = BuildDefinition() });
            var data = response?.Data;
            if (data is null)
            {
                _snackbar.Error(_api.LastError ?? "اجرای گزارش ناموفق بود");
                return;
            }

            IsAvailable = data.Available;
            UnavailableMessage = data.Available ? null : data.Message;
            HasResult = data.Available;
            if (!data.Available) return;

            SummaryCards.Clear();
            if (data.Summary is not null)
                foreach (var prop in data.Summary.Value.EnumerateObject())
                {
                    var value = prop.Value.ValueKind == JsonValueKind.Number
                        ? ((double)prop.Value.GetDouble()).ToString("N0")
                        : prop.Value.GetString() ?? "—";
                    SummaryCards.Add(new ReportSummaryCard(prop.Name, value, "#1E88E5"));
                }

            Columns.Clear();
            foreach (var col in data.Columns ?? new List<string>()) Columns.Add(col);

            Rows.Clear();
            if (data.Rows is not null)
                foreach (var row in data.Rows)
                {
                    var cells = row.Select(c => c.ValueKind switch
                    {
                        JsonValueKind.Number => c.TryGetInt64(out var l) ? l.ToString("N0") : c.GetDouble().ToString("N0"),
                        JsonValueKind.String => c.GetString() ?? "—",
                        _ => c.ToString(),
                    }).ToList();
                    Rows.Add(new ReportResultRow(cells.Select(t => new ReportResultCell(t)).ToList()));
                }

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
                    foreach (var p in arr.EnumerateArray())
                    {
                        var label = p.TryGetProperty("label", out var l) ? l.GetString() ?? "—" : "—";
                        var value = p.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.Number
                            ? v.GetDouble() : 0;
                        pts.Add(new ChartPointData(label, value, $"{label}: {value:N0} {ChartUnit}"));
                    }
                ChartPoints = pts;
                ChartCenterText = pts.Count > 0 ? ((decimal)pts.Sum(x => x.Value)).ToString("N0") : "";
            }

            _snackbar.Success($"گزارش اجرا شد — {Rows.Count} ردیف");
        }
        finally { IsBusy = false; }
    }

    /// <summary>ذخیره تعریف فعلی با نام</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(ReportName)) { _snackbar.Error("نام گزارش را وارد کنید"); return; }
        IsBusy = true;
        try
        {
            var response = await _api.PostAsync<SaveResponse>("reports/adhoc/definitions",
                new { name = ReportName.Trim(), definition = BuildDefinition() });
            if (response?.Data is null)
            {
                _snackbar.Error(_api.LastError ?? "ذخیره گزارش ناموفق بود");
                return;
            }
            _snackbar.Success($"گزارش «{ReportName.Trim()}» ذخیره شد ({response.Data.Code})");
            ReportName = null;
            await LoadSavedAsync();
        }
        finally { IsBusy = false; }
    }

    /// <summary>بارگذاری فهرست تعریف‌های ذخیره‌شده</summary>
    [RelayCommand]
    private async Task LoadSavedAsync()
    {
        var response = await _api.GetAsync<SavedListResponse>("reports/adhoc/definitions");
        SavedReports.Clear();
        foreach (var s in response?.Data ?? new List<SavedDto>())
            SavedReports.Add(new SavedAdhocReport(s.Id, s.ReportName, s.Code, s.Parameters));
    }

    /// <summary>اجرای مجدد تعریف ذخیره‌شده انتخابی</summary>
    [RelayCommand]
    private async Task RunSavedAsync()
    {
        var saved = SelectedSavedReport;
        if (saved is null || string.IsNullOrEmpty(saved.Parameters))
        { _snackbar.Info("ابتدا یک گزارش ذخیره‌شده انتخاب کنید"); return; }

        try
        {
            var doc = JsonDocument.Parse(saved.Parameters);
            var root = doc.RootElement;
            var entityKey = root.TryGetProperty("entity", out var e) ? e.GetString() : null;
            var entity = _cachedSchema?.Entities.FirstOrDefault(x => x.Key == entityKey)
                ?? _cachedSchema?.Entities.FirstOrDefault();
            if (entity is not null) SelectedEntity = entity.Title;

            // بازگرداندن انتخاب فیلدها
            var fieldNames = root.TryGetProperty("fields", out var fl) && fl.ValueKind == JsonValueKind.Array
                ? fl.EnumerateArray().Select(x => x.GetString()).ToHashSet() : new HashSet<string?>();
            foreach (var f in Fields) f.IsSelected = fieldNames.Contains(f.Name);

            // بازگرداندن فیلترها
            Filters.Clear();
            if (root.TryGetProperty("filters", out var fil) && fil.ValueKind == JsonValueKind.Array)
                foreach (var fr in fil.EnumerateArray())
                {
                    var fieldName = fr.TryGetProperty("field", out var fn) ? fn.GetString() : null;
                    var fieldItem = FilterableFields.FirstOrDefault(x => x.Name == fieldName);
                    if (fieldItem is null) continue;
                    Filters.Add(new AdhocFilterRow
                    {
                        Field = fieldItem,
                        Op = fr.TryGetProperty("op", out var op) ? op.GetString() ?? "eq" : "eq",
                        Value1 = fr.TryGetProperty("value1", out var v1) ? v1.GetString() : null,
                        Value2 = fr.TryGetProperty("value2", out var v2) ? v2.GetString() : null,
                    });
                }

            // گروه/تجمیع
            var gb = root.TryGetProperty("groupBy", out var g) ? g.GetString() : null;
            GroupByField = string.IsNullOrEmpty(gb) ? null : Fields.FirstOrDefault(x => x.Name == gb);
            var agg = root.TryGetProperty("aggregate", out var a) ? a.GetString() : "count";
            Aggregate = agg switch
            {
                "count" => "شمارش", "sum" => "جمع", "avg" => "میانگین",
                "min" => "کمینه", "max" => "بیشینه", _ => "بدون تجمیع (فهرست)",
            };
            var agf = root.TryGetProperty("aggregateField", out var af) ? af.GetString() : null;
            AggregateField = NumericFields.FirstOrDefault(x => x.Name == agf);

            if (root.TryGetProperty("limit", out var lm) && lm.TryGetInt32(out var lim)) Limit = lim;
            if (root.TryGetProperty("chart", out var ch))
            {
                ChartType = ch.GetString() switch
                {
                    "doughnut" => "دونات", "hbar" => "میله‌ای", "none" => "بدون نمودار", _ => "ستونی",
                };
            }

            await RunAsync();
        }
        catch (JsonException)
        {
            _snackbar.Error("تعریف ذخیره‌شده خراب است — قابل بازخوانی نیست");
        }
    }

    /// <summary>حذف تعریف ذخیره‌شده انتخابی</summary>
    [RelayCommand]
    private async Task DeleteSavedAsync()
    {
        var saved = SelectedSavedReport;
        if (saved is null) { _snackbar.Info("ابتدا یک گزارش ذخیره‌شده انتخاب کنید"); return; }

        var ok = await _api.DeleteAsync($"reports/adhoc/definitions/{saved.Id}");
        if (ok)
        {
            _snackbar.Success("تعریف حذف شد");
            await LoadSavedAsync();
        }
        else _snackbar.Error(_api.LastError ?? "حذف ناموفق بود");
    }

    /// <summary>خروجی CSV از نتایج فعلی — با BOM برای سازگاری فارسی</summary>
    [RelayCommand]
    private void ExportCsv()
    {
        if (Rows.Count == 0) { _snackbar.Info("ابتدا گزارش را اجرا کنید"); return; }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV UTF-8|*.csv",
            FileName = $"adhoc_{DateTime.Now:yyyyMMdd_HHmm}.csv",
        };
        if (dialog.ShowDialog() != true) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(string.Join(",", Columns.Select(c => $"\"{c}\"")));
        foreach (var row in Rows)
            sb.AppendLine(string.Join(",", row.Cells.Select(c => $"\"{c.Text}\"")));
        System.IO.File.WriteAllText(dialog.FileName, sb.ToString(), new System.Text.UTF8Encoding(true));
        _snackbar.Success("خروجی CSV ذخیره شد");
    }

    /// <summary>نگاشت عنوان عملگر فارسی — برای کمبوباکس فیلتر</summary>
    partial void OnAggregateChanged(string value)
    {
        // بدون عمل خاص — فقط برای سازگاری binding
    }

    /* ─── پاسخ‌ها ─── */

    private sealed class SchemaResponse
    {
        public bool IsSuccess { get; set; }
        public AdhocSchemaData? Data { get; set; }
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
        public List<List<JsonElement>>? Rows { get; set; }
        public JsonElement? Chart { get; set; }
    }

    private sealed class SaveResponse
    {
        public bool IsSuccess { get; set; }
        public SaveData? Data { get; set; }
    }

    private sealed class SaveData
    {
        public long Id { get; set; }
        public string Code { get; set; } = "";
    }

    private sealed class SavedListResponse
    {
        public bool IsSuccess { get; set; }
        public List<SavedDto>? Data { get; set; }
    }

    private sealed class SavedDto
    {
        public long Id { get; set; }
        public string ReportName { get; set; } = "";
        public string? Code { get; set; }
        public string? Parameters { get; set; }
    }
}

/// <summary>طرح گزارش‌ساز رایگانه — پاسخ endpoint schema</summary>
public sealed class AdhocSchemaData
{
    /// <summary>سقف ردیف</summary>
    public int MaxLimit { get; set; }

    /// <summary>سقف ستون</summary>
    public int MaxFields { get; set; }

    /// <summary>موجودیت‌های لیست سفید</summary>
    public List<AdhocSchemaEntity> Entities { get; set; } = new();

    /// <summary>عملگرهای مجاز</summary>
    public List<AdhocSchemaOperator> Operators { get; set; } = new();

    /// <summary>عمل‌های تجمیع</summary>
    public List<AdhocSchemaOperator>? Aggregates { get; set; }
}

/// <summary>موجودیت طرح لیست سفید</summary>
public sealed class AdhocSchemaEntity
{
    /// <summary>کلید لاتین</summary>
    public string Key { get; set; } = "";

    /// <summary>عنوان فارسی</summary>
    public string Title { get; set; } = "";

    /// <summary>آیکن</summary>
    public string Icon { get; set; } = "";

    /// <summary>فیلدها</summary>
    public List<AdhocSchemaField> Fields { get; set; } = new();
}

/// <summary>فیلد طرح لیست سفید</summary>
public sealed class AdhocSchemaField
{
    /// <summary>نام فنی</summary>
    public string Name { get; set; } = "";

    /// <summary>عنوان فارسی</summary>
    public string Title { get; set; } = "";

    /// <summary>نوع: text/number/money/date/enum/boolean/computed</summary>
    public string Type { get; set; } = "";

    /// <summary>قابل فیلتر؟</summary>
    public bool Filterable { get; set; }

    /// <summary>گزینه‌های شمارشی</summary>
    public List<string>? Options { get; set; }
}

/// <summary>عملگر طرح</summary>
public sealed class AdhocSchemaOperator
{
    /// <summary>کلید</summary>
    public string Key { get; set; } = "";

    /// <summary>عنوان فارسی</summary>
    public string Title { get; set; } = "";
}

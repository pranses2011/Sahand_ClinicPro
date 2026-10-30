using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel فرم اختصاصی ثبت نتیجهٔ «یک آزمایش» — چرخهٔ ۶۹ (گزارش مالک:
/// «نحوه ثبت نتیجه خیلی بده. برای ثبت نتیجه باید روی آزمایش دابل کلیک کنیم
/// تا در یک فرم دیگه که مختص اون آزمایش هست، اطلاعات آزمایش رو نشون بده و
/// نتیجه رو اونجا ثبت بکنیم») + فاز ۳ / F-28 (بازهٔ مرجع سنی/جنسیتی).
///
/// برخلاف پنجرهٔ قدیمی که همهٔ اقلام را در یک جدول شلوغ می‌آورد، این فرم
/// فقط «همین آزمایش» را نشان می‌دهد: شناسنامهٔ بیمار (نام/سن — مبنای بازهٔ
/// مرجع)، پنل رنگی «حد طبیعی مخصوص همین بیمار»، فیلدهای بزرگ نتیجه، و
/// ثبت در همین‌جا. پس از ذخیره، سرور تفسیر خودکار (فلگ L/H/LL/HH) را
/// برمی‌گرداند و فرم، وضعیت رنگی نتیجه را همان‌جا نشان می‌دهد.
/// </summary>
public partial class LabTestResultEntryViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly long _itemId;
    private readonly long _orderId;
    private readonly Func<Task>? _onSaved;

    /// <summary>نام آزمایش — عنوان فرم</summary>
    [ObservableProperty]
    private string _testName = string.Empty;

    /// <summary>واحد سنجش آزمایش</summary>
    [ObservableProperty]
    private string? _unit;

    /// <summary>بازهٔ طبیعی عمومی تست (متنی از کاتالوگ)</summary>
    [ObservableProperty]
    private string? _generalRange;

    /// <summary>
    /// بازهٔ مرجع «مخصوص همین بیمار» — سرور با موتور واحد (LabResultInterpreter)
    /// بر اساس سن و جنسیت محاسبه می‌کند (چرخهٔ ۶۹ / F-28).
    /// </summary>
    [ObservableProperty]
    private string _patientRangeText = "—";

    /// <summary>
    /// حدود بحرانی آزمایش — چرخهٔ ۷۹ (گزارش مالک): «کمتر از ۲٫۵ / بیشتر از ۶٫۲»؛
    /// هنگام ثبت نتیجه دیده می‌شود تا آزمایشگر پیش از ثبت هشدار بحرانی را بداند.
    /// تهی = آزمایش بحرانی ثبت‌نشده دارد و خط پنهان می‌شود.
    /// </summary>
    [ObservableProperty]
    private string? _criticalRangeText;

    /// <summary>نام کامل بیمار</summary>
    [ObservableProperty]
    private string _patientName = string.Empty;

    /// <summary>سن بیمار به فارسی — نمایش کنار نام</summary>
    [ObservableProperty]
    private string _patientAgeText = string.Empty;

    /// <summary>شماره درخواست آزمایش</summary>
    [ObservableProperty]
    private string _orderNumber = string.Empty;

    /// <summary>
    /// خط زمانی نمونه — طراحی نسخهٔ ۳ (تأیید مالک): مراحل درخواست تا نتیجه
    /// با علامت مرحلهٔ طی‌شده/جاری، از وضعیت عددی درخواست ساخته می‌شود.
    /// </summary>
    [ObservableProperty]
    private string _timelineText = string.Empty;

    /// <summary>ردیف سابقهٔ قبلی همین آزمایش برای همین بیمار — طراحی نسخهٔ ۳</summary>
    public record LabHistoryRow(string Date, string ResultText, string FlagText, string FlagBrush, double BarWidth);

    /// <summary>سوابق قبلی همین آزمایش (روند بیمار) — از درخواست‌های قبلی بیمار</summary>
    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<LabHistoryRow> _historyRows = new();

    /// <summary>آیا سابقهٔ قبلی برای نمایش روند وجود دارد؟</summary>
    [ObservableProperty]
    private bool _hasHistory;

    /// <summary>نتیجهٔ عددی — فیلد اصلی (فارسی‌نویس با جداکنندهٔ اعشار)</summary>
    [ObservableProperty]
    private string _resultNumeric = string.Empty;

    /// <summary>نتیجهٔ متنی — برای آزمایش‌های کیفی مثل «منفی»</summary>
    [ObservableProperty]
    private string _result = string.Empty;

    /// <summary>توضیح آزمایشگاه</summary>
    [ObservableProperty]
    private string _comment = string.Empty;

    /// <summary>در حال ذخیره</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>پیام موفقیت</summary>
    [ObservableProperty]
    private string _successMessage = string.Empty;

    /// <summary>پیام خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// فلگ تفسیر پس از ذخیره — رنگ وضعیت نتیجه در همین فرم (سبز/نارنجی/قرمز).
    /// </summary>
    [ObservableProperty]
    private string _savedFlagText = string.Empty;

    /// <summary>رنگ بج وضعیت نتیجهٔ ذخیره‌شده</summary>
    [ObservableProperty]
    private string _savedFlagBrush = "#43A047";

    /// <summary>آیا نتیجه‌ای ذخیره شده؟ (نمایش بج وضعیت)</summary>
    [ObservableProperty]
    private bool _hasSavedResult;

    /// <summary>رویداد درخواست بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// سازنده — زمینهٔ قلم از پنجرهٔ مادر می‌آید (LabItemRow + مشخصات بیمار)
    /// </summary>
    /// <param name="api">کلاینت API</param>
    /// <param name="item">قلم انتخاب‌شده با دابل‌کلیک</param>
    /// <param name="orderId">شناسه درخواست — بازخوانی فلگ تفسیر پس از ذخیره</param>
    /// <param name="patientName">نام کامل بیمار</param>
    /// <param name="patientAgeText">سن بیمار به فارسی (متن)</param>
    /// <param name="orderNumber">شماره درخواست</param>
    /// <param name="onSaved">فراخوانی پس از ذخیرهٔ موفق (تازه‌سازی لیست مادر)</param>
    /// <param name="patientId">شناسه بیمار برای دریافت سابقه نتیجه</param>
    /// <param name="orderStatus">وضعیت درخواست آزمایش</param>
    public LabTestResultEntryViewModel(IApiClient api, LabItemRow item, long orderId,
        string patientName, string patientAgeText, string orderNumber,
        Func<Task>? onSaved = null, long patientId = 0, int orderStatus = 0)
    {
        _api = api;
        _itemId = item.Id;
        _orderId = orderId;
        _onSaved = onSaved;
        TimelineText = BuildTimelineText(orderStatus);
        if (patientId > 0)
            _ = LoadHistoryAsync(patientId, item.TestName);

        TestName = item.TestName;
        Unit = item.Unit;
        GeneralRange = item.NormalRange;
        PatientRangeText = item.PatientRangeText ?? item.NormalRange ?? "—";
        // چرخهٔ ۷۹ — حدود بحرانی در فرم ثبت نتیجه (هشدار پیش از ثبت)
        CriticalRangeText = item.CriticalRangeText;
        PatientName = patientName;
        PatientAgeText = patientAgeText;
        OrderNumber = orderNumber;
        Result = item.Result ?? string.Empty;
        ResultNumeric = item.ResultNumeric ?? string.Empty;
        Comment = item.Comment ?? string.Empty;

        // اگر از قبل نتیجه داشته، بج وضعیت نتیجهٔ قبلی را نشان بده
        if (!string.IsNullOrWhiteSpace(Result) || !string.IsNullOrWhiteSpace(ResultNumeric))
            ShowFlag(item.AbnormalFlag);
    }

    /// <summary>ساخت متن خط زمانی نمونه از وضعیت درخواست — طراحی نسخهٔ ۳</summary>
    private static string BuildTimelineText(int status)
    {
        string Step(string label, int stepStatus) =>
            status >= stepStatus ? $"✓ {label}" : $"○ {label}";
        var current = status switch
        {
            1 => "مرحلهٔ جاری: در انتظار نمونه‌گیری",
            2 => "مرحلهٔ جاری: نمونه‌گیری‌شده",
            3 => "مرحلهٔ جاری: در حال انجام",
            4 => "مرحلهٔ جاری: نتیجه آماده",
            _ => ""
        };
        return $"{Step("ثبت درخواست", 1)} ← {Step("نمونه‌گیری", 2)} ← {Step("در حال انجام", 3)} ← {Step("نتیجه آماده", 4)}" +
               (string.IsNullOrEmpty(current) ? "" : $" · {current}");
    }

    /// <summary>
    /// بارگذاری سوابق قبلی همین آزمایش برای همین بیمار — طراحی نسخهٔ ۳:
    /// فهرست درخواست‌های بیمار از سرور گرفته می‌شود و از جزئیات چند درخواست
    /// اخیر، نتیجه‌های قبلی همین آزمایش (با تفسیر رنگی و میلهٔ روند) ساخته
    /// می‌شود تا آزمایشگر افت/خیز بیمار را پیش از ثبت نتیجه ببیند.
    /// </summary>
    private async Task LoadHistoryAsync(long patientId, string testName)
    {
        try
        {
            var ordersResp = await _api.GetAsync<LabViewModel.LabOrdersResponse>(
                $"lab/orders?patientId={patientId}");
            var orders = ordersResp?.Data;
            if (orders is null || orders.Count == 0) return;

            var found = new List<(string Date, decimal Value, string? Flag)>();
            foreach (var order in orders.Where(o => o.Id != _orderId).Take(8))
            {
                var detailResp = await _api.GetAsync<LabResultViewModel.LabOrderDetailEnvelope>(
                    $"lab/orders/{order.Id}");
                var items = detailResp?.Data?.Items;
                if (items is null) continue;
                foreach (var it in items.Where(i => i.TestName == testName && i.ResultNumeric.HasValue))
                    found.Add((detailResp!.Data!.DateShamsi ?? order.DateShamsi,
                        it.ResultNumeric!.Value, it.AbnormalFlag));
            }
            if (found.Count == 0) return;

            var max = Math.Max(1m, found.Max(f => Math.Abs(f.Value)));
            HistoryRows = new System.Collections.ObjectModel.ObservableCollection<LabHistoryRow>(
                found.Take(5).Select(f => new LabHistoryRow(
                    f.Date,
                    LabNumericResult.Format(f.Value),
                    f.Flag switch
                    {
                        "H" => "بالاتر از حد",
                        "L" => "پایین‌تر از حد",
                        "HH" => "بحرانی بالا",
                        "LL" => "بحرانی پایین",
                        _ => "بدون هشدار ثبت‌شده"
                    },
                    f.Flag switch
                    {
                        "HH" or "LL" => "#E53935",
                        "H" or "L" => "#FB8C00",
                        _ => "#607D8B"
                    },
                    140d * (double)(Math.Abs(f.Value) / max))));
            HasHistory = HistoryRows.Count > 0;
        }
        catch
        {
            // روند سابقه بخش کمکی فرم است؛ خطای آن نباید فرم ثبت نتیجه را مختل کند
        }
    }

    /// <summary>ذخیرهٔ نتیجهٔ همین قلم در سرور</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        // نتیجهٔ عددی باید عدد معتبر باشد — خطای فارسی (قانون دیالوگ خود برنامه)
        decimal? resultNumeric = null;
        if (!string.IsNullOrWhiteSpace(ResultNumeric))
        {
            if (!LabNumericResult.TryParse(ResultNumeric, out var parsed))
            {
                ErrorMessage = "نتیجهٔ عددی معتبر نیست — مثال درست: ۱۲/۵ یا 12.5";
                return;
            }
            resultNumeric = parsed;
        }

        // حداقل یکی از نتیجه‌ها لازم است — ذخیرهٔ خالی معنا ندارد
        if (resultNumeric is null && string.IsNullOrWhiteSpace(Result))
        {
            ErrorMessage = "نتیجهٔ عددی یا متنی را وارد کنید";
            return;
        }

        IsBusy = true;
        var body = new
        {
            itemId = _itemId,
            result = string.IsNullOrWhiteSpace(Result) ? null : Result.Trim(),
            resultNumeric,
            comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim()
        };

        var ok = await _api.PutAsync("lab/results", body);
        IsBusy = false;

        if (!ok)
        {
            ErrorMessage = _api.LastError ?? "ثبت نتیجه ناموفق بود";
            return;
        }

        // پس از ذخیرهٔ موفق، تفسیر تازه را از سرور بگیریم — جزئیات درخواست
        // دوباره خوانده می‌شود تا فلگ و بازهٔ بیمار از موتور واحد بیاید و
        // بج وضعیت همین فرم رنگ شود (سبز/نارنجی/قرمز).
        await ReloadFlagAsync();

        SuccessMessage = $"نتیجهٔ «{TestName}» ثبت شد";
        if (_onSaved is not null)
            await _onSaved();
    }

    /// <summary>
    /// بازخوانی فلگ تفسیر از سرور پس از ذخیره — جزئیات درخواست دوباره خوانده
    /// می‌شود، قلم همین آزمایش پیدا و بج وضعیت همین فرم با رنگ مناسب
    /// (سبز/نارنجی/قرمز) به‌روز می‌شود؛ تفسیر همیشه از موتور واحد سرور می‌آید.
    /// </summary>
    private async Task ReloadFlagAsync()
    {
        try
        {
            var response = await _api.GetAsync<LabResultViewModel.LabOrderDetailEnvelope>(
                $"lab/orders/{_orderId}");
            var item = response?.Data?.Items?.FirstOrDefault(i => i.Id == _itemId);
            if (item is not null)
                ShowFlag(item.AbnormalFlag);
            else
                HasSavedResult = true;
        }
        catch
        {
            // بج وضعیت اختیاری است — خطای شبکه ذخیرهٔ موفق را خراب نمی‌کند
            HasSavedResult = true;
        }
    }

    /// <summary>نمایش بج وضعیت نتیجه با رنگ مناسب</summary>
    private void ShowFlag(string? flag)
    {
        SavedFlagText = flag switch
        {
            "H" => "بالاتر از حد طبیعی",
            "L" => "پایین‌تر از حد طبیعی",
            "HH" => "بحرانی بالا — اطلاع فوری پزشک",
            "LL" => "بحرانی پایین — اطلاع فوری پزشک",
            _ => "بدون هشدار ثبت‌شده؛ طبیعی بودن تأیید نشده"
        };
        SavedFlagBrush = flag switch
        {
            "HH" or "LL" => "#E53935",
            "H" or "L" => "#FB8C00",
            _ => "#607D8B"
        };
        HasSavedResult = true;
    }

    /// <summary>بستن فرم</summary>
    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);
}

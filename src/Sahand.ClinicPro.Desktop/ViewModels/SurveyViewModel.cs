using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف نظرسنجی — تب ۱ فرم ۳۴</summary>
public record SurveyListRow(long Id, string Title, string? Description, string TypeText,
    bool IsActive, string ActiveText, int ResponseCount, string AverageText,
    /// <summary>متن خام JSON سوالات — برای بارگذاری در طراح ویرایش (چرخهٔ ۲۸)</summary>
    string? QuestionsJson);

/// <summary>یک سوال از سازنده پویا — قابل سریالایز به JSON برای فیلد Questions</summary>
public partial class SurveyQuestionItem : ObservableObject
{
    /// <summary>متن سوال</summary>
    [ObservableProperty]
    private string _text = string.Empty;

    /// <summary>نوع سوال — امتیاز ۱-۵ / متن آزاد / چندگزینه‌ای</summary>
    [ObservableProperty]
    private string _questionType = "امتیاز ۱ تا ۵";

    /// <summary>گزینه‌های چندگزینه‌ای — فقط برای نوع Choice با جداکننده «|»</summary>
    [ObservableProperty]
    private string _choices = string.Empty;

    /// <summary>گزینه‌های مجاز نوع سوال</summary>
    public static string[] TypeOptions { get; } = { "امتیاز ۱ تا ۵", "متن آزاد", "چندگزینه‌ای" };

    /// <summary>کلید نوع برای JSON</summary>
    public string TypeKey => QuestionType switch
    {
        "متن آزاد" => "text",
        "چندگزینه‌ای" => "choice",
        _ => "rating"
    };

    /// <summary>نمای JSON این سوال — قرارداد سازنده پویا</summary>
    public string ToJson() => JsonSerializer.Serialize(new
    {
        text = Text,
        type = TypeKey,
        options = TypeKey == "choice" && !string.IsNullOrWhiteSpace(Choices)
            ? Choices.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : null
    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    /// <summary>
    /// تجزیهٔ آرایهٔ JSON سوالات به آیتم‌های طراح — چرخهٔ بیست‌وهشتم
    /// (گزارش هجدهم مالک): «نظرسنجی‌ها طراحی نمی‌شوند، فقط به لیست اضافه
    /// می‌شوند» — ریشه: فرم ویرایش سوالات موجود نظرسنجی را بارگذاری
    /// نمی‌کرد و هر بار از نو بازنویسی می‌کرد. این پارسر هر دو قرارداد
    /// کلید قدیم («q») و جدید («text») و گزینه‌ها (آرایه یا رشتهٔ «|»جدا)
    /// را می‌پذیرد؛ متن تهی/خراب → فهرست خالی (فرم با سوال پیش‌فرض باز می‌شود).
    /// </summary>
    /// <param name="json">متن خام JSON آرایهٔ سوالات از سرور</param>
    /// <returns>آیتم‌های سازندهٔ پویا — خالی در نبود/خرابی متن</returns>
    public static List<SurveyQuestionItem> ParseList(string? json)
    {
        var result = new List<SurveyQuestionItem>();
        if (string.IsNullOrWhiteSpace(json))
            return result;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var q in doc.RootElement.EnumerateArray())
            {
                if (q.ValueKind != JsonValueKind.Object)
                    continue;

                // متن — کلید «text» (قرارداد جدید) یا «q» (قرارداد قدیمی)
                string? rawText = null;
                if (q.TryGetProperty("text", out var t))
                    rawText = t.GetString();
                else if (q.TryGetProperty("q", out var qk))
                    rawText = qk.GetString();
                var text = rawText ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                // نوع — rating پیش‌فرض
                var typeKey = q.TryGetProperty("type", out var ty) ? ty.GetString() : "rating";
                var type = typeKey switch
                {
                    "text" => TypeOptions[1],
                    "choice" => TypeOptions[2],
                    _ => TypeOptions[0]
                };

                // گزینه‌ها — آرایهٔ JSON یا رشتهٔ جداشده با «|»
                string choices = string.Empty;
                if (q.TryGetProperty("options", out var opt))
                {
                    if (opt.ValueKind == JsonValueKind.Array)
                        choices = string.Join("|", opt.EnumerateArray()
                            .Select(o => o.GetString()).Where(s => !string.IsNullOrWhiteSpace(s)));
                    else if (opt.ValueKind == JsonValueKind.String)
                        choices = opt.GetString() ?? string.Empty;
                }

                result.Add(new SurveyQuestionItem
                {
                    Text = text,
                    QuestionType = type,
                    Choices = choices
                });
            }
        }
        catch (JsonException)
        {
            // متن خراب/قدیمی — فهرست خالی؛ فرم ویرایش سوال پیش‌فرض می‌گذارد
        }
        return result;
    }
}

/// <summary>ستون توزیع ستاره‌ها — ارتفاع نوار از پیش مقیاس‌شده</summary>
public record StarBar(int Star, int Count, double BarHeight, string StarText);

/// <summary>ردیف میانگین امتیاز یک پزشک</summary>
public record DoctorRatingRow(long DoctorId, string DoctorName, int ResponseCount, string AverageText, double BarHeight);

/// <summary>ردیف نظر آزاد بیمار</summary>
public record SurveyCommentRow(long Id, string PatientName, string DoctorName,
    string RatingText, string Comment, string SubmittedShamsi);

/// <summary>
/// ViewModel نظرسنجی بیماران — گام V2 چرخه هفتم (فرم ۳۴):
/// دو تب: لیست نظرسنجی‌ها (جدول + فرم ایجاد/ویرایش با سازنده پویا سوالات)
/// و نتایج و آمار (کارت‌ها + توزیع ستاره + میانگین پزشکان + نظرات)
/// </summary>
public partial class SurveyViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "نظرسنجی بیماران";

    /* ─── حالت مشترک ─── */

    /// <summary>زبانه فعال — ۰ لیست / ۱ نتایج</summary>
    [ObservableProperty]
    private int _tabIndex;

    /* ─── تب ۱: لیست نظرسنجی‌ها ─── */

    /// <summary>فهرست نظرسنجی‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<SurveyListRow> _surveys = new();

    /// <summary>نظرسنجی انتخاب‌شده در جدول</summary>
    [ObservableProperty]
    private SurveyListRow? _selectedSurvey;

    /// <summary>آیا فرم ایجاد/ویرایش باز است</summary>
    [ObservableProperty]
    private bool _isFormOpen;

    /// <summary>آیا فرم در حالت ویرایش است (وگرنه ایجاد)</summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>شناسه نظرسنجی در حال ویرایش</summary>
    private long _editingId;

    /// <summary>عنوان فرم</summary>
    [ObservableProperty]
    private string _formTitle = string.Empty;

    /// <summary>توضیح فرم</summary>
    [ObservableProperty]
    private string? _formDescription;

    /// <summary>نوع نظرسنجی — متن فارسی</summary>
    [ObservableProperty]
    private string _formType = "رضایت بیماران";

    /// <summary>گزینه‌های نوع نظرسنجی</summary>
    public List<string> TypeOptions { get; } = new() { "رضایت بیماران", "کیفیت خدمات", "ارزیابی پزشک" };

    /// <summary>سوالات سازنده پویا</summary>
    public ObservableCollection<SurveyQuestionItem> Questions { get; } = new();

    /* ─── تب ۲: نتایج و آمار ─── */

    /// <summary>نظرسنجی انتخاب‌شده برای مشاهده نتایج</summary>
    [ObservableProperty]
    private SurveyListRow? _resultsSurvey;

    /// <summary>متن کارت کل پاسخ‌ها</summary>
    [ObservableProperty]
    private string _totalResponsesText = "۰";

    /// <summary>متن کارت میانگین کل</summary>
    [ObservableProperty]
    private string _averageText = "—";

    /// <summary>متن کارت پاسخ ۳۰ روز اخیر</summary>
    [ObservableProperty]
    private string _last30Text = "۰";

    /// <summary>توزیع ستاره‌ها — ۵ تا ۱</summary>
    public ObservableCollection<StarBar> Stars { get; } = new();

    /// <summary>میانگین امتیاز پزشکان</summary>
    public ObservableCollection<DoctorRatingRow> DoctorRatings { get; } = new();

    /// <summary>نظرات آزاد اخیر</summary>
    public ObservableCollection<SurveyCommentRow> Comments { get; } = new();

    /// <summary>سازنده — بارگذاری اولیه فهرست</summary>
    public SurveyViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadSurveysAsync();
    }

    /// <summary>بارگذاری داده زبانه فعال</summary>
    partial void OnTabIndexChanged(int value)
    {
        if (value == 1 && ResultsSurvey is not null)
            _ = LoadResultsAsync();
    }

    /// <summary>با انتخاب نظرسنجی نتایج، آمار بارگذاری شود</summary>
    partial void OnResultsSurveyChanged(SurveyListRow? value)
    {
        if (value is not null && TabIndex == 1)
            _ = LoadResultsAsync();
    }

    /* ─── تب ۱: فرمان‌ها ─── */

    /// <summary>بارگذاری فهرست نظرسنجی‌ها</summary>
    [RelayCommand]
    public async Task LoadSurveysAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<SurveysResponse>("surveys");
            Surveys.Clear();
            foreach (var s in response?.Data ?? new List<SurveyDto>())
                Surveys.Add(ToRow(s));
            if (Surveys.Count > 0 && ResultsSurvey is null)
                ResultsSurvey = Surveys[0];
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>باز کردن فرم ایجاد</summary>
    [RelayCommand]
    private void OpenCreateForm()
    {
        IsEditing = false;
        _editingId = 0;
        FormTitle = string.Empty;
        FormDescription = null;
        FormType = TypeOptions[0];
        Questions.Clear();
        // دو سوال پیش‌فرض برای شروع سریع
        Questions.Add(new SurveyQuestionItem { Text = "میزان رضایت کلی شما از خدمات درمانگاه؟" });
        IsFormOpen = true;
    }

    /// <summary>باز کردن فرم ویرایش — سوالات JSON موجود تجزیه می‌شود</summary>
    [RelayCommand]
    private void OpenEditForm()
    {
        if (SelectedSurvey is null)
        {
            _snackbar.Error("ابتدا یک نظرسنجی از جدول انتخاب کنید");
            return;
        }

        IsBusy = true;
        try
        {
            IsEditing = true;
            _editingId = SelectedSurvey.Id;
            FormTitle = SelectedSurvey.Title;
            FormDescription = SelectedSurvey.Description;
            FormType = SelectedSurvey.TypeText switch
            {
                "Service" => TypeOptions[1],
                "Doctor" => TypeOptions[2],
                _ => TypeOptions[0]
            };

            // ═══ چرخهٔ بیست‌وهشتم (گزارش هجدهم مالک) ═══
            // «نظرسنجی‌ها طراحی نمی‌شوند، فقط به لیست اضافه می‌شوند» —
            // ریشه: فرم ویرایش سوالات موجود را از JSON فعلی بارگذاری
            // نمی‌کرد و هر بار یک سوال پیش‌فرض می‌گذاشت و ذخیره، کل
            // نظرسنجی را «بازنویسی» می‌کرد. اکنون سوالات موجود با پارسر
            // مشترک ParseList بارگذاری می‌شوند؛ فقط در نبود/خرابی متن،
            // سوال پیش‌فرض پیشنهاد می‌شود.
            Questions.Clear();
            var existing = SurveyQuestionItem.ParseList(SelectedSurvey.QuestionsJson);
            if (existing.Count > 0)
            {
                foreach (var q in existing)
                    Questions.Add(q);
            }
            else
            {
                Questions.Add(new SurveyQuestionItem { Text = "میزان رضایت کلی شما از خدمات درمانگاه؟" });
            }
            IsFormOpen = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>افزودن سوال جدید به سازنده پویا</summary>
    [RelayCommand]
    private void AddQuestion() => Questions.Add(new SurveyQuestionItem());

    /// <summary>حذف سوال</summary>
    [RelayCommand]
    private void RemoveQuestion(SurveyQuestionItem question) => Questions.Remove(question);

    /// <summary>ذخیره فرم — ایجاد یا ویرایش بر اساس حالت</summary>
    [RelayCommand]
    private async Task SaveFormAsync()
    {
        if (string.IsNullOrWhiteSpace(FormTitle))
        {
            _snackbar.Error("عنوان نظرسنجی الزامی است");
            return;
        }

        IsBusy = true;
        try
        {
            var typeKey = FormType switch
            {
                "کیفیت خدمات" => 2,
                "ارزیابی پزشک" => 3,
                _ => 1
            };
            var questionsJson = Questions.Count == 0
                ? null
                : "[" + string.Join(",", Questions.Select(q => q.ToJson())) + "]";

            var body = new
            {
                title = FormTitle.Trim(),
                description = FormDescription,
                questions = questionsJson,
                surveyType = typeKey,
                isActive = (bool?)true
            };

            var ok = IsEditing
                ? await _api.PutAsync($"surveys/{_editingId}", body)
                : await _api.PostAsync("surveys", body);

            if (ok)
            {
                _snackbar.Success(IsEditing ? "نظرسنجی به‌روزرسانی شد" : "نظرسنجی ایجاد شد");
                IsFormOpen = false;
                await LoadSurveysAsync();
            }
            else
            {
                _snackbar.Error(_api.LastError ?? "ذخیره ناموفق بود");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>لغو فرم</summary>
    [RelayCommand]
    private void CancelForm() => IsFormOpen = false;

    /// <summary>فعال/غیرفعال‌سازی نظرسنجی انتخابی</summary>
    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        if (SelectedSurvey is null)
        {
            _snackbar.Error("ابتدا یک نظرسنجی از جدول انتخاب کنید");
            return;
        }

        IsBusy = true;
        try
        {
            var ok = await _api.PostAsync($"surveys/{SelectedSurvey.Id}/toggle-active", null);
            if (ok)
            {
                _snackbar.Success("وضعیت نظرسنجی تغییر کرد");
                await LoadSurveysAsync();
            }
            else
                _snackbar.Error(_api.LastError ?? "تغییر وضعیت ناموفق بود");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>ارسال لینک پیامکی نظرسنجی به بیماران ویزیت‌شده امروز — گام V3-ب</summary>
    [RelayCommand]
    private async Task SendLinksAsync()
    {
        if (SelectedSurvey is null)
        {
            _snackbar.Error("ابتدا یک نظرسنجی از جدول انتخاب کنید");
            return;
        }

        IsBusy = true;
        try
        {
            var response = await _api.PostAsync<SendLinksResponse>(
                $"surveys/{SelectedSurvey.Id}/send-links", new { baseUrl = (string?)null });
            if (response?.IsSuccess == true)
            {
                var count = response.Data?.QueuedCount ?? 0;
                _snackbar.Success(count == 0
                    ? "امروز ویزیت فعالی برای ارسال لینک یافت نشد"
                    : $"{count} پیامک لینک نظرسنجی در صف ارسال قرار گرفت");
            }
            else
                _snackbar.Error(_api.LastError ?? "ارسال لینک ناموفق بود");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /* ─── تب ۲: نتایج و آمار ─── */

    /// <summary>بارگذاری نتایج نظرسنجی انتخاب‌شده</summary>
    [RelayCommand]
    public async Task LoadResultsAsync()
    {
        if (ResultsSurvey is null) return;

        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<ResultsResponse>($"surveys/{ResultsSurvey.Id}/results");
            var data = response?.Data;
            if (data is null)
            {
                _snackbar.Error(_api.LastError ?? "دریافت نتایج ناموفق بود");
                return;
            }

            // کارت‌های خلاصه
            TotalResponsesText = Money.Format(data.Summary.TotalResponses);
            AverageText = data.Summary.AverageRating > 0
                ? data.Summary.AverageRating.ToString("0.##") + " از ۵"
                : "—";
            Last30Text = Money.Format(data.Summary.ResponsesLast30Days);

            // توزیع ستاره‌ها — نوار افقی درصدی
            var maxStar = Math.Max(1, data.StarDistribution.Max(s => s.Count));
            Stars.Clear();
            foreach (var s in data.StarDistribution)
            {
                var starText = s.Star == 1 ? "⭐" : new string('⭐', s.Star);
                Stars.Add(new StarBar(s.Star, s.Count, 100.0 * s.Count / maxStar, starText));
            }

            // میانگین پزشکان — نوار مقایسه‌ای
            var maxDoctor = Math.Max(0.01, data.DoctorRatings.Max(d => d.Average));
            DoctorRatings.Clear();
            foreach (var d in data.DoctorRatings)
                DoctorRatings.Add(new DoctorRatingRow(d.DoctorId, d.DoctorName,
                    d.ResponseCount, d.Average.ToString("0.##"), 100.0 * d.Average / maxDoctor));

            // نظرات آزاد
            Comments.Clear();
            foreach (var c in data.RecentComments)
            {
                var rating = c.Rating.HasValue ? new string('⭐', c.Rating.Value) : "بدون امتیاز";
                Comments.Add(new SurveyCommentRow(c.Id, c.PatientName, c.DoctorName,
                    rating, c.Comment, c.SubmittedShamsi));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /* ─── نگاشت ─── */

    /// <summary>تبدیل DTO به ردیف نمایشی</summary>
    private static SurveyListRow ToRow(SurveyDto s)
    {
        var typeText = s.Type switch
        {
            "Service" => "کیفیت خدمات",
            "Doctor" => "ارزیابی پزشک",
            _ => "رضایت بیماران"
        };
        return new SurveyListRow(s.Id, s.Title, s.Description, s.Type,
            s.IsActive, s.IsActive ? "🟢 فعال" : "🔴 غیرفعال",
            s.ResponseCount,
            s.AverageRating > 0 ? s.AverageRating.ToString("0.##") + " از ۵" : "—",
            s.Questions);
    }

    /* ─── پاسخ‌های API ─── */

    private sealed class SurveysResponse
    {
        public bool IsSuccess { get; set; }
        public List<SurveyDto>? Data { get; set; }
    }

    private sealed class SurveyDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = "Satisfaction";
        public bool IsActive { get; set; }
        public int ResponseCount { get; set; }
        public double AverageRating { get; set; }
        /// <summary>سوالات JSON — چرخهٔ ۲۸: از GET / فهرست برای طراح ویرایش</summary>
        public string? Questions { get; set; }
    }

    private sealed class SendLinksResponse
    {
        public bool IsSuccess { get; set; }
        public SendLinksData? Data { get; set; }
        public string? Message { get; set; }
    }

    private sealed class SendLinksData
    {
        public int QueuedCount { get; set; }
        public string? Link { get; set; }
    }

    private sealed class ResultsResponse
    {
        public bool IsSuccess { get; set; }
        public ResultsData? Data { get; set; }
    }

    private sealed class ResultsData
    {
        public ResultsSummary Summary { get; set; } = new();
        public List<StarDto> StarDistribution { get; set; } = new();
        public List<DoctorRatingDto> DoctorRatings { get; set; } = new();
        public List<CommentDto> RecentComments { get; set; } = new();
    }

    private sealed class ResultsSummary
    {
        public int TotalResponses { get; set; }
        public double AverageRating { get; set; }
        public int ResponsesLast30Days { get; set; }
    }

    private sealed class StarDto
    {
        public int Star { get; set; }
        public int Count { get; set; }
    }

    private sealed class DoctorRatingDto
    {
        public long DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public int ResponseCount { get; set; }
        public double Average { get; set; }
    }

    private sealed class CommentDto
    {
        public long Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = "—";
        public byte? Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string SubmittedShamsi { get; set; } = string.Empty;
    }
}

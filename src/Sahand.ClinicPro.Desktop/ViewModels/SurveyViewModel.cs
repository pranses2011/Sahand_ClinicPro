using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.ViewModels;

internal static class SurveyNumberFormatting
{
    public static string Count(long value) => Money.ToPersianDigits(
        value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '٬'));
}

/// <summary>ردیف نظرسنجی — تب ۱ فرم ۳۴</summary>
public record SurveyListRow(long Id, string Title, string? Description, string TypeText,
    bool IsActive, string ActiveText, int ResponseCount, string AverageText,
    string? QuestionsJson)
{
    public string QuestionCountText => SurveyQuestionItem.TryParseList(QuestionsJson, out var questions)
        ? SurveyNumberFormatting.Count(questions.Count) : "—";
}

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
        => TryParseList(json, out var items) ? items : new();

    /// <summary>خواندن محافظه‌کارانه؛ قالب ناشناخته نباید با ذخیرهٔ ویرایش از بین برود.</summary>
    public static bool TryParseList(string? json, out List<SurveyQuestionItem> result)
    {
        result = new();
        if (string.IsNullOrWhiteSpace(json)) return true;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return false;
            foreach (var q in doc.RootElement.EnumerateArray())
            {
                if (q.ValueKind != JsonValueKind.Object || q.EnumerateObject().Any(p => p.Name is not ("text" or "q" or "type" or "options"))) return false;
                if (q.TryGetProperty("text", out _) && q.TryGetProperty("q", out _)) return false;
                if (!q.TryGetProperty("text", out var textElement) && !q.TryGetProperty("q", out textElement)) return false;
                if (textElement.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(textElement.GetString())) return false;
                var typeKey = "rating";
                if (q.TryGetProperty("type", out var ty))
                {
                    if (ty.ValueKind != JsonValueKind.String) return false;
                    typeKey = ty.GetString() ?? "";
                }
                if (typeKey is not ("rating" or "text" or "choice")) return false;
                var choices = "";
                if (q.TryGetProperty("options", out var opt))
                {
                    if (typeKey != "choice" && opt.ValueKind != JsonValueKind.Null) return false;
                    if (opt.ValueKind == JsonValueKind.Array)
                    {
                        if (opt.EnumerateArray().Any(o => o.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(o.GetString()) || o.GetString()!.Contains('|'))) return false;
                        choices = string.Join("|", opt.EnumerateArray().Select(o => o.GetString()));
                    }
                    else if (opt.ValueKind == JsonValueKind.String) choices = opt.GetString() ?? "";
                    else if (opt.ValueKind != JsonValueKind.Null) return false;
                }
                result.Add(new SurveyQuestionItem { Text = textElement.GetString()!, QuestionType = typeKey switch
                {
                    "text" => TypeOptions[1], "choice" => TypeOptions[2], _ => TypeOptions[0]
                }, Choices = choices });
            }
            return true;
        }
        catch (JsonException) { result.Clear(); return false; }
    }

}

/// <summary>یک پرسش برای ثبت پاسخ داخلی توسط پرسنل.</summary>
public partial class SurveyAnswerItem : ObservableObject
{
    public string Text { get; init; } = string.Empty;
    public string TypeKey { get; init; } = "rating";
    public string[] Options { get; init; } = Array.Empty<string>();
    [ObservableProperty] private string? _answer;
}

/// <summary>پاسخ تاریخی به همراه متن پرسش هنگام ثبت.</summary>
public record SurveyAnswerHistoryRow(string SubmittedShamsi, string Details);

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
    private int _resultsVersion;
    [ObservableProperty] private bool _isResultsLoading;
    [ObservableProperty] private string _resultsMessage = "نظرسنجی را برای مشاهدهٔ نتایج انتخاب کنید.";

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

    /// <summary>نمای پالایش‌شده؛ فهرست اصلی برای انتخاب نتایج دست‌نخورده می‌ماند.</summary>
    public ObservableCollection<SurveyListRow> VisibleSurveys { get; } = new();
    public string[] SurveyStatusOptions { get; } = { "همه", "فعال", "غیرفعال" };
    [ObservableProperty] private string _surveySearch = string.Empty;
    [ObservableProperty] private int _surveyStatusIndex;
    [ObservableProperty] private bool _hasSurveyList;
    public string SurveyCountText => HasSurveyList ? SurveyNumberFormatting.Count(Surveys.Count) : "—";
    public string ActiveSurveyCountText => HasSurveyList ? SurveyNumberFormatting.Count(Surveys.Count(s => s.IsActive)) : "—";
    public string SurveyResponseCountText => HasSurveyList ? SurveyNumberFormatting.Count(Surveys.Sum(s => (long)s.ResponseCount)) : "—";
    partial void OnSurveySearchChanged(string value) => RefreshVisibleSurveys();
    partial void OnSurveyStatusIndexChanged(int value) => RefreshVisibleSurveys();

    private void RefreshVisibleSurveys(SurveyListRow? preferred = null)
    {
        var selected = preferred ?? SelectedSurvey;
        var search = SurveySearch?.Trim() ?? string.Empty;
        var rows = Surveys.Where(s =>
            (SurveyStatusIndex == 0 || (SurveyStatusIndex == 1 && s.IsActive) || (SurveyStatusIndex == 2 && !s.IsActive)) &&
            (search.Length == 0 || s.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             (s.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
             s.TypeText.Contains(search, StringComparison.OrdinalIgnoreCase))).ToArray();
        VisibleSurveys.Clear();
        foreach (var row in rows) VisibleSurveys.Add(row);
        SelectedSurvey = selected is not null && rows.Contains(selected) ? selected : null;
    }

    [RelayCommand]
    private void ShowSelectedResults()
    {
        if (SelectedSurvey is null) { _snackbar.Error("ابتدا یک نظرسنجی از جدول انتخاب کنید"); return; }
        ResultsSurvey = SelectedSurvey;
        TabIndex = 1;
    }

    [ObservableProperty] private bool _isSurveySaving;
    public bool CanEditSurvey => !IsSurveySaving;
    partial void OnIsSurveySavingChanged(bool value) => OnPropertyChanged(nameof(CanEditSurvey));

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

    private SurveyListRow? _respondingSurvey;
    [ObservableProperty] private bool _isResponseFormOpen;
    [ObservableProperty] private string _responseSurveyTitle = string.Empty;
    [ObservableProperty] private string? _responseComment;
    [ObservableProperty] private int _responseRatingIndex = -1;
    public string[] ResponseRatingOptions { get; } = { "۱", "۲", "۳", "۴", "۵" };
    public ObservableCollection<SurveyAnswerItem> ResponseAnswers { get; } = new();
    public ObservableCollection<SurveyAnswerHistoryRow> AnswerHistory { get; } = new();

    /// <summary>ساخت فرم پاسخ برای نظرسنجی انتخاب‌شده، به صورت ناشناس.</summary>
    [RelayCommand]
    private void OpenResponseForm()
    {
        if (IsFormOpen || IsResponseFormOpen) { _snackbar.Info("ابتدا فرم باز را ثبت یا لغو کنید"); return; }
        var survey = SelectedSurvey;
        if (survey is null || !survey.IsActive)
        { _snackbar.Error("یک نظرسنجی فعال انتخاب کنید"); return; }
        if (!SurveyQuestionItem.TryParseList(survey.QuestionsJson, out var questions))
        { _snackbar.Error("ساختار سؤال‌های این نظرسنجی قابل پاسخ‌گویی در این فرم نیست"); return; }
        _respondingSurvey = survey;
        ResponseSurveyTitle = survey.Title;
        ResponseComment = null; ResponseRatingIndex = -1;
        ResponseAnswers.Clear();
        foreach (var q in questions)
            ResponseAnswers.Add(new SurveyAnswerItem { Text = q.Text, TypeKey = q.TypeKey,
                Options = q.TypeKey switch
                {
                    "rating" => new[] { "1", "2", "3", "4", "5" },
                    "choice" => q.Choices.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    _ => Array.Empty<string>()
                } });
        IsResponseFormOpen = true;
    }

    [RelayCommand]
    private void CancelResponseForm() { IsResponseFormOpen = false; _respondingSurvey = null; }

    /// <summary>ثبت پاسخ‌های یک فرم به همراه امتیاز کلی و نظر اختیاری.</summary>
    [RelayCommand]
    private async Task SubmitSurveyResponseAsync()
    {
        var survey = _respondingSurvey;
        if (survey is null) return;
        if (ResponseAnswers.Any(a => string.IsNullOrWhiteSpace(a.Answer) || a.Answer.Trim().Length > 2000 ||
            (a.TypeKey != "text" && !a.Options.Contains(a.Answer.Trim()))))
        { _snackbar.Error("پاسخ همهٔ سؤال‌ها را به‌درستی وارد کنید (حداکثر ۲۰۰۰ نویسه)"); return; }
        if (ResponseAnswers.Count == 0 && ResponseRatingIndex < 0 && string.IsNullOrWhiteSpace(ResponseComment))
        { _snackbar.Error("حداقل یک امتیاز یا نظر وارد کنید"); return; }
        IsBusy = true;
        IsSurveySaving = true;
        try
        {
            var ok = await _api.PostAsync($"surveys/{survey.Id}/responses", new
            {
                patientId = (long?)null, visitId = (long?)null, doctorId = (long?)null,
                rating = ResponseRatingIndex is >= 0 and < 5 ? (byte?)(ResponseRatingIndex + 1) : null,
                comments = ResponseComment, questionsSnapshot = survey.QuestionsJson,
                answers = ResponseAnswers.Select(a => a.Answer?.Trim()).ToArray()
            });
            if (!ok) { _snackbar.Error(_api.LastError ?? "ثبت پاسخ ناموفق بود"); return; }
            _snackbar.Success($"پاسخ ناشناس نظرسنجی «{survey.Title}» ثبت شد");
            if (ReferenceEquals(_respondingSurvey, survey)) CancelResponseForm();
            await LoadSurveysAsync();
        }
        finally { IsSurveySaving = false; IsBusy = false; }
    }

    /* ─── تب ۲: نتایج و آمار ─── */

    /// <summary>نظرسنجی انتخاب‌شده برای مشاهده نتایج</summary>
    [ObservableProperty]
    private SurveyListRow? _resultsSurvey;

    /// <summary>متن کارت کل پاسخ‌ها</summary>
    [ObservableProperty]
    private string _totalResponsesText = "—";

    /// <summary>متن کارت میانگین کل</summary>
    [ObservableProperty]
    private string _averageText = "—";

    /// <summary>متن کارت پاسخ ۳۰ روز اخیر</summary>
    [ObservableProperty]
    private string _last30Text = "—";

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
        if (value == 1 && ResultsSurvey is not null) _ = LoadResultsAsync();
    }

    partial void OnResultsSurveyChanged(SurveyListRow? value)
    {
        _resultsVersion++;
        ResetResults();
        IsResultsLoading = false;
        ResultsMessage = value is null ? "نظرسنجی را برای مشاهدهٔ نتایج انتخاب کنید." : "نتایج هنوز دریافت نشده‌اند.";
        if (value is not null && TabIndex == 1) _ = LoadResultsAsync();
    }

    private void ResetResults()
    {
        TotalResponsesText = AverageText = Last30Text = "—";
        Stars.Clear(); DoctorRatings.Clear(); Comments.Clear(); AnswerHistory.Clear();
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
            if (response?.Data is null)
            {
                _snackbar.Error(_api.LastError ?? "دریافت فهرست نظرسنجی‌ها ناموفق بود");
                return;
            }
            var selectedId = SelectedSurvey?.Id;
            var resultsId = ResultsSurvey?.Id;
            Surveys.Clear();
            foreach (var s in response.Data) Surveys.Add(ToRow(s));
            RefreshVisibleSurveys(Surveys.FirstOrDefault(s => s.Id == selectedId));
            HasSurveyList = true;
            OnPropertyChanged(nameof(SurveyCountText));
            OnPropertyChanged(nameof(ActiveSurveyCountText));
            OnPropertyChanged(nameof(SurveyResponseCountText));
            ResultsSurvey = Surveys.FirstOrDefault(s => s.Id == resultsId) ?? Surveys.FirstOrDefault();
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
        if (IsFormOpen || IsResponseFormOpen) { _snackbar.Info("ابتدا فرم باز را ذخیره یا لغو کنید"); return; }
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
        if (IsFormOpen || IsResponseFormOpen) { _snackbar.Info("ابتدا فرم باز را ذخیره یا لغو کنید"); return; }
        if (SelectedSurvey is null)
        {
            _snackbar.Error("ابتدا یک نظرسنجی از جدول انتخاب کنید");
            return;
        }

        IsBusy = true;
        try
        {
            if (!SurveyQuestionItem.TryParseList(SelectedSurvey.QuestionsJson, out var existing))
            {
                _snackbar.Error("ساختار سؤالات این نظرسنجی پشتیبانی نمی‌شود؛ برای جلوگیری از حذف اطلاعات، ویرایش باز نشد.");
                return;
            }
            IsEditing = true;
            _editingId = SelectedSurvey.Id;
            FormTitle = SelectedSurvey.Title;
            FormDescription = SelectedSurvey.Description;
            FormType = SelectedSurvey.TypeText switch
            {
                "کیفیت خدمات" => TypeOptions[1],
                "ارزیابی پزشک" => TypeOptions[2],
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
            foreach (var q in existing) Questions.Add(q);
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

        if (FormTitle.Trim().Length > 150)
        { _snackbar.Error("عنوان نظرسنجی حداکثر ۱۵۰ نویسه است"); return; }
        if (Questions.Any(q => string.IsNullOrWhiteSpace(q.Text)))
        { _snackbar.Error("متن همهٔ سؤال‌ها را وارد کنید"); return; }
        if (Questions.Any(q => q.TypeKey == "choice" && q.Choices.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length < 2))
        { _snackbar.Error("برای سؤال چندگزینه‌ای حداقل دو گزینه وارد کنید"); return; }

        IsBusy = true;
        IsSurveySaving = true;
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
                isActive = (bool?)null
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
            IsSurveySaving = false;
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
        var selected = ResultsSurvey;
        if (selected is null) return;
        var version = ++_resultsVersion;
        ResetResults();
        IsResultsLoading = true;
        ResultsMessage = "در حال دریافت نتایج…";
        try
        {
            var response = await _api.GetAsync<ResultsResponse>($"surveys/{selected.Id}/results");
            if (version != _resultsVersion || ResultsSurvey?.Id != selected.Id) return;
            var data = response?.Data;
            if (data is null)
            {
                ResultsMessage = "دریافت نتایج ناموفق بود.";
                _snackbar.Error(_api.LastError ?? ResultsMessage);
                return;
            }
            ResultsMessage = $"نتایج نظرسنجی «{selected.Title}»";

            // کارت‌های خلاصه
            TotalResponsesText = SurveyNumberFormatting.Count(data.Summary.TotalResponses);
            AverageText = data.Summary.AverageRating > 0
                ? data.Summary.AverageRating.ToString("0.##") + " از ۵"
                : "—";
            Last30Text = SurveyNumberFormatting.Count(data.Summary.ResponsesLast30Days);

            // توزیع ستاره‌ها — نوار افقی درصدی
            var maxStar = Math.Max(1, data.StarDistribution.Select(s => s.Count).DefaultIfEmpty(0).Max());
            Stars.Clear();
            foreach (var s in data.StarDistribution)
            {
                var starText = s.Star == 1 ? "⭐" : new string('⭐', Math.Clamp(s.Star, 0, 5));
                Stars.Add(new StarBar(s.Star, s.Count, 100.0 * s.Count / maxStar, starText));
            }

            // میانگین پزشکان — نوار مقایسه‌ای
            var maxDoctor = Math.Max(0.01, data.DoctorRatings.Select(d => d.Average).DefaultIfEmpty(0).Max());
            DoctorRatings.Clear();
            foreach (var d in data.DoctorRatings)
                DoctorRatings.Add(new DoctorRatingRow(d.DoctorId, d.DoctorName,
                    d.ResponseCount, d.Average.ToString("0.##"), 100.0 * d.Average / maxDoctor));

            AnswerHistory.Clear();
            foreach (var item in data.RecentAnswers ?? new())
            {
                try
                {
                    using var doc = JsonDocument.Parse(item.Answers);
                    if (doc.RootElement.ValueKind != JsonValueKind.Array) continue;
                    var lines = new List<string>();
                    foreach (var answer in doc.RootElement.EnumerateArray())
                    {
                        if (answer.ValueKind != JsonValueKind.Object ||
                            !answer.TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String ||
                            !answer.TryGetProperty("answer", out var value) || value.ValueKind != JsonValueKind.String) continue;
                        lines.Add($"{text.GetString()}: {value.GetString()}");
                    }
                    if (lines.Count > 0) AnswerHistory.Add(new SurveyAnswerHistoryRow(item.SubmittedShamsi, string.Join(Environment.NewLine, lines)));
                }
                catch (JsonException) { /* پاسخ قدیمی یا ناسازگار نمایش داده نمی‌شود */ }
            }

            // نظرات آزاد
            Comments.Clear();
            foreach (var c in data.RecentComments)
            {
                var rating = c.Rating.HasValue ? new string('⭐', Math.Clamp((int)c.Rating.Value, 0, 5)) : "بدون امتیاز";
                Comments.Add(new SurveyCommentRow(c.Id, c.PatientName, c.DoctorName,
                    rating, c.Comment, c.SubmittedShamsi));
            }
        }
        catch (Exception)
        {
            if (version == _resultsVersion)
            {
                ResultsMessage = "دریافت نتایج ناموفق بود.";
                _snackbar.Error(_api.LastError ?? ResultsMessage);
            }
        }
        finally
        {
            if (version == _resultsVersion) IsResultsLoading = false;
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
        return new SurveyListRow(s.Id, s.Title, s.Description, typeText,
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
        public List<AnswerHistoryDto>? RecentAnswers { get; set; } = new();
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

    private sealed class AnswerHistoryDto
    {
        public string SubmittedShamsi { get; set; } = string.Empty;
        public string Answers { get; set; } = string.Empty;
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

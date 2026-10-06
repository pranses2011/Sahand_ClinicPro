using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف درس در فهرست مرکز آموزش</summary>
public partial class TrainingLessonRow : ObservableObject
{
    /// <summary>شناسه درس</summary>
    public string Id { get; init; } = "";

    /// <summary>آیکن</summary>
    public string Icon { get; init; } = "";

    /// <summary>عنوان</summary>
    public string Title { get; init; } = "";

    /// <summary>زیرعنوان — خلاصه و مدت</summary>
    public string Subtitle { get; init; } = "";

    /// <summary>محتوای HTML درس</summary>
    public string Html { get; init; } = "";

    /// <summary>تکمیل‌شده؟</summary>
    [ObservableProperty]
    private bool _isDone;

    /// <summary>بازخورد تغییر تکمیل — تازه‌سازی نشانگرها</summary>
    partial void OnIsDoneChanged(bool value)
    {
        OnPropertyChanged(nameof(DoneBadge));
        OnPropertyChanged(nameof(DoneColor));
    }

    /// <summary>متن نشانگر تکمیل</summary>
    public string DoneBadge => IsDone ? "✓ تکمیل" : "در انتظار";

    /// <summary>رنگ نشانگر تکمیل</summary>
    public string DoneColor => IsDone ? "#2E7D32" : "#9E9E9E";
}

/// <summary>
/// ViewModel مرکز آموزش — چرخه سیزدهم (درخواست ۸-۶):
/// فهرست ۱۰ درس تعاملی HTML داخلی + پیشرفت یادگیری هر کاربر در سرور.
/// </summary>
public partial class TrainingCenterViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private TrainingCenterView? _view;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "مرکز آموزش";

    /// <summary>فهرست درس‌ها</summary>
    public ObservableCollection<TrainingLessonRow> Lessons { get; } = new();

    /// <summary>درس انتخاب‌شده</summary>
    [ObservableProperty]
    private TrainingLessonRow? _selectedLesson;

    /// <summary>درسی برای نمایش هست؟</summary>
    [ObservableProperty]
    private bool _hasLesson;

    /// <summary>متادیتای درس جاری — مدت و وضعیت</summary>
    [ObservableProperty]
    private string _lessonMeta = "";

    /// <summary>درصد پیشرفت</summary>
    [ObservableProperty]
    private double _progressPercent;

    /// <summary>متن پیشرفت — «۳ از ۱۰»</summary>
    [ObservableProperty]
    private string _progressText = "۰ از ۱۰";

    /// <summary>سازنده — بارگذاری درس‌ها و پیشرفت</summary>
    public TrainingCenterViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        foreach (var l in TrainingCatalog.Lessons)
            Lessons.Add(new TrainingLessonRow
            {
                Id = l.Id, Icon = l.Icon, Title = l.Title, Html = l.Html,
                Subtitle = $"{l.Summary} • {l.Minutes} دقیقه",
            });
        _ = LoadProgressAsync();
    }

    /// <summary>اتصال نما برای نمایشگر HTML — از کد پشت فراخوانده می‌شود</summary>
    public void AttachView(TrainingCenterView view) => _view = view;

    /// <summary>بارگذاری پیشرفت از سرور</summary>
    [RelayCommand]
    private async Task LoadProgressAsync()
    {
        var response = await _api.GetAsync<ProgressResponse>("training/progress");
        var completed = response?.Data?.CompletedLessons ?? new List<string>();
        foreach (var lesson in Lessons)
            lesson.IsDone = completed.Contains(lesson.Id);
        RefreshProgress();
    }

    /// <summary>انتخاب درس — نمایش HTML در نمایشگر</summary>
    partial void OnSelectedLessonChanged(TrainingLessonRow? value)
    {
        HasLesson = value is not null;
        if (value is null) return;
        LessonMeta = $"درس «{value.Title}» — {value.Subtitle}";
        _view?.ShowLesson(value.Html);
    }

    /// <summary>علامت‌گذاری درس جاری تکمیل‌شده در سرور</summary>
    [RelayCommand]
    private async Task MarkDoneAsync()
    {
        if (SelectedLesson is null) { _snackbar.Info("ابتدا یک درس انتخاب کنید"); return; }

        var ok = await _api.PutAsync($"training/progress/{SelectedLesson.Id}", null);
        if (!ok)
        {
            _snackbar.Error(_api.LastError ?? "ثبت پیشرفت ناموفق بود");
            return;
        }
        SelectedLesson.IsDone = true;
        RefreshProgress();
        _snackbar.Success($"درس «{SelectedLesson.Title}» تکمیل شد");
    }

    /// <summary>بازنشانی پیشرفت</summary>
    [RelayCommand]
    private async Task ResetProgressAsync()
    {
        var ok = await _api.DeleteAsync("training/progress");
        if (!ok)
        {
            _snackbar.Error(_api.LastError ?? "بازنشانی ناموفق بود");
            return;
        }
        foreach (var lesson in Lessons) lesson.IsDone = false;
        RefreshProgress();
        _snackbar.Success("پیشرفت آموزش بازنشانی شد — می‌توانید دوباره شروع کنید");
    }

    /// <summary>تازه‌سازی شمارنده و نوار پیشرفت</summary>
    private void RefreshProgress()
    {
        var done = Lessons.Count(l => l.IsDone);
        ProgressPercent = done * 100.0 / Lessons.Count;
        var fa = done.ToString().Replace('0', '۰').Replace('1', '۱').Replace('2', '۲').Replace('3', '۳')
            .Replace('4', '۴').Replace('5', '۵').Replace('6', '۶').Replace('7', '۷').Replace('8', '۸').Replace('9', '۹');
        ProgressText = $"{fa} از ۱۰";
    }

    /* ─── پاسخ‌ها ─── */

    private sealed class ProgressResponse
    {
        public bool IsSuccess { get; set; }
        public ProgressData? Data { get; set; }
    }

    private sealed class ProgressData
    {
        public List<string>? CompletedLessons { get; set; }
        public int CompletedCount { get; set; }
        public int TotalLessons { get; set; }
    }
}

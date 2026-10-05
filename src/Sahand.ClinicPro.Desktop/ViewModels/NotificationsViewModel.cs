using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف اعلان در مرکز اعلان‌ها — ۵ نوع طبق فرم ۲۶</summary>
public partial class NotificationRow : ObservableObject
{
    /// <summary>شناسه</summary>
    public long Id { get; init; }

    /// <summary>عنوان</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>متن</summary>
    public string? Message { get; init; }

    /// <summary>نوع — Info/Warning/Error/Success/License</summary>
    public string Type { get; init; } = "Info";

    /// <summary>زمان ایجاد</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>خوانده‌شده</summary>
    [ObservableProperty]
    private bool _isRead;

    /// <summary>نشان نوع با رنگ</summary>
    public string TypeText => Type switch
    {
        "Warning" => "⚠️ هشدار", "Error" => "❌ خطا",
        "Success" => "✅ موفقیت", "License" => "🔑 لایسنس",
        _ => "ℹ️ اطلاع"
    };

    /// <summary>رنگ نوار کناری اعلان</summary>
    public string TypeColor => Type switch
    {
        "Warning" => "#F9A825", "Error" => "#C62828",
        "Success" => "#2E7D32", "License" => "#5E35B1",
        _ => "#1E88E5"
    };

    /// <summary>شفافیت ردیف خوانده‌نشده — متمایز از خوانده‌شده</summary>
    public double RowOpacity => IsRead ? 0.55 : 1.0;

    /// <summary>بازخورد تغییر خوانده‌شده — تازه‌سازی شفافیت</summary>
    partial void OnIsReadChanged(bool value) => OnPropertyChanged(nameof(RowOpacity));
}

/// <summary>
/// ViewModel مرکز اعلان‌ها — گام U14:
/// فهرست ۵ نوع + علامت‌گذاری خوانده‌شده (تکی/همه) + شمارنده زنگ TopBar
/// </summary>
public partial class NotificationsViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "مرکز اعلان‌ها";

    /// <summary>فهرست اعلان‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<NotificationRow> _notifications = new();

    /// <summary>اعلان انتخاب‌شده</summary>
    [ObservableProperty]
    private NotificationRow? _selectedNotification;

    /// <summary>شمارنده خوانده‌نشده — همگام با زنگ TopBar</summary>
    [ObservableProperty]
    private int _unreadCount;

    /// <summary>متن سربرگ</summary>
    [ObservableProperty]
    private string _countText = "اعلان‌ها";

    /// <summary>سازنده</summary>
    public NotificationsViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadAsync();
    }

    /// <summary>بارگذاری اعلان‌ها</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<NotificationsResponse>("notifications?page=1&pageSize=50");
            var data = response?.Data;
            Notifications = new ObservableCollection<NotificationRow>(
                (data?.Items ?? new List<NotificationDto>()).Select(n => new NotificationRow
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead
                }));
            UnreadCount = data?.UnreadCount ?? 0;
            CountText = UnreadCount > 0 ? $"{UnreadCount} اعلان خوانده‌نشده" : "همه اعلان‌ها خوانده‌شده";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>علامت‌گذاری یک اعلان خوانده‌شده — با تازه‌سازی شفافیت</summary>
    [RelayCommand]
    private async Task MarkReadAsync(NotificationRow? row)
    {
        row ??= SelectedNotification;
        if (row is null || row.IsRead) return;

        var ok = await _api.PostAsync($"notifications/{row.Id}/mark-read", null);
        if (ok)
        {
            row.IsRead = true;
            UnreadCount = Math.Max(0, UnreadCount - 1);
            CountText = UnreadCount > 0 ? $"{UnreadCount} اعلان خوانده‌نشده" : "همه اعلان‌ها خوانده‌شده";
        }
    }

    /// <summary>علامت‌گذاری همه خوانده‌شده</summary>
    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        var ok = await _api.PostAsync("notifications/mark-all-read", null);
        if (ok)
        {
            _snackbar.Success("همه اعلان‌ها خوانده‌شده علامت خورد");
            await LoadAsync();
        }
        else
            _snackbar.Error(_api.LastError ?? "عملیات ناموفق بود");
    }

    /* ─── پاسخ‌ها ─── */

    private sealed class NotificationsResponse
    {
        public bool IsSuccess { get; set; }
        public NotificationsPage? Data { get; set; }
    }

    private sealed class NotificationsPage
    {
        public List<NotificationDto> Items { get; set; } = new();
        public int UnreadCount { get; set; }
    }

    private sealed class NotificationDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string Type { get; set; } = "Info";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

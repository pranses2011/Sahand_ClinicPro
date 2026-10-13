using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>ردیف نمایش کیوسک — نام بیمار + زیرعنوان (پزشک/ساعت)</summary>
public record WaitingRoomRow(string QueueNumber, string PatientName, string Subtitle, string Time);

/// <summary>
/// کد پشت نمایشگر سالن انتظار — چرخهٔ ۶۹ (فاز ۳ / F-14)
/// ساعت/تاریخ با تایمر یک‌ثانیه‌ای؛ فهرست‌ها هر ۳۰ ثانیه و با هر رویداد
/// بلادرنگ صف (QueueHub) از dashboard/appointments-today تازه می‌شوند؛
/// خطای شبکه فهرست قبلی را نگه می‌دارد (نمایشگر عمومی هرگز خالی نمی‌شود).
/// خروج: Escape (دکمه‌ای وجود ندارد تا مراجع کلیک نکند).
/// </summary>
public partial class WaitingRoomDisplayWindow : Window
{
    private readonly IApiClient _api;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _refreshTimer;
    private readonly IRealtimeClient? _realtime;

    /// <summary>سازنده — سرویس‌ها از DI سراسری برنامه</summary>
    public WaitingRoomDisplayWindow()
    {
        InitializeComponent();
        _api = App.Services.GetRequiredService<IApiClient>();

        // نام درمانگاه از تنظیمات چاپ — همان سربرگ برگه‌ها
        var clinic = LocalSettingsStore.Get("Printer.ClinicName");
        if (!string.IsNullOrWhiteSpace(clinic))
            ClinicNameText.Text = clinic;

        // ساعت و تاریخ — هر ثانیه
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();

        // تازه‌سازی فهرست‌ها — هر ۳۰ ثانیه
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _refreshTimer.Tick += (_, _) => _ = LoadQueueAsync();
        _refreshTimer.Start();

        // تازه‌سازی بی‌درنگ با رویداد صف (فراخوانی بیمار/ثبت نوبت) — best-effort
        try
        {
            _realtime = App.Services.GetService(typeof(IRealtimeClient)) as IRealtimeClient;
            if (_realtime is not null)
                _realtime.QueueChanged += (_, _) => Dispatcher.BeginInvoke(() => _ = LoadQueueAsync());
        }
        catch
        {
            // بدون بلادرنگ هم تایمر ۳۰ ثانیه کار را انجام می‌دهد
        }

        Loaded += (_, _) => _ = LoadQueueAsync();
    }

    /// <summary>به‌روزرسانی ساعت و تاریخ شمسی — ارقام فارسی</summary>
    private void UpdateClock()
    {
        var now = DateTime.Now;
        ClockText.Text = Application.Common.Helpers.PersianDateHelper.ToPersianDigits(now.ToString("HH:mm"));
        DateText.Text = Application.Common.Helpers.PersianDateHelper.ToShamsiFa(now);
    }

    /// <summary>
    /// بارگذاری نوبت‌های امروز و تفکیک «در حال ویزیت» از «پیش‌رو» —
    /// پاکت استاندارد ApiResponse باز می‌شود (درس چرخهٔ ۴۸).
    /// </summary>
    private async Task LoadQueueAsync()
    {
        try
        {
            var root = await _api.GetAsync<JsonElement>("dashboard/appointments-today?limit=20");
            var data = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d)
                ? d : root;
            if (data.ValueKind != JsonValueKind.Array)
                return;

            var inProgress = new List<WaitingRoomRow>();
            var upcoming = new List<WaitingRoomRow>();

            foreach (var a in data.EnumerateArray())
            {
                var patientName = a.TryGetProperty("patientName", out var p) ? p.GetString() ?? "" : "";
                var doctorName = a.TryGetProperty("doctorName", out var doc) ? doc.GetString() ?? "" : "";
                var time = a.TryGetProperty("time", out var t) ? t.GetString() ?? "" : "";
                var status = a.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";
                var queue = a.TryGetProperty("queueNumber", out var q) && q.ValueKind != JsonValueKind.Null
                    ? PersianDigits(q.GetInt32().ToString()) : "—";

                var row = new WaitingRoomRow(queue, patientName,
                    string.IsNullOrWhiteSpace(doctorName) ? "" : $"پزشک: {doctorName}", time);

                // وضعیت InProgress = در اتاق پزشک؛ بقیهٔ نوبت‌های امروز = پیش‌رو
                if (status == "InProgress")
                    inProgress.Add(row);
                else if (status is "Booked" or "Confirmed" or "Waiting")
                    upcoming.Add(row);
            }

            InProgressList.ItemsSource = inProgress.Take(4);
            InProgressEmpty.Visibility = inProgress.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            UpcomingList.ItemsSource = upcoming.Take(8);
            UpcomingEmpty.Visibility = upcoming.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch
        {
            // خطای شبکه — فهرست قبلی می‌ماند؛ تلاش بعدی با تایمر/رویداد صف
        }
    }

    /// <summary>خروج با Escape — تنها راه بستن (بدون دکمهٔ قابل‌کلیک برای مراجع)</summary>
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    /// <summary>تبدیل ارقام لاتین به فارسی — نمایش عمومی کیوسک</summary>
    private static string PersianDigits(string value)
    {
        var digits = new[] { '۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹' };
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            if (char.IsAsciiDigit(chars[i]))
                chars[i] = digits[chars[i] - '0'];
        return new string(chars);
    }
}

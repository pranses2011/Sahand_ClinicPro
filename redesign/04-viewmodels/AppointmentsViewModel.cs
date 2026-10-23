using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Desktop.Base;
using Sahand.ClinicPro.Desktop.Localization;
using Sahand.ClinicPro.Desktop.Themes;
namespace Sahand.ClinicPro.Desktop.ViewModels.Redesign
{
    /// <summary>
    /// ViewModel صفحه‌ی «نوبت‌دهی» — زمان‌بندی و مدیریت نوبت‌ها
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/AppointmentsView.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/AppointmentsView.html</c>
    /// 🎨 View متناظر:     <c>03-views/AppointmentsView.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class AppointmentsViewModel : PageViewModelBase<AppointmentsItem>
    {
        public AppointmentsViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════

        private string _kpi1Value = "—";
        private double? _kpi1Trend;

        /// <summary>مقدار شاخص 1 — «نوبت امروز»</summary>
        public string Kpi1Value
        {
            get => _kpi1Value;
            set => SetProperty(ref _kpi1Value, value);
        }

        /// <summary>روند شاخص 1 بر حسب درصد</summary>
        public double? Kpi1Trend
        {
            get => _kpi1Trend;
            set => SetProperty(ref _kpi1Trend, value);
        }

        private string _kpi2Value = "—";
        private double? _kpi2Trend;

        /// <summary>مقدار شاخص 2 — «در انتظار»</summary>
        public string Kpi2Value
        {
            get => _kpi2Value;
            set => SetProperty(ref _kpi2Value, value);
        }

        /// <summary>روند شاخص 2 بر حسب درصد</summary>
        public double? Kpi2Trend
        {
            get => _kpi2Trend;
            set => SetProperty(ref _kpi2Trend, value);
        }

        private string _kpi3Value = "—";
        private double? _kpi3Trend;

        /// <summary>مقدار شاخص 3 — «تأیید شده»</summary>
        public string Kpi3Value
        {
            get => _kpi3Value;
            set => SetProperty(ref _kpi3Value, value);
        }

        /// <summary>روند شاخص 3 بر حسب درصد</summary>
        public double? Kpi3Trend
        {
            get => _kpi3Trend;
            set => SetProperty(ref _kpi3Trend, value);
        }

        private string _kpi4Value = "—";
        private double? _kpi4Trend;

        /// <summary>مقدار شاخص 4 — «لغو شده»</summary>
        public string Kpi4Value
        {
            get => _kpi4Value;
            set => SetProperty(ref _kpi4Value, value);
        }

        /// <summary>روند شاخص 4 بر حسب درصد</summary>
        public double? Kpi4Trend
        {
            get => _kpi4Trend;
            set => SetProperty(ref _kpi4Trend, value);
        }


        private string _filterDoctor = null;

        /// <summary>فیلتر: پزشک</summary>
        public string FilterDoctor
        {
            get => _filterDoctor;
            set
            {
                if (SetProperty(ref _filterDoctor, value))
                    _ = LoadAsync();
            }
        }

        private DateTime? _filterDate = null;

        /// <summary>فیلتر: تاریخ</summary>
        public DateTime? FilterDate
        {
            get => _filterDate;
            set
            {
                if (SetProperty(ref _filterDate, value))
                    _ = LoadAsync();
            }
        }

        private string _filterStatus = null;

        /// <summary>فیلتر: وضعیت</summary>
        public string FilterStatus
        {
            get => _filterStatus;
            set
            {
                if (SetProperty(ref _filterStatus, value))
                    _ = LoadAsync();
            }
        }

        private string _filterRoom = null;

        /// <summary>فیلتر: اتاق</summary>
        public string FilterRoom
        {
            get => _filterRoom;
            set
            {
                if (SetProperty(ref _filterRoom, value))
                    _ = LoadAsync();
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _newCommand;

        /// <summary>+ نوبت جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            _newCommand ??= new AsyncRelayCommand(NewAsync);

        private IAsyncRelayCommand? _todayViewCommand;

        /// <summary>نمای روز</summary>
        public IAsyncRelayCommand TodayViewCommand =>
            _todayViewCommand ??= new AsyncRelayCommand(TodayViewAsync);

        private IAsyncRelayCommand? _weekViewCommand;

        /// <summary>نمای هفته</summary>
        public IAsyncRelayCommand WeekViewCommand =>
            _weekViewCommand ??= new AsyncRelayCommand(WeekViewAsync);

        private IAsyncRelayCommand? _sendReminderCommand;

        /// <summary>ارسال یادآوری</summary>
        public IAsyncRelayCommand SendReminderCommand =>
            _sendReminderCommand ??= new AsyncRelayCommand(SendReminderAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override async Task<PagedResult<AppointmentsItem>?> FetchAsync(CancellationToken ct)
        {
            // TODO: فراخوانی واقعی سرویس؛ نمونه:
            //   var response = await _apiClient.GetAsync<PagedResponse<AppointmentsItem>>(
            //       "api/v1/<built-in method lower of str object at 0x7f5384a94b30>s?page={PageNumber}&size={PageSize}", ct);
            //   return new PagedResult<AppointmentsItem>(response.Items, response.TotalCount);

            await Task.Delay(200, ct);
            var items = new List<AppointmentsItem>();
            return new PagedResult<AppointmentsItem>(items, 0);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// + نوبت جدید
        /// </summary>
        private async Task newAsync()
        {
            // TODO: باز کردن فرم ایجاد از طریق IDialogService
            await Task.CompletedTask;
        }

        /// <summary>
        /// نمای روز
        /// </summary>
        private async Task todayViewAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "نمای روز");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

        /// <summary>
        /// نمای هفته
        /// </summary>
        private async Task weekViewAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "نمای هفته");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

        /// <summary>
        /// ارسال یادآوری
        /// </summary>
        private async Task sendReminderAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "ارسال یادآوری");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }


    /// <summary>
    /// مدل نمایش یک ردیف در فهرست «نوبت‌دهی»
    /// </summary>
    public sealed class AppointmentsItem
    {
        /// <summary>ساعت</summary>
        public string Time { get; set; } = string.Empty;

        /// <summary>بیمار</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>پزشک</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>خدمت</summary>
        public string Service { get; set; } = string.Empty;

        /// <summary>اتاق</summary>
        public string Room { get; set; } = string.Empty;

        /// <summary>وضعیت</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>مدت</summary>
        public string Duration { get; set; } = string.Empty;

        /// <summary>سطح رنگیِ نشان وضعیت</summary>
        public string StatusLevel { get; set; } = "Neutral";
    }
}

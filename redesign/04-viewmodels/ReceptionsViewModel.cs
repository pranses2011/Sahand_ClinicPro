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
    /// ViewModel صفحه‌ی «پذیرش و صف» — پذیرش بیماران و مدیریت صف انتظار
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/ReceptionsView.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/ReceptionsView.html</c>
    /// 🎨 View متناظر:     <c>03-views/ReceptionsView.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class ReceptionsViewModel : PageViewModelBase<ReceptionsItem>
    {
        public ReceptionsViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════

        private string _kpi1Value = "—";
        private double? _kpi1Trend;

        /// <summary>مقدار شاخص 1 — «در صف»</summary>
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

        /// <summary>مقدار شاخص 2 — «پذیرش شده»</summary>
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

        /// <summary>مقدار شاخص 3 — «میانگین انتظار»</summary>
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

        /// <summary>مقدار شاخص 4 — «دریافت امروز»</summary>
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

        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _newReceptionCommand;

        /// <summary>+ پذیرش جدید</summary>
        public IAsyncRelayCommand NewReceptionCommand =>
            _newReceptionCommand ??= new AsyncRelayCommand(NewReceptionAsync);

        private IAsyncRelayCommand? _callNextCommand;

        /// <summary>فراخوان بعدی</summary>
        public IAsyncRelayCommand CallNextCommand =>
            _callNextCommand ??= new AsyncRelayCommand(CallNextAsync);

        private IAsyncRelayCommand? _payCommand;

        /// <summary>دریافت وجه</summary>
        public IAsyncRelayCommand PayCommand =>
            _payCommand ??= new AsyncRelayCommand(PayAsync);

        private IAsyncRelayCommand? _cancelReceptionCommand;

        /// <summary>لغو پذیرش</summary>
        public IAsyncRelayCommand CancelReceptionCommand =>
            _cancelReceptionCommand ??= new AsyncRelayCommand(CancelReceptionAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override async Task<PagedResult<ReceptionsItem>?> FetchAsync(CancellationToken ct)
        {
            // TODO: فراخوانی واقعی سرویس؛ نمونه:
            //   var response = await _apiClient.GetAsync<PagedResponse<ReceptionsItem>>(
            //       "api/v1/<built-in method lower of str object at 0x7f5384a94ab0>s?page={PageNumber}&size={PageSize}", ct);
            //   return new PagedResult<ReceptionsItem>(response.Items, response.TotalCount);

            await Task.Delay(200, ct);
            var items = new List<ReceptionsItem>();
            return new PagedResult<ReceptionsItem>(items, 0);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// + پذیرش جدید
        /// </summary>
        private async Task newReceptionAsync()
        {
            // TODO: باز کردن فرم ایجاد از طریق IDialogService
            await Task.CompletedTask;
        }

        /// <summary>
        /// فراخوان بعدی
        /// </summary>
        private async Task callNextAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "فراخوان بعدی");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

        /// <summary>
        /// دریافت وجه
        /// </summary>
        private async Task payAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "دریافت وجه");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

        /// <summary>
        /// لغو پذیرش
        /// </summary>
        private async Task cancelReceptionAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "لغو پذیرش");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }


    /// <summary>
    /// مدل نمایش یک ردیف در فهرست «پذیرش و صف»
    /// </summary>
    public sealed class ReceptionsItem
    {
        /// <summary>نوبت</summary>
        public string QueueNumber { get; set; } = string.Empty;

        /// <summary>بیمار</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>پزشک</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>خدمت</summary>
        public string Service { get; set; } = string.Empty;

        /// <summary>زمان ورود</summary>
        public string ArrivalTime { get; set; } = string.Empty;

        /// <summary>انتظار</summary>
        public string WaitMinutes { get; set; } = string.Empty;

        /// <summary>وضعیت</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>مبلغ</summary>
        public string Amount { get; set; } = string.Empty;

        /// <summary>سطح رنگیِ نشان وضعیت</summary>
        public string StatusLevel { get; set; } = "Neutral";
    }
}

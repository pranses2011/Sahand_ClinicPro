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
    /// ViewModel صفحه‌ی «پیشخوان» — نمای کلی وضعیت امروز درمانگاه
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/DashboardView.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/DashboardView.html</c>
    /// 🎨 View متناظر:     <c>03-views/DashboardView.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class DashboardViewModel : ViewModelBase
    {
        public DashboardViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════

        private string _kpi1Value = "—";
        private double? _kpi1Trend;

        /// <summary>مقدار شاخص 1 — «بیماران امروز»</summary>
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

        /// <summary>مقدار شاخص 2 — «نوبت‌های امروز»</summary>
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

        /// <summary>مقدار شاخص 3 — «درآمد امروز»</summary>
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

        /// <summary>مقدار شاخص 4 — «میانگین انتظار»</summary>
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



        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _newReceptionCommand;

        /// <summary>پذیرش جدید</summary>
        public IAsyncRelayCommand NewReceptionCommand =>
            _newReceptionCommand ??= new AsyncRelayCommand(NewReceptionAsync);

        private IAsyncRelayCommand? _newAppointmentCommand;

        /// <summary>نوبت جدید</summary>
        public IAsyncRelayCommand NewAppointmentCommand =>
            _newAppointmentCommand ??= new AsyncRelayCommand(NewAppointmentAsync);

        private IAsyncRelayCommand? _refreshCommand;

        /// <summary>تازه‌سازی</summary>
        public IAsyncRelayCommand RefreshCommand =>
            _refreshCommand ??= new AsyncRelayCommand(RefreshAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// بارگذاری داده‌های اولیه
        /// </summary>
        public async Task LoadAsync()
        {
            await RunAsync(async ct =>
            {
                // TODO: بارگذاری داده‌ها از سرویس مربوط
                await Task.Delay(200, ct);
            }, "در حال بارگذاری…");
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// پذیرش جدید
        /// </summary>
        private async Task newReceptionAsync()
        {
            // TODO: باز کردن فرم ایجاد از طریق IDialogService
            await Task.CompletedTask;
        }

        /// <summary>
        /// نوبت جدید
        /// </summary>
        private async Task newAppointmentAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "نوبت جدید");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

        /// <summary>
        /// تازه‌سازی
        /// </summary>
        private async Task refreshAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "تازه‌سازی");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}

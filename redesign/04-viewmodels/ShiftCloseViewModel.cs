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
    /// ViewModel صفحه‌ی «بستن شیفت» — تسویه و بستن شیفت جاری
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/ShiftCloseWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/ShiftCloseWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/ShiftCloseWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class ShiftCloseViewModel : FormViewModelBase
    {
        public ShiftCloseViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════

        private string _kpi1Value = "—";
        private double? _kpi1Trend;

        /// <summary>مقدار شاخص 1 — «مبلغ مورد انتظار»</summary>
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

        /// <summary>مقدار شاخص 2 — «مبلغ واقعی»</summary>
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

        /// <summary>مقدار شاخص 3 — «اختلاف»</summary>
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

        private decimal _expectedAmount = 0m;

        /// <summary>مبلغ مورد انتظار</summary>
        public decimal ExpectedAmount
        {
            get => _expectedAmount;
            set
            {
                if (SetProperty(ref _expectedAmount, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _countedAmount = 0m;

        /// <summary>مبلغ شمارش‌شده (الزامی)</summary>
        public decimal CountedAmount
        {
            get => _countedAmount;
            set
            {
                if (SetProperty(ref _countedAmount, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _differenceAmount = 0m;

        /// <summary>اختلاف</summary>
        public decimal DifferenceAmount
        {
            get => _differenceAmount;
            set
            {
                if (SetProperty(ref _differenceAmount, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _toleranceReason = string.Empty;

        /// <summary>علت اختلاف</summary>
        public string ToleranceReason
        {
            get => _toleranceReason;
            set
            {
                if (SetProperty(ref _toleranceReason, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }


        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _confirmCommand;

        /// <summary>بستن شیفت</summary>
        public IAsyncRelayCommand ConfirmCommand =>
            _confirmCommand ??= new AsyncRelayCommand(ConfirmAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override bool Validate()
        {
            ClearAllErrors();
            if (CountedAmount <= 0)
                AddError("مبلغ شمارش‌شده باید بزرگ‌تر از صفر باشد", nameof(CountedAmount));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<ShiftCloseDto>("api/v1/<built-in method lower of str object at 0x7f5384a95130>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// بستن شیفت
        /// </summary>
        private async Task confirmAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "بستن شیفت");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}

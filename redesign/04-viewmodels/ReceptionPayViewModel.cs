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
    /// ViewModel صفحه‌ی «دریافت وجه» — ثبت پرداختِ پذیرش
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/ReceptionPayWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/ReceptionPayWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/ReceptionPayWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class ReceptionPayViewModel : FormViewModelBase
    {
        public ReceptionPayViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private decimal _totalAmount = 0m;

        /// <summary>مبلغ قابل پرداخت</summary>
        public decimal TotalAmount
        {
            get => _totalAmount;
            set
            {
                if (SetProperty(ref _totalAmount, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _paidAmount = 0m;

        /// <summary>مبلغ پرداختی (الزامی)</summary>
        public decimal PaidAmount
        {
            get => _paidAmount;
            set
            {
                if (SetProperty(ref _paidAmount, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _remainingAmount = 0m;

        /// <summary>باقی‌مانده</summary>
        public decimal RemainingAmount
        {
            get => _remainingAmount;
            set
            {
                if (SetProperty(ref _remainingAmount, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _paymentMethod = string.Empty;

        /// <summary>روش پرداخت (الزامی)</summary>
        public string PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                if (SetProperty(ref _paymentMethod, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _referenceNumber = string.Empty;

        /// <summary>شماره مرجع</summary>
        public string ReferenceNumber
        {
            get => _referenceNumber;
            set
            {
                if (SetProperty(ref _referenceNumber, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _cardNumber = string.Empty;

        /// <summary>شماره کارت</summary>
        public string CardNumber
        {
            get => _cardNumber;
            set
            {
                if (SetProperty(ref _cardNumber, value))
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

        /// <summary>تأیید پرداخت</summary>
        public IAsyncRelayCommand ConfirmCommand =>
            _confirmCommand ??= new AsyncRelayCommand(ConfirmAsync);

        private IAsyncRelayCommand? _printCommand;

        /// <summary>چاپ رسید</summary>
        public IAsyncRelayCommand PrintCommand =>
            _printCommand ??= new AsyncRelayCommand(PrintAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override bool Validate()
        {
            ClearAllErrors();
            if (PaidAmount <= 0)
                AddError("مبلغ پرداختی باید بزرگ‌تر از صفر باشد", nameof(PaidAmount));
            if (string.IsNullOrWhiteSpace(PaymentMethod))
                AddError("روش پرداخت الزامی است", nameof(PaymentMethod));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<ReceptionPayDto>("api/v1/<built-in method lower of str object at 0x7f5384a94ab0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// تأیید پرداخت
        /// </summary>
        private async Task confirmAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "تأیید پرداخت");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

        /// <summary>
        /// چاپ رسید
        /// </summary>
        private async Task printAsync()
        {
            // TODO: تولید خروجی چاپ با استفاده از قالب‌های چاپ
            await Task.CompletedTask;
        }

    }
}

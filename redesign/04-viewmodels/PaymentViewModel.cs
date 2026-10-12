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
    /// ViewModel صفحه‌ی «پرداخت» — دریافت وجه عمومی
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/PaymentWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/PaymentWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/PaymentWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class PaymentViewModel : FormViewModelBase
    {
        public PaymentViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private decimal _amount = 0m;

        /// <summary>مبلغ (الزامی)</summary>
        public decimal Amount
        {
            get => _amount;
            set
            {
                if (SetProperty(ref _amount, value))
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

        private string _description = string.Empty;

        /// <summary>توضیحات</summary>
        public string Description
        {
            get => _description;
            set
            {
                if (SetProperty(ref _description, value))
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

        /// <summary>تأیید</summary>
        public IAsyncRelayCommand ConfirmCommand =>
            _confirmCommand ??= new AsyncRelayCommand(ConfirmAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override bool Validate()
        {
            ClearAllErrors();
            if (Amount <= 0)
                AddError("مبلغ باید بزرگ‌تر از صفر باشد", nameof(Amount));
            if (string.IsNullOrWhiteSpace(PaymentMethod))
                AddError("روش پرداخت الزامی است", nameof(PaymentMethod));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<PaymentDto>("api/v1/<built-in method lower of str object at 0x7f5384a950b0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// تأیید
        /// </summary>
        private async Task confirmAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "تأیید");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}

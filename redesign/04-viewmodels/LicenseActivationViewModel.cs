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
    /// ViewModel صفحه‌ی «فعال‌سازی لایسنس» — وارد کردن کلید لایسنس
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/LicenseActivationWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/LicenseActivationWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/LicenseActivationWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class LicenseActivationViewModel : FormViewModelBase
    {
        public LicenseActivationViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private string _licenseKey = string.Empty;

        /// <summary>کلید لایسنس (الزامی)</summary>
        public string LicenseKey
        {
            get => _licenseKey;
            set
            {
                if (SetProperty(ref _licenseKey, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _customerName = string.Empty;

        /// <summary>نام مشتری</summary>
        public string CustomerName
        {
            get => _customerName;
            set
            {
                if (SetProperty(ref _customerName, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _email = string.Empty;

        /// <summary>ایمیل</summary>
        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }


        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _activateCommand;

        /// <summary>فعال‌سازی</summary>
        public IAsyncRelayCommand ActivateCommand =>
            _activateCommand ??= new AsyncRelayCommand(ActivateAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override bool Validate()
        {
            ClearAllErrors();
            if (string.IsNullOrWhiteSpace(LicenseKey))
                AddError("کلید لایسنس الزامی است", nameof(LicenseKey));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<LicenseActivationDto>("api/v1/<built-in method lower of str object at 0x7f5384c8b6e0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// فعال‌سازی
        /// </summary>
        private async Task activateAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "فعال‌سازی");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}

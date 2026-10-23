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
    /// ViewModel صفحه‌ی «ورود به برنامه» — احراز هویت کاربر
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/LoginWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/LoginWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/LoginWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class LoginViewModel : ViewModelBase
    {
        public LoginViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private string _userName = string.Empty;

        /// <summary>نام کاربری (الزامی)</summary>
        public string UserName
        {
            get => _userName;
            set
            {
                if (SetProperty(ref _userName, value))
                {
                    ClearErrors();
                }
            }
        }

        private string _password = string.Empty;

        /// <summary>رمز عبور (الزامی)</summary>
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ClearErrors();
                }
            }
        }

        private bool _rememberMe = false;

        /// <summary>مرا به خاطر بسپار</summary>
        public bool RememberMe
        {
            get => _rememberMe;
            set
            {
                if (SetProperty(ref _rememberMe, value))
                {
                    ClearErrors();
                }
            }
        }


        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _loginCommand;

        /// <summary>ورود</summary>
        public IAsyncRelayCommand LoginCommand =>
            _loginCommand ??= new AsyncRelayCommand(LoginAsync);

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
        /// ورود
        /// </summary>
        private async Task loginAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "ورود");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}

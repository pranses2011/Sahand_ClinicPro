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
    /// ViewModel صفحه‌ی «تغییر رمز عبور» — تغییر رمز عبور کاربر جاری
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/ChangePasswordWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/ChangePasswordWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/ChangePasswordWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class ChangePasswordViewModel : FormViewModelBase
    {
        public ChangePasswordViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private string _currentPassword = string.Empty;

        /// <summary>رمز فعلی (الزامی)</summary>
        public string CurrentPassword
        {
            get => _currentPassword;
            set
            {
                if (SetProperty(ref _currentPassword, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _newPassword = string.Empty;

        /// <summary>رمز جدید (الزامی)</summary>
        public string NewPassword
        {
            get => _newPassword;
            set
            {
                if (SetProperty(ref _newPassword, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _confirmPassword = string.Empty;

        /// <summary>تکرار رمز جدید (الزامی)</summary>
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (SetProperty(ref _confirmPassword, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }


        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════


        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override bool Validate()
        {
            ClearAllErrors();
            if (string.IsNullOrWhiteSpace(CurrentPassword))
                AddError("رمز فعلی الزامی است", nameof(CurrentPassword));
            if (string.IsNullOrWhiteSpace(NewPassword))
                AddError("رمز جدید الزامی است", nameof(NewPassword));
            if (string.IsNullOrWhiteSpace(ConfirmPassword))
                AddError("تکرار رمز جدید الزامی است", nameof(ConfirmPassword));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<ChangePasswordDto>("api/v1/<built-in method lower of str object at 0x7f5384a94ab0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════


    }
}

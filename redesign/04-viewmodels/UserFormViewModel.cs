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
    /// ViewModel صفحه‌ی «فرم کاربر» — ایجاد یا ویرایش حساب کاربری
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/UserFormWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/UserFormWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/UserFormWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class UserFormViewModel : FormViewModelBase
    {
        public UserFormViewModel(ILogger? logger = null) : base(logger)
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
                    MarkDirty();
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
                    MarkDirty();
                }
            }
        }

        private string _confirmPassword = string.Empty;

        /// <summary>تکرار رمز (الزامی)</summary>
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

        private bool _isActive = false;

        /// <summary>فعال</summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private bool _mustChangePassword = false;

        /// <summary>تغییر رمز در اولین ورود</summary>
        public bool MustChangePassword
        {
            get => _mustChangePassword;
            set
            {
                if (SetProperty(ref _mustChangePassword, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _roleId = null;

        /// <summary>نقش اصلی (الزامی)</summary>
        public Guid? RoleId
        {
            get => _roleId;
            set
            {
                if (SetProperty(ref _roleId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _branchId = null;

        /// <summary>شعبه</summary>
        public Guid? BranchId
        {
            get => _branchId;
            set
            {
                if (SetProperty(ref _branchId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _employeeId = null;

        /// <summary>پرسنل مرتبط</summary>
        public Guid? EmployeeId
        {
            get => _employeeId;
            set
            {
                if (SetProperty(ref _employeeId, value))
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
            if (string.IsNullOrWhiteSpace(UserName))
                AddError("نام کاربری الزامی است", nameof(UserName));
            if (string.IsNullOrWhiteSpace(Password))
                AddError("رمز عبور الزامی است", nameof(Password));
            if (string.IsNullOrWhiteSpace(ConfirmPassword))
                AddError("تکرار رمز الزامی است", nameof(ConfirmPassword));
            if (RoleId is null)
                AddError("نقش اصلی الزامی است", nameof(RoleId));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<UserFormDto>("api/v1/<built-in method lower of str object at 0x7f5384a94ab0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════


    }
}

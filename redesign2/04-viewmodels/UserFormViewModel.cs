// ═══════════════════════════════════════════════════════════════════════
//  UserFormViewModel
//  صفحه‌ی «فرم کاربر» · گروه «منابع انسانی» · کهن‌الگوی «form-narrow»
//
//  متناظر با:  03-views/UserFormWindow.xaml
//  وایرفریم:    01-wireframes-html/UserFormWindow.html
//  پایه:        FormViewModelBase
//
//  این کلاس با مولدِ tools/gen_viewmodels.py از همان مشخصاتِ وایرفریم
//  ساخته شده است؛ دادهٔ نمونه درونِ آن همان چیزی است که در وایرفریم دیده
//  می‌شود تا رفتارِ صفحه پیش از اتصالِ سرویسِ واقعی قابلِ بررسی باشد.
// ═══════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Desktop.Base;

namespace Sahand.ClinicPro.Desktop.ViewModels
{
    /// <summary>
    /// ViewModelِ صفحه‌ی «فرم کاربر»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-narrow</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: منابع انسانی
    /// </remarks>
    public sealed class UserFormViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public UserFormViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("فرم کاربر", "نقش‌ها و مجوزها", "نقش تعیین‌کننده دسترسی"));
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>پرسنل — الزامی</summary>
        public string PersonnelId
        {
            get => _personnelId;
            set
            {
                if (SetProperty(ref _personnelId, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _personnelId = "مریم نوری — E-۱۰۰۱";

        /// <summary>نام کاربری — الزامی</summary>
        public string UserName
        {
            get => _userName;
            set
            {
                if (SetProperty(ref _userName, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _userName = "mnouri";

        /// <summary>رمز عبور — الزامی</summary>
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _password = "••••••••";

        /// <summary>نقش — الزامی</summary>
        public string RoleId
        {
            get => _roleId;
            set
            {
                if (SetProperty(ref _roleId, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _roleId = "پذیرش";

        /// <summary>شعبه</summary>
        public string BranchId
        {
            get => _branchId;
            set
            {
                if (SetProperty(ref _branchId, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _branchId = "مرکزی";

        /// <summary>تغییر رمز در نخستین ورود</summary>
        public bool MustChangePassword
        {
            get => _mustChangePassword;
            set
            {
                if (SetProperty(ref _mustChangePassword, value))
                {
                    MarkDirty();
                }
            }
        }
        private bool _mustChangePassword = true;

        /// <summary>فعال</summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    MarkDirty();
                }
            }
        }
        private bool _isActive = true;

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

            if (string.IsNullOrWhiteSpace(PersonnelId))
            {
                AddError("پرسنل الزامی است", nameof(PersonnelId));
            }
            else
            {
                ClearErrors(nameof(PersonnelId));
            }

            if (string.IsNullOrWhiteSpace(UserName))
            {
                AddError("نام کاربری الزامی است", nameof(UserName));
            }
            else
            {
                ClearErrors(nameof(UserName));
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                AddError("رمز عبور الزامی است", nameof(Password));
            }
            else
            {
                ClearErrors(nameof(Password));
            }

            if (string.IsNullOrWhiteSpace(RoleId))
            {
                AddError("نقش الزامی است", nameof(RoleId));
            }
            else
            {
                ClearErrors(nameof(RoleId));
            }
            return ok && !HasErrors;
        }

        /// <summary>
        /// ذخیره‌ی داده‌ها. در صورت نبودِ سرویس، فقط وضعیت موفقیت نمایش
        /// داده می‌شود تا رفتارِ فرم قابلِ بررسی باشد.
        /// </summary>
        protected override Task PersistAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی
            return Task.CompletedTask;
        }

        /// <summary>جستجو در یک فیلدِ جستجومحور</summary>
        private Task LookupAsync(string field)
            => RunAsync(_ => Task.CompletedTask, $"جستجوی {field}…");

        /// <summary>خواندنِ بارکد برای یک فیلد</summary>
        private void ScanBarcode(string field)
            => ShowSuccess($"بارکدِ {field} خوانده شد");
    }
}
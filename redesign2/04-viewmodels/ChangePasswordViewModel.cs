// ═══════════════════════════════════════════════════════════════════════
//  ChangePasswordViewModel
//  صفحه‌ی «تغییر رمز عبور» · گروه «پیشخوان» · کهن‌الگوی «form-narrow»
//
//  متناظر با:  03-views/ChangePasswordWindow.xaml
//  وایرفریم:    01-wireframes-html/ChangePasswordWindow.html
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
    /// ViewModelِ صفحه‌ی «تغییر رمز عبور»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-narrow</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: پیشخوان
    /// </remarks>
    public sealed class ChangePasswordViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public ChangePasswordViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("تغییر رمز", "ورود", "پس از تغییر، نشست بسته می‌شود"));
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>رمز عبور فعلی — الزامی</summary>
        public string CurrentPassword
        {
            get => _currentPassword;
            set
            {
                if (SetProperty(ref _currentPassword, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _currentPassword = "••••••••";

        /// <summary>رمز عبور جدید — الزامی</summary>
        public string NewPassword
        {
            get => _newPassword;
            set
            {
                if (SetProperty(ref _newPassword, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _newPassword = "••••••••";

        /// <summary>تکرار رمز عبور جدید — الزامی</summary>
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (SetProperty(ref _confirmPassword, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _confirmPassword = "••••••••";

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

            if (string.IsNullOrWhiteSpace(CurrentPassword))
            {
                AddError("رمز عبور فعلی الزامی است", nameof(CurrentPassword));
            }
            else
            {
                ClearErrors(nameof(CurrentPassword));
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                AddError("رمز عبور جدید الزامی است", nameof(NewPassword));
            }
            else
            {
                ClearErrors(nameof(NewPassword));
            }

            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                AddError("تکرار رمز عبور جدید الزامی است", nameof(ConfirmPassword));
            }
            else
            {
                ClearErrors(nameof(ConfirmPassword));
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
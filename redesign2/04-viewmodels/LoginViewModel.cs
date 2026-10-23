// ═══════════════════════════════════════════════════════════════════════
//  LoginViewModel
//  صفحه‌ی «ورود به برنامه» · گروه «پیشخوان» · کهن‌الگوی «form-narrow»
//
//  متناظر با:  03-views/LoginWindow.xaml
//  وایرفریم:    01-wireframes-html/LoginWindow.html
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
    /// ViewModelِ صفحه‌ی «ورود به برنامه»
    /// </summary>
    /// <remarks>
    /// پنلِ راست شامل برند و سه مزیت کلیدی است؛ فرم در پنل چپ و با
    /// اندازه‌ی کاملِ ستون.
    ///
    /// کهن‌الگوی طراحی: <c>form-narrow</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: پیشخوان
    /// </remarks>
    public sealed class LoginViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public LoginViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("ورود", "پنجره اصلی", "پس از احراز هویت"));
            Relations.Add(new RelationItem("ورود", "تغییر رمز عبور", "در صورت MustChangePassword"));

            // سربرگِ برجسته
            Hero = new HeroModel("", "", "");
        }

        /// <summary>ویژگی‌های برجسته‌ی برنامه در صفحه‌ی ورود</summary>
        public ObservableCollection<FeatureItem> Features { get; } = new();

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

            // ویژگی‌های صفحه‌ی ورود
            Features.Add(new FeatureItem(Application.Current.TryFindResource("Icon.ShieldCheck") as Geometry, "داده‌ها با رمزنگاری AES-256 محافظت می‌شوند"));
            Features.Add(new FeatureItem(Application.Current.TryFindResource("Icon.Database") as Geometry, "پشتیبان‌گیری خودکار هر شب"));
            Features.Add(new FeatureItem(Application.Current.TryFindResource("Icon.Monitor") as Geometry, "سازگار با ویندوز ۱۰ و ۱۱"));

        /// <summary>سربرگِ برجسته</summary>
        public HeroModel? Hero
        {
            get => _hero;
            set => SetProperty(ref _hero, value);
        }
        private HeroModel? _hero;

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

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
        private string _userName = "admin";

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

        /// <summary>مرا در این دستگاه به خاطر بسپار</summary>
        public bool RememberMe
        {
            get => _rememberMe;
            set
            {
                if (SetProperty(ref _rememberMe, value))
                {
                    MarkDirty();
                }
            }
        }
        private bool _rememberMe = true;

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>ورود</summary>
        public IAsyncRelayCommand LoginCommand =>
            new AsyncRelayCommand(LoginAsync);

        /// <summary>اجرای «ورود»</summary>
        private async Task LoginAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ورود»
            await RunAsync(_ => Task.CompletedTask, "ورود…");
        }

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

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
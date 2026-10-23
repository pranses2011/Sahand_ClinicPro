// ═══════════════════════════════════════════════════════════════════════
//  UsersViewModel
//  صفحه‌ی «کاربران» · گروه «منابع انسانی» · کهن‌الگوی «list-dense»
//
//  متناظر با:  03-views/UsersView.xaml
//  وایرفریم:    01-wireframes-html/UsersView.html
//  پایه:        PageViewModelBase<UsersRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «کاربران»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class UsersRow
    {
        /// <summary>کاربر — who</summary>
        public string? User { get; set; }
        /// <summary>زیرنویسِ «کاربر» (مانند کد یا سن)</summary>
        public string? UserSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «کاربر»</summary>
        public string? UserInitials { get; set; }
        /// <summary>رنگِ نشانِ «کاربر» — از تم خوانده می‌شود</summary>
        public Brush? UserAvatar { get; set; }

        /// <summary>نقش — text</summary>
        public string? Role { get; set; }

        /// <summary>آخرین ورود — date</summary>
        public string? LastLogin { get; set; }

        /// <summary>شعبه — text</summary>
        public string? Branch { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «کاربران»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-dense</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: منابع انسانی
    /// </remarks>
    public sealed class UsersViewModel : PageViewModelBase<UsersRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public UsersViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("کاربران", "فرم کاربر", "ایجاد/ویرایش"));
            Relations.Add(new RelationItem("کاربران", "نقش‌ها و مجوزها", "تخصیص نقش"));
            Relations.Add(new RelationItem("کاربران", "گزارش حسابرسی", "ردپای عملیات"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>فیلترِ «نقش»</summary>
        public object? Filter2
        {
            get => _filter2;
            set
            {
                if (SetProperty(ref _filter2, value))
                {
                    PageNumber = 1;
                    _ = LoadAsync();
                }
            }
        }
        private object? _filter2 = "همه";

        /// <summary>فیلترِ «وضعیت»</summary>
        public object? Filter3
        {
            get => _filter3;
            set
            {
                if (SetProperty(ref _filter3, value))
                {
                    PageNumber = 1;
                    _ = LoadAsync();
                }
            }
        }
        private object? _filter3 = "فعال";

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>کاربر جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «کاربر جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «کاربر جدید»
            await RunAsync(_ => Task.CompletedTask, "کاربر جدید…");
        }

        /// <summary>مجوزها</summary>
        public IAsyncRelayCommand PermissionsCommand =>
            new AsyncRelayCommand(PermissionsAsync);

        /// <summary>اجرای «مجوزها»</summary>
        private async Task PermissionsAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «مجوزها»
            await RunAsync(_ => Task.CompletedTask, "مجوزها…");
        }

        /// <summary>بازنشانی رمز</summary>
        public IAsyncRelayCommand ResetPassCommand =>
            new AsyncRelayCommand(ResetPassAsync);

        /// <summary>اجرای «بازنشانی رمز»</summary>
        private async Task ResetPassAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «بازنشانی رمز»
            await RunAsync(_ => Task.CompletedTask, "بازنشانی رمز…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<UsersRow> SampleData()
        {
            yield return new UsersRow { User = "admin", UserSub = "سعید نجفی", UserInitials = "عا", UserAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Role = "مدیر سیستم", LastLogin = "۱۴۰۴/۰۷/۱۹ ۰۸:۱۲", Branch = "مرکزی", Status = "فعال", StatusTone = "ok" };
            yield return new UsersRow { User = "mnouri", UserSub = "مریم نوری", UserInitials = "من", UserAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Role = "پذیرش", LastLogin = "۱۴۰۴/۰۷/۱۹ ۰۸:۰۵", Branch = "مرکزی", Status = "فعال", StatusTone = "ok" };
            yield return new UsersRow { User = "lhasani", UserSub = "لیلا حسنی", UserInitials = "لح", UserAvatar = Application.Current.TryFindResource("Brush.Success") as Brush, Role = "حسابدار", LastLogin = "۱۴۰۴/۰۷/۱۸ ۱۶:۴۰", Branch = "مرکزی", Status = "فعال", StatusTone = "ok" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<UsersRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<UsersRow>(SampleData());
            return Task.FromResult<PagedResult<UsersRow>?>(
                new PagedResult<UsersRow>(items, items.Count));
        }
    }
}
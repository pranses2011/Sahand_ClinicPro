// ═══════════════════════════════════════════════════════════════════════
//  RolePermissionViewModel
//  صفحه‌ی «نقش‌ها و مجوزها» · گروه «منابع انسانی» · کهن‌الگوی «split»
//
//  متناظر با:  03-views/RolePermissionView.xaml
//  وایرفریم:    01-wireframes-html/RolePermissionView.html
//  پایه:        PageViewModelBase<RolePermissionRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «نقش‌ها و مجوزها»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class RolePermissionRow
    {
        /// <summary>ماژول — text</summary>
        public string? Module { get; set; }

        /// <summary>مجوز — text</summary>
        public string? Permission { get; set; }

        /// <summary>نقش‌های دارا — text</summary>
        public string? Roles { get; set; }

        /// <summary>سطح — chip</summary>
        public string? Level { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «سطح»</summary>
        public string LevelTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «نقش‌ها و مجوزها»
    /// </summary>
    /// <remarks>
    /// ستون راست: فهرست نقش‌ها با شمار مجوز. ستون چپ: درخت مجوزها با
    /// گروه‌بندی ماژول و چک‌باکس.
    ///
    /// کهن‌الگوی طراحی: <c>split</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: منابع انسانی
    /// </remarks>
    public sealed class RolePermissionViewModel : PageViewModelBase<RolePermissionRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public RolePermissionViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("نقش‌ها و مجوزها", "کاربران", "تخصیص نقش به کاربر"));
            Relations.Add(new RelationItem("نقش‌ها و مجوزها", "مدیریت سیستم", "بخشی از مدیریت"));

            // خلاصه‌ی کناری
            Summary.Add(new KeyValueItem("مدیر سیستم", "۳۹ مجوز"));
            Summary.Add(new KeyValueItem("پزشک", "۱۸ مجوز"));
            Summary.Add(new KeyValueItem("پذیرش", "۱۲ مجوز"));
            Summary.Add(new KeyValueItem("حسابدار", "۱۴ مجوز"));
            Summary.Add(new KeyValueItem("پرستار", "۸ مجوز"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>فیلترِ «ماژول»</summary>
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

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        /// <summary>خلاصه‌ی کناریِ صفحه</summary>
        public ObservableCollection<KeyValueItem> Summary { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>نقش جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «نقش جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «نقش جدید»
            await RunAsync(_ => Task.CompletedTask, "نقش جدید…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<RolePermissionRow> SampleData()
        {
            yield return new RolePermissionRow { Module = "بیماران", Permission = "مشاهده فهرست بیماران", Roles = "همه نقش‌ها", Level = "پایه", LevelTone = "nu" };
            yield return new RolePermissionRow { Module = "بیماران", Permission = "ایجاد پرونده جدید", Roles = "پذیرش · مدیر", Level = "نوشتن", LevelTone = "cy" };
            yield return new RolePermissionRow { Module = "بیماران", Permission = "حذف پرونده", Roles = "مدیر سیستم", Level = "حساس", LevelTone = "dg" };
            yield return new RolePermissionRow { Module = "مالی", Permission = "صدور فاکتور", Roles = "پذیرش · حسابدار", Level = "نوشتن", LevelTone = "cy" };
            yield return new RolePermissionRow { Module = "مالی", Permission = "اعلام تراز دستی سند", Roles = "حسابدار · مدیر", Level = "حساس", LevelTone = "dg" };
            yield return new RolePermissionRow { Module = "مالی", Permission = "مشاهده گزارش مالی", Roles = "حسابدار · مدیر", Level = "پایه", LevelTone = "nu" };
            yield return new RolePermissionRow { Module = "داروخانه", Permission = "تحویل نسخه", Roles = "داروخانه", Level = "نوشتن", LevelTone = "cy" };
            yield return new RolePermissionRow { Module = "داروخانه", Permission = "ورود کالا", Roles = "داروخانه · مدیر", Level = "نوشتن", LevelTone = "cy" };
            yield return new RolePermissionRow { Module = "سیستم", Permission = "مدیریت کاربران و مجوزها", Roles = "مدیر سیستم", Level = "حساس", LevelTone = "dg" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<RolePermissionRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<RolePermissionRow>(SampleData());
            return Task.FromResult<PagedResult<RolePermissionRow>?>(
                new PagedResult<RolePermissionRow>(items, items.Count));
        }
    }
}
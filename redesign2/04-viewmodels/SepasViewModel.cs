// ═══════════════════════════════════════════════════════════════════════
//  SepasViewModel
//  صفحه‌ی «سامانه سپاس» · گروه «سازمانی» · کهن‌الگوی «detail-tabs»
//
//  متناظر با:  03-views/SepasView.xaml
//  وایرفریم:    01-wireframes-html/SepasView.html
//  پایه:        PageViewModelBase<SepasRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «سامانه سپاس»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class SepasRow
    {
        /// <summary>زمان — date</summary>
        public string? Time { get; set; }

        /// <summary>نوع داده — text</summary>
        public string? Type { get; set; }

        /// <summary>رکوردها — num</summary>
        public int Records { get; set; }

        /// <summary>نتیجه — chip</summary>
        public string? Result { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «نتیجه»</summary>
        public string ResultTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «سامانه سپاس»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>detail-tabs</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: سازمانی
    /// </remarks>
    public sealed class SepasViewModel : PageViewModelBase<SepasRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public SepasViewModel(ILogger? logger = null) : base(logger)
        {
            // تب‌ها
            Tabs.Add(new TabItemModel("تنظیمات اتصال", Application.Current.TryFindResource("Icon.Plug") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «تنظیمات اتصال»
            Tabs.Add(new TabItemModel("ارسال‌ها", Application.Current.TryFindResource("Icon.Send") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «ارسال‌ها»
            Tabs.Add(new TabItemModel("خطاها", Application.Current.TryFindResource("Icon.TriangleAlert") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «خطاها»

            // ارتباطات
            Relations.Add(new RelationItem("سامانه سپاس", "نسخه الکترونیک", "ارسال داده"));
            Relations.Add(new RelationItem("سامانه سپاس", "پرونده‌ی بیمار", "منبع داده"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>محتوای تب‌ها</summary>
        public ObservableCollection<TabItemModel> Tabs { get; } = new();

        /// <summary>محتوای تب‌ها — هر اندیس متناظر با یک تب در View است</summary>
        public ObservableCollection<object?> TabContents { get; } = new();

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>نشانی سرویس — الزامی</summary>
        public string Endpoint
        {
            get => _endpoint;
            set
            {
                if (SetProperty(ref _endpoint, value))
                {
                    Validate();
                }
            }
        }
        private string _endpoint = "https://sepas.example.ir/api";

        /// <summary>فعال</summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (SetProperty(ref _enabled, value))
                {
                }
            }
        }
        private bool _enabled = true;

        /// <summary>نام کاربری</summary>
        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                }
            }
        }
        private string _username = "clinic_sahand";

        /// <summary>رمز عبور</summary>
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                }
            }
        }
        private string _password = "••••••••";

        /// <summary>دوره ارسال</summary>
        public string Interval
        {
            get => _interval;
            set
            {
                if (SetProperty(ref _interval, value))
                {
                }
            }
        }
        private string _interval = "روزانه";

        /// <summary>آخرین همگام‌سازی</summary>
        public string LastSync
        {
            get => _lastSync;
            set
            {
                if (SetProperty(ref _lastSync, value))
                {
                }
            }
        }
        private string _lastSync = "۱۴۰۴/۰۷/۱۹ ۰۷:۰۰";

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>تست اتصال</summary>
        public IAsyncRelayCommand TestCommand =>
            new AsyncRelayCommand(TestAsync);

        /// <summary>اجرای «تست اتصال»</summary>
        private async Task TestAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «تست اتصال»
            await RunAsync(_ => Task.CompletedTask, "تست اتصال…");
        }

        /// <summary>همگام‌سازی الآن</summary>
        public IAsyncRelayCommand SyncNowCommand =>
            new AsyncRelayCommand(SyncNowAsync);

        /// <summary>اجرای «همگام‌سازی الآن»</summary>
        private async Task SyncNowAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «همگام‌سازی الآن»
            await RunAsync(_ => Task.CompletedTask, "همگام‌سازی الآن…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<SepasRow> SampleData()
        {
            yield return new SepasRow { Time = "۱۴۰۴/۰۷/۱۹ ۰۷:۰۰", Type = "ویزیت‌های روزانه", Records = 36, Result = "موفق", ResultTone = "ok" };
            yield return new SepasRow { Time = "۱۴۰۴/۰۷/۱۸ ۰۷:۰۰", Type = "ویزیت‌های روزانه", Records = 31, Result = "موفق", ResultTone = "ok" };
            yield return new SepasRow { Time = "۱۴۰۴/۰۷/۱۷ ۰۷:۰۰", Type = "نسخه‌های الکترونیک", Records = 28, Result = "خطا", ResultTone = "dg" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<SepasRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<SepasRow>(SampleData());
            return Task.FromResult<PagedResult<SepasRow>?>(
                new PagedResult<SepasRow>(items, items.Count));
        }
    }
}
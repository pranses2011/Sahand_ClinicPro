// ═══════════════════════════════════════════════════════════════════════
//  BackupViewModel
//  صفحه‌ی «پشتیبان‌گیری» · گروه «سیستم» · کهن‌الگوی «detail-tabs»
//
//  متناظر با:  03-views/BackupView.xaml
//  وایرفریم:    01-wireframes-html/BackupView.html
//  پایه:        PageViewModelBase<BackupRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «پشتیبان‌گیری»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class BackupRow
    {
        /// <summary>نام فایل — text</summary>
        public string? Name { get; set; }

        /// <summary>تاریخ — date</summary>
        public string? Date { get; set; }

        /// <summary>حجم — num</summary>
        public string? Size { get; set; }

        /// <summary>نوع — chip</summary>
        public string? Type { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «نوع»</summary>
        public string TypeTone { get; set; } = "nu";

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «پشتیبان‌گیری»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>detail-tabs</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: سیستم
    /// </remarks>
    public sealed class BackupViewModel : PageViewModelBase<BackupRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public BackupViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("آخرین پشتیبان", "امروز ۰۲:۰۰", null, Application.Current.TryFindResource("Icon.DatabaseBackup") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("حجم کل", "۴٫۲ گیگابایت", null, Application.Current.TryFindResource("Icon.HardDrive") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));
            Kpis.Add(new KpiItem("تعداد نسخه‌ها", "۳۰", null, Application.Current.TryFindResource("Icon.History") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));

            // تب‌ها
            Tabs.Add(new TabItemModel("نسخه‌ها", Application.Current.TryFindResource("Icon.HardDrive") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «نسخه‌ها»
            Tabs.Add(new TabItemModel("زمان‌بندی", Application.Current.TryFindResource("Icon.Clock") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «زمان‌بندی»
            Tabs.Add(new TabItemModel("بازیابی", Application.Current.TryFindResource("Icon.RotateCcw") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «بازیابی»
            Tabs.Add(new TabItemModel("تنظیمات", Application.Current.TryFindResource("Icon.Settings") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «تنظیمات»

            // ارتباطات
            Relations.Add(new RelationItem("پشتیبان‌گیری", "تنظیمات", "تنظیمات بکاپ در تنظیمات"));
            Relations.Add(new RelationItem("پشتیبان‌گیری", "مدیریت سیستم", "ابزار مدیریتی"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

        /// <summary>محتوای تب‌ها</summary>
        public ObservableCollection<TabItemModel> Tabs { get; } = new();

        /// <summary>محتوای تب‌ها — هر اندیس متناظر با یک تب در View است</summary>
        public ObservableCollection<object?> TabContents { get; } = new();

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>پشتیبان‌گیری خودکار</summary>
        public bool AutoEnabled
        {
            get => _autoEnabled;
            set
            {
                if (SetProperty(ref _autoEnabled, value))
                {
                }
            }
        }
        private bool _autoEnabled = true;

        /// <summary>دوره</summary>
        public string Schedule
        {
            get => _schedule;
            set
            {
                if (SetProperty(ref _schedule, value))
                {
                }
            }
        }
        private string _schedule = "روزانه";

        /// <summary>ساعت اجرا</summary>
        public string Time
        {
            get => _time;
            set
            {
                if (SetProperty(ref _time, value))
                {
                }
            }
        }
        private string _time = "۰۲:۰۰";

        /// <summary>نگهداری (روز)</summary>
        public int Retention
        {
            get => _retention;
            set
            {
                if (SetProperty(ref _retention, value))
                {
                }
            }
        }
        private int _retention = 30;

        /// <summary>مسیر ذخیره</summary>
        public string Path
        {
            get => _path;
            set
            {
                if (SetProperty(ref _path, value))
                {
                }
            }
        }
        private string _path = "D:\\Backups\\ClinicPro";

        /// <summary>فشرده‌سازی</summary>
        public bool Compress
        {
            get => _compress;
            set
            {
                if (SetProperty(ref _compress, value))
                {
                }
            }
        }
        private bool _compress = true;

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>پشتیبان‌گیری الآن</summary>
        public IAsyncRelayCommand BackupNowCommand =>
            new AsyncRelayCommand(BackupNowAsync);

        /// <summary>اجرای «پشتیبان‌گیری الآن»</summary>
        private async Task BackupNowAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «پشتیبان‌گیری الآن»
            await RunAsync(_ => Task.CompletedTask, "پشتیبان‌گیری الآن…");
        }

        /// <summary>بازیابی</summary>
        public IAsyncRelayCommand RestoreCommand =>
            new AsyncRelayCommand(RestoreAsync);

        /// <summary>اجرای «بازیابی»</summary>
        private async Task RestoreAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «بازیابی»
            await RunAsync(_ => Task.CompletedTask, "بازیابی…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<BackupRow> SampleData()
        {
            yield return new BackupRow { Name = "clinicpro-14040719-0200.bak", Date = "۱۴۰۴/۰۷/۱۹ ۰۲:۰۰", Size = "۱۴۲ مگابایت", Type = "خودکار", TypeTone = "cy", Status = "موفق", StatusTone = "ok" };
            yield return new BackupRow { Name = "clinicpro-14040718-0200.bak", Date = "۱۴۰۴/۰۷/۱۸ ۰۲:۰۰", Size = "۱۳۹ مگابایت", Type = "خودکار", TypeTone = "cy", Status = "موفق", StatusTone = "ok" };
            yield return new BackupRow { Name = "clinicpro-14040717-0200.bak", Date = "۱۴۰۴/۰۷/۱۷ ۰۲:۰۰", Size = "۱۳۷ مگابایت", Type = "خودکار", TypeTone = "cy", Status = "موفق", StatusTone = "ok" };
            yield return new BackupRow { Name = "clinicpro-manual-14040716.bak", Date = "۱۴۰۴/۰۷/۱۶ ۱۴:۳۰", Size = "۱۳۵ مگابایت", Type = "دستی", TypeTone = "nu", Status = "موفق", StatusTone = "ok" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<BackupRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<BackupRow>(SampleData());
            return Task.FromResult<PagedResult<BackupRow>?>(
                new PagedResult<BackupRow>(items, items.Count));
        }
    }
}
// ═══════════════════════════════════════════════════════════════════════
//  LicenseStatusViewModel
//  صفحه‌ی «وضعیت لایسنس» · گروه «سیستم» · کهن‌الگوی «detail-tabs»
//
//  متناظر با:  03-views/LicenseStatusView.xaml
//  وایرفریم:    01-wireframes-html/LicenseStatusView.html
//  پایه:        PageViewModelBase<LicenseStatusRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «وضعیت لایسنس»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class LicenseStatusRow
    {
        /// <summary>ماژول — text</summary>
        public string? Module { get; set; }

        /// <summary>کلید — text</summary>
        public string? Key { get; set; }

        /// <summary>تاریخ انقضا — date</summary>
        public string? Expiry { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «وضعیت لایسنس»
    /// </summary>
    /// <remarks>
    /// کارت وضعیت با نوار پیشرفتِ روزهای باقیمانده؛ جدول ماژول‌ها با
    /// چیپ‌های سبز/خاکستری.
    ///
    /// کهن‌الگوی طراحی: <c>detail-tabs</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: سیستم
    /// </remarks>
    public sealed class LicenseStatusViewModel : PageViewModelBase<LicenseStatusRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public LicenseStatusViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("وضعیت", "فعال", null, Application.Current.TryFindResource("Icon.ShieldCheck") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("روز باقیمانده", "۳۴۲", null, Application.Current.TryFindResource("Icon.CalendarClock") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("ماژول‌های فعال", "۲۷ از ۳۹", null, Application.Current.TryFindResource("Icon.Boxes") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));
            Kpis.Add(new KpiItem("کاربران مجاز", "۱۲", null, Application.Current.TryFindResource("Icon.Users") as Geometry, Application.Current.TryFindResource("Brush.Warning") as Brush));

            // تب‌ها
            Tabs.Add(new TabItemModel("نمای کلی", Application.Current.TryFindResource("Icon.LayoutDashboard") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «نمای کلی»
            Tabs.Add(new TabItemModel("ماژول‌ها", Application.Current.TryFindResource("Icon.Boxes") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «ماژول‌ها»
            Tabs.Add(new TabItemModel("تاریخچه", Application.Current.TryFindResource("Icon.History") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «تاریخچه»
            Tabs.Add(new TabItemModel("پشتیبانی", Application.Current.TryFindResource("Icon.LifeBuoy") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «پشتیبانی»

            // ارتباطات
            Relations.Add(new RelationItem("وضعیت لایسنس", "فعال‌سازی لایسنس", "ورود کلید مجوز"));
            Relations.Add(new RelationItem("وضعیت لایسنس", "تنظیمات", "تنظیمات پنل لایسنس"));

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
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>تمدید</summary>
        public IAsyncRelayCommand RenewCommand =>
            new AsyncRelayCommand(RenewAsync);

        /// <summary>اجرای «تمدید»</summary>
        private async Task RenewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «تمدید»
            await RunAsync(_ => Task.CompletedTask, "تمدید…");
        }

        /// <summary>فعال‌سازی</summary>
        public IAsyncRelayCommand ActivateCommand =>
            new AsyncRelayCommand(ActivateAsync);

        /// <summary>اجرای «فعال‌سازی»</summary>
        private async Task ActivateAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «فعال‌سازی»
            await RunAsync(_ => Task.CompletedTask, "فعال‌سازی…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<LicenseStatusRow> SampleData()
        {
            yield return new LicenseStatusRow { Module = "بیماران و پرونده الکترونیک", Key = "pat.emr", Expiry = "۱۴۰۵/۰۷/۱۹", Status = "فعال", StatusTone = "ok" };
            yield return new LicenseStatusRow { Module = "نوبت‌دهی", Key = "apt.core", Expiry = "۱۴۰۵/۰۷/۱۹", Status = "فعال", StatusTone = "ok" };
            yield return new LicenseStatusRow { Module = "پذیرش و صف", Key = "rcp.core", Expiry = "۱۴۰۵/۰۷/۱۹", Status = "فعال", StatusTone = "ok" };
            yield return new LicenseStatusRow { Module = "ویزیت (SOAP)", Key = "vst.soap", Expiry = "۱۴۰۵/۰۷/۱۹", Status = "فعال", StatusTone = "ok" };
            yield return new LicenseStatusRow { Module = "نسخه و تداخل دارویی", Key = "rx.core", Expiry = "۱۴۰۵/۰۷/۱۹", Status = "فعال", StatusTone = "ok" };
            yield return new LicenseStatusRow { Module = "نسخه الکترونیک", Key = "rx.epres", Expiry = "۱۴۰۵/۰۷/۱۹", Status = "فعال", StatusTone = "ok" };
            yield return new LicenseStatusRow { Module = "داروخانه و انبار", Key = "phr.inv", Expiry = "۱۴۰۵/۰۷/۱۹", Status = "فعال", StatusTone = "ok" };
            yield return new LicenseStatusRow { Module = "آزمایشگاه", Key = "lab.core", Expiry = "۱۴۰۵/۰۷/۱۹", Status = "فعال", StatusTone = "ok" };
            yield return new LicenseStatusRow { Module = "تصویربرداری (DICOM)", Key = "img.dicom", Expiry = "۱۴۰۵/۰۷/۱۹", Status = "فعال", StatusTone = "ok" };
            yield return new LicenseStatusRow { Module = "گزارش‌ساز پویا", Key = "rpt.adhoc", Expiry = "—", Status = "غیرفعال", StatusTone = "nu" };
            yield return new LicenseStatusRow { Module = "سامانه سپاس", Key = "int.sepas", Expiry = "—", Status = "غیرفعال", StatusTone = "nu" };
            yield return new LicenseStatusRow { Module = "مرکز آموزش", Key = "edu.hub", Expiry = "—", Status = "غیرفعال", StatusTone = "nu" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<LicenseStatusRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<LicenseStatusRow>(SampleData());
            return Task.FromResult<PagedResult<LicenseStatusRow>?>(
                new PagedResult<LicenseStatusRow>(items, items.Count));
        }
    }
}
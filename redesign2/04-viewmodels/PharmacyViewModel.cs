// ═══════════════════════════════════════════════════════════════════════
//  PharmacyViewModel
//  صفحه‌ی «داروخانه» · گروه «داروخانه» · کهن‌الگوی «pos»
//
//  متناظر با:  03-views/PharmacyView.xaml
//  وایرفریم:    01-wireframes-html/PharmacyView.html
//  پایه:        PageViewModelBase<PharmacyRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «داروخانه»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class PharmacyRow
    {
        /// <summary>کد — text</summary>
        public string? Code { get; set; }

        /// <summary>نام دارو — text</summary>
        public string? Name { get; set; }

        /// <summary>نام ژنریک — text</summary>
        public string? Generic { get; set; }

        /// <summary>بچ — num</summary>
        public string? Batch { get; set; }

        /// <summary>موجودی — num</summary>
        public int Stock { get; set; }

        /// <summary>قیمت — money</summary>
        public decimal Price { get; set; }

        /// <summary>انقضا — date</summary>
        public string? Expiry { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «داروخانه»
    /// </summary>
    /// <remarks>
    /// ستون چپ: کاتالوگِ داروها با جستجوی بارکد. ستون راست: سبدِ تحویل با
    /// جمع مبالغ؛ الگوی FEFO بچ نزدیک‌تر به انقضا را اول مصرف می‌کند.
    ///
    /// کهن‌الگوی طراحی: <c>pos</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: داروخانه
    /// </remarks>
    public sealed class PharmacyViewModel : PageViewModelBase<PharmacyRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public PharmacyViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("اقلام موجود", "۴۲۸", null, Application.Current.TryFindResource("Icon.Package") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("زیر حد سفارش", "۱۷", -3, Application.Current.TryFindResource("Icon.TriangleAlert") as Geometry, Application.Current.TryFindResource("Brush.Warning") as Brush));
            Kpis.Add(new KpiItem("نزدیک انقضا", "۲۹", -5, Application.Current.TryFindResource("Icon.CalendarX") as Geometry, Application.Current.TryFindResource("Brush.Danger") as Brush));
            Kpis.Add(new KpiItem("فروش امروز", "۵,۸۴۰,۰۰۰", 14, Application.Current.TryFindResource("Icon.Banknote") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));

            // ارتباطات
            Relations.Add(new RelationItem("داروخانه", "فرم قلم دارویی", "تعریف قلم"));
            Relations.Add(new RelationItem("داروخانه", "ورود کالا", "تأمین موجودی"));
            Relations.Add(new RelationItem("داروخانه", "انبارگردانی", "شمارش"));
            Relations.Add(new RelationItem("داروخانه", "نسخه‌ها", "تحویل بر اساس نسخه"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

        /// <summary>فیلترِ «گروه دارویی»</summary>
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

        /// <summary>فیلترِ «فقط موجود»</summary>
        public bool Filter3
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
        private bool _filter3 = false;

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        /// <summary>اقلامِ سبدِ فروش</summary>
        public ObservableCollection<CartLine> CartItems { get; } = new();

        /// <summary>جمعِ قابلِ پرداختِ سبد</summary>
        public string CartTotal => "۰ تومان";

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>تحویل نسخه</summary>
        public IAsyncRelayCommand DispenseCommand =>
            new AsyncRelayCommand(DispenseAsync);

        /// <summary>اجرای «تحویل نسخه»</summary>
        private async Task DispenseAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «تحویل نسخه»
            await RunAsync(_ => Task.CompletedTask, "تحویل نسخه…");
        }

        /// <summary>ورود کالا</summary>
        public IAsyncRelayCommand StockInCommand =>
            new AsyncRelayCommand(StockInAsync);

        /// <summary>اجرای «ورود کالا»</summary>
        private async Task StockInAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ورود کالا»
            await RunAsync(_ => Task.CompletedTask, "ورود کالا…");
        }

        /// <summary>انبارگردانی</summary>
        public IAsyncRelayCommand StocktakeCommand =>
            new AsyncRelayCommand(StocktakeAsync);

        /// <summary>اجرای «انبارگردانی»</summary>
        private async Task StocktakeAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «انبارگردانی»
            await RunAsync(_ => Task.CompletedTask, "انبارگردانی…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<PharmacyRow> SampleData()
        {
            yield return new PharmacyRow { Code = "D-۱۰۰۱", Name = "آموکسی‌سیلین ۵۰۰ کپسول", Generic = "Amoxicillin", Batch = "A-۲۲", Stock = 320, Price = 45000m, Expiry = "۱۴۰۵/۰۲/۱۵", Status = "کافی", StatusTone = "ok" };
            yield return new PharmacyRow { Code = "D-۱۰۰۲", Name = "امپرازول ۲۰ کپسول", Generic = "Omeprazole", Batch = "B-۱۴", Stock = 8, Price = 32000m, Expiry = "۱۴۰۴/۱۱/۰۸", Status = "کمبود", StatusTone = "dg" };
            yield return new PharmacyRow { Code = "D-۱۰۰۳", Name = "متفورمین ۵۰۰ قرص", Generic = "Metformin", Batch = "C-۳۱", Stock = 540, Price = 28000m, Expiry = "۱۴۰۵/۰۶/۲۰", Status = "کافی", StatusTone = "ok" };
            yield return new PharmacyRow { Code = "D-۱۰۰۴", Name = "آسپرین ۸۰ قرص", Generic = "Aspirin", Batch = "D-۰۷", Stock = 62, Price = 18000m, Expiry = "۱۴۰۴/۰۸/۰۱", Status = "نزدیک انقضا", StatusTone = "wn" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<PharmacyRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<PharmacyRow>(SampleData());
            return Task.FromResult<PagedResult<PharmacyRow>?>(
                new PagedResult<PharmacyRow>(items, items.Count));
        }
    }
}
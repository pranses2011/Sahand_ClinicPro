// ═══════════════════════════════════════════════════════════════════════
//  ConsumablesViewModel
//  صفحه‌ی «لوازم مصرفی» · گروه «ویزیت» · کهن‌الگوی «list-dense»
//
//  متناظر با:  03-views/ConsumablesView.xaml
//  وایرفریم:    01-wireframes-html/ConsumablesView.html
//  پایه:        PageViewModelBase<ConsumablesRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «لوازم مصرفی»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class ConsumablesRow
    {
        /// <summary>کد — text</summary>
        public string? Code { get; set; }

        /// <summary>نام قلم — text</summary>
        public string? Name { get; set; }

        /// <summary>واحد — text</summary>
        public string? Unit { get; set; }

        /// <summary>موجودی — num</summary>
        public int Stock { get; set; }

        /// <summary>حد سفارش — num</summary>
        public int MinStock { get; set; }

        /// <summary>قیمت — money</summary>
        public decimal Price { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «لوازم مصرفی»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-dense</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: ویزیت
    /// </remarks>
    public sealed class ConsumablesViewModel : PageViewModelBase<ConsumablesRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public ConsumablesViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("لوازم مصرفی", "ورود کالا", "تأمین موجودی"));
            Relations.Add(new RelationItem("لوازم مصرفی", "انبارگردانی", "شمارش دوره‌ای"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>فیلترِ «گروه»</summary>
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

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>قلم جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «قلم جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «قلم جدید»
            await RunAsync(_ => Task.CompletedTask, "قلم جدید…");
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

        /// <summary>خروجی</summary>
        public IAsyncRelayCommand ExportCommand =>
            new AsyncRelayCommand(ExportAsync);

        /// <summary>اجرای «خروجی»</summary>
        private async Task ExportAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «خروجی»
            await RunAsync(_ => Task.CompletedTask, "خروجی…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<ConsumablesRow> SampleData()
        {
            yield return new ConsumablesRow { Code = "K-۱۰۱", Name = "دستکش لاتکس (بسته ۱۰۰)", Unit = "بسته", Stock = 42, MinStock = 20, Price = 75000m, Status = "کافی", StatusTone = "ok" };
            yield return new ConsumablesRow { Code = "K-۱۰۲", Name = "سرنگ ۵ سی‌سی", Unit = "عدد", Stock = 11, MinStock = 50, Price = 3500m, Status = "کمبود", StatusTone = "dg" };
            yield return new ConsumablesRow { Code = "K-۱۰۳", Name = "پنبه استریل ۵۰۰ گرمی", Unit = "بسته", Stock = 28, MinStock = 15, Price = 45000m, Status = "کافی", StatusTone = "ok" };
            yield return new ConsumablesRow { Code = "K-۱۰۴", Name = "الکل ۷۰٪ یک لیتری", Unit = "بطری", Stock = 16, MinStock = 12, Price = 62000m, Status = "نزدیک حد", StatusTone = "wn" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<ConsumablesRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<ConsumablesRow>(SampleData());
            return Task.FromResult<PagedResult<ConsumablesRow>?>(
                new PagedResult<ConsumablesRow>(items, items.Count));
        }
    }
}
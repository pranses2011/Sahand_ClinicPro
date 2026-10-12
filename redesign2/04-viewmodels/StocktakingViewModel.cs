// ═══════════════════════════════════════════════════════════════════════
//  StocktakingViewModel
//  صفحه‌ی «انبارگردانی» · گروه «داروخانه» · کهن‌الگوی «list-dense»
//
//  متناظر با:  03-views/StocktakingView.xaml
//  وایرفریم:    01-wireframes-html/StocktakingView.html
//  پایه:        PageViewModelBase<StocktakingRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «انبارگردانی»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class StocktakingRow
    {
        /// <summary>کد — text</summary>
        public string? Code { get; set; }

        /// <summary>نام قلم — text</summary>
        public string? Name { get; set; }

        /// <summary>موجودی سیستم — num</summary>
        public int System { get; set; }

        /// <summary>شمارش‌شده — num</summary>
        public int Counted { get; set; }

        /// <summary>مغایرت — num</summary>
        public string? Diff { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «انبارگردانی»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-dense</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: داروخانه
    /// </remarks>
    public sealed class StocktakingViewModel : PageViewModelBase<StocktakingRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public StocktakingViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("انبارگردانی", "داروخانه", "موجودی اصلاح می‌شود"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>فیلترِ «انبار»</summary>
        public object? Filter1
        {
            get => _filter1;
            set
            {
                if (SetProperty(ref _filter1, value))
                {
                    PageNumber = 1;
                    _ = LoadAsync();
                }
            }
        }
        private object? _filter1 = "اصلی";

        /// <summary>فیلترِ «تاریخ شمارش»</summary>
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
        private object? _filter2 = "۱۴۰۴/۰۷/۱۹";

        /// <summary>فیلترِ «فقط مغایرت‌دار»</summary>
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

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

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
        private static IEnumerable<StocktakingRow> SampleData()
        {
            yield return new StocktakingRow { Code = "D-۱۰۰۱", Name = "آموکسی‌سیلین ۵۰۰ کپسول", System = 320, Counted = 320, Diff = "۰", Status = "تطابق", StatusTone = "ok" };
            yield return new StocktakingRow { Code = "D-۱۰۰۲", Name = "امپرازول ۲۰ کپسول", System = 12, Counted = 8, Diff = "−۴", Status = "مغایرت", StatusTone = "dg" };
            yield return new StocktakingRow { Code = "D-۱۰۰۳", Name = "متفورمین ۵۰۰ قرص", System = 540, Counted = 540, Diff = "۰", Status = "تطابق", StatusTone = "ok" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<StocktakingRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<StocktakingRow>(SampleData());
            return Task.FromResult<PagedResult<StocktakingRow>?>(
                new PagedResult<StocktakingRow>(items, items.Count));
        }
    }
}
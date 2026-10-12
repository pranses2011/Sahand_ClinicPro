// ═══════════════════════════════════════════════════════════════════════
//  ReportsViewModel
//  صفحه‌ی «گزارشات» · گروه «گزارشات» · کهن‌الگوی «settings»
//
//  متناظر با:  03-views/ReportsView.xaml
//  وایرفریم:    01-wireframes-html/ReportsView.html
//  پایه:        PageViewModelBase<ReportsRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «گزارشات»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class ReportsRow
    {
        /// <summary>گزارش — text</summary>
        public string? Title { get; set; }

        /// <summary>دسته — text</summary>
        public string? Category { get; set; }

        /// <summary>آخرین اجرا — date</summary>
        public string? LastRun { get; set; }

        /// <summary>قالب خروجی — text</summary>
        public string? Format { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «گزارشات»
    /// </summary>
    /// <remarks>
    /// کتابخانه با دسته‌بندیِ سمت راست؛ هر گزارش کارت خود را با آیکون،
    /// عنوان، توضیح و زمان آخرین اجرا دارد.
    ///
    /// کهن‌الگوی طراحی: <c>settings</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: گزارشات
    /// </remarks>
    public sealed class ReportsViewModel : PageViewModelBase<ReportsRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public ReportsViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("گزارشات", "مرورگر گزارشات", "مشاهده خروجی"));
            Relations.Add(new RelationItem("گزارشات", "گزارش‌ساز پویا", "ساخت گزارش جدید"));

            // بخش‌های تنظیمات

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>فیلترِ «دسته»</summary>
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

        /// <summary>بخش‌های تنظیمات</summary>
        public ObservableCollection<string> Sections { get; } = new();

        /// <summary>بخشِ انتخاب‌شده</summary>
        public string? SelectedSection
        {
            get => _selectedSection;
            set => SetProperty(ref _selectedSection, value);
        }
        private string? _selectedSection;

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>اجرای گزارش</summary>
        public IAsyncRelayCommand RunCommand =>
            new AsyncRelayCommand(RunAsync);

        /// <summary>اجرای «اجرای گزارش»</summary>
        private async Task RunAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «اجرای گزارش»
            await RunAsync(_ => Task.CompletedTask, "اجرای گزارش…");
        }

        /// <summary>زمان‌بندی</summary>
        public IAsyncRelayCommand ScheduleCommand =>
            new AsyncRelayCommand(ScheduleAsync);

        /// <summary>اجرای «زمان‌بندی»</summary>
        private async Task ScheduleAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «زمان‌بندی»
            await RunAsync(_ => Task.CompletedTask, "زمان‌بندی…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<ReportsRow> SampleData()
        {
            yield return new ReportsRow { Title = "گزارش درآمد روزانه", Category = "مالی", LastRun = "۱۴۰۴/۰۷/۱۹ ۰۸:۰۰", Format = "PDF · Excel", Status = "آماده", StatusTone = "ok" };
            yield return new ReportsRow { Title = "گزارش ویزیت‌های پزشک", Category = "درمانی", LastRun = "۱۴۰۴/۰۷/۱۹ ۰۸:۰۰", Format = "PDF · Excel", Status = "آماده", StatusTone = "ok" };
            yield return new ReportsRow { Title = "گزارش موجودی داروخانه", Category = "عملیاتی", LastRun = "۱۴۰۴/۰۷/۱۸ ۲۳:۰۰", Format = "Excel", Status = "آماده", StatusTone = "ok" };
            yield return new ReportsRow { Title = "گزارش مطالبات بیمه", Category = "مالی", LastRun = "۱۴۰۴/۰۷/۱۸ ۲۳:۰۰", Format = "PDF · Excel", Status = "آماده", StatusTone = "ok" };
            yield return new ReportsRow { Title = "گزارش حضور و غیاب", Category = "پرسنلی", LastRun = "۱۴۰۴/۰۷/۱۹ ۰۸:۰۰", Format = "Excel", Status = "در حال اجرا", StatusTone = "in" };
            yield return new ReportsRow { Title = "گزارش عملکرد پزشکان", Category = "درمانی", LastRun = "۱۴۰۴/۰۷/۱۷ ۲۳:۰۰", Format = "PDF", Status = "آماده", StatusTone = "ok" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<ReportsRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<ReportsRow>(SampleData());
            return Task.FromResult<PagedResult<ReportsRow>?>(
                new PagedResult<ReportsRow>(items, items.Count));
        }
    }
}
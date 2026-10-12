// ═══════════════════════════════════════════════════════════════════════
//  DentistryViewModel
//  صفحه‌ی «دندانپزشکی» · گروه «ویزیت» · کهن‌الگوی «detail-tabs»
//
//  متناظر با:  03-views/DentistryView.xaml
//  وایرفریم:    01-wireframes-html/DentistryView.html
//  پایه:        PageViewModelBase<DentistryRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «دندانپزشکی»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class DentistryRow
    {
        /// <summary>دندان — num</summary>
        public int Tooth { get; set; }

        /// <summary>وضعیت — text</summary>
        public string? Condition { get; set; }

        /// <summary>خدمت انجام‌شده — text</summary>
        public string? Service { get; set; }

        /// <summary>تاریخ — date</summary>
        public string? Date { get; set; }

        /// <summary>وضعیت درمان — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت درمان»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «دندانپزشکی»
    /// </summary>
    /// <remarks>
    /// چارت دندانی با ۳۲ دندانِ تعاملی؛ هر دندان رنگِ وضعیت خود را دارد
    /// (سالم/پوسیده/پرکرده/کشیده‌شده).
    ///
    /// کهن‌الگوی طراحی: <c>detail-tabs</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: ویزیت
    /// </remarks>
    public sealed class DentistryViewModel : PageViewModelBase<DentistryRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public DentistryViewModel(ILogger? logger = null) : base(logger)
        {
            // تب‌ها
            Tabs.Add(new TabItemModel("چارت دندان", Application.Current.TryFindResource("Icon.Grid3x3") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «چارت دندان»
            Tabs.Add(new TabItemModel("خدمات", Application.Current.TryFindResource("Icon.Briefcase") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «خدمات»
            Tabs.Add(new TabItemModel("تصاویر", Application.Current.TryFindResource("Icon.Image") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «تصاویر»
            Tabs.Add(new TabItemModel("طرح درمان", Application.Current.TryFindResource("Icon.ClipboardList") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «طرح درمان»

            // ارتباطات
            Relations.Add(new RelationItem("دندانپزشکی", "ویزیت (SOAP)", "بخشی از ویزیت تخصصی"));
            Relations.Add(new RelationItem("دندانپزشکی", "صورتحساب", "خدمات فاکتور می‌شود"));

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
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>چاپ چارت</summary>
        public IAsyncRelayCommand PrintCommand =>
            new AsyncRelayCommand(PrintAsync);

        /// <summary>اجرای «چاپ چارت»</summary>
        private async Task PrintAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «چاپ چارت»
            await RunAsync(_ => Task.CompletedTask, "چاپ چارت…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<DentistryRow> SampleData()
        {
            yield return new DentistryRow { Tooth = 16, Condition = "پوسیدگی سطحی", Service = "ترمیم کامپوزیت", Date = "۱۴۰۴/۰۷/۱۸", Status = "انجام شد", StatusTone = "ok" };
            yield return new DentistryRow { Tooth = 26, Condition = "پوسیدگی عمیق", Service = "عصب‌کشی", Date = "۱۴۰۴/۰۷/۱۸", Status = "در حال درمان", StatusTone = "wn" };
            yield return new DentistryRow { Tooth = 36, Condition = "کشیده‌شده", Service = "—", Date = "۱۴۰۳/۱۱/۰۵", Status = "نیاز به ایمپلنت", StatusTone = "in" };
            yield return new DentistryRow { Tooth = 46, Condition = "سالم", Service = "—", Date = "—", Status = "سالم", StatusTone = "ok" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<DentistryRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<DentistryRow>(SampleData());
            return Task.FromResult<PagedResult<DentistryRow>?>(
                new PagedResult<DentistryRow>(items, items.Count));
        }
    }
}
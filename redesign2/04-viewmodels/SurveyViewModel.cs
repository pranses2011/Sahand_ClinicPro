// ═══════════════════════════════════════════════════════════════════════
//  SurveyViewModel
//  صفحه‌ی «نظرسنجی» · گروه «ارتباطات» · کهن‌الگوی «detail-tabs»
//
//  متناظر با:  03-views/SurveyView.xaml
//  وایرفریم:    01-wireframes-html/SurveyView.html
//  پایه:        PageViewModelBase<SurveyRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «نظرسنجی»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class SurveyRow
    {
        /// <summary>پرسش — text</summary>
        public string? Question { get; set; }

        /// <summary>نوع — text</summary>
        public string? Type { get; set; }

        /// <summary>پاسخ‌ها — num</summary>
        public int Answers { get; set; }

        /// <summary>میانگین — num</summary>
        public string? Avg { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «نظرسنجی»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>detail-tabs</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: ارتباطات
    /// </remarks>
    public sealed class SurveyViewModel : PageViewModelBase<SurveyRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public SurveyViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("میانگین امتیاز", "۴٫۵", 0.2, Application.Current.TryFindResource("Icon.Star") as Geometry, Application.Current.TryFindResource("Brush.Warning") as Brush));
            Kpis.Add(new KpiItem("پاسخ‌دهندگان", "۳۱۸", 44, Application.Current.TryFindResource("Icon.Users") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("نظرات متنی", "۹۲", null, Application.Current.TryFindResource("Icon.MessageSquare") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));

            // تب‌ها
            Tabs.Add(new TabItemModel("پرسش‌ها", Application.Current.TryFindResource("Icon.ListChecks") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «پرسش‌ها»
            Tabs.Add(new TabItemModel("پاسخ‌ها", Application.Current.TryFindResource("Icon.MessageSquare") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «پاسخ‌ها»
            Tabs.Add(new TabItemModel("تحلیل", Application.Current.TryFindResource("Icon.ChartBar") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «تحلیل»
            Tabs.Add(new TabItemModel("تنظیمات", Application.Current.TryFindResource("Icon.Settings") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «تنظیمات»

            // ارتباطات
            Relations.Add(new RelationItem("نظرسنجی", "پیامک", "ارسال لینک نظرسنجی"));
            Relations.Add(new RelationItem("نظرسنجی", "عملکرد پزشکان", "نمره رضایت"));

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

        /// <summary>پرسش جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «پرسش جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «پرسش جدید»
            await RunAsync(_ => Task.CompletedTask, "پرسش جدید…");
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
        private static IEnumerable<SurveyRow> SampleData()
        {
            yield return new SurveyRow { Question = "میزان رضایت از برخورد پرسنل پذیرش", Type = "امتیاز ۱ تا ۵", Answers = 318, Avg = "۴٫۷", Status = "فعال", StatusTone = "ok" };
            yield return new SurveyRow { Question = "زمان انتظار تا ویزیت", Type = "امتیاز ۱ تا ۵", Answers = 318, Avg = "۳٫۹", Status = "فعال", StatusTone = "ok" };
            yield return new SurveyRow { Question = "وضوح توضیحات پزشک", Type = "امتیاز ۱ تا ۵", Answers = 316, Avg = "۴٫۵", Status = "فعال", StatusTone = "ok" };
            yield return new SurveyRow { Question = "پیشنهاد یا انتقاد شما", Type = "متن آزاد", Answers = 92, Avg = "—", Status = "فعال", StatusTone = "ok" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<SurveyRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<SurveyRow>(SampleData());
            return Task.FromResult<PagedResult<SurveyRow>?>(
                new PagedResult<SurveyRow>(items, items.Count));
        }
    }
}
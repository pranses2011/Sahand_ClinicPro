// ═══════════════════════════════════════════════════════════════════════
//  PhysiotherapyViewModel
//  صفحه‌ی «فیزیوتراپی» · گروه «ویزیت» · کهن‌الگوی «detail-tabs»
//
//  متناظر با:  03-views/PhysiotherapyView.xaml
//  وایرفریم:    01-wireframes-html/PhysiotherapyView.html
//  پایه:        PageViewModelBase<PhysiotherapyRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «فیزیوتراپی»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class PhysiotherapyRow
    {
        /// <summary>جلسه — num</summary>
        public int Session { get; set; }

        /// <summary>تاریخ — date</summary>
        public string? Date { get; set; }

        /// <summary>نمره درد — num</summary>
        public int Pain { get; set; }

        /// <summary>برنامه تمرینی — text</summary>
        public string? Exercise { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «فیزیوتراپی»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>detail-tabs</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: ویزیت
    /// </remarks>
    public sealed class PhysiotherapyViewModel : PageViewModelBase<PhysiotherapyRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public PhysiotherapyViewModel(ILogger? logger = null) : base(logger)
        {
            // تب‌ها
            Tabs.Add(new TabItemModel("جلسات", Application.Current.TryFindResource("Icon.CalendarDays") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «جلسات»
            Tabs.Add(new TabItemModel("ارزیابی درد", Application.Current.TryFindResource("Icon.Activity") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «ارزیابی درد»
            Tabs.Add(new TabItemModel("برنامه تمرینی", Application.Current.TryFindResource("Icon.ListChecks") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «برنامه تمرینی»

            // ارتباطات
            Relations.Add(new RelationItem("فیزیوتراپی", "ویزیت (SOAP)", "ویزیت تخصصی"));

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

        /// <summary>نمره درد (۰ تا ۱۰)</summary>
        public int PainLevel
        {
            get => _painLevel;
            set
            {
                if (SetProperty(ref _painLevel, value))
                {
                }
            }
        }
        private int _painLevel = 4;

        /// <summary>محل درد</summary>
        public string PainLocation
        {
            get => _painLocation;
            set
            {
                if (SetProperty(ref _painLocation, value))
                {
                }
            }
        }
        private string _painLocation = "کمر";

        /// <summary>تعداد جلسات</summary>
        public int Sessions
        {
            get => _sessions;
            set
            {
                if (SetProperty(ref _sessions, value))
                {
                }
            }
        }
        private int _sessions = 12;

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>جلسه جدید</summary>
        public IAsyncRelayCommand NewSessionCommand =>
            new AsyncRelayCommand(NewSessionAsync);

        /// <summary>اجرای «جلسه جدید»</summary>
        private async Task NewSessionAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «جلسه جدید»
            await RunAsync(_ => Task.CompletedTask, "جلسه جدید…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<PhysiotherapyRow> SampleData()
        {
            yield return new PhysiotherapyRow { Session = 1, Date = "۱۴۰۴/۰۷/۱۲", Pain = 7, Exercise = "کشش کمر · ۳ نوبت ۱۵ تایی", Status = "انجام شد", StatusTone = "ok" };
            yield return new PhysiotherapyRow { Session = 2, Date = "۱۴۰۴/۰۷/۱۴", Pain = 6, Exercise = "تقویت عضلات مرکزی", Status = "انجام شد", StatusTone = "ok" };
            yield return new PhysiotherapyRow { Session = 3, Date = "۱۴۰۴/۰۷/۱۶", Pain = 5, Exercise = "الکتروتراپی · گرمادرمانی", Status = "انجام شد", StatusTone = "ok" };
            yield return new PhysiotherapyRow { Session = 4, Date = "۱۴۰۴/۰۷/۱۸", Pain = 4, Exercise = "کشش + تقویت پیشرفته", Status = "برنامه‌ریزی شده", StatusTone = "in" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<PhysiotherapyRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<PhysiotherapyRow>(SampleData());
            return Task.FromResult<PagedResult<PhysiotherapyRow>?>(
                new PagedResult<PhysiotherapyRow>(items, items.Count));
        }
    }
}
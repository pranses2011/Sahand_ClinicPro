// ═══════════════════════════════════════════════════════════════════════
//  AppointmentsViewModel
//  صفحه‌ی «نوبت‌دهی» · گروه «نوبت‌دهی» · کهن‌الگوی «kanban»
//
//  متناظر با:  03-views/AppointmentsView.xaml
//  وایرفریم:    01-wireframes-html/AppointmentsView.html
//  پایه:        PageViewModelBase<AppointmentsRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «نوبت‌دهی»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class AppointmentsRow
    {
        /// <summary>بیمار — who</summary>
        public string? Patient { get; set; }
        /// <summary>زیرنویسِ «بیمار» (مانند کد یا سن)</summary>
        public string? PatientSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «بیمار»</summary>
        public string? PatientInitials { get; set; }
        /// <summary>رنگِ نشانِ «بیمار» — از تم خوانده می‌شود</summary>
        public Brush? PatientAvatar { get; set; }

        /// <summary>ساعت — date</summary>
        public string? Time { get; set; }

        /// <summary>پزشک — text</summary>
        public string? Doctor { get; set; }

        /// <summary>نوع ویزیت — text</summary>
        public string? Type { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «نوبت‌دهی»
    /// </summary>
    /// <remarks>
    /// چهار ستون کانبان: در انتظار · تأیید شده · حاضر شده · لغو شده. هر
    /// کارت شامل نام بیمار، ساعت، پزشک، نوع ویزیت و نشان وضعیت.
    ///
    /// کهن‌الگوی طراحی: <c>kanban</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: نوبت‌دهی
    /// </remarks>
    public sealed class AppointmentsViewModel : PageViewModelBase<AppointmentsRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public AppointmentsViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("نوبت‌های امروز", "۳۶", 8, Application.Current.TryFindResource("Icon.CalendarCheck") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("تأیید شده", "۲۴", null, Application.Current.TryFindResource("Icon.CircleCheck") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("در انتظار", "۸", null, Application.Current.TryFindResource("Icon.Clock") as Geometry, Application.Current.TryFindResource("Brush.Warning") as Brush));
            Kpis.Add(new KpiItem("لغو شده", "۴", -1, Application.Current.TryFindResource("Icon.CircleX") as Geometry, Application.Current.TryFindResource("Brush.Danger") as Brush));

            // ارتباطات
            Relations.Add(new RelationItem("نوبت‌دهی", "بیماران", "نوبت برای بیمار"));
            Relations.Add(new RelationItem("نوبت‌دهی", "پذیرش و صف", "تأیید نوبت → پذیرش"));
            Relations.Add(new RelationItem("نوبت‌دهی", "پزشکان", "ظرفیت برنامه هفتگی"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

        /// <summary>فیلترِ «تاریخ»</summary>
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
        private object? _filter1 = "امروز ۱۴۰۴/۰۷/۱۹";

        /// <summary>فیلترِ «پزشک»</summary>
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
        private object? _filter2 = "همه پزشکان";

        /// <summary>فیلترِ «نما»</summary>
        public object? Filter4
        {
            get => _filter4;
            set
            {
                if (SetProperty(ref _filter4, value))
                {
                    PageNumber = 1;
                    _ = LoadAsync();
                }
            }
        }
        private object? _filter4 = "کانبان";

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        /// <summary>ستون‌های کانبان</summary>
        public ObservableCollection<KanbanColumn> Columns { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>نوبت جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «نوبت جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «نوبت جدید»
            await RunAsync(_ => Task.CompletedTask, "نوبت جدید…");
        }

        /// <summary>تأیید</summary>
        public IAsyncRelayCommand ConfirmCommand =>
            new AsyncRelayCommand(ConfirmAsync);

        /// <summary>اجرای «تأیید»</summary>
        private async Task ConfirmAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «تأیید»
            await RunAsync(_ => Task.CompletedTask, "تأیید…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<AppointmentsRow> SampleData()
        {
            yield return new AppointmentsRow { Patient = "سارا احمدی", PatientSub = "P-۱۰۲۴", PatientInitials = "سا", PatientAvatar = Application.Current.TryFindResource("Brush.Info") as Brush, Time = "۱۰:۳۰", Doctor = "دکتر موسوی", Type = "ویزیت عمومی", Status = "در انتظار", StatusTone = "wn" };
            yield return new AppointmentsRow { Patient = "محمد رضایی", PatientSub = "P-۱۰۲۵", PatientInitials = "مر", PatientAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Time = "۱۰:۵۰", Doctor = "دکتر رضایی", Type = "ویزیت تخصصی", Status = "در انتظار", StatusTone = "wn" };
            yield return new AppointmentsRow { Patient = "فاطمه کریمی", PatientSub = "P-۱۰۲۶", PatientInitials = "فک", PatientAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Time = "۱۱:۱۰", Doctor = "دکتر موسوی", Type = "پیگیری", Status = "در انتظار", StatusTone = "wn" };
            yield return new AppointmentsRow { Patient = "علی حسینی", PatientSub = "P-۱۰۲۷", PatientInitials = "اح", PatientAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Time = "۰۹:۰۰", Doctor = "دکتر کریمی", Type = "ویزیت عمومی", Status = "تأیید شده", StatusTone = "ok" };
            yield return new AppointmentsRow { Patient = "زهرا موسوی", PatientSub = "P-۱۰۲۸", PatientInitials = "زم", PatientAvatar = Application.Current.TryFindResource("Brush.Warning") as Brush, Time = "۰۹:۲۰", Doctor = "دکتر رضایی", Type = "معاینه دوره‌ای", Status = "تأیید شده", StatusTone = "ok" };
            yield return new AppointmentsRow { Patient = "رضا قاسمی", PatientSub = "P-۱۰۲۹", PatientInitials = "رق", PatientAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Time = "۰۹:۴۰", Doctor = "دکتر موسوی", Type = "ویزیت عمومی", Status = "حاضر شده", StatusTone = "in" };
            yield return new AppointmentsRow { Patient = "مریم نوری", PatientSub = "P-۱۰۳۰", PatientInitials = "من", PatientAvatar = Application.Current.TryFindResource("Brush.Info") as Brush, Time = "۱۰:۰۰", Doctor = "دکتر کریمی", Type = "اورژانس", Status = "حاضر شده", StatusTone = "in" };
            yield return new AppointmentsRow { Patient = "حسن کاظمی", PatientSub = "P-۱۰۳۱", PatientInitials = "حک", PatientAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Time = "۰۸:۳۰", Doctor = "دکتر رضایی", Type = "ویزیت تخصصی", Status = "لغو شده", StatusTone = "dg" };
            yield return new AppointmentsRow { Patient = "نرگس صمدی", PatientSub = "P-۱۰۳۲", PatientInitials = "نس", PatientAvatar = Application.Current.TryFindResource("Brush.Warning") as Brush, Time = "۰۸:۱۰", Doctor = "دکتر موسوی", Type = "پیگیری", Status = "لغو شده", StatusTone = "dg" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<AppointmentsRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<AppointmentsRow>(SampleData());
            return Task.FromResult<PagedResult<AppointmentsRow>?>(
                new PagedResult<AppointmentsRow>(items, items.Count));
        }
    }
}
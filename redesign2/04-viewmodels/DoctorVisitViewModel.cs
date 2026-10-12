// ═══════════════════════════════════════════════════════════════════════
//  DoctorVisitViewModel
//  صفحه‌ی «ویزیت پزشک» · گروه «نوبت‌دهی» · کهن‌الگوی «split»
//
//  متناظر با:  03-views/DoctorVisitView.xaml
//  وایرفریم:    01-wireframes-html/DoctorVisitView.html
//  پایه:        PageViewModelBase<DoctorVisitRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «ویزیت پزشک»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class DoctorVisitRow
    {
        /// <summary>نوبت — num</summary>
        public int Queue { get; set; }

        /// <summary>بیمار — who</summary>
        public string? Patient { get; set; }
        /// <summary>زیرنویسِ «بیمار» (مانند کد یا سن)</summary>
        public string? PatientSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «بیمار»</summary>
        public string? PatientInitials { get; set; }
        /// <summary>رنگِ نشانِ «بیمار» — از تم خوانده می‌شود</summary>
        public Brush? PatientAvatar { get; set; }

        /// <summary>خدمت — text</summary>
        public string? Service { get; set; }

        /// <summary>ورود — date</summary>
        public string? Time { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «ویزیت پزشک»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>split</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: نوبت‌دهی
    /// </remarks>
    public sealed class DoctorVisitViewModel : PageViewModelBase<DoctorVisitRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public DoctorVisitViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("در صف", "۷", null, Application.Current.TryFindResource("Icon.Users") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("ویزیت‌شده امروز", "۱۹", 12, Application.Current.TryFindResource("Icon.CircleCheck") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("میانگین ویزیت", "۱۶ دقیقه", -4, Application.Current.TryFindResource("Icon.Timer") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));

            // ارتباطات
            Relations.Add(new RelationItem("ویزیت پزشک", "پذیرش و صف", "بیمار از صف می‌آید"));
            Relations.Add(new RelationItem("ویزیت پزشک", "ویزیت (SOAP)", "فرم ویزیت باز می‌شود"));
            Relations.Add(new RelationItem("ویزیت پزشک", "فرم نسخه", "پس از ویزیت"));

            // خلاصه‌ی کناری
            Summary.Add(new KeyValueItem("بیمار", "سارا احمدی"));
            Summary.Add(new KeyValueItem("سن", "۴۲ سال"));
            Summary.Add(new KeyValueItem("بیمه", "تأمین اجتماعی"));
            Summary.Add(new KeyValueItem("حساسیت", "پنی‌سیلین"));
            Summary.Add(new KeyValueItem("آخرین ویزیت", "۱۴۰۴/۰۵/۱۲"));
            Summary.Add(new KeyValueItem("تعداد ویزیت", "۱۸"));
            Summary.Add(new KeyValueItem("فشار خون", "۱۲۰/۸۰"));
            Summary.Add(new KeyValueItem("وزن", "۶۵ کیلوگرم"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        /// <summary>خلاصه‌ی کناریِ صفحه</summary>
        public ObservableCollection<KeyValueItem> Summary { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>شروع ویزیت</summary>
        public IAsyncRelayCommand StartVisitCommand =>
            new AsyncRelayCommand(StartVisitAsync);

        /// <summary>اجرای «شروع ویزیت»</summary>
        private async Task StartVisitAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «شروع ویزیت»
            await RunAsync(_ => Task.CompletedTask, "شروع ویزیت…");
        }

        /// <summary>فراخوانی بعدی</summary>
        public IAsyncRelayCommand CallNextCommand =>
            new AsyncRelayCommand(CallNextAsync);

        /// <summary>اجرای «فراخوانی بعدی»</summary>
        private async Task CallNextAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «فراخوانی بعدی»
            await RunAsync(_ => Task.CompletedTask, "فراخوانی بعدی…");
        }

        /// <summary>پایان ویزیت</summary>
        public IAsyncRelayCommand CompleteCommand =>
            new AsyncRelayCommand(CompleteAsync);

        /// <summary>اجرای «پایان ویزیت»</summary>
        private async Task CompleteAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «پایان ویزیت»
            await RunAsync(_ => Task.CompletedTask, "پایان ویزیت…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<DoctorVisitRow> SampleData()
        {
            yield return new DoctorVisitRow { Queue = 12, Patient = "سارا احمدی", PatientSub = "P-۱۰۲۴ · ۴۲ سال", PatientInitials = "سا", PatientAvatar = Application.Current.TryFindResource("Brush.Info") as Brush, Service = "ویزیت عمومی", Time = "۰۹:۱۲", Status = "در انتظار", StatusTone = "wn" };
            yield return new DoctorVisitRow { Queue = 13, Patient = "محمد رضایی", PatientSub = "P-۱۰۲۵ · ۳۵ سال", PatientInitials = "مر", PatientAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Service = "ویزیت تخصصی", Time = "۰۹:۲۵", Status = "در انتظار", StatusTone = "wn" };
            yield return new DoctorVisitRow { Queue = 14, Patient = "فاطمه کریمی", PatientSub = "P-۱۰۲۶ · ۲۸ سال", PatientInitials = "فک", PatientAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Service = "پیگیری", Time = "۰۹:۳۱", Status = "در انتظار", StatusTone = "wn" };
            yield return new DoctorVisitRow { Queue = 15, Patient = "علی حسینی", PatientSub = "P-۱۰۲۷ · ۶۱ سال", PatientInitials = "اح", PatientAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Service = "ویزیت عمومی", Time = "۰۹:۴۰", Status = "در انتظار", StatusTone = "wn" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<DoctorVisitRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<DoctorVisitRow>(SampleData());
            return Task.FromResult<PagedResult<DoctorVisitRow>?>(
                new PagedResult<DoctorVisitRow>(items, items.Count));
        }
    }
}
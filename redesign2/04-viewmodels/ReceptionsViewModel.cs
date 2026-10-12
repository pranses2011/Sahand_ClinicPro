// ═══════════════════════════════════════════════════════════════════════
//  ReceptionsViewModel
//  صفحه‌ی «پذیرش و صف» · گروه «پذیرش» · کهن‌الگوی «list-detail»
//
//  متناظر با:  03-views/ReceptionsView.xaml
//  وایرفریم:    01-wireframes-html/ReceptionsView.html
//  پایه:        PageViewModelBase<ReceptionsRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «پذیرش و صف»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class ReceptionsRow
    {
        /// <summary>نوبت — num</summary>
        public int QueueNo { get; set; }

        /// <summary>بیمار — who</summary>
        public string? Patient { get; set; }
        /// <summary>زیرنویسِ «بیمار» (مانند کد یا سن)</summary>
        public string? PatientSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «بیمار»</summary>
        public string? PatientInitials { get; set; }
        /// <summary>رنگِ نشانِ «بیمار» — از تم خوانده می‌شود</summary>
        public Brush? PatientAvatar { get; set; }

        /// <summary>پزشک — text</summary>
        public string? Doctor { get; set; }

        /// <summary>خدمت — text</summary>
        public string? Service { get; set; }

        /// <summary>بیمه — text</summary>
        public string? Insurance { get; set; }

        /// <summary>ورود — date</summary>
        public string? TimeIn { get; set; }

        /// <summary>انتظار — num</summary>
        public string? Wait { get; set; }

        /// <summary>مبلغ — money</summary>
        public decimal Amount { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «پذیرش و صف»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-detail</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: پذیرش
    /// </remarks>
    public sealed class ReceptionsViewModel : PageViewModelBase<ReceptionsRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public ReceptionsViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("در صف", "۱۲", null, Application.Current.TryFindResource("Icon.Users") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("میانگین انتظار", "۱۴ دقیقه", -6, Application.Current.TryFindResource("Icon.Clock") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));
            Kpis.Add(new KpiItem("پذیرش امروز", "۴۸", 12, Application.Current.TryFindResource("Icon.CircleCheck") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("دریافتی امروز", "۱۴,۲۰۰,۰۰۰", 19, Application.Current.TryFindResource("Icon.HandCoins") as Geometry, Application.Current.TryFindResource("Brush.Warning") as Brush));

            // ارتباطات
            Relations.Add(new RelationItem("پذیرش و صف", "بیماران", "بیمار انتخاب می‌شود"));
            Relations.Add(new RelationItem("پذیرش و صف", "نوبت‌دهی", "از نوبت تأییدشده"));
            Relations.Add(new RelationItem("پذیرش و صف", "ویزیت (SOAP)", "پس از فراخوانی"));
            Relations.Add(new RelationItem("پذیرش و صف", "صورتحساب", "فاکتور صادر می‌شود"));
            Relations.Add(new RelationItem("پذیرش و صف", "شیفت‌ها", "تحویل در پایان شیفت"));

            // خلاصه‌ی کناری
            Summary.Add(new KeyValueItem("شماره در صف", "۱۲"));
            Summary.Add(new KeyValueItem("بیمار", "سارا احمدی"));
            Summary.Add(new KeyValueItem("پزشک", "دکتر موسوی"));
            Summary.Add(new KeyValueItem("خدمت", "ویزیت عمومی"));
            Summary.Add(new KeyValueItem("بیمه", "تأمین اجتماعی"));
            Summary.Add(new KeyValueItem("سهم بیمه", "۱۷۵,۰۰۰"));
            Summary.Add(new KeyValueItem("سهم بیمار", "۷۵,۰۰۰"));
            Summary.Add(new KeyValueItem("زمان انتظار", "۱۸ دقیقه"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

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

        /// <summary>فیلترِ «وضعیت»</summary>
        public object? Filter3
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
        private object? _filter3 = "همه";

        /// <summary>فیلترِ «تاریخ»</summary>
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
        private object? _filter4 = "امروز";

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        /// <summary>خلاصه‌ی کناریِ صفحه</summary>
        public ObservableCollection<KeyValueItem> Summary { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>پذیرش جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «پذیرش جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «پذیرش جدید»
            await RunAsync(_ => Task.CompletedTask, "پذیرش جدید…");
        }

        /// <summary>دریافت وجه</summary>
        public IAsyncRelayCommand PayCommand =>
            new AsyncRelayCommand(PayAsync);

        /// <summary>اجرای «دریافت وجه»</summary>
        private async Task PayAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «دریافت وجه»
            await RunAsync(_ => Task.CompletedTask, "دریافت وجه…");
        }

        /// <summary>فراخوانی</summary>
        public IAsyncRelayCommand CallCommand =>
            new AsyncRelayCommand(CallAsync);

        /// <summary>اجرای «فراخوانی»</summary>
        private async Task CallAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «فراخوانی»
            await RunAsync(_ => Task.CompletedTask, "فراخوانی…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<ReceptionsRow> SampleData()
        {
            yield return new ReceptionsRow { QueueNo = 12, Patient = "سارا احمدی", PatientSub = "P-۱۰۲۴", PatientInitials = "سا", PatientAvatar = Application.Current.TryFindResource("Brush.Info") as Brush, Doctor = "دکتر موسوی", Service = "ویزیت عمومی", Insurance = "تأمین اجتماعی", TimeIn = "۰۹:۱۲", Wait = "۱۸ دقیقه", Amount = 250000m, Status = "در انتظار", StatusTone = "wn" };
            yield return new ReceptionsRow { QueueNo = 13, Patient = "محمد رضایی", PatientSub = "P-۱۰۲۵", PatientInitials = "مر", PatientAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Doctor = "دکتر رضایی", Service = "ویزیت تخصصی", Insurance = "سلامت", TimeIn = "۰۹:۲۵", Wait = "۵ دقیقه", Amount = 400000m, Status = "در حال ویزیت", StatusTone = "in" };
            yield return new ReceptionsRow { QueueNo = 14, Patient = "فاطمه کریمی", PatientSub = "P-۱۰۲۶", PatientInitials = "فک", PatientAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Doctor = "دکتر موسوی", Service = "پیگیری", Insurance = "آزاد", TimeIn = "۰۹:۳۱", Wait = "۲ دقیقه", Amount = 150000m, Status = "در انتظار", StatusTone = "wn" };
            yield return new ReceptionsRow { QueueNo = 11, Patient = "علی حسینی", PatientSub = "P-۱۰۲۷", PatientInitials = "اح", PatientAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Doctor = "دکتر کریمی", Service = "ویزیت عمومی", Insurance = "تأمین اجتماعی", TimeIn = "۰۸:۵۰", Wait = "۰", Amount = 250000m, Status = "اتمام", StatusTone = "ok" };
            yield return new ReceptionsRow { QueueNo = 10, Patient = "زهرا موسوی", PatientSub = "P-۱۰۲۸", PatientInitials = "زم", PatientAvatar = Application.Current.TryFindResource("Brush.Warning") as Brush, Doctor = "دکتر رضایی", Service = "معاینه دوره‌ای", Insurance = "آزاد", TimeIn = "۰۸:۳۵", Wait = "۰", Amount = 0m, Status = "لغو", StatusTone = "dg" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<ReceptionsRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<ReceptionsRow>(SampleData());
            return Task.FromResult<PagedResult<ReceptionsRow>?>(
                new PagedResult<ReceptionsRow>(items, items.Count));
        }
    }
}
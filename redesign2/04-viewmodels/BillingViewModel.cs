// ═══════════════════════════════════════════════════════════════════════
//  BillingViewModel
//  صفحه‌ی «صورتحساب» · گروه «مالی» · کهن‌الگوی «list-dense»
//
//  متناظر با:  03-views/BillingView.xaml
//  وایرفریم:    01-wireframes-html/BillingView.html
//  پایه:        PageViewModelBase<BillingRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «صورتحساب»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class BillingRow
    {
        /// <summary>شماره — text</summary>
        public string? No { get; set; }

        /// <summary>بیمار — who</summary>
        public string? Patient { get; set; }
        /// <summary>زیرنویسِ «بیمار» (مانند کد یا سن)</summary>
        public string? PatientSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «بیمار»</summary>
        public string? PatientInitials { get; set; }
        /// <summary>رنگِ نشانِ «بیمار» — از تم خوانده می‌شود</summary>
        public Brush? PatientAvatar { get; set; }

        /// <summary>تاریخ — date</summary>
        public string? Date { get; set; }

        /// <summary>مبلغ کل — money</summary>
        public decimal Total { get; set; }

        /// <summary>سهم بیمه — money</summary>
        public decimal InsShare { get; set; }

        /// <summary>سهم بیمار — money</summary>
        public decimal PatShare { get; set; }

        /// <summary>پرداختی — money</summary>
        public decimal Paid { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «صورتحساب»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-dense</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: مالی
    /// </remarks>
    public sealed class BillingViewModel : PageViewModelBase<BillingRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public BillingViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("فاکتور امروز", "۴۲", 10, Application.Current.TryFindResource("Icon.Receipt") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("مبلغ کل", "۲۸,۴۵۰,۰۰۰", 23, Application.Current.TryFindResource("Icon.Banknote") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("پرداخت‌نشده", "۵,۲۰۰,۰۰۰", -4, Application.Current.TryFindResource("Icon.Clock") as Geometry, Application.Current.TryFindResource("Brush.Warning") as Brush));
            Kpis.Add(new KpiItem("سهم بیمه", "۱۱,۳۰۰,۰۰۰", null, Application.Current.TryFindResource("Icon.ShieldCheck") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));

            // ارتباطات
            Relations.Add(new RelationItem("صورتحساب", "فرم صورتحساب", "ایجاد/ویرایش"));
            Relations.Add(new RelationItem("صورتحساب", "حسابداری", "سند دوبلِ خودکار"));
            Relations.Add(new RelationItem("صورتحساب", "بیمه و مطالبات", "مطالبات دوره‌ای"));
            Relations.Add(new RelationItem("صورتحساب", "پذیرش و صف", "از پذیرش می‌آید"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

        /// <summary>فیلترِ «وضعیت»</summary>
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

        /// <summary>فیلترِ «از تاریخ»</summary>
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
        private object? _filter3 = "۱۴۰۴/۰۷/۰۱";

        /// <summary>فیلترِ «تا تاریخ»</summary>
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
        private object? _filter4 = "۱۴۰۴/۰۷/۱۹";

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>فاکتور جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «فاکتور جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «فاکتور جدید»
            await RunAsync(_ => Task.CompletedTask, "فاکتور جدید…");
        }

        /// <summary>ثبت پرداخت</summary>
        public IAsyncRelayCommand PayCommand =>
            new AsyncRelayCommand(PayAsync);

        /// <summary>اجرای «ثبت پرداخت»</summary>
        private async Task PayAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ثبت پرداخت»
            await RunAsync(_ => Task.CompletedTask, "ثبت پرداخت…");
        }

        /// <summary>چاپ</summary>
        public IAsyncRelayCommand PrintCommand =>
            new AsyncRelayCommand(PrintAsync);

        /// <summary>اجرای «چاپ»</summary>
        private async Task PrintAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «چاپ»
            await RunAsync(_ => Task.CompletedTask, "چاپ…");
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
        private static IEnumerable<BillingRow> SampleData()
        {
            yield return new BillingRow { No = "INV-۹۵۱۲", Patient = "سارا احمدی", PatientSub = "P-۱۰۲۴", PatientInitials = "سا", PatientAvatar = Application.Current.TryFindResource("Brush.Info") as Brush, Date = "۱۴۰۴/۰۷/۱۹", Total = 250000m, InsShare = 175000m, PatShare = 75000m, Paid = 75000m, Status = "پرداخت شده", StatusTone = "ok" };
            yield return new BillingRow { No = "INV-۹۵۱۳", Patient = "محمد رضایی", PatientSub = "P-۱۰۲۵", PatientInitials = "مر", PatientAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Date = "۱۴۰۴/۰۷/۱۹", Total = 400000m, InsShare = 280000m, PatShare = 120000m, Paid = 0m, Status = "پرداخت‌نشده", StatusTone = "dg" };
            yield return new BillingRow { No = "INV-۹۵۱۴", Patient = "فاطمه کریمی", PatientSub = "P-۱۰۲۶", PatientInitials = "فک", PatientAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Date = "۱۴۰۴/۰۷/۱۸", Total = 150000m, InsShare = 0m, PatShare = 150000m, Paid = 80000m, Status = "نیمه‌پرداخت", StatusTone = "wn" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<BillingRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<BillingRow>(SampleData());
            return Task.FromResult<PagedResult<BillingRow>?>(
                new PagedResult<BillingRow>(items, items.Count));
        }
    }
}
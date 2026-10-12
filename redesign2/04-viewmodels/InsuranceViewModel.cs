// ═══════════════════════════════════════════════════════════════════════
//  InsuranceViewModel
//  صفحه‌ی «بیمه و مطالبات» · گروه «مالی» · کهن‌الگوی «detail-tabs»
//
//  متناظر با:  03-views/InsuranceView.xaml
//  وایرفریم:    01-wireframes-html/InsuranceView.html
//  پایه:        PageViewModelBase<InsuranceRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «بیمه و مطالبات»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class InsuranceRow
    {
        /// <summary>دوره — text</summary>
        public string? Period { get; set; }

        /// <summary>شرکت بیمه — text</summary>
        public string? Company { get; set; }

        /// <summary>تعداد — num</summary>
        public int Claims { get; set; }

        /// <summary>مبلغ — money</summary>
        public decimal Amount { get; set; }

        /// <summary>تأیید شده — money</summary>
        public decimal Approved { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «بیمه و مطالبات»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>detail-tabs</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: مالی
    /// </remarks>
    public sealed class InsuranceViewModel : PageViewModelBase<InsuranceRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public InsuranceViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("مطالبات جاری", "۸۴,۵۰۰,۰۰۰", 7, Application.Current.TryFindResource("Icon.ShieldCheck") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));
            Kpis.Add(new KpiItem("در انتظار تأیید", "۲۳,۱۰۰,۰۰۰", null, Application.Current.TryFindResource("Icon.Clock") as Geometry, Application.Current.TryFindResource("Brush.Warning") as Brush));
            Kpis.Add(new KpiItem("وصول شده", "۶۱,۴۰۰,۰۰۰", 12, Application.Current.TryFindResource("Icon.CircleCheck") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("برگشتی", "۳,۲۰۰,۰۰۰", -2, Application.Current.TryFindResource("Icon.CircleX") as Geometry, Application.Current.TryFindResource("Brush.Danger") as Brush));

            // تب‌ها
            Tabs.Add(new TabItemModel("دوره‌ها", Application.Current.TryFindResource("Icon.CalendarRange") as Geometry, 6));
            TabContents.Add(null);  // محتوای تب «دوره‌ها»
            Tabs.Add(new TabItemModel("صورت‌حساب‌ها", Application.Current.TryFindResource("Icon.Receipt") as Geometry, 128));
            TabContents.Add(null);  // محتوای تب «صورت‌حساب‌ها»
            Tabs.Add(new TabItemModel("برگشتی‌ها", Application.Current.TryFindResource("Icon.RotateCcw") as Geometry, 9));
            TabContents.Add(null);  // محتوای تب «برگشتی‌ها»
            Tabs.Add(new TabItemModel("قراردادها", Application.Current.TryFindResource("Icon.Signature") as Geometry, 4));
            TabContents.Add(null);  // محتوای تب «قراردادها»

            // ارتباطات
            Relations.Add(new RelationItem("بیمه و مطالبات", "صورتحساب", "فاکتورهای دوره"));
            Relations.Add(new RelationItem("بیمه و مطالبات", "فرم شرکت بیمه", "شرکت‌های طرف قرارداد"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

        /// <summary>محتوای تب‌ها</summary>
        public ObservableCollection<TabItemModel> Tabs { get; } = new();

        /// <summary>فیلترِ «شرکت بیمه»</summary>
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
        private object? _filter1 = "همه";

        /// <summary>فیلترِ «دوره»</summary>
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
        private object? _filter2 = "مهر ۱۴۰۴";

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

        /// <summary>محتوای تب‌ها — هر اندیس متناظر با یک تب در View است</summary>
        public ObservableCollection<object?> TabContents { get; } = new();

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>مطالبه جدید</summary>
        public IAsyncRelayCommand NewClaimCommand =>
            new AsyncRelayCommand(NewClaimAsync);

        /// <summary>اجرای «مطالبه جدید»</summary>
        private async Task NewClaimAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «مطالبه جدید»
            await RunAsync(_ => Task.CompletedTask, "مطالبه جدید…");
        }

        /// <summary>ارسال به بیمه</summary>
        public IAsyncRelayCommand SendCommand =>
            new AsyncRelayCommand(SendAsync);

        /// <summary>اجرای «ارسال به بیمه»</summary>
        private async Task SendAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ارسال به بیمه»
            await RunAsync(_ => Task.CompletedTask, "ارسال به بیمه…");
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
        private static IEnumerable<InsuranceRow> SampleData()
        {
            yield return new InsuranceRow { Period = "مهر ۱۴۰۴", Company = "تأمین اجتماعی", Claims = 42, Amount = 38500000m, Approved = 31200000m, Status = "در انتظار", StatusTone = "wn" };
            yield return new InsuranceRow { Period = "شهریور ۱۴۰۴", Company = "تأمین اجتماعی", Claims = 39, Amount = 34100000m, Approved = 34100000m, Status = "وصول شده", StatusTone = "ok" };
            yield return new InsuranceRow { Period = "مهر ۱۴۰۴", Company = "سلامت", Claims = 18, Amount = 11900000m, Approved = 0m, Status = "ارسال شده", StatusTone = "in" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<InsuranceRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<InsuranceRow>(SampleData());
            return Task.FromResult<PagedResult<InsuranceRow>?>(
                new PagedResult<InsuranceRow>(items, items.Count));
        }
    }
}
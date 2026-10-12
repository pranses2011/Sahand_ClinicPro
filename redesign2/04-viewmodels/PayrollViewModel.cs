// ═══════════════════════════════════════════════════════════════════════
//  PayrollViewModel
//  صفحه‌ی «حقوق و دستمزد» · گروه «مالی» · کهن‌الگوی «list-dense»
//
//  متناظر با:  03-views/PayrollView.xaml
//  وایرفریم:    01-wireframes-html/PayrollView.html
//  پایه:        PageViewModelBase<PayrollRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «حقوق و دستمزد»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class PayrollRow
    {
        /// <summary>کد — text</summary>
        public string? Code { get; set; }

        /// <summary>پرسنل — who</summary>
        public string? Employee { get; set; }
        /// <summary>زیرنویسِ «پرسنل» (مانند کد یا سن)</summary>
        public string? EmployeeSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «پرسنل»</summary>
        public string? EmployeeInitials { get; set; }
        /// <summary>رنگِ نشانِ «پرسنل» — از تم خوانده می‌شود</summary>
        public Brush? EmployeeAvatar { get; set; }

        /// <summary>دپارتمان — text</summary>
        public string? Department { get; set; }

        /// <summary>حقوق پایه — money</summary>
        public decimal Base { get; set; }

        /// <summary>اضافه‌کار — money</summary>
        public decimal Overtime { get; set; }

        /// <summary>کسورات — money</summary>
        public decimal Deductions { get; set; }

        /// <summary>خالص پرداختی — money</summary>
        public decimal Net { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «حقوق و دستمزد»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-dense</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: مالی
    /// </remarks>
    public sealed class PayrollViewModel : PageViewModelBase<PayrollRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public PayrollViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("پرسنل", "۳۴", null, Application.Current.TryFindResource("Icon.Users") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("جمع حقوق ماه", "۱۸۵,۰۰۰,۰۰۰", 5, Application.Current.TryFindResource("Icon.Banknote") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("اضافه‌کار", "۱۲,۴۰۰,۰۰۰", null, Application.Current.TryFindResource("Icon.HandCoins") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));
            Kpis.Add(new KpiItem("کسورات", "۲۱,۳۰۰,۰۰۰", null, Application.Current.TryFindResource("Icon.TrendingDown") as Geometry, Application.Current.TryFindResource("Brush.Warning") as Brush));

            // ارتباطات
            Relations.Add(new RelationItem("حقوق و دستمزد", "فرم کارمند", "حقوق پایه از پرونده"));
            Relations.Add(new RelationItem("حقوق و دستمزد", "ویرایش حقوق", "تغییر حقوق"));
            Relations.Add(new RelationItem("حقوق و دستمزد", "فیش حقوقی", "صدور فیش"));
            Relations.Add(new RelationItem("حقوق و دستمزد", "حسابداری", "سند حقوق"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

        /// <summary>فیلترِ «دوره»</summary>
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
        private object? _filter1 = "مهر ۱۴۰۴";

        /// <summary>فیلترِ «دپارتمان»</summary>
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

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>محاسبه</summary>
        public IAsyncRelayCommand CalculateCommand =>
            new AsyncRelayCommand(CalculateAsync);

        /// <summary>اجرای «محاسبه»</summary>
        private async Task CalculateAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «محاسبه»
            await RunAsync(_ => Task.CompletedTask, "محاسبه…");
        }

        /// <summary>فیش حقوقی</summary>
        public IAsyncRelayCommand PayslipCommand =>
            new AsyncRelayCommand(PayslipAsync);

        /// <summary>اجرای «فیش حقوقی»</summary>
        private async Task PayslipAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «فیش حقوقی»
            await RunAsync(_ => Task.CompletedTask, "فیش حقوقی…");
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
        private static IEnumerable<PayrollRow> SampleData()
        {
            yield return new PayrollRow { Code = "E-۱۰۰۱", Employee = "مریم نوری", EmployeeSub = "پذیرش", EmployeeInitials = "من", EmployeeAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Department = "پذیرش", Base = 12000000m, Overtime = 800000m, Deductions = 1200000m, Net = 11600000m, Status = "پرداخت شده", StatusTone = "ok" };
            yield return new PayrollRow { Code = "E-۱۰۰۲", Employee = "احمد صادقی", EmployeeSub = "پذیرش", EmployeeInitials = "اص", EmployeeAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Department = "پذیرش", Base = 12000000m, Overtime = 400000m, Deductions = 1100000m, Net = 11300000m, Status = "پرداخت شده", StatusTone = "ok" };
            yield return new PayrollRow { Code = "E-۱۰۰۳", Employee = "دکتر موسوی", EmployeeSub = "درمان", EmployeeInitials = "دم", EmployeeAvatar = Application.Current.TryFindResource("Brush.Warning") as Brush, Department = "درمان", Base = 45000000m, Overtime = 2000000m, Deductions = 4500000m, Net = 42500000m, Status = "در انتظار", StatusTone = "wn" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<PayrollRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<PayrollRow>(SampleData());
            return Task.FromResult<PagedResult<PayrollRow>?>(
                new PagedResult<PayrollRow>(items, items.Count));
        }
    }
}
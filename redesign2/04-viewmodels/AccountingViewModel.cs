// ═══════════════════════════════════════════════════════════════════════
//  AccountingViewModel
//  صفحه‌ی «حسابداری» · گروه «مالی» · کهن‌الگوی «split»
//
//  متناظر با:  03-views/AccountingView.xaml
//  وایرفریم:    01-wireframes-html/AccountingView.html
//  پایه:        PageViewModelBase<AccountingRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «حسابداری»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class AccountingRow
    {
        /// <summary>شماره سند — text</summary>
        public string? No { get; set; }

        /// <summary>تاریخ — date</summary>
        public string? Date { get; set; }

        /// <summary>شرح — text</summary>
        public string? Desc { get; set; }

        /// <summary>بدهکار — money</summary>
        public decimal Debit { get; set; }

        /// <summary>بستانکار — money</summary>
        public decimal Credit { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «حسابداری»
    /// </summary>
    /// <remarks>
    /// قانون طلایی حسابداری دوبل: جمع بدهکار باید برابر با جمع بستانکار
    /// باشد؛ سند نامتوازن هرگز ذخیره نمی‌شود.
    ///
    /// کهن‌الگوی طراحی: <c>split</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: مالی
    /// </remarks>
    public sealed class AccountingViewModel : PageViewModelBase<AccountingRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public AccountingViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("درآمد ماه", "۴۲۸,۰۰۰,۰۰۰", 18, Application.Current.TryFindResource("Icon.TrendingUp") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("هزینه ماه", "۲۹۱,۰۰۰,۰۰۰", 11, Application.Current.TryFindResource("Icon.TrendingDown") as Geometry, Application.Current.TryFindResource("Brush.Warning") as Brush));
            Kpis.Add(new KpiItem("سود خالص", "۱۳۷,۰۰۰,۰۰۰", 24, Application.Current.TryFindResource("Icon.Equal") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("تراز", "متوازن", null, Application.Current.TryFindResource("Icon.Scale") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));

            // ارتباطات
            Relations.Add(new RelationItem("حسابداری", "جزئیات سند حسابداری", "سطرهای سند"));
            Relations.Add(new RelationItem("حسابداری", "صورتحساب", "سند خودکارِ فاکتور"));
            Relations.Add(new RelationItem("حسابداری", "حقوق و دستمزد", "سند حقوق"));

            // خلاصه‌ی کناری
            Summary.Add(new KeyValueItem("۱ — دارایی‌ها", "۱,۲۴۰,۰۰۰,۰۰۰"));
            Summary.Add(new KeyValueItem("۲ — بدهی‌ها", "۴۱۸,۰۰۰,۰۰۰"));
            Summary.Add(new KeyValueItem("۳ — سرمایه", "۶۸۰,۰۰۰,۰۰۰"));
            Summary.Add(new KeyValueItem("۴ — درآمدها", "۴۲۸,۰۰۰,۰۰۰"));
            Summary.Add(new KeyValueItem("۵ — هزینه‌ها", "۲۹۱,۰۰۰,۰۰۰"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

        /// <summary>فیلترِ «از تاریخ»</summary>
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
        private object? _filter2 = "۱۴۰۴/۰۷/۰۱";

        /// <summary>فیلترِ «تا تاریخ»</summary>
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
        private object? _filter3 = "۱۴۰۴/۰۷/۱۹";

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        /// <summary>خلاصه‌ی کناریِ صفحه</summary>
        public ObservableCollection<KeyValueItem> Summary { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>سند جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «سند جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «سند جدید»
            await RunAsync(_ => Task.CompletedTask, "سند جدید…");
        }

        /// <summary>جزئیات سند</summary>
        public IAsyncRelayCommand DetailCommand =>
            new AsyncRelayCommand(DetailAsync);

        /// <summary>اجرای «جزئیات سند»</summary>
        private async Task DetailAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «جزئیات سند»
            await RunAsync(_ => Task.CompletedTask, "جزئیات سند…");
        }

        /// <summary>تراز آزمایشی</summary>
        public IAsyncRelayCommand TrialCommand =>
            new AsyncRelayCommand(TrialAsync);

        /// <summary>اجرای «تراز آزمایشی»</summary>
        private async Task TrialAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «تراز آزمایشی»
            await RunAsync(_ => Task.CompletedTask, "تراز آزمایشی…");
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
        private static IEnumerable<AccountingRow> SampleData()
        {
            yield return new AccountingRow { No = "JV-۱۲۴۰", Date = "۱۴۰۴/۰۷/۱۹", Desc = "فاکتور INV-۹۵۱۲ — درآمد ویزیت", Debit = 250000m, Credit = 250000m, Status = "تراز", StatusTone = "ok" };
            yield return new AccountingRow { No = "JV-۱۲۴۱", Date = "۱۴۰۴/۰۷/۱۹", Desc = "خرید دارو — فاکتور F-۹۸۷۶۵", Debit = 12400000m, Credit = 12400000m, Status = "تراز", StatusTone = "ok" };
            yield return new AccountingRow { No = "JV-۱۲۴۲", Date = "۱۴۰۴/۰۷/۱۸", Desc = "حقوق پرسنل — تیر ماه", Debit = 185000000m, Credit = 185000000m, Status = "تراز", StatusTone = "ok" };
            yield return new AccountingRow { No = "JV-۱۲۴۳", Date = "۱۴۰۴/۰۷/۱۸", Desc = "پیش‌پرداخت بیمه تکمیلی", Debit = 8000000m, Credit = 0m, Status = "نامتوازن", StatusTone = "dg" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<AccountingRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<AccountingRow>(SampleData());
            return Task.FromResult<PagedResult<AccountingRow>?>(
                new PagedResult<AccountingRow>(items, items.Count));
        }
    }
}
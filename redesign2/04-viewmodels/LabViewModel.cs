// ═══════════════════════════════════════════════════════════════════════
//  LabViewModel
//  صفحه‌ی «آزمایشگاه» · گروه «آزمایشگاه» · کهن‌الگوی «list-dense»
//
//  متناظر با:  03-views/LabView.xaml
//  وایرفریم:    01-wireframes-html/LabView.html
//  پایه:        PageViewModelBase<LabRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «آزمایشگاه»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class LabRow
    {
        /// <summary>کد — text</summary>
        public string? Code { get; set; }

        /// <summary>بیمار — who</summary>
        public string? Patient { get; set; }
        /// <summary>زیرنویسِ «بیمار» (مانند کد یا سن)</summary>
        public string? PatientSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «بیمار»</summary>
        public string? PatientInitials { get; set; }
        /// <summary>رنگِ نشانِ «بیمار» — از تم خوانده می‌شود</summary>
        public Brush? PatientAvatar { get; set; }

        /// <summary>آزمایش‌ها — text</summary>
        public string? Tests { get; set; }

        /// <summary>تاریخ — date</summary>
        public string? Date { get; set; }

        /// <summary>مبلغ — money</summary>
        public decimal Amount { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «آزمایشگاه»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-dense</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: آزمایشگاه
    /// </remarks>
    public sealed class LabViewModel : PageViewModelBase<LabRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public LabViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("درخواست امروز", "۲۴", 6, Application.Current.TryFindResource("Icon.TestTube") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("در انتظار نمونه", "۹", null, Application.Current.TryFindResource("Icon.Clock") as Geometry, Application.Current.TryFindResource("Brush.Warning") as Brush));
            Kpis.Add(new KpiItem("نتیجه آماده", "۱۱", null, Application.Current.TryFindResource("Icon.CircleCheck") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("ارسال‌شده با پیامک", "۸", null, Application.Current.TryFindResource("Icon.Send") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));

            // ارتباطات
            Relations.Add(new RelationItem("آزمایشگاه", "درخواست آزمایش", "ثبت درخواست"));
            Relations.Add(new RelationItem("آزمایشگاه", "ثبت نتیجه آزمایش", "ورود نتایج"));
            Relations.Add(new RelationItem("آزمایشگاه", "پیامک", "اطلاع‌رسانی نتیجه"));
            Relations.Add(new RelationItem("آزمایشگاه", "پرونده‌ی بیمار", "نتایج در پرونده"));

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

        /// <summary>فیلترِ «تاریخ»</summary>
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

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>درخواست جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «درخواست جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «درخواست جدید»
            await RunAsync(_ => Task.CompletedTask, "درخواست جدید…");
        }

        /// <summary>ثبت نتیجه</summary>
        public IAsyncRelayCommand ResultCommand =>
            new AsyncRelayCommand(ResultAsync);

        /// <summary>اجرای «ثبت نتیجه»</summary>
        private async Task ResultAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ثبت نتیجه»
            await RunAsync(_ => Task.CompletedTask, "ثبت نتیجه…");
        }

        /// <summary>چاپ جواب</summary>
        public IAsyncRelayCommand PrintCommand =>
            new AsyncRelayCommand(PrintAsync);

        /// <summary>اجرای «چاپ جواب»</summary>
        private async Task PrintAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «چاپ جواب»
            await RunAsync(_ => Task.CompletedTask, "چاپ جواب…");
        }

        /// <summary>ارسال پیامک</summary>
        public IAsyncRelayCommand SendSmsCommand =>
            new AsyncRelayCommand(SendSmsAsync);

        /// <summary>اجرای «ارسال پیامک»</summary>
        private async Task SendSmsAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ارسال پیامک»
            await RunAsync(_ => Task.CompletedTask, "ارسال پیامک…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<LabRow> SampleData()
        {
            yield return new LabRow { Code = "L-۷۴۰", Patient = "سارا احمدی", PatientSub = "P-۱۰۲۴", PatientInitials = "سا", PatientAvatar = Application.Current.TryFindResource("Brush.Info") as Brush, Tests = "CBC، قند ناشتا، چربی", Date = "۱۴۰۴/۰۷/۱۸", Amount = 420000m, Status = "آماده", StatusTone = "ok" };
            yield return new LabRow { Code = "L-۷۴۱", Patient = "محمد رضایی", PatientSub = "P-۱۰۲۵", PatientInitials = "مر", PatientAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Tests = "TSH، T4", Date = "۱۴۰۴/۰۷/۱۸", Amount = 380000m, Status = "در حال انجام", StatusTone = "in" };
            yield return new LabRow { Code = "L-۷۴۲", Patient = "فاطمه کریمی", PatientSub = "P-۱۰۲۶", PatientInitials = "فک", PatientAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Tests = "آزمایش ادرار", Date = "۱۴۰۴/۰۷/۱۹", Amount = 95000m, Status = "نمونه‌گیری", StatusTone = "wn" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<LabRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<LabRow>(SampleData());
            return Task.FromResult<PagedResult<LabRow>?>(
                new PagedResult<LabRow>(items, items.Count));
        }
    }
}
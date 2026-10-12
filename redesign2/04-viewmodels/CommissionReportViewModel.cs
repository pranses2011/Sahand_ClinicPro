// ═══════════════════════════════════════════════════════════════════════
//  CommissionReportViewModel
//  صفحه‌ی «گزارش کارمزد» · گروه «مالی» · کهن‌الگوی «dialog»
//
//  متناظر با:  03-views/CommissionReportWindow.xaml
//  وایرفریم:    01-wireframes-html/CommissionReportWindow.html
//  پایه:        PageViewModelBase<CommissionReportRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «گزارش کارمزد»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class CommissionReportRow
    {
        /// <summary>پزشک — who</summary>
        public string? Doctor { get; set; }
        /// <summary>زیرنویسِ «پزشک» (مانند کد یا سن)</summary>
        public string? DoctorSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «پزشک»</summary>
        public string? DoctorInitials { get; set; }
        /// <summary>رنگِ نشانِ «پزشک» — از تم خوانده می‌شود</summary>
        public Brush? DoctorAvatar { get; set; }

        /// <summary>ویزیت‌ها — num</summary>
        public int Visits { get; set; }

        /// <summary>ناخالص — money</summary>
        public decimal Gross { get; set; }

        /// <summary>درصد — num</summary>
        public int Percent { get; set; }

        /// <summary>کارمزد — money</summary>
        public decimal Commission { get; set; }
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «گزارش کارمزد»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>dialog</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: مالی
    /// </remarks>
    public sealed class CommissionReportViewModel : PageViewModelBase<CommissionReportRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public CommissionReportViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("گزارش کارمزد", "فهرست ویزیت‌ها", "مبنای محاسبه"));
            Relations.Add(new RelationItem("گزارش کارمزد", "پزشکان", "درصد قرارداد"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>فیلترِ «پزشک»</summary>
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

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

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
        private static IEnumerable<CommissionReportRow> SampleData()
        {
            yield return new CommissionReportRow { Doctor = "دکتر موسوی", DoctorSub = "متخصص داخلی", DoctorInitials = "دم", DoctorAvatar = Application.Current.TryFindResource("Brush.Warning") as Brush, Visits = 124, Gross = 31000000m, Percent = 50, Commission = 15500000m };
            yield return new CommissionReportRow { Doctor = "دکتر رضایی", DoctorSub = "متخصص قلب", DoctorInitials = "در", DoctorAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Visits = 87, Gross = 34800000m, Percent = 55, Commission = 19140000m };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<CommissionReportRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<CommissionReportRow>(SampleData());
            return Task.FromResult<PagedResult<CommissionReportRow>?>(
                new PagedResult<CommissionReportRow>(items, items.Count));
        }
        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

            return ok && !HasErrors;
        }

        /// <summary>
        /// ذخیره‌ی داده‌ها. در صورت نبودِ سرویس، فقط وضعیت موفقیت نمایش
        /// داده می‌شود تا رفتارِ فرم قابلِ بررسی باشد.
        /// </summary>
        protected override Task PersistAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی
            return Task.CompletedTask;
        }

        /// <summary>جستجو در یک فیلدِ جستجومحور</summary>
        private Task LookupAsync(string field)
            => RunAsync(_ => Task.CompletedTask, $"جستجوی {field}…");

        /// <summary>خواندنِ بارکد برای یک فیلد</summary>
        private void ScanBarcode(string field)
            => ShowSuccess($"بارکدِ {field} خوانده شد");
    }
}
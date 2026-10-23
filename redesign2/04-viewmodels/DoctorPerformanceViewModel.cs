// ═══════════════════════════════════════════════════════════════════════
//  DoctorPerformanceViewModel
//  صفحه‌ی «عملکرد پزشکان» · گروه «مالی» · کهن‌الگوی «dialog»
//
//  متناظر با:  03-views/DoctorPerformanceWindow.xaml
//  وایرفریم:    01-wireframes-html/DoctorPerformanceWindow.html
//  پایه:        PageViewModelBase<DoctorPerformanceRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «عملکرد پزشکان»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class DoctorPerformanceRow
    {
        /// <summary>پزشک — who</summary>
        public string? Doctor { get; set; }
        /// <summary>زیرنویسِ «پزشک» (مانند کد یا سن)</summary>
        public string? DoctorSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «پزشک»</summary>
        public string? DoctorInitials { get; set; }
        /// <summary>رنگِ نشانِ «پزشک» — از تم خوانده می‌شود</summary>
        public Brush? DoctorAvatar { get; set; }

        /// <summary>ویزیت — num</summary>
        public int Visits { get; set; }

        /// <summary>درآمد — money</summary>
        public decimal Revenue { get; set; }

        /// <summary>میانگین ویزیت — num</summary>
        public string? AvgTime { get; set; }

        /// <summary>رضایت — chip</summary>
        public string? Satisfaction { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «رضایت»</summary>
        public string SatisfactionTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «عملکرد پزشکان»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>dialog</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: مالی
    /// </remarks>
    public sealed class DoctorPerformanceViewModel : PageViewModelBase<DoctorPerformanceRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public DoctorPerformanceViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("عملکرد پزشکان", "گزارش کارمزد", "محاسبه مالی"));
            Relations.Add(new RelationItem("عملکرد پزشکان", "نظرسنجی", "نمره رضایت"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

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
        private static IEnumerable<DoctorPerformanceRow> SampleData()
        {
            yield return new DoctorPerformanceRow { Doctor = "دکتر موسوی", DoctorSub = "متخصص داخلی", DoctorInitials = "دم", DoctorAvatar = Application.Current.TryFindResource("Brush.Warning") as Brush, Visits = 124, Revenue = 31000000m, AvgTime = "۱۴ دقیقه", Satisfaction = "۴٫۶", SatisfactionTone = "ok" };
            yield return new DoctorPerformanceRow { Doctor = "دکتر رضایی", DoctorSub = "متخصص قلب", DoctorInitials = "در", DoctorAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Visits = 87, Revenue = 34800000m, AvgTime = "۲۱ دقیقه", Satisfaction = "۴٫۳", SatisfactionTone = "cy" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<DoctorPerformanceRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<DoctorPerformanceRow>(SampleData());
            return Task.FromResult<PagedResult<DoctorPerformanceRow>?>(
                new PagedResult<DoctorPerformanceRow>(items, items.Count));
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
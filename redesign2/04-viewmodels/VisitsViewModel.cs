// ═══════════════════════════════════════════════════════════════════════
//  VisitsViewModel
//  صفحه‌ی «فهرست ویزیت‌ها» · گروه «ویزیت» · کهن‌الگوی «list-dense»
//
//  متناظر با:  03-views/VisitsView.xaml
//  وایرفریم:    01-wireframes-html/VisitsView.html
//  پایه:        PageViewModelBase<VisitsRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «فهرست ویزیت‌ها»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class VisitsRow
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

        /// <summary>پزشک — text</summary>
        public string? Doctor { get; set; }

        /// <summary>تاریخ — date</summary>
        public string? Date { get; set; }

        /// <summary>ICD-10 — text</summary>
        public string? Icd { get; set; }

        /// <summary>مبلغ — money</summary>
        public decimal Amount { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «فهرست ویزیت‌ها»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-dense</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: ویزیت
    /// </remarks>
    public sealed class VisitsViewModel : PageViewModelBase<VisitsRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public VisitsViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("فهرست ویزیت‌ها", "ویزیت (SOAP)", "هر ردیف ویزیت کامل"));
            Relations.Add(new RelationItem("فهرست ویزیت‌ها", "گزارش کارمزد", "مبنای کارمزد پزشک"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

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

        /// <summary>خروجی Excel</summary>
        public IAsyncRelayCommand ExportCommand =>
            new AsyncRelayCommand(ExportAsync);

        /// <summary>اجرای «خروجی Excel»</summary>
        private async Task ExportAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «خروجی Excel»
            await RunAsync(_ => Task.CompletedTask, "خروجی Excel…");
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

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<VisitsRow> SampleData()
        {
            yield return new VisitsRow { Code = "V-۴۱۲۰", Patient = "سارا احمدی", PatientSub = "P-۱۰۲۴", PatientInitials = "سا", PatientAvatar = Application.Current.TryFindResource("Brush.Info") as Brush, Doctor = "دکتر موسوی", Date = "۱۴۰۴/۰۷/۱۲", Icd = "K21.9", Amount = 250000m, Status = "تکمیل", StatusTone = "ok" };
            yield return new VisitsRow { Code = "V-۴۱۲۱", Patient = "محمد رضایی", PatientSub = "P-۱۰۲۵", PatientInitials = "مر", PatientAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Doctor = "دکتر رضایی", Date = "۱۴۰۴/۰۷/۱۲", Icd = "J06.9", Amount = 400000m, Status = "تکمیل", StatusTone = "ok" };
            yield return new VisitsRow { Code = "V-۴۱۲۲", Patient = "فاطمه کریمی", PatientSub = "P-۱۰۲۶", PatientInitials = "فک", PatientAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Doctor = "دکتر موسوی", Date = "۱۴۰۴/۰۷/۱۱", Icd = "Z00.0", Amount = 150000m, Status = "تکمیل", StatusTone = "ok" };
            yield return new VisitsRow { Code = "V-۴۱۲۳", Patient = "علی حسینی", PatientSub = "P-۱۰۲۷", PatientInitials = "اح", PatientAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Doctor = "دکتر کریمی", Date = "۱۴۰۴/۰۷/۱۰", Icd = "I10", Amount = 250000m, Status = "پیش‌نویس", StatusTone = "wn" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<VisitsRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<VisitsRow>(SampleData());
            return Task.FromResult<PagedResult<VisitsRow>?>(
                new PagedResult<VisitsRow>(items, items.Count));
        }
    }
}
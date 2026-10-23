// ═══════════════════════════════════════════════════════════════════════
//  ReportBrowserViewModel
//  صفحه‌ی «مرورگر گزارشات» · گروه «گزارشات» · کهن‌الگوی «report»
//
//  متناظر با:  03-views/ReportBrowserView.xaml
//  وایرفریم:    01-wireframes-html/ReportBrowserView.html
//  پایه:        PageViewModelBase<ReportBrowserRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «مرورگر گزارشات»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class ReportBrowserRow
    {
        /// <summary>گزارش — text</summary>
        public string? Title { get; set; }

        /// <summary>دسته — text</summary>
        public string? Category { get; set; }

        /// <summary>تهیه‌کننده — who</summary>
        public string? Owner { get; set; }
        /// <summary>زیرنویسِ «تهیه‌کننده» (مانند کد یا سن)</summary>
        public string? OwnerSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «تهیه‌کننده»</summary>
        public string? OwnerInitials { get; set; }
        /// <summary>رنگِ نشانِ «تهیه‌کننده» — از تم خوانده می‌شود</summary>
        public Brush? OwnerAvatar { get; set; }

        /// <summary>تعداد اجرا — num</summary>
        public int Runs { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «مرورگر گزارشات»
    /// </summary>
    /// <remarks>
    /// پنلِ راست: فیلترها و پارامترهای گزارش. پنلِ چپ: پیش‌نمایشِ کاغذیِ
    /// گزارش با هدر و صفحه‌بندی.
    ///
    /// کهن‌الگوی طراحی: <c>report</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: گزارشات
    /// </remarks>
    public sealed class ReportBrowserViewModel : PageViewModelBase<ReportBrowserRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public ReportBrowserViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("مرورگر گزارشات", "گزارشات", "انتخاب از کتابخانه"));
            Relations.Add(new RelationItem("مرورگر گزارشات", "گزارش‌ساز پویا", "ساخت گزارش سفارشی"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>فیلترِ «دسته»</summary>
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

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>خروجی PDF</summary>
        public IAsyncRelayCommand ExportPdfCommand =>
            new AsyncRelayCommand(ExportPdfAsync);

        /// <summary>اجرای «خروجی PDF»</summary>
        private async Task ExportPdfAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «خروجی PDF»
            await RunAsync(_ => Task.CompletedTask, "خروجی PDF…");
        }

        /// <summary>خروجی Excel</summary>
        public IAsyncRelayCommand ExportExcelCommand =>
            new AsyncRelayCommand(ExportExcelAsync);

        /// <summary>اجرای «خروجی Excel»</summary>
        private async Task ExportExcelAsync()
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
        private static IEnumerable<ReportBrowserRow> SampleData()
        {
            yield return new ReportBrowserRow { Title = "گزارش درآمد روزانه", Category = "مالی", Owner = "سیستم", OwnerSub = "خودکار", OwnerInitials = "س", OwnerAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Runs = 214, Status = "آماده", StatusTone = "ok" };
            yield return new ReportBrowserRow { Title = "گزارش ویزیت‌های پزشک", Category = "درمانی", Owner = "سیستم", OwnerSub = "خودکار", OwnerInitials = "س", OwnerAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Runs = 189, Status = "آماده", StatusTone = "ok" };
            yield return new ReportBrowserRow { Title = "گزارش موجودی داروخانه", Category = "عملیاتی", Owner = "سیستم", OwnerSub = "خودکار", OwnerInitials = "س", OwnerAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Runs = 156, Status = "آماده", StatusTone = "ok" };
            yield return new ReportBrowserRow { Title = "تحلیل روند بیماران جدید", Category = "درمانی", Owner = "لیلا حسنی", OwnerSub = "حسابدار", OwnerInitials = "لح", OwnerAvatar = Application.Current.TryFindResource("Brush.Success") as Brush, Runs = 31, Status = "پیش‌نویس", StatusTone = "wn" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<ReportBrowserRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<ReportBrowserRow>(SampleData());
            return Task.FromResult<PagedResult<ReportBrowserRow>?>(
                new PagedResult<ReportBrowserRow>(items, items.Count));
        }
    }
}
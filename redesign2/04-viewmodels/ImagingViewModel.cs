// ═══════════════════════════════════════════════════════════════════════
//  ImagingViewModel
//  صفحه‌ی «تصویربرداری» · گروه «تصویربرداری» · کهن‌الگوی «list-dense»
//
//  متناظر با:  03-views/ImagingView.xaml
//  وایرفریم:    01-wireframes-html/ImagingView.html
//  پایه:        PageViewModelBase<ImagingRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «تصویربرداری»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class ImagingRow
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

        /// <summary>نوع — text</summary>
        public string? Modality { get; set; }

        /// <summary>ناحیه — text</summary>
        public string? BodyPart { get; set; }

        /// <summary>تاریخ — date</summary>
        public string? Date { get; set; }

        /// <summary>تصاویر — num</summary>
        public int Images { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «تصویربرداری»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-dense</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: تصویربرداری
    /// </remarks>
    public sealed class ImagingViewModel : PageViewModelBase<ImagingRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public ImagingViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("تصویربرداری", "نمایشگر تصاویر پزشکی", "مشاهده DICOM"));
            Relations.Add(new RelationItem("تصویربرداری", "پرونده‌ی بیمار", "تصاویر در پرونده"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>فیلترِ «نوع تصویر»</summary>
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

        /// <summary>نمایش تصاویر</summary>
        public IAsyncRelayCommand OpenCommand =>
            new AsyncRelayCommand(OpenAsync);

        /// <summary>اجرای «نمایش تصاویر»</summary>
        private async Task OpenAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «نمایش تصاویر»
            await RunAsync(_ => Task.CompletedTask, "نمایش تصاویر…");
        }

        /// <summary>ثبت گزارش</summary>
        public IAsyncRelayCommand ReportCommand =>
            new AsyncRelayCommand(ReportAsync);

        /// <summary>اجرای «ثبت گزارش»</summary>
        private async Task ReportAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ثبت گزارش»
            await RunAsync(_ => Task.CompletedTask, "ثبت گزارش…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<ImagingRow> SampleData()
        {
            yield return new ImagingRow { Code = "I-۳۱۰", Patient = "سارا احمدی", PatientSub = "P-۱۰۲۴", PatientInitials = "سا", PatientAvatar = Application.Current.TryFindResource("Brush.Info") as Brush, Modality = "رادیوگرافی", BodyPart = "قفسه سینه", Date = "۱۴۰۴/۰۷/۱۸", Images = 2, Status = "گزارش شده", StatusTone = "ok" };
            yield return new ImagingRow { Code = "I-۳۱۱", Patient = "محمد رضایی", PatientSub = "P-۱۰۲۵", PatientInitials = "مر", PatientAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Modality = "سونوگرافی", BodyPart = "شکم", Date = "۱۴۰۴/۰۷/۱۹", Images = 6, Status = "در انتظار گزارش", StatusTone = "wn" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<ImagingRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<ImagingRow>(SampleData());
            return Task.FromResult<PagedResult<ImagingRow>?>(
                new PagedResult<ImagingRow>(items, items.Count));
        }
    }
}
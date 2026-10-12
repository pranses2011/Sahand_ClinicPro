// ═══════════════════════════════════════════════════════════════════════
//  OphthalmologyViewModel
//  صفحه‌ی «چشم‌پزشکی» · گروه «ویزیت» · کهن‌الگوی «detail-tabs»
//
//  متناظر با:  03-views/OphthalmologyView.xaml
//  وایرفریم:    01-wireframes-html/OphthalmologyView.html
//  پایه:        PageViewModelBase<OphthalmologyRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «چشم‌پزشکی»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class OphthalmologyRow
    {
        /// <summary>تاریخ — date</summary>
        public string? Date { get; set; }

        /// <summary>چشم راست — text</summary>
        public string? RightEye { get; set; }

        /// <summary>چشم چپ — text</summary>
        public string? LeftEye { get; set; }

        /// <summary>فشار چشم — num</summary>
        public int IOP { get; set; }

        /// <summary>پزشک — text</summary>
        public string? Doctor { get; set; }
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «چشم‌پزشکی»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>detail-tabs</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: ویزیت
    /// </remarks>
    public sealed class OphthalmologyViewModel : PageViewModelBase<OphthalmologyRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public OphthalmologyViewModel(ILogger? logger = null) : base(logger)
        {
            // تب‌ها
            Tabs.Add(new TabItemModel("حدت بینایی", Application.Current.TryFindResource("Icon.Eye") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «حدت بینایی»
            Tabs.Add(new TabItemModel("فشار چشم", Application.Current.TryFindResource("Icon.Gauge") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «فشار چشم»
            Tabs.Add(new TabItemModel("فوندوس", Application.Current.TryFindResource("Icon.ScanEye") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «فوندوس»
            Tabs.Add(new TabItemModel("نسخه عینک", Application.Current.TryFindResource("Icon.Glasses") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «نسخه عینک»

            // ارتباطات
            Relations.Add(new RelationItem("چشم‌پزشکی", "ویزیت (SOAP)", "ویزیت تخصصی"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>محتوای تب‌ها</summary>
        public ObservableCollection<TabItemModel> Tabs { get; } = new();

        /// <summary>محتوای تب‌ها — هر اندیس متناظر با یک تب در View است</summary>
        public ObservableCollection<object?> TabContents { get; } = new();

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>کروی راست</summary>
        public int RightSph
        {
            get => _rightSph;
            set
            {
                if (SetProperty(ref _rightSph, value))
                {
                }
            }
        }
        private int _rightSph = 0;

        /// <summary>استیگمات راست</summary>
        public int RightCyl
        {
            get => _rightCyl;
            set
            {
                if (SetProperty(ref _rightCyl, value))
                {
                }
            }
        }
        private int _rightCyl = 0;

        /// <summary>محور راست</summary>
        public int RightAxis
        {
            get => _rightAxis;
            set
            {
                if (SetProperty(ref _rightAxis, value))
                {
                }
            }
        }
        private int _rightAxis = 180;

        /// <summary>کروی چپ</summary>
        public int LeftSph
        {
            get => _leftSph;
            set
            {
                if (SetProperty(ref _leftSph, value))
                {
                }
            }
        }
        private int _leftSph = 0;

        /// <summary>استیگمات چپ</summary>
        public int LeftCyl
        {
            get => _leftCyl;
            set
            {
                if (SetProperty(ref _leftCyl, value))
                {
                }
            }
        }
        private int _leftCyl = 0;

        /// <summary>محور چپ</summary>
        public int LeftAxis
        {
            get => _leftAxis;
            set
            {
                if (SetProperty(ref _leftAxis, value))
                {
                }
            }
        }
        private int _leftAxis = 170;

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>چاپ نسخه عینک</summary>
        public IAsyncRelayCommand PrintCommand =>
            new AsyncRelayCommand(PrintAsync);

        /// <summary>اجرای «چاپ نسخه عینک»</summary>
        private async Task PrintAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «چاپ نسخه عینک»
            await RunAsync(_ => Task.CompletedTask, "چاپ نسخه عینک…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<OphthalmologyRow> SampleData()
        {
            yield return new OphthalmologyRow { Date = "۱۴۰۴/۰۷/۱۸", RightEye = "−۱٫۵۰ / −۰٫۵۰ × ۱۸۰", LeftEye = "−۱٫۲۵ / −۰٫۷۵ × ۱۷۰", IOP = 14, Doctor = "دکتر کریمی" };
            yield return new OphthalmologyRow { Date = "۱۴۰۳/۰۹/۲۲", RightEye = "−۱٫۲۵ / −۰٫۵۰ × ۱۷۵", LeftEye = "−۱٫۰۰ / −۰٫۵۰ × ۱۷۰", IOP = 15, Doctor = "دکتر کریمی" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<OphthalmologyRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<OphthalmologyRow>(SampleData());
            return Task.FromResult<PagedResult<OphthalmologyRow>?>(
                new PagedResult<OphthalmologyRow>(items, items.Count));
        }
    }
}
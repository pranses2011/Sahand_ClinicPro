// ═══════════════════════════════════════════════════════════════════════
//  PrescriptionsViewModel
//  صفحه‌ی «نسخه‌ها» · گروه «نسخه» · کهن‌الگوی «list-dense»
//
//  متناظر با:  03-views/PrescriptionsView.xaml
//  وایرفریم:    01-wireframes-html/PrescriptionsView.html
//  پایه:        PageViewModelBase<PrescriptionsRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «نسخه‌ها»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class PrescriptionsRow
    {
        /// <summary>کد نسخه — text</summary>
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

        /// <summary>اقلام — num</summary>
        public int Items { get; set; }

        /// <summary>مبلغ — money</summary>
        public decimal Amount { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «نسخه‌ها»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-dense</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: نسخه
    /// </remarks>
    public sealed class PrescriptionsViewModel : PageViewModelBase<PrescriptionsRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public PrescriptionsViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("نسخه‌ها", "داروخانه", "تحویل از داروخانه"));
            Relations.Add(new RelationItem("نسخه‌ها", "نسخه الکترونیک", "ارسال به سامانه"));

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
        private object? _filter2 = "همه";

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

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>نسخه جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «نسخه جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «نسخه جدید»
            await RunAsync(_ => Task.CompletedTask, "نسخه جدید…");
        }

        /// <summary>تحویل دارو</summary>
        public IAsyncRelayCommand DispenseCommand =>
            new AsyncRelayCommand(DispenseAsync);

        /// <summary>اجرای «تحویل دارو»</summary>
        private async Task DispenseAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «تحویل دارو»
            await RunAsync(_ => Task.CompletedTask, "تحویل دارو…");
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
        private static IEnumerable<PrescriptionsRow> SampleData()
        {
            yield return new PrescriptionsRow { Code = "RX-۸۵۲۰", Patient = "سارا احمدی", PatientSub = "P-۱۰۲۴", PatientInitials = "سا", PatientAvatar = Application.Current.TryFindResource("Brush.Info") as Brush, Doctor = "دکتر موسوی", Date = "۱۴۰۴/۰۷/۱۲", Items = 3, Amount = 245000m, Status = "تحویل شده", StatusTone = "ok" };
            yield return new PrescriptionsRow { Code = "RX-۸۵۲۱", Patient = "محمد رضایی", PatientSub = "P-۱۰۲۵", PatientInitials = "مر", PatientAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Doctor = "دکتر رضایی", Date = "۱۴۰۴/۰۷/۱۲", Items = 2, Amount = 180000m, Status = "صادر شده", StatusTone = "in" };
            yield return new PrescriptionsRow { Code = "RX-۸۵۲۲", Patient = "فاطمه کریمی", PatientSub = "P-۱۰۲۶", PatientInitials = "فک", PatientAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Doctor = "دکتر موسوی", Date = "۱۴۰۴/۰۷/۱۱", Items = 4, Amount = 310000m, Status = "تحویل شده", StatusTone = "ok" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<PrescriptionsRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<PrescriptionsRow>(SampleData());
            return Task.FromResult<PagedResult<PrescriptionsRow>?>(
                new PagedResult<PrescriptionsRow>(items, items.Count));
        }
    }
}
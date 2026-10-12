// ═══════════════════════════════════════════════════════════════════════
//  ShiftsViewModel
//  صفحه‌ی «شیفت‌ها» · گروه «پذیرش» · کهن‌الگوی «list-dense»
//
//  متناظر با:  03-views/ShiftsView.xaml
//  وایرفریم:    01-wireframes-html/ShiftsView.html
//  پایه:        PageViewModelBase<ShiftsRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «شیفت‌ها»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class ShiftsRow
    {
        /// <summary>کد — text</summary>
        public string? Code { get; set; }

        /// <summary>مسئول — who</summary>
        public string? User { get; set; }
        /// <summary>زیرنویسِ «مسئول» (مانند کد یا سن)</summary>
        public string? UserSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «مسئول»</summary>
        public string? UserInitials { get; set; }
        /// <summary>رنگِ نشانِ «مسئول» — از تم خوانده می‌شود</summary>
        public Brush? UserAvatar { get; set; }

        /// <summary>شروع — date</summary>
        public string? Start { get; set; }

        /// <summary>پایان — date</summary>
        public string? End { get; set; }

        /// <summary>پذیرش‌ها — num</summary>
        public int Receptions { get; set; }

        /// <summary>مبلغ کل — money</summary>
        public decimal Total { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «شیفت‌ها»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-dense</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: پذیرش
    /// </remarks>
    public sealed class ShiftsViewModel : PageViewModelBase<ShiftsRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public ShiftsViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("شیفت‌ها", "بستن شیفت", "جمع‌بندی مالی"));
            Relations.Add(new RelationItem("شیفت‌ها", "پذیرش و صف", "پذیرش‌های دوره شیفت"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>فیلترِ «وضعیت»</summary>
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

        /// <summary>فیلترِ «کاربر»</summary>
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

        /// <summary>شیفت جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «شیفت جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «شیفت جدید»
            await RunAsync(_ => Task.CompletedTask, "شیفت جدید…");
        }

        /// <summary>بستن شیفت</summary>
        public IAsyncRelayCommand CloseCommand =>
            new AsyncRelayCommand(CloseAsync);

        /// <summary>اجرای «بستن شیفت»</summary>
        private async Task CloseAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «بستن شیفت»
            await RunAsync(_ => Task.CompletedTask, "بستن شیفت…");
        }

        /// <summary>جزئیات</summary>
        public IAsyncRelayCommand DetailCommand =>
            new AsyncRelayCommand(DetailAsync);

        /// <summary>اجرای «جزئیات»</summary>
        private async Task DetailAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «جزئیات»
            await RunAsync(_ => Task.CompletedTask, "جزئیات…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<ShiftsRow> SampleData()
        {
            yield return new ShiftsRow { Code = "SH-۲۱۴", User = "مریم نوری", UserSub = "پذیرش", UserInitials = "من", UserAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Start = "۱۴۰۴/۰۷/۱۹ ۰۸:۰۰", End = "۱۴۰۴/۰۷/۱۹ ۱۴:۰۰", Receptions = 24, Total = 6200000m, Status = "فعال", StatusTone = "ok" };
            yield return new ShiftsRow { Code = "SH-۲۱۳", User = "احمد صادقی", UserSub = "پذیرش", UserInitials = "اص", UserAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Start = "۱۴۰۴/۰۷/۱۸ ۱۴:۰۰", End = "۱۴۰۴/۰۷/۱۸ ۲۰:۰۰", Receptions = 19, Total = 4850000m, Status = "بسته", StatusTone = "nu" };
            yield return new ShiftsRow { Code = "SH-۲۱۲", User = "مریم نوری", UserSub = "پذیرش", UserInitials = "من", UserAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Start = "۱۴۰۴/۰۷/۱۸ ۰۸:۰۰", End = "۱۴۰۴/۰۷/۱۸ ۱۴:۰۰", Receptions = 21, Total = 5400000m, Status = "بسته", StatusTone = "nu" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<ShiftsRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<ShiftsRow>(SampleData());
            return Task.FromResult<PagedResult<ShiftsRow>?>(
                new PagedResult<ShiftsRow>(items, items.Count));
        }
    }
}
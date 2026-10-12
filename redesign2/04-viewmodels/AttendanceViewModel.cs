// ═══════════════════════════════════════════════════════════════════════
//  AttendanceViewModel
//  صفحه‌ی «حضور و غیاب» · گروه «منابع انسانی» · کهن‌الگوی «list-dense»
//
//  متناظر با:  03-views/AttendanceView.xaml
//  وایرفریم:    01-wireframes-html/AttendanceView.html
//  پایه:        PageViewModelBase<AttendanceRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «حضور و غیاب»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class AttendanceRow
    {
        /// <summary>پرسنل — who</summary>
        public string? Employee { get; set; }
        /// <summary>زیرنویسِ «پرسنل» (مانند کد یا سن)</summary>
        public string? EmployeeSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «پرسنل»</summary>
        public string? EmployeeInitials { get; set; }
        /// <summary>رنگِ نشانِ «پرسنل» — از تم خوانده می‌شود</summary>
        public Brush? EmployeeAvatar { get; set; }

        /// <summary>تاریخ — date</summary>
        public string? Date { get; set; }

        /// <summary>ورود — date</summary>
        public string? In { get; set; }

        /// <summary>خروج — date</summary>
        public string? Out { get; set; }

        /// <summary>ساعت کار — num</summary>
        public string? Hours { get; set; }

        /// <summary>تأخیر — num</summary>
        public string? Delay { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «حضور و غیاب»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-dense</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: منابع انسانی
    /// </remarks>
    public sealed class AttendanceViewModel : PageViewModelBase<AttendanceRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public AttendanceViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("حاضر", "۲۹", null, Application.Current.TryFindResource("Icon.CircleCheck") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("تأخیر", "۳", null, Application.Current.TryFindResource("Icon.Clock") as Geometry, Application.Current.TryFindResource("Brush.Warning") as Brush));
            Kpis.Add(new KpiItem("غایب", "۲", null, Application.Current.TryFindResource("Icon.CircleX") as Geometry, Application.Current.TryFindResource("Brush.Danger") as Brush));
            Kpis.Add(new KpiItem("مرخصی", "۰", null, Application.Current.TryFindResource("Icon.CalendarX") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));

            // ارتباطات
            Relations.Add(new RelationItem("حضور و غیاب", "پرسنل", "تردد پرسنل"));
            Relations.Add(new RelationItem("حضور و غیاب", "حقوق و دستمزد", "مبنای اضافه‌کار"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

        /// <summary>فیلترِ «تاریخ»</summary>
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
        private object? _filter1 = "۱۴۰۴/۰۷/۱۹";

        /// <summary>فیلترِ «دپارتمان»</summary>
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

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>خروجی</summary>
        public IAsyncRelayCommand ExportCommand =>
            new AsyncRelayCommand(ExportAsync);

        /// <summary>اجرای «خروجی»</summary>
        private async Task ExportAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «خروجی»
            await RunAsync(_ => Task.CompletedTask, "خروجی…");
        }

        /// <summary>ثبت دستی</summary>
        public IAsyncRelayCommand ManualCommand =>
            new AsyncRelayCommand(ManualAsync);

        /// <summary>اجرای «ثبت دستی»</summary>
        private async Task ManualAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ثبت دستی»
            await RunAsync(_ => Task.CompletedTask, "ثبت دستی…");
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
        private static IEnumerable<AttendanceRow> SampleData()
        {
            yield return new AttendanceRow { Employee = "مریم نوری", EmployeeSub = "پذیرش", EmployeeInitials = "من", EmployeeAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Date = "۱۴۰۴/۰۷/۱۹", In = "۰۸:۰۲", Out = "۱۴:۰۵", Hours = "۶:۰۳", Delay = "۰", Status = "حاضر", StatusTone = "ok" };
            yield return new AttendanceRow { Employee = "احمد صادقی", EmployeeSub = "پذیرش", EmployeeInitials = "اص", EmployeeAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Date = "۱۴۰۴/۰۷/۱۹", In = "۰۸:۲۷", Out = "۱۴:۰۰", Hours = "۵:۳۳", Delay = "۲۷", Status = "تأخیر", StatusTone = "wn" };
            yield return new AttendanceRow { Employee = "لیلا حسنی", EmployeeSub = "مالی", EmployeeInitials = "لح", EmployeeAvatar = Application.Current.TryFindResource("Brush.Success") as Brush, Date = "۱۴۰۴/۰۷/۱۹", In = "—", Out = "—", Hours = "۰", Delay = "—", Status = "غایب", StatusTone = "dg" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<AttendanceRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<AttendanceRow>(SampleData());
            return Task.FromResult<PagedResult<AttendanceRow>?>(
                new PagedResult<AttendanceRow>(items, items.Count));
        }
    }
}
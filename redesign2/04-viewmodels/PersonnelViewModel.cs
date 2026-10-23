// ═══════════════════════════════════════════════════════════════════════
//  PersonnelViewModel
//  صفحه‌ی «پرسنل» · گروه «منابع انسانی» · کهن‌الگوی «list-detail»
//
//  متناظر با:  03-views/PersonnelView.xaml
//  وایرفریم:    01-wireframes-html/PersonnelView.html
//  پایه:        PageViewModelBase<PersonnelRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «پرسنل»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class PersonnelRow
    {
        /// <summary>کد — text</summary>
        public string? Code { get; set; }

        /// <summary>نام — who</summary>
        public string? Name { get; set; }
        /// <summary>زیرنویسِ «نام» (مانند کد یا سن)</summary>
        public string? NameSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «نام»</summary>
        public string? NameInitials { get; set; }
        /// <summary>رنگِ نشانِ «نام» — از تم خوانده می‌شود</summary>
        public Brush? NameAvatar { get; set; }

        /// <summary>سمت — text</summary>
        public string? Position { get; set; }

        /// <summary>دپارتمان — text</summary>
        public string? Department { get; set; }

        /// <summary>موبایل — num</summary>
        public string? Phone { get; set; }

        /// <summary>تاریخ استخدام — date</summary>
        public string? HireDate { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «پرسنل»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-detail</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: منابع انسانی
    /// </remarks>
    public sealed class PersonnelViewModel : PageViewModelBase<PersonnelRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public PersonnelViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("کل پرسنل", "۳۴", 2, Application.Current.TryFindResource("Icon.Users") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("دپارتمان‌ها", "۱۷", null, Application.Current.TryFindResource("Icon.Briefcase") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));
            Kpis.Add(new KpiItem("فعال", "۳۲", null, Application.Current.TryFindResource("Icon.CircleCheck") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("غیرفعال", "۲", null, Application.Current.TryFindResource("Icon.CircleX") as Geometry, Application.Current.TryFindResource("Brush.Danger") as Brush));

            // ارتباطات
            Relations.Add(new RelationItem("پرسنل", "فرم کارمند", "ایجاد/ویرایش"));
            Relations.Add(new RelationItem("پرسنل", "حضور و غیاب", "ساعات کاری"));
            Relations.Add(new RelationItem("پرسنل", "حقوق و دستمزد", "پرداخت"));
            Relations.Add(new RelationItem("پرسنل", "پزشکان", "پزشک از میان پرسنل"));

            // خلاصه‌ی کناری
            Summary.Add(new KeyValueItem("کد پرسنلی", "E-۱۰۰۱"));
            Summary.Add(new KeyValueItem("نام", "مریم نوری"));
            Summary.Add(new KeyValueItem("کد ملی", "۰۰۹-۸۷۶۵۴۳-۲"));
            Summary.Add(new KeyValueItem("سمت", "کارشناس پذیرش"));
            Summary.Add(new KeyValueItem("دپارتمان", "پذیرش"));
            Summary.Add(new KeyValueItem("موبایل", "۰۹۱۴۴۲۲۰۴۳۰"));
            Summary.Add(new KeyValueItem("تاریخ استخدام", "۱۴۰۱/۰۲/۱۵"));
            Summary.Add(new KeyValueItem("حقوق پایه", "۱۲,۰۰۰,۰۰۰"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

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
        private object? _filter3 = "فعال";

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        /// <summary>خلاصه‌ی کناریِ صفحه</summary>
        public ObservableCollection<KeyValueItem> Summary { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>پرسنل جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «پرسنل جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «پرسنل جدید»
            await RunAsync(_ => Task.CompletedTask, "پرسنل جدید…");
        }

        /// <summary>ویرایش حقوق</summary>
        public IAsyncRelayCommand SalaryCommand =>
            new AsyncRelayCommand(SalaryAsync);

        /// <summary>اجرای «ویرایش حقوق»</summary>
        private async Task SalaryAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ویرایش حقوق»
            await RunAsync(_ => Task.CompletedTask, "ویرایش حقوق…");
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
        private static IEnumerable<PersonnelRow> SampleData()
        {
            yield return new PersonnelRow { Code = "E-۱۰۰۱", Name = "مریم نوری", NameSub = "کارشناس پذیرش", NameInitials = "من", NameAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Position = "کارشناس پذیرش", Department = "پذیرش", Phone = "۰۹۱۴۴۲۲۰۴۳۰", HireDate = "۱۴۰۱/۰۲/۱۵", Status = "فعال", StatusTone = "ok" };
            yield return new PersonnelRow { Code = "E-۱۰۰۲", Name = "احمد صادقی", NameSub = "مسئول پذیرش", NameInitials = "اص", NameAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Position = "مسئول پذیرش", Department = "پذیرش", Phone = "۰۹۱۴۴۲۲۰۴۳۰", HireDate = "۱۴۰۰/۰۸/۰۱", Status = "فعال", StatusTone = "ok" };
            yield return new PersonnelRow { Code = "E-۱۰۰۳", Name = "دکتر موسوی", NameSub = "متخصص داخلی", NameInitials = "دم", NameAvatar = Application.Current.TryFindResource("Brush.Warning") as Brush, Position = "متخصص داخلی", Department = "درمان", Phone = "۰۹۱۴۴۲۲۰۴۳۰", HireDate = "۱۳۹۸/۰۴/۱۰", Status = "فعال", StatusTone = "ok" };
            yield return new PersonnelRow { Code = "E-۱۰۰۴", Name = "لیلا حسنی", NameSub = "حسابدار", NameInitials = "لح", NameAvatar = Application.Current.TryFindResource("Brush.Success") as Brush, Position = "حسابدار", Department = "مالی", Phone = "۰۹۱۴۴۲۲۰۴۳۰", HireDate = "۱۴۰۲/۰۱/۲۰", Status = "فعال", StatusTone = "ok" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<PersonnelRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<PersonnelRow>(SampleData());
            return Task.FromResult<PagedResult<PersonnelRow>?>(
                new PagedResult<PersonnelRow>(items, items.Count));
        }
    }
}
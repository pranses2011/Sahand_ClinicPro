// ═══════════════════════════════════════════════════════════════════════
//  PatientsViewModel
//  صفحه‌ی «بیماران» · گروه «بیماران» · کهن‌الگوی «list-detail»
//
//  متناظر با:  03-views/PatientsView.xaml
//  وایرفریم:    01-wireframes-html/PatientsView.html
//  پایه:        PageViewModelBase<PatientsRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «بیماران»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class PatientsRow
    {
        /// <summary>کد پرونده — text</summary>
        public string? Code { get; set; }

        /// <summary>نام بیمار — who</summary>
        public string? FullName { get; set; }
        /// <summary>زیرنویسِ «نام بیمار» (مانند کد یا سن)</summary>
        public string? FullNameSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «نام بیمار»</summary>
        public string? FullNameInitials { get; set; }
        /// <summary>رنگِ نشانِ «نام بیمار» — از تم خوانده می‌شود</summary>
        public Brush? FullNameAvatar { get; set; }

        /// <summary>کد ملی — num</summary>
        public string? NationalId { get; set; }

        /// <summary>موبایل — num</summary>
        public string? Phone { get; set; }

        /// <summary>سن — num</summary>
        public int Age { get; set; }

        /// <summary>جنسیت — text</summary>
        public string? Gender { get; set; }

        /// <summary>آخرین ویزیت — date</summary>
        public string? LastVisit { get; set; }

        /// <summary>مانده — money</summary>
        public decimal Balance { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «بیماران»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-detail</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: بیماران
    /// </remarks>
    public sealed class PatientsViewModel : PageViewModelBase<PatientsRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public PatientsViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("کل بیماران", "۲,۸۴۱", 3, Application.Current.TryFindResource("Icon.Users") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("جدید این ماه", "۱۲۷", 18, Application.Current.TryFindResource("Icon.UserPlus") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("پرونده فعال", "۲,۶۰۳", null, Application.Current.TryFindResource("Icon.CircleCheck") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));
            Kpis.Add(new KpiItem("بایگانی‌شده", "۲۳۸", -2, Application.Current.TryFindResource("Icon.Archive") as Geometry, Application.Current.TryFindResource("Brush.Warning") as Brush));

            // ارتباطات
            Relations.Add(new RelationItem("بیماران", "پرونده‌ی بیمار", "هر ردیف به پرونده می‌رود"));
            Relations.Add(new RelationItem("بیماران", "پذیرش و صف", "بیمار پذیرش می‌شود"));
            Relations.Add(new RelationItem("بیماران", "نوبت‌دهی", "نوبت برای بیمار ثبت می‌شود"));
            Relations.Add(new RelationItem("بیماران", "صورتحساب", "مانده حساب از فاکتورها"));

            // خلاصه‌ی کناری
            Summary.Add(new KeyValueItem("کد پرونده", "P-۱۰۲۴"));
            Summary.Add(new KeyValueItem("نام", "سارا احمدی"));
            Summary.Add(new KeyValueItem("کد ملی", "۰۰۱-۲۳۴۵۶۷-۸"));
            Summary.Add(new KeyValueItem("سن/جنسیت", "۴۲ سال · زن"));
            Summary.Add(new KeyValueItem("بیمه", "تأمین اجتماعی"));
            Summary.Add(new KeyValueItem("موبایل", "۰۹۱۴-۴۲۲-۰۸۷۱"));
            Summary.Add(new KeyValueItem("آخرین ویزیت", "۱۴۰۴/۰۷/۱۲"));
            Summary.Add(new KeyValueItem("مانده حساب", "۰ تومان"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

        /// <summary>فیلترِ «شعبه»</summary>
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
        private object? _filter2 = "همه شعب";

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

        /// <summary>فیلترِ «جنسیت»</summary>
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
        private object? _filter4 = "همه";

        /// <summary>فیلترِ «از تاریخ»</summary>
        public object? Filter5
        {
            get => _filter5;
            set
            {
                if (SetProperty(ref _filter5, value))
                {
                    PageNumber = 1;
                    _ = LoadAsync();
                }
            }
        }
        private object? _filter5 = "۱۴۰۴/۰۱/۰۱";

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        /// <summary>خلاصه‌ی کناریِ صفحه</summary>
        public ObservableCollection<KeyValueItem> Summary { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>بیمار جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «بیمار جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «بیمار جدید»
            await RunAsync(_ => Task.CompletedTask, "بیمار جدید…");
        }

        /// <summary>خروجی Excel</summary>
        public IAsyncRelayCommand ExportCommand =>
            new AsyncRelayCommand(ExportAsync);

        /// <summary>اجرای «خروجی Excel»</summary>
        private async Task ExportAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «خروجی Excel»
            await RunAsync(_ => Task.CompletedTask, "خروجی Excel…");
        }

        /// <summary>چاپ کارت</summary>
        public IAsyncRelayCommand PrintCommand =>
            new AsyncRelayCommand(PrintAsync);

        /// <summary>اجرای «چاپ کارت»</summary>
        private async Task PrintAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «چاپ کارت»
            await RunAsync(_ => Task.CompletedTask, "چاپ کارت…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<PatientsRow> SampleData()
        {
            yield return new PatientsRow { Code = "P-۱۰۲۴", FullName = "سارا احمدی", FullNameSub = "۴۲ سال · زن", FullNameInitials = "سا", FullNameAvatar = Application.Current.TryFindResource("Brush.Info") as Brush, NationalId = "۰۰۱-۲۳۴۵۶۷-۸", Phone = "۰۹۱۴-۴۲۲-۰۸۷۱", Age = 42, Gender = "زن", LastVisit = "۱۴۰۴/۰۷/۱۲", Balance = 0m, Status = "فعال", StatusTone = "ok" };
            yield return new PatientsRow { Code = "P-۱۰۲۵", FullName = "محمد رضایی", FullNameSub = "۳۵ سال · مرد", FullNameInitials = "مر", FullNameAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, NationalId = "۰۰۲-۳۴۵۶۷۸-۹", Phone = "۰۹۱۲-۳۳۵-۱۲۹۰", Age = 35, Gender = "مرد", LastVisit = "۱۴۰۴/۰۷/۱۰", Balance = 180000m, Status = "فعال", StatusTone = "ok" };
            yield return new PatientsRow { Code = "P-۱۰۲۶", FullName = "فاطمه کریمی", FullNameSub = "۲۸ سال · زن", FullNameInitials = "فک", FullNameAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, NationalId = "۰۰۳-۴۵۶۷۸۹-۰", Phone = "۰۹۳۸-۱۱۴-۲۳۴۵", Age = 28, Gender = "زن", LastVisit = "۱۴۰۴/۰۶/۲۸", Balance = 0m, Status = "فعال", StatusTone = "ok" };
            yield return new PatientsRow { Code = "P-۱۰۲۷", FullName = "علی حسینی", FullNameSub = "۶۱ سال · مرد", FullNameInitials = "اح", FullNameAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, NationalId = "۰۰۴-۵۶۷۸۹۰-۱", Phone = "۰۹۱۱-۷۷۸-۴۵۱۲", Age = 61, Gender = "مرد", LastVisit = "۱۴۰۴/۰۵/۱۴", Balance = 0m, Status = "بایگانی", StatusTone = "nu" };
            yield return new PatientsRow { Code = "P-۱۰۲۸", FullName = "زهرا موسوی", FullNameSub = "۱۹ سال · زن", FullNameInitials = "زم", FullNameAvatar = Application.Current.TryFindResource("Brush.Warning") as Brush, NationalId = "۰۰۵-۶۷۸۹۰۱-۲", Phone = "۰۹۳۵-۲۲۳-۹۰۱۴", Age = 19, Gender = "زن", LastVisit = "۱۴۰۴/۰۷/۱۸", Balance = 45000m, Status = "فعال", StatusTone = "ok" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<PatientsRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<PatientsRow>(SampleData());
            return Task.FromResult<PagedResult<PatientsRow>?>(
                new PagedResult<PatientsRow>(items, items.Count));
        }
    }
}
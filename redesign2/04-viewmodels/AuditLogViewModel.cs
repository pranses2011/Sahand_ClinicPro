// ═══════════════════════════════════════════════════════════════════════
//  AuditLogViewModel
//  صفحه‌ی «گزارش حسابرسی» · گروه «گزارشات» · کهن‌الگوی «list-dense»
//
//  متناظر با:  03-views/AuditLogView.xaml
//  وایرفریم:    01-wireframes-html/AuditLogView.html
//  پایه:        PageViewModelBase<AuditLogRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «گزارش حسابرسی»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class AuditLogRow
    {
        /// <summary>زمان — date</summary>
        public string? Time { get; set; }

        /// <summary>کاربر — who</summary>
        public string? User { get; set; }
        /// <summary>زیرنویسِ «کاربر» (مانند کد یا سن)</summary>
        public string? UserSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «کاربر»</summary>
        public string? UserInitials { get; set; }
        /// <summary>رنگِ نشانِ «کاربر» — از تم خوانده می‌شود</summary>
        public Brush? UserAvatar { get; set; }

        /// <summary>عملیات — chip</summary>
        public string? Action { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «عملیات»</summary>
        public string ActionTone { get; set; } = "nu";

        /// <summary>موجودیت — text</summary>
        public string? Entity { get; set; }

        /// <summary>توضیح — text</summary>
        public string? Desc { get; set; }

        /// <summary>IP — num</summary>
        public string? Ip { get; set; }
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «گزارش حسابرسی»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-dense</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: گزارشات
    /// </remarks>
    public sealed class AuditLogViewModel : PageViewModelBase<AuditLogRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public AuditLogViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("رویداد امروز", "۱,۲۴۸", null, Application.Current.TryFindResource("Icon.History") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("ویرایش‌ها", "۳۴۲", null, Application.Current.TryFindResource("Icon.Pencil") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));
            Kpis.Add(new KpiItem("حذف‌ها", "۷", null, Application.Current.TryFindResource("Icon.Trash2") as Geometry, Application.Current.TryFindResource("Brush.Danger") as Brush));
            Kpis.Add(new KpiItem("ورودها", "۴۶", null, Application.Current.TryFindResource("Icon.LogIn") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));

            // ارتباطات
            Relations.Add(new RelationItem("گزارش حسابرسی", "کاربران", "عامل عملیات"));
            Relations.Add(new RelationItem("گزارش حسابرسی", "مدیریت سیستم", "بخشی از مدیریت"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

        /// <summary>فیلترِ «عملیات»</summary>
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
        private object? _filter3 = "۱۴۰۴/۰۷/۱۹";

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

        /// <summary>فیلتر پیشرفته</summary>
        public IAsyncRelayCommand FilterCommand =>
            new AsyncRelayCommand(FilterAsync);

        /// <summary>اجرای «فیلتر پیشرفته»</summary>
        private async Task FilterAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «فیلتر پیشرفته»
            await RunAsync(_ => Task.CompletedTask, "فیلتر پیشرفته…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<AuditLogRow> SampleData()
        {
            yield return new AuditLogRow { Time = "۱۴۰۴/۰۷/۱۹ ۰۹:۴۲:۱۱", User = "mnouri", UserSub = "مریم نوری", UserInitials = "من", UserAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Action = "ایجاد", ActionTone = "ok", Entity = "Reception", Desc = "پذیرش جدید — سارا احمدی", Ip = "۱۹۲٫۱۶۸٫۱٫۲۴" };
            yield return new AuditLogRow { Time = "۱۴۰۴/۰۷/۱۹ ۰۹:۳۸:۰۵", User = "admin", UserSub = "سعید نجفی", UserInitials = "عا", UserAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Action = "ویرایش", ActionTone = "in", Entity = "Tariff", Desc = "تغییر تعرفه ویزیت عمومی", Ip = "۱۹۲٫۱۶۸٫۱٫۱۰" };
            yield return new AuditLogRow { Time = "۱۴۰۴/۰۷/۱۹ ۰۹:۳۰:۵۲", User = "lhasani", UserSub = "لیلا حسنی", UserInitials = "لح", UserAvatar = Application.Current.TryFindResource("Brush.Success") as Brush, Action = "حذف", ActionTone = "dg", Entity = "Invoice", Desc = "حذف فاکتور INV-۹۵۰۱", Ip = "۱۹۲٫۱۶۸٫۱٫۱۸" };
            yield return new AuditLogRow { Time = "۱۴۰۴/۰۷/۱۹ ۰۸:۱۲:۰۰", User = "admin", UserSub = "سعید نجفی", UserInitials = "عا", UserAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Action = "ورود", ActionTone = "cy", Entity = "Auth", Desc = "ورود موفق", Ip = "۱۹۲٫۱۶۸٫۱٫۱۰" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<AuditLogRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<AuditLogRow>(SampleData());
            return Task.FromResult<PagedResult<AuditLogRow>?>(
                new PagedResult<AuditLogRow>(items, items.Count));
        }
    }
}
// ═══════════════════════════════════════════════════════════════════════
//  ShiftCloseViewModel
//  صفحه‌ی «بستن شیفت» · گروه «پذیرش» · کهن‌الگوی «form-std»
//
//  متناظر با:  03-views/ShiftCloseWindow.xaml
//  وایرفریم:    01-wireframes-html/ShiftCloseWindow.html
//  پایه:        FormViewModelBase
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
    /// <summary>
    /// ViewModelِ صفحه‌ی «بستن شیفت»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-std</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: پذیرش
    /// </remarks>
    public sealed class ShiftCloseViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public ShiftCloseViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("دریافتی نقد", "۳,۲۰۰,۰۰۰", null, Application.Current.TryFindResource("Icon.HandCoins") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("کارت‌خوان", "۲,۴۰۰,۰۰۰", null, Application.Current.TryFindResource("Icon.CreditCard") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));
            Kpis.Add(new KpiItem("سهم بیمه", "۶۰۰,۰۰۰", null, Application.Current.TryFindResource("Icon.ShieldCheck") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("جمع کل", "۶,۲۰۰,۰۰۰", null, Application.Current.TryFindResource("Icon.Banknote") as Geometry, Application.Current.TryFindResource("Brush.Warning") as Brush));

            // ارتباطات
            Relations.Add(new RelationItem("بستن شیفت", "پیش‌نمایش رسید شیفت", "رسید چاپ می‌شود"));
            Relations.Add(new RelationItem("بستن شیفت", "حسابداری", "سند حسابداری صادر می‌شود"));
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>موجودی شمارش‌شده — الزامی</summary>
        public decimal CashCounted
        {
            get => _cashCounted;
            set
            {
                if (SetProperty(ref _cashCounted, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private decimal _cashCounted = 3150000m;

        /// <summary>موجودی مورد انتظار</summary>
        public decimal CashExpected
        {
            get => _cashExpected;
            set
            {
                if (SetProperty(ref _cashExpected, value))
                {
                    MarkDirty();
                }
            }
        }
        private decimal _cashExpected = 3200000m;

        /// <summary>مغایرت</summary>
        public decimal Difference
        {
            get => _difference;
            set
            {
                if (SetProperty(ref _difference, value))
                {
                    MarkDirty();
                }
            }
        }
        private decimal _difference = 0m;

        /// <summary>علت مغایرت</summary>
        public string Reason
        {
            get => _reason;
            set
            {
                if (SetProperty(ref _reason, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _reason = "پرداخت خردِ ثبت‌نشده در پایان شیفت";

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>بستن شیفت</summary>
        public IAsyncRelayCommand CloseCommand =>
            new AsyncRelayCommand(CloseAsync);

        /// <summary>اجرای «بستن شیفت»</summary>
        private async Task CloseAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «بستن شیفت»
            await RunAsync(_ => Task.CompletedTask, "بستن شیفت…");
        }

        /// <summary>چاپ رسید</summary>
        public IAsyncRelayCommand PrintCommand =>
            new AsyncRelayCommand(PrintAsync);

        /// <summary>اجرای «چاپ رسید»</summary>
        private async Task PrintAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «چاپ رسید»
            await RunAsync(_ => Task.CompletedTask, "چاپ رسید…");
        }

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

            if (CashCounted <= 0)
            {
                AddError("موجودی شمارش‌شده الزامی است", nameof(CashCounted));
            }
            else
            {
                ClearErrors(nameof(CashCounted));
            }
            return ok && !HasErrors;
        }

        /// <summary>
        /// ذخیره‌ی داده‌ها. در صورت نبودِ سرویس، فقط وضعیت موفقیت نمایش
        /// داده می‌شود تا رفتارِ فرم قابلِ بررسی باشد.
        /// </summary>
        protected override Task PersistAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی
            return Task.CompletedTask;
        }

        /// <summary>جستجو در یک فیلدِ جستجومحور</summary>
        private Task LookupAsync(string field)
            => RunAsync(_ => Task.CompletedTask, $"جستجوی {field}…");

        /// <summary>خواندنِ بارکد برای یک فیلد</summary>
        private void ScanBarcode(string field)
            => ShowSuccess($"بارکدِ {field} خوانده شد");
    }
}
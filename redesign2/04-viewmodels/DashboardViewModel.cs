// ═══════════════════════════════════════════════════════════════════════
//  DashboardViewModel
//  صفحه‌ی «پیشخوان» · گروه «پیشخوان» · کهن‌الگوی «dashboard»
//
//  متناظر با:  03-views/DashboardView.xaml
//  وایرفریم:    01-wireframes-html/DashboardView.html
//  پایه:        ViewModelBase
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
    /// ViewModelِ صفحه‌ی «پیشخوان»
    /// </summary>
    /// <remarks>
    /// پنج کارت شاخص در بالا، «صف انتظار زنده» در ستون راست، نمودار درآمد
    /// ۳۰ روزه و نمودار توزیع ویزیت در وسط، و فید فعالیّت‌های اخیر در
    /// پایین.
    ///
    /// کهن‌الگوی طراحی: <c>dashboard</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: پیشخوان
    /// </remarks>
    public sealed class DashboardViewModel : ViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public DashboardViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("بیماران امروز", "۴۸", 12, Application.Current.TryFindResource("Icon.Users") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("نوبت‌های امروز", "۳۶", 8, Application.Current.TryFindResource("Icon.CalendarCheck") as Geometry, Application.Current.TryFindResource("Brush.Info") as Brush));
            Kpis.Add(new KpiItem("درآمد امروز", "۲۸,۴۵۰,۰۰۰", 23, Application.Current.TryFindResource("Icon.Banknote") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("میانگین انتظار", "۱۴ دقیقه", -6, Application.Current.TryFindResource("Icon.Timer") as Geometry, Application.Current.TryFindResource("Brush.Warning") as Brush));
            Kpis.Add(new KpiItem("هشدارهای فعال", "۳", null, Application.Current.TryFindResource("Icon.TriangleAlert") as Geometry, Application.Current.TryFindResource("Brush.Danger") as Brush));

            // ارتباطات
            Relations.Add(new RelationItem("پیشخوان", "پذیرش و صف", "پذیرش جدید می‌سازد"));
            Relations.Add(new RelationItem("پیشخوان", "نوبت‌دهی", "نوبت امروز را نشان می‌دهد"));
            Relations.Add(new RelationItem("پیشخوان", "صورتحساب", "درآمد از فاکتورها می‌آید"));
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

        /// <summary>نمودارِ درآمد ۳۰ روزه — در پیاده‌سازی جایگزین می‌شود</summary>
        public object? RevenueChart
{
            get => _revenueChart;
            set => SetProperty(ref _revenueChart, value);
        }
        private object? _revenueChart;

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>پذیرش جدید</summary>
        public IAsyncRelayCommand NewReceptionCommand =>
            new AsyncRelayCommand(NewReceptionAsync);

        /// <summary>اجرای «پذیرش جدید»</summary>
        private async Task NewReceptionAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «پذیرش جدید»
            await RunAsync(_ => Task.CompletedTask, "پذیرش جدید…");
        }

        /// <summary>نوبت جدید</summary>
        public IAsyncRelayCommand NewAppointmentCommand =>
            new AsyncRelayCommand(NewAppointmentAsync);

        /// <summary>اجرای «نوبت جدید»</summary>
        private async Task NewAppointmentAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «نوبت جدید»
            await RunAsync(_ => Task.CompletedTask, "نوبت جدید…");
        }
    }
}
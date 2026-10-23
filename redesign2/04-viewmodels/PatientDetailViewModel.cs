// ═══════════════════════════════════════════════════════════════════════
//  PatientDetailViewModel
//  صفحه‌ی «پرونده‌ی بیمار» · گروه «بیماران» · کهن‌الگوی «detail-timeline»
//
//  متناظر با:  03-views/PatientDetailWindow.xaml
//  وایرفریم:    01-wireframes-html/PatientDetailWindow.html
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
    /// ViewModelِ صفحه‌ی «پرونده‌ی بیمار»
    /// </summary>
    /// <remarks>
    /// سربرگ با کارتِ هویت، آمار و اطلاعات تماس؛ سپس تب‌ها؛ تب نخست «خط
    /// زمان درمان» رویدادها را به ترتیب زمان نشان می‌دهد.
    ///
    /// کهن‌الگوی طراحی: <c>detail-timeline</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: بیماران
    /// </remarks>
    public sealed class PatientDetailViewModel : ViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public PatientDetailViewModel(ILogger? logger = null) : base(logger)
        {
            // تب‌ها
            Tabs.Add(new TabItemModel("خط زمان درمان", Application.Current.TryFindResource("Icon.History") as Geometry, 8));
            TabContents.Add(null);  // محتوای تب «خط زمان درمان»
            Tabs.Add(new TabItemModel("ویزیت‌ها", Application.Current.TryFindResource("Icon.Stethoscope") as Geometry, 18));
            TabContents.Add(null);  // محتوای تب «ویزیت‌ها»
            Tabs.Add(new TabItemModel("نوبت‌ها", Application.Current.TryFindResource("Icon.CalendarDays") as Geometry, 24));
            TabContents.Add(null);  // محتوای تب «نوبت‌ها»
            Tabs.Add(new TabItemModel("نسخه‌ها", Application.Current.TryFindResource("Icon.FileText") as Geometry, 11));
            TabContents.Add(null);  // محتوای تب «نسخه‌ها»
            Tabs.Add(new TabItemModel("آزمایش‌ها", Application.Current.TryFindResource("Icon.FlaskConical") as Geometry, 6));
            TabContents.Add(null);  // محتوای تب «آزمایش‌ها»
            Tabs.Add(new TabItemModel("تصویربرداری", Application.Current.TryFindResource("Icon.ScanLine") as Geometry, 2));
            TabContents.Add(null);  // محتوای تب «تصویربرداری»
            Tabs.Add(new TabItemModel("صورتحساب‌ها", Application.Current.TryFindResource("Icon.Receipt") as Geometry, 9));
            TabContents.Add(null);  // محتوای تب «صورتحساب‌ها»
            Tabs.Add(new TabItemModel("پیوست‌ها", Application.Current.TryFindResource("Icon.Paperclip") as Geometry, 3));
            TabContents.Add(null);  // محتوای تب «پیوست‌ها»

            // ارتباطات
            Relations.Add(new RelationItem("پرونده بیمار", "ویزیت (SOAP)", "هر ویزیت در خط زمان"));
            Relations.Add(new RelationItem("پرونده بیمار", "صورتحساب", "مانده از فاکتورها"));
            Relations.Add(new RelationItem("پرونده بیمار", "آزمایشگاه", "نتایج آزمایش"));

            // سربرگِ برجسته
            Hero = new HeroModel("سارا احمدی", "کد پرونده P-۱۰۲۴ · ۴۲ سال · زن", "سا");
        }

        /// <summary>محتوای تب‌ها</summary>
        public ObservableCollection<TabItemModel> Tabs { get; } = new();

        /// <summary>محتوای تب‌ها — هر اندیس متناظر با یک تب در View است</summary>
        public ObservableCollection<object?> TabContents { get; } = new();

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        /// <summary>رخدادهای خطِ زمان</summary>
        public ObservableCollection<TimelineItem> Timeline { get; } = new();

        /// <summary>سربرگِ برجسته</summary>
        public HeroModel? Hero
        {
            get => _hero;
            set => SetProperty(ref _hero, value);
        }
        private HeroModel? _hero;

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>ویزیت جدید</summary>
        public IAsyncRelayCommand NewVisitCommand =>
            new AsyncRelayCommand(NewVisitAsync);

        /// <summary>اجرای «ویزیت جدید»</summary>
        private async Task NewVisitAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ویزیت جدید»
            await RunAsync(_ => Task.CompletedTask, "ویزیت جدید…");
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

        /// <summary>چاپ پرونده</summary>
        public IAsyncRelayCommand PrintCommand =>
            new AsyncRelayCommand(PrintAsync);

        /// <summary>اجرای «چاپ پرونده»</summary>
        private async Task PrintAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «چاپ پرونده»
            await RunAsync(_ => Task.CompletedTask, "چاپ پرونده…");
        }
    }
}
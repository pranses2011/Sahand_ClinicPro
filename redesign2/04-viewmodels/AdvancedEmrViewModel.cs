// ═══════════════════════════════════════════════════════════════════════
//  AdvancedEmrViewModel
//  صفحه‌ی «پرونده بالینی پیشرفته» · گروه «بیماران» · کهن‌الگوی «detail-tabs»
//
//  متناظر با:  03-views/AdvancedEmrView.xaml
//  وایرفریم:    01-wireframes-html/AdvancedEmrView.html
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
    /// ViewModelِ صفحه‌ی «پرونده بالینی پیشرفته»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>detail-tabs</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: بیماران
    /// </remarks>
    public sealed class AdvancedEmrViewModel : ViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public AdvancedEmrViewModel(ILogger? logger = null) : base(logger)
        {
            // تب‌ها
            Tabs.Add(new TabItemModel("شرح‌حال", Application.Current.TryFindResource("Icon.ClipboardList") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «شرح‌حال»
            Tabs.Add(new TabItemModel("معاینه فیزیکی", Application.Current.TryFindResource("Icon.Stethoscope") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «معاینه فیزیکی»
            Tabs.Add(new TabItemModel("تشخیص‌ها", Application.Current.TryFindResource("Icon.CircleCheck") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «تشخیص‌ها»
            Tabs.Add(new TabItemModel("داروهای جاری", Application.Current.TryFindResource("Icon.Pill") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «داروهای جاری»
            Tabs.Add(new TabItemModel("یادداشت‌ها", Application.Current.TryFindResource("Icon.NotebookPen") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «یادداشت‌ها»
            Tabs.Add(new TabItemModel("پیگیری‌ها", Application.Current.TryFindResource("Icon.CalendarClock") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «پیگیری‌ها»

            // ارتباطات
            Relations.Add(new RelationItem("پرونده بالینی", "ویزیت (SOAP)", "بخشی از ویزیت"));
            Relations.Add(new RelationItem("پرونده بالینی", "پرونده‌ی بیمار", "زیرمجموعه پرونده"));
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

        /// <summary>شکایت اصلی — الزامی</summary>
        public string ChiefComplaint
        {
            get => _chiefComplaint;
            set
            {
                if (SetProperty(ref _chiefComplaint, value))
                {
                    Validate();
                }
            }
        }
        private string _chiefComplaint = "درد شکمی از ۳ روز پیش";

        /// <summary>مدت زمان</summary>
        public string Duration
        {
            get => _duration;
            set
            {
                if (SetProperty(ref _duration, value))
                {
                }
            }
        }
        private string _duration = "۳ روز";

        /// <summary>شرح بیماری فعلی</summary>
        public string HistoryPresent
        {
            get => _historyPresent;
            set
            {
                if (SetProperty(ref _historyPresent, value))
                {
                }
            }
        }
        private string _historyPresent = "درد مبهم در ناحیه اپی‌گاستر که پس از غذا تشدید می‌شود…";

        /// <summary>سابقه بیماری‌ها</summary>
        public string PastMedical
        {
            get => _pastMedical;
            set
            {
                if (SetProperty(ref _pastMedical, value))
                {
                }
            }
        }
        private string _pastMedical = "دیابت نوع ۲ از ۵ سال پیش";

        /// <summary>سابقه دارویی</summary>
        public string DrugHistory
        {
            get => _drugHistory;
            set
            {
                if (SetProperty(ref _drugHistory, value))
                {
                }
            }
        }
        private string _drugHistory = "متفورمین ۵۰۰";

        /// <summary>سابقه خانوادگی</summary>
        public string FamilyHistory
        {
            get => _familyHistory;
            set
            {
                if (SetProperty(ref _familyHistory, value))
                {
                }
            }
        }
        private string _familyHistory = "پدر: فشار خون";

        /// <summary>سابقه اجتماعی</summary>
        public string SocialHistory
        {
            get => _socialHistory;
            set
            {
                if (SetProperty(ref _socialHistory, value))
                {
                }
            }
        }
        private string _socialHistory = "غیرسیگاری";

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>پیش‌نویس</summary>
        public IAsyncRelayCommand SaveDraftCommand =>
            new AsyncRelayCommand(SaveDraftAsync);

        /// <summary>اجرای «پیش‌نویس»</summary>
        private async Task SaveDraftAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «پیش‌نویس»
            await RunAsync(_ => Task.CompletedTask, "پیش‌نویس…");
        }
    }
}
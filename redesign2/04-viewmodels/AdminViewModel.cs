// ═══════════════════════════════════════════════════════════════════════
//  AdminViewModel
//  صفحه‌ی «مدیریت سیستم» · گروه «منابع انسانی» · کهن‌الگوی «settings»
//
//  متناظر با:  03-views/AdminView.xaml
//  وایرفریم:    01-wireframes-html/AdminView.html
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
    /// ViewModelِ صفحه‌ی «مدیریت سیستم»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>settings</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: منابع انسانی
    /// </remarks>
    public sealed class AdminViewModel : ViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public AdminViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("مدیریت سیستم", "نقش‌ها و مجوزها", "مدیریت دسترسی"));
            Relations.Add(new RelationItem("مدیریت سیستم", "گزارش حسابرسی", "ردپای عملیات"));

            // بخش‌های تنظیمات
            Sections.Add("عملیات");
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        /// <summary>بخش‌های تنظیمات</summary>
        public ObservableCollection<string> Sections { get; } = new();

        /// <summary>بخشِ انتخاب‌شده</summary>
        public string? SelectedSection
        {
            get => _selectedSection;
            set => SetProperty(ref _selectedSection, value);
        }
        private string? _selectedSection;

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>پاک‌سازی حافظه پنهان</summary>
        public bool ClearCache
        {
            get => _clearCache;
            set
            {
                if (SetProperty(ref _clearCache, value))
                {
                }
            }
        }
        private bool _clearCache = false;

        /// <summary>بازسازی نمایه‌ها</summary>
        public bool Reindex
        {
            get => _reindex;
            set
            {
                if (SetProperty(ref _reindex, value))
                {
                }
            }
        }
        private bool _reindex = false;

        /// <summary>حالت نگهداری</summary>
        public bool MaintenanceMode
        {
            get => _maintenanceMode;
            set
            {
                if (SetProperty(ref _maintenanceMode, value))
                {
                }
            }
        }
        private bool _maintenanceMode = false;

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>اجرا</summary>
        public IAsyncRelayCommand RunCommand =>
            new AsyncRelayCommand(RunAsync);

        /// <summary>اجرای «اجرا»</summary>
        private async Task RunAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «اجرا»
            await RunAsync(_ => Task.CompletedTask, "اجرا…");
        }
    }
}
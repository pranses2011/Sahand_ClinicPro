// ═══════════════════════════════════════════════════════════════════════
//  AdhocReportViewModel
//  صفحه‌ی «گزارش‌ساز پویا» · گروه «گزارشات» · کهن‌الگوی «report»
//
//  متناظر با:  03-views/AdhocReportWindow.xaml
//  وایرفریم:    01-wireframes-html/AdhocReportWindow.html
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
    /// ViewModelِ صفحه‌ی «گزارش‌ساز پویا»
    /// </summary>
    /// <remarks>
    /// پنج مرحله با نوار پیشرفت در بالا؛ پیش‌نمایشِ زنده در پایین با
    /// به‌روزرسانیِ خودکار هنگام تغییر تنظیمات.
    ///
    /// کهن‌الگوی طراحی: <c>report</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: گزارشات
    /// </remarks>
    public sealed class AdhocReportViewModel : ViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public AdhocReportViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("گزارش‌ساز پویا", "مرورگر گزارشات", "ذخیره در کتابخانه"));
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>موجودیت — الزامی</summary>
        public string Entity
        {
            get => _entity;
            set
            {
                if (SetProperty(ref _entity, value))
                {
                    Validate();
                }
            }
        }
        private string _entity = "ویزیت‌ها";

        /// <summary>ستون‌ها — الزامی</summary>
        public string Columns
        {
            get => _columns;
            set
            {
                if (SetProperty(ref _columns, value))
                {
                    Validate();
                }
            }
        }
        private string _columns = "تاریخ، پزشک، مبلغ";

        /// <summary>گروه‌بندی</summary>
        public string GroupBy
        {
            get => _groupBy;
            set
            {
                if (SetProperty(ref _groupBy, value))
                {
                }
            }
        }
        private string _groupBy = "پزشک";

        /// <summary>مرتب‌سازی</summary>
        public string SortBy
        {
            get => _sortBy;
            set
            {
                if (SetProperty(ref _sortBy, value))
                {
                }
            }
        }
        private string _sortBy = "تاریخ (نزولی)";

        /// <summary>از تاریخ</summary>
        public DateTime? FromDate
        {
            get => _fromDate;
            set
            {
                if (SetProperty(ref _fromDate, value))
                {
                }
            }
        }
        private DateTime? _fromDate = null;

        /// <summary>تا تاریخ</summary>
        public DateTime? ToDate
        {
            get => _toDate;
            set
            {
                if (SetProperty(ref _toDate, value))
                {
                }
            }
        }
        private DateTime? _toDate = null;

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>پیش‌نمایش</summary>
        public IAsyncRelayCommand PreviewCommand =>
            new AsyncRelayCommand(PreviewAsync);

        /// <summary>اجرای «پیش‌نمایش»</summary>
        private async Task PreviewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «پیش‌نمایش»
            await RunAsync(_ => Task.CompletedTask, "پیش‌نمایش…");
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
    }
}
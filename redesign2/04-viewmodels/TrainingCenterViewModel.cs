// ═══════════════════════════════════════════════════════════════════════
//  TrainingCenterViewModel
//  صفحه‌ی «مرکز آموزش» · گروه «پیشخوان» · کهن‌الگوی «list-cards»
//
//  متناظر با:  03-views/TrainingCenterView.xaml
//  وایرفریم:    01-wireframes-html/TrainingCenterView.html
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
    /// ViewModelِ صفحه‌ی «مرکز آموزش»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-cards</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: پیشخوان
    /// </remarks>
    public sealed class TrainingCenterViewModel : ViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public TrainingCenterViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("مرکز آموزش", "راهنمای تعاملی", "مکمل تور"));

            // کارت‌ها
            Cards.Add(new CardItem("آموزش مقدماتی کار با برنامه", "۶ ویدئو · ۴۵ دقیقه", ""));
            Cards.Add(new CardItem("ماژول مالی و صورتحساب", "۹ ویدئو · ۷۲ دقیقه", ""));
            Cards.Add(new CardItem("مدیریت داروخانه و انبار", "۵ ویدئو · ۳۸ دقیقه", ""));
            Cards.Add(new CardItem("گزارش‌ساز پویا", "۴ ویدئو · ۳۱ دقیقه", ""));
        }

        /// <summary>فیلترِ «دسته»</summary>
        public object? Filter2
        {
            get => _filter2;
            set
            {
                if (SetProperty(ref _filter2, value))
                {
                }
            }
        }
        private object? _filter2 = "همه";

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        /// <summary>کارت‌های نمایش‌داده‌شده</summary>
        public ObservableCollection<CardItem> Cards { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>دوره جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «دوره جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «دوره جدید»
            await RunAsync(_ => Task.CompletedTask, "دوره جدید…");
        }
    }
}
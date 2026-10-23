// ═══════════════════════════════════════════════════════════════════════
//  SplashViewModel
//  صفحه‌ی «در حال بارگذاری» · گروه «پیشخوان» · کهن‌الگوی «splash»
//
//  متناظر با:  03-views/SplashWindow.xaml
//  وایرفریم:    01-wireframes-html/SplashWindow.html
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
    /// ViewModelِ صفحه‌ی «در حال بارگذاری»
    /// </summary>
    /// <remarks>
    /// نشان برنامه با حلقهٔ پیشرفت و نوار وضعیتِ مرحله‌ای (در حال اتصال… ←
    /// بارگذاری تنظیمات… ← آماده).
    ///
    /// کهن‌الگوی طراحی: <c>splash</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: پیشخوان
    /// </remarks>
    public sealed class SplashViewModel : ViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public SplashViewModel(ILogger? logger = null) : base(logger)
        {
        }

        /// <summary>نامِ برنامه</summary>
        public string AppName => "سهند کلینیک‌پرو";

        /// <summary>شعارِ برنامه</summary>
        public string AppTagline => "مدیریت یکپارچه‌ی درمانگاه";

        /// <summary>متنِ وضعیتِ بارگذاری</summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }
        private string _statusText = "در حال بارگذاری…";
    }
}
// ═══════════════════════════════════════════════════════════════════════
//  SettingsViewModel
//  صفحه‌ی «تنظیمات» · گروه «پیشخوان» · کهن‌الگوی «settings»
//
//  متناظر با:  03-views/SettingsView.xaml
//  وایرفریم:    01-wireframes-html/SettingsView.html
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
    /// ViewModelِ صفحه‌ی «تنظیمات»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>settings</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: پیشخوان
    /// </remarks>
    public sealed class SettingsViewModel : ViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public SettingsViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("تنظیمات", "پشتیبان‌گیری", "تنظیمات بکاپ در این‌جا"));
            Relations.Add(new RelationItem("تنظیمات", "شعب", "نام شعبهٔ اصلی"));

            // بخش‌های تنظیمات
            Sections.Add("عمومی");
            Sections.Add("مالی");
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

        /// <summary>نام درمانگاه — الزامی</summary>
        public string ClinicName
        {
            get => _clinicName;
            set
            {
                if (SetProperty(ref _clinicName, value))
                {
                    Validate();
                }
            }
        }
        private string _clinicName = "درمانگاه تخصصی سهند";

        /// <summary>تلفن</summary>
        public string ClinicPhone
        {
            get => _clinicPhone;
            set
            {
                if (SetProperty(ref _clinicPhone, value))
                {
                }
            }
        }
        private string _clinicPhone = "۰۴۱-۳۴۴۳۶۱۴۱";

        /// <summary>نشانی</summary>
        public string Address
        {
            get => _address;
            set
            {
                if (SetProperty(ref _address, value))
                {
                }
            }
        }
        private string _address = "تبریز، خیابان ولیعصر، پلاک ۱۲۰";

        /// <summary>پشتیبان‌گیری خودکار هر شب</summary>
        public bool AutoBackup
        {
            get => _autoBackup;
            set
            {
                if (SetProperty(ref _autoBackup, value))
                {
                }
            }
        }
        private bool _autoBackup = true;

        /// <summary>تم تاریک</summary>
        public bool DarkTheme
        {
            get => _darkTheme;
            set
            {
                if (SetProperty(ref _darkTheme, value))
                {
                }
            }
        }
        private bool _darkTheme = false;

        /// <summary>واحد پول — الزامی</summary>
        public string Currency
        {
            get => _currency;
            set
            {
                if (SetProperty(ref _currency, value))
                {
                    Validate();
                }
            }
        }
        private string _currency = "تومان";

        /// <summary>درصد مالیات</summary>
        public int TaxPercent
        {
            get => _taxPercent;
            set
            {
                if (SetProperty(ref _taxPercent, value))
                {
                }
            }
        }
        private int _taxPercent = 9;

        /// <summary>بیمه پیش‌فرض</summary>
        public string DefaultInsurance
        {
            get => _defaultInsurance;
            set
            {
                if (SetProperty(ref _defaultInsurance, value))
                {
                }
            }
        }
        private string _defaultInsurance = "آزاد";

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>بازگردانی</summary>
        public IAsyncRelayCommand ResetCommand =>
            new AsyncRelayCommand(ResetAsync);

        /// <summary>اجرای «بازگردانی»</summary>
        private async Task ResetAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «بازگردانی»
            await RunAsync(_ => Task.CompletedTask, "بازگردانی…");
        }
    }
}
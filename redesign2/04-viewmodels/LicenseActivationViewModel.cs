// ═══════════════════════════════════════════════════════════════════════
//  LicenseActivationViewModel
//  صفحه‌ی «فعال‌سازی لایسنس» · گروه «سیستم» · کهن‌الگوی «form-narrow»
//
//  متناظر با:  03-views/LicenseActivationWindow.xaml
//  وایرفریم:    01-wireframes-html/LicenseActivationWindow.html
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
    /// ViewModelِ صفحه‌ی «فعال‌سازی لایسنس»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-narrow</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: سیستم
    /// </remarks>
    public sealed class LicenseActivationViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public LicenseActivationViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("فعال‌سازی لایسنس", "وضعیت لایسنس", "پس از فعال‌سازی"));
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>کلید مجوز — الزامی</summary>
        public string LicenseKey
        {
            get => _licenseKey;
            set
            {
                if (SetProperty(ref _licenseKey, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _licenseKey = "SAHD-XXXX-XXXX-XXXX";

        /// <summary>نام درمانگاه</summary>
        public string ClinicName
        {
            get => _clinicName;
            set
            {
                if (SetProperty(ref _clinicName, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _clinicName = "درمانگاه سهند";

        /// <summary>نشانی پنل</summary>
        public string PortalUrl
        {
            get => _portalUrl;
            set
            {
                if (SetProperty(ref _portalUrl, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _portalUrl = "https://clinic-license.ea-fixer.ir/api.php";

        /// <summary>فایل درخواست آفلاین</summary>
        public string OfflineFile
        {
            get => _offlineFile;
            set
            {
                if (SetProperty(ref _offlineFile, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _offlineFile = "";

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>فعال‌سازی</summary>
        public IAsyncRelayCommand ActivateCommand =>
            new AsyncRelayCommand(ActivateAsync);

        /// <summary>اجرای «فعال‌سازی»</summary>
        private async Task ActivateAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «فعال‌سازی»
            await RunAsync(_ => Task.CompletedTask, "فعال‌سازی…");
        }

        /// <summary>دریافت فایل .req</summary>
        public IAsyncRelayCommand RequestFileCommand =>
            new AsyncRelayCommand(RequestFileAsync);

        /// <summary>اجرای «دریافت فایل .req»</summary>
        private async Task RequestFileAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «دریافت فایل .req»
            await RunAsync(_ => Task.CompletedTask, "دریافت فایل .req…");
        }

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

            if (string.IsNullOrWhiteSpace(LicenseKey))
            {
                AddError("کلید مجوز الزامی است", nameof(LicenseKey));
            }
            else
            {
                ClearErrors(nameof(LicenseKey));
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Desktop.Base;
using Sahand.ClinicPro.Desktop.Localization;
using Sahand.ClinicPro.Desktop.Themes;
namespace Sahand.ClinicPro.Desktop.ViewModels.Redesign
{
    /// <summary>
    /// ViewModel صفحه‌ی «پنجره‌ی اصلی» — قاب اصلی برنامه شامل ناوبری و ناحیه‌ی محتوا
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/MainWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/MainWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/MainWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════




        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════


        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <summary>آیتم‌های نوار کناری</summary>
        public ObservableCollection<NavItemViewModel> NavItems { get; } = new();

        private string _globalSearchText = string.Empty;

        /// <summary>متنِ جستجوی یکپارچه در نوار بالا</summary>
        public string GlobalSearchText
        {
            get => _globalSearchText;
            set => SetProperty(ref _globalSearchText, value);
        }

        private object? _currentPage;

        /// <summary>صفحه‌ی در حال نمایش در ناحیه‌ی محتوا</summary>
        public object? CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        /// <summary>تغییر تم میان روشن و تاریک</summary>
        public IRelayCommand ToggleThemeCommand => new RelayCommand(ToggleTheme);

        /// <summary>تغییر زبان میان فارسی و انگلیسی</summary>
        public IRelayCommand ToggleLanguageCommand => new RelayCommand(ToggleLanguage);

        /// <summary>نمایش پنل اعلان‌ها</summary>
        public IAsyncRelayCommand OpenNotificationsCommand =>
            new AsyncRelayCommand(OpenNotificationsAsync);

        private void ToggleTheme()
        {
            ThemeManager.Toggle();
            ShowSuccess("تم با موفقیت تغییر کرد");
        }

        private void ToggleLanguage()
        {
            var next = LocalizationManager.IsRightToLeft ? "en-US" : "fa-IR";
            LocalizationManager.SetCulture(next);
            BuildNavigation();
        }

        private async Task OpenNotificationsAsync()
        {
            await RunAsync(async ct => await Task.Delay(120, ct), "دریافت اعلان‌ها…");
        }

        /// <summary>
        /// ساختِ درخت ناوبریِ نوار کناری بر اساس گروه‌بندی فرم‌ها.
        /// </summary>
        /// <remarks>
        /// برای افزودن یک صفحه‌ی جدید کافی است آن را در
        /// <c>tools/forms_spec.py</c> ثبت کنید تا در این‌جا هم پدیدار شود.
        /// </remarks>
        private void BuildNavigation()
        {
            NavItems.Clear();
            var groups = new Dictionary<string, List<NavItemViewModel>>();

            if (!groups.ContainsKey("پیشخوان"))
                groups["پیشخوان"] = new List<NavItemViewModel>();

            groups["پیشخوان"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Dashboard.Title"),
                Icon = "🏠",
                TargetKey = "Dashboard",
            });
            if (!groups.ContainsKey("پیشخوان"))
                groups["پیشخوان"] = new List<NavItemViewModel>();

            groups["پیشخوان"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("ChangePassword.Title"),
                Icon = "🔑",
                TargetKey = "ChangePassword",
            });
            if (!groups.ContainsKey("پیشخوان"))
                groups["پیشخوان"] = new List<NavItemViewModel>();

            groups["پیشخوان"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Settings.Title"),
                Icon = "⚙",
                TargetKey = "Settings",
            });
            if (!groups.ContainsKey("پیشخوان"))
                groups["پیشخوان"] = new List<NavItemViewModel>();

            groups["پیشخوان"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Notifications.Title"),
                Icon = "🔔",
                TargetKey = "Notifications",
            });
            if (!groups.ContainsKey("پیشخوان"))
                groups["پیشخوان"] = new List<NavItemViewModel>();

            groups["پیشخوان"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("TrainingCenter.Title"),
                Icon = "🎓",
                TargetKey = "TrainingCenter",
            });
            if (!groups.ContainsKey("بیماران"))
                groups["بیماران"] = new List<NavItemViewModel>();

            groups["بیماران"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Patients.Title"),
                Icon = "👥",
                TargetKey = "Patients",
            });
            if (!groups.ContainsKey("بیماران"))
                groups["بیماران"] = new List<NavItemViewModel>();

            groups["بیماران"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("PatientForm.Title"),
                Icon = "👤",
                TargetKey = "PatientForm",
            });
            if (!groups.ContainsKey("بیماران"))
                groups["بیماران"] = new List<NavItemViewModel>();

            groups["بیماران"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("PatientDetail.Title"),
                Icon = "📁",
                TargetKey = "PatientDetail",
            });
            if (!groups.ContainsKey("بیماران"))
                groups["بیماران"] = new List<NavItemViewModel>();

            groups["بیماران"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("AdvancedEmr.Title"),
                Icon = "❤",
                TargetKey = "AdvancedEmr",
            });
            if (!groups.ContainsKey("نوبت‌دهی"))
                groups["نوبت‌دهی"] = new List<NavItemViewModel>();

            groups["نوبت‌دهی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Appointments.Title"),
                Icon = "📅",
                TargetKey = "Appointments",
            });
            if (!groups.ContainsKey("نوبت‌دهی"))
                groups["نوبت‌دهی"] = new List<NavItemViewModel>();

            groups["نوبت‌دهی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("AppointmentForm.Title"),
                Icon = "🗓",
                TargetKey = "AppointmentForm",
            });
            if (!groups.ContainsKey("نوبت‌دهی"))
                groups["نوبت‌دهی"] = new List<NavItemViewModel>();

            groups["نوبت‌دهی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("DoctorVisit.Title"),
                Icon = "🩺",
                TargetKey = "DoctorVisit",
            });
            if (!groups.ContainsKey("پذیرش"))
                groups["پذیرش"] = new List<NavItemViewModel>();

            groups["پذیرش"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Receptions.Title"),
                Icon = "🛎",
                TargetKey = "Receptions",
            });
            if (!groups.ContainsKey("پذیرش"))
                groups["پذیرش"] = new List<NavItemViewModel>();

            groups["پذیرش"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("ReceptionForm.Title"),
                Icon = "📝",
                TargetKey = "ReceptionForm",
            });
            if (!groups.ContainsKey("پذیرش"))
                groups["پذیرش"] = new List<NavItemViewModel>();

            groups["پذیرش"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("ReceptionPay.Title"),
                Icon = "💳",
                TargetKey = "ReceptionPay",
            });
            if (!groups.ContainsKey("پذیرش"))
                groups["پذیرش"] = new List<NavItemViewModel>();

            groups["پذیرش"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Payment.Title"),
                Icon = "💰",
                TargetKey = "Payment",
            });
            if (!groups.ContainsKey("پذیرش"))
                groups["پذیرش"] = new List<NavItemViewModel>();

            groups["پذیرش"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Shifts.Title"),
                Icon = "🕐",
                TargetKey = "Shifts",
            });
            if (!groups.ContainsKey("پذیرش"))
                groups["پذیرش"] = new List<NavItemViewModel>();

            groups["پذیرش"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("ShiftClose.Title"),
                Icon = "🔒",
                TargetKey = "ShiftClose",
            });
            if (!groups.ContainsKey("پذیرش"))
                groups["پذیرش"] = new List<NavItemViewModel>();

            groups["پذیرش"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Rooms.Title"),
                Icon = "🚪",
                TargetKey = "Rooms",
            });
            if (!groups.ContainsKey("ویزیت"))
                groups["ویزیت"] = new List<NavItemViewModel>();

            groups["ویزیت"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Visit.Title"),
                Icon = "🩻",
                TargetKey = "Visit",
            });
            if (!groups.ContainsKey("ویزیت"))
                groups["ویزیت"] = new List<NavItemViewModel>();

            groups["ویزیت"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Visits.Title"),
                Icon = "📋",
                TargetKey = "Visits",
            });
            if (!groups.ContainsKey("ویزیت"))
                groups["ویزیت"] = new List<NavItemViewModel>();

            groups["ویزیت"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Dentistry.Title"),
                Icon = "🦷",
                TargetKey = "Dentistry",
            });
            if (!groups.ContainsKey("ویزیت"))
                groups["ویزیت"] = new List<NavItemViewModel>();

            groups["ویزیت"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Ophthalmology.Title"),
                Icon = "👁",
                TargetKey = "Ophthalmology",
            });
            if (!groups.ContainsKey("ویزیت"))
                groups["ویزیت"] = new List<NavItemViewModel>();

            groups["ویزیت"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Physiotherapy.Title"),
                Icon = "🏃",
                TargetKey = "Physiotherapy",
            });
            if (!groups.ContainsKey("ویزیت"))
                groups["ویزیت"] = new List<NavItemViewModel>();

            groups["ویزیت"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Complementary.Title"),
                Icon = "➕",
                TargetKey = "Complementary",
            });
            if (!groups.ContainsKey("ویزیت"))
                groups["ویزیت"] = new List<NavItemViewModel>();

            groups["ویزیت"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Consumables.Title"),
                Icon = "🧰",
                TargetKey = "Consumables",
            });
            if (!groups.ContainsKey("نسخه"))
                groups["نسخه"] = new List<NavItemViewModel>();

            groups["نسخه"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Prescriptions.Title"),
                Icon = "💊",
                TargetKey = "Prescriptions",
            });
            if (!groups.ContainsKey("نسخه"))
                groups["نسخه"] = new List<NavItemViewModel>();

            groups["نسخه"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("PrescriptionForm.Title"),
                Icon = "💊",
                TargetKey = "PrescriptionForm",
            });
            if (!groups.ContainsKey("نسخه"))
                groups["نسخه"] = new List<NavItemViewModel>();

            groups["نسخه"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("PrescriptionDetail.Title"),
                Icon = "📄",
                TargetKey = "PrescriptionDetail",
            });
            if (!groups.ContainsKey("نسخه"))
                groups["نسخه"] = new List<NavItemViewModel>();

            groups["نسخه"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("EPrescription.Title"),
                Icon = "📲",
                TargetKey = "EPrescription",
            });
            if (!groups.ContainsKey("داروخانه"))
                groups["داروخانه"] = new List<NavItemViewModel>();

            groups["داروخانه"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Pharmacy.Title"),
                Icon = "🏥",
                TargetKey = "Pharmacy",
            });
            if (!groups.ContainsKey("داروخانه"))
                groups["داروخانه"] = new List<NavItemViewModel>();

            groups["داروخانه"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("PharmacyItemForm.Title"),
                Icon = "💉",
                TargetKey = "PharmacyItemForm",
            });
            if (!groups.ContainsKey("داروخانه"))
                groups["داروخانه"] = new List<NavItemViewModel>();

            groups["داروخانه"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("StockInForm.Title"),
                Icon = "📥",
                TargetKey = "StockInForm",
            });
            if (!groups.ContainsKey("داروخانه"))
                groups["داروخانه"] = new List<NavItemViewModel>();

            groups["داروخانه"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Stocktaking.Title"),
                Icon = "🔢",
                TargetKey = "Stocktaking",
            });
            if (!groups.ContainsKey("آزمایشگاه"))
                groups["آزمایشگاه"] = new List<NavItemViewModel>();

            groups["آزمایشگاه"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Lab.Title"),
                Icon = "🧪",
                TargetKey = "Lab",
            });
            if (!groups.ContainsKey("آزمایشگاه"))
                groups["آزمایشگاه"] = new List<NavItemViewModel>();

            groups["آزمایشگاه"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("LabOrderForm.Title"),
                Icon = "🧫",
                TargetKey = "LabOrderForm",
            });
            if (!groups.ContainsKey("آزمایشگاه"))
                groups["آزمایشگاه"] = new List<NavItemViewModel>();

            groups["آزمایشگاه"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("LabResult.Title"),
                Icon = "📊",
                TargetKey = "LabResult",
            });
            if (!groups.ContainsKey("تصویربرداری"))
                groups["تصویربرداری"] = new List<NavItemViewModel>();

            groups["تصویربرداری"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Imaging.Title"),
                Icon = "🩻",
                TargetKey = "Imaging",
            });
            if (!groups.ContainsKey("تصویربرداری"))
                groups["تصویربرداری"] = new List<NavItemViewModel>();

            groups["تصویربرداری"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("DicomViewer.Title"),
                Icon = "🖼",
                TargetKey = "DicomViewer",
            });
            if (!groups.ContainsKey("مالی"))
                groups["مالی"] = new List<NavItemViewModel>();

            groups["مالی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Billing.Title"),
                Icon = "🧾",
                TargetKey = "Billing",
            });
            if (!groups.ContainsKey("مالی"))
                groups["مالی"] = new List<NavItemViewModel>();

            groups["مالی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("InvoiceForm.Title"),
                Icon = "📃",
                TargetKey = "InvoiceForm",
            });
            if (!groups.ContainsKey("مالی"))
                groups["مالی"] = new List<NavItemViewModel>();

            groups["مالی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Accounting.Title"),
                Icon = "📒",
                TargetKey = "Accounting",
            });
            if (!groups.ContainsKey("مالی"))
                groups["مالی"] = new List<NavItemViewModel>();

            groups["مالی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("TariffForm.Title"),
                Icon = "🏷",
                TargetKey = "TariffForm",
            });
            if (!groups.ContainsKey("مالی"))
                groups["مالی"] = new List<NavItemViewModel>();

            groups["مالی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("CompanyForm.Title"),
                Icon = "🏢",
                TargetKey = "CompanyForm",
            });
            if (!groups.ContainsKey("مالی"))
                groups["مالی"] = new List<NavItemViewModel>();

            groups["مالی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Insurance.Title"),
                Icon = "🏥",
                TargetKey = "Insurance",
            });
            if (!groups.ContainsKey("مالی"))
                groups["مالی"] = new List<NavItemViewModel>();

            groups["مالی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Payroll.Title"),
                Icon = "💵",
                TargetKey = "Payroll",
            });
            if (!groups.ContainsKey("منابع انسانی"))
                groups["منابع انسانی"] = new List<NavItemViewModel>();

            groups["منابع انسانی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Personnel.Title"),
                Icon = "👔",
                TargetKey = "Personnel",
            });
            if (!groups.ContainsKey("منابع انسانی"))
                groups["منابع انسانی"] = new List<NavItemViewModel>();

            groups["منابع انسانی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("EmployeeForm.Title"),
                Icon = "👤",
                TargetKey = "EmployeeForm",
            });
            if (!groups.ContainsKey("منابع انسانی"))
                groups["منابع انسانی"] = new List<NavItemViewModel>();

            groups["منابع انسانی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("EmployeeSalaryEdit.Title"),
                Icon = "💵",
                TargetKey = "EmployeeSalaryEdit",
            });
            if (!groups.ContainsKey("منابع انسانی"))
                groups["منابع انسانی"] = new List<NavItemViewModel>();

            groups["منابع انسانی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Attendance.Title"),
                Icon = "🕐",
                TargetKey = "Attendance",
            });
            if (!groups.ContainsKey("منابع انسانی"))
                groups["منابع انسانی"] = new List<NavItemViewModel>();

            groups["منابع انسانی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Doctors.Title"),
                Icon = "👨‍⚕️",
                TargetKey = "Doctors",
            });
            if (!groups.ContainsKey("منابع انسانی"))
                groups["منابع انسانی"] = new List<NavItemViewModel>();

            groups["منابع انسانی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("DoctorForm.Title"),
                Icon = "🩺",
                TargetKey = "DoctorForm",
            });
            if (!groups.ContainsKey("منابع انسانی"))
                groups["منابع انسانی"] = new List<NavItemViewModel>();

            groups["منابع انسانی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Users.Title"),
                Icon = "👤",
                TargetKey = "Users",
            });
            if (!groups.ContainsKey("منابع انسانی"))
                groups["منابع انسانی"] = new List<NavItemViewModel>();

            groups["منابع انسانی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("UserForm.Title"),
                Icon = "👤",
                TargetKey = "UserForm",
            });
            if (!groups.ContainsKey("منابع انسانی"))
                groups["منابع انسانی"] = new List<NavItemViewModel>();

            groups["منابع انسانی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("RolePermission.Title"),
                Icon = "🔐",
                TargetKey = "RolePermission",
            });
            if (!groups.ContainsKey("منابع انسانی"))
                groups["منابع انسانی"] = new List<NavItemViewModel>();

            groups["منابع انسانی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Admin.Title"),
                Icon = "🛠",
                TargetKey = "Admin",
            });
            if (!groups.ContainsKey("سازمانی"))
                groups["سازمانی"] = new List<NavItemViewModel>();

            groups["سازمانی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Branches.Title"),
                Icon = "🏘",
                TargetKey = "Branches",
            });
            if (!groups.ContainsKey("ارتباطات"))
                groups["ارتباطات"] = new List<NavItemViewModel>();

            groups["ارتباطات"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Sms.Title"),
                Icon = "📨",
                TargetKey = "Sms",
            });
            if (!groups.ContainsKey("ارتباطات"))
                groups["ارتباطات"] = new List<NavItemViewModel>();

            groups["ارتباطات"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Survey.Title"),
                Icon = "📝",
                TargetKey = "Survey",
            });
            if (!groups.ContainsKey("سازمانی"))
                groups["سازمانی"] = new List<NavItemViewModel>();

            groups["سازمانی"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Sepas.Title"),
                Icon = "🏛",
                TargetKey = "Sepas",
            });
            if (!groups.ContainsKey("گزارشات"))
                groups["گزارشات"] = new List<NavItemViewModel>();

            groups["گزارشات"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Reports.Title"),
                Icon = "📊",
                TargetKey = "Reports",
            });
            if (!groups.ContainsKey("گزارشات"))
                groups["گزارشات"] = new List<NavItemViewModel>();

            groups["گزارشات"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("ReportBrowser.Title"),
                Icon = "📑",
                TargetKey = "ReportBrowser",
            });
            if (!groups.ContainsKey("گزارشات"))
                groups["گزارشات"] = new List<NavItemViewModel>();

            groups["گزارشات"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("AuditLog.Title"),
                Icon = "📜",
                TargetKey = "AuditLog",
            });
            if (!groups.ContainsKey("سیستم"))
                groups["سیستم"] = new List<NavItemViewModel>();

            groups["سیستم"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("Backup.Title"),
                Icon = "💾",
                TargetKey = "Backup",
            });
            if (!groups.ContainsKey("سیستم"))
                groups["سیستم"] = new List<NavItemViewModel>();

            groups["سیستم"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("LicenseStatus.Title"),
                Icon = "🔑",
                TargetKey = "LicenseStatus",
            });
            if (!groups.ContainsKey("سیستم"))
                groups["سیستم"] = new List<NavItemViewModel>();

            groups["سیستم"].Add(new NavItemViewModel
            {
                Title = LocalizationManager.Get("LicenseActivation.Title"),
                Icon = "🔐",
                TargetKey = "LicenseActivation",
            });

            foreach (var kv in groups)
            {
                var group = new NavItemViewModel
                {
                    Title = kv.Key,
                    Icon = "🗂",
                    IsGroup = true,
                };

                foreach (var child in kv.Value)
                    group.Children.Add(child);

                NavItems.Add(group);
            }

            if (NavItems.Count > 0)
                NavItems[0].IsExpanded = true;
        }

        /// <inheritdoc />
        public override async Task LoadAsync()
        {
            BuildNavigation();

            await RunAsync(async ct =>
            {
                // TODO: بارگذاریِ تنظیمات کاربر، مجوزها و اعلان‌های خوانده‌نشده
                await Task.Delay(200, ct);
            }, "در حال آماده‌سازی…");
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════


    }


    /// <summary>
    /// یک آیتم در نوار کناریِ برنامه
    /// </summary>
    public sealed class NavItemViewModel : ViewModelBase
    {
        /// <summary>عنوان نمایشی</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>شکلکِ نمایشی</summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>آیا این آیتم یک گروه است؟</summary>
        public bool IsGroup { get; set; }

        private bool _isExpanded;

        /// <summary>آیا گروه باز است؟</summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        /// <summary>نامِ نوعِ صفحه برای ناوبری (در صورت نیاز)</summary>
        public string? TargetKey { get; set; }

        /// <summary>زیرآیتم‌ها</summary>
        public ObservableCollection<NavItemViewModel> Children { get; } = new();
    }
}

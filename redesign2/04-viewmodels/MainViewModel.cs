// ═══════════════════════════════════════════════════════════════════════
//  MainViewModel
//  صفحه‌ی «پنجره‌ی اصلی» · گروه «پیشخوان» · کهن‌الگوی «shell»
//
//  متناظر با:  03-views/MainWindow.xaml
//  وایرفریم:    01-wireframes-html/MainWindow.html
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
    /// ViewModelِ صفحه‌ی «پنجره‌ی اصلی»
    /// </summary>
    /// <remarks>
    /// نوار کناری با گروه‌بندیِ ۱۵گانه و شمارندهٔ آیتم؛ نوار بالا با
    /// جستجوی یکپارچه، زنگولهٔ اعلان، انتخاب‌گر تم و زبان و منوی کاربر.
    ///
    /// کهن‌الگوی طراحی: <c>shell</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: پیشخوان
    /// </remarks>
    public sealed class MainViewModel : ViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public MainViewModel(ILogger? logger = null) : base(logger)
        {
        }

            // منوی کناری — ساخته‌شده از فهرستِ صفحاتِ برنامه
            MenuGroups.Add(new MenuGroup("پیشخوان", new[]
            {
                new MenuItemModel("پیشخوان", Application.Current.TryFindResource("Icon.LayoutDashboard") as Geometry, "DashboardView"),
                new MenuItemModel("ورود به برنامه", Application.Current.TryFindResource("Icon.LogIn") as Geometry, "LoginWindow"),
                new MenuItemModel("تغییر رمز عبور", Application.Current.TryFindResource("Icon.KeyRound") as Geometry, "ChangePasswordWindow"),
                new MenuItemModel("تنظیمات", Application.Current.TryFindResource("Icon.Settings") as Geometry, "SettingsView"),
                new MenuItemModel("انتخاب میانبر", Application.Current.TryFindResource("Icon.LayoutGrid") as Geometry, "ShortcutPickerWindow"),
                new MenuItemModel("اعلان‌ها", Application.Current.TryFindResource("Icon.Bell") as Geometry, "NotificationsView"),
                new MenuItemModel("راهنمای تعاملی", Application.Current.TryFindResource("Icon.Compass") as Geometry, "GuidedTourWindow"),
                new MenuItemModel("مرکز آموزش", Application.Current.TryFindResource("Icon.GraduationCap") as Geometry, "TrainingCenterView"),
            }));
            MenuGroups.Add(new MenuGroup("بیماران", new[]
            {
                new MenuItemModel("بیماران", Application.Current.TryFindResource("Icon.Users") as Geometry, "PatientsView"),
                new MenuItemModel("فرم بیمار", Application.Current.TryFindResource("Icon.UserPlus") as Geometry, "PatientFormWindow"),
                new MenuItemModel("پرونده‌ی بیمار", Application.Current.TryFindResource("Icon.IdCard") as Geometry, "PatientDetailWindow"),
                new MenuItemModel("پرونده بالینی پیشرفته", Application.Current.TryFindResource("Icon.NotebookPen") as Geometry, "AdvancedEmrView"),
                new MenuItemModel("نمایش تصویر", Application.Current.TryFindResource("Icon.Image") as Geometry, "PhotoViewerWindow"),
            }));
            MenuGroups.Add(new MenuGroup("نوبت‌دهی", new[]
            {
                new MenuItemModel("نوبت‌دهی", Application.Current.TryFindResource("Icon.CalendarDays") as Geometry, "AppointmentsView"),
                new MenuItemModel("فرم نوبت", Application.Current.TryFindResource("Icon.CalendarPlus") as Geometry, "AppointmentFormWindow"),
                new MenuItemModel("ویزیت پزشک", Application.Current.TryFindResource("Icon.Stethoscope") as Geometry, "DoctorVisitView"),
            }));
            MenuGroups.Add(new MenuGroup("پذیرش", new[]
            {
                new MenuItemModel("پذیرش و صف", Application.Current.TryFindResource("Icon.ClipboardList") as Geometry, "ReceptionsView"),
                new MenuItemModel("فرم پذیرش", Application.Current.TryFindResource("Icon.ClipboardPlus") as Geometry, "ReceptionFormWindow"),
                new MenuItemModel("دریافت وجه", Application.Current.TryFindResource("Icon.HandCoins") as Geometry, "ReceptionPayWindow"),
                new MenuItemModel("پرداخت", Application.Current.TryFindResource("Icon.CreditCard") as Geometry, "PaymentWindow"),
                new MenuItemModel("شیفت‌ها", Application.Current.TryFindResource("Icon.Clock") as Geometry, "ShiftsView"),
                new MenuItemModel("بستن شیفت", Application.Current.TryFindResource("Icon.Lock") as Geometry, "ShiftCloseWindow"),
                new MenuItemModel("پیش‌نمایش رسید شیفت", Application.Current.TryFindResource("Icon.Receipt") as Geometry, "ShiftReceiptPreviewWindow"),
                new MenuItemModel("اتاق‌ها", Application.Current.TryFindResource("Icon.DoorOpen") as Geometry, "RoomsView"),
                new MenuItemModel("مدیریت اتاق‌ها", Application.Current.TryFindResource("Icon.DoorClosed") as Geometry, "RoomsManagerWindow"),
            }));
            MenuGroups.Add(new MenuGroup("ویزیت", new[]
            {
                new MenuItemModel("ویزیت (SOAP)", Application.Current.TryFindResource("Icon.Stethoscope") as Geometry, "VisitWindow"),
                new MenuItemModel("فهرست ویزیت‌ها", Application.Current.TryFindResource("Icon.ClipboardCheck") as Geometry, "VisitsView"),
                new MenuItemModel("دندانپزشکی", Application.Current.TryFindResource("Icon.Smile") as Geometry, "DentistryView"),
                new MenuItemModel("چشم‌پزشکی", Application.Current.TryFindResource("Icon.Eye") as Geometry, "OphthalmologyView"),
                new MenuItemModel("فیزیوتراپی", Application.Current.TryFindResource("Icon.Accessibility") as Geometry, "PhysiotherapyView"),
                new MenuItemModel("خدمات تکمیلی", Application.Current.TryFindResource("Icon.Sparkles") as Geometry, "ComplementaryView"),
                new MenuItemModel("لوازم مصرفی", Application.Current.TryFindResource("Icon.Package") as Geometry, "ConsumablesView"),
            }));
            MenuGroups.Add(new MenuGroup("نسخه", new[]
            {
                new MenuItemModel("نسخه‌ها", Application.Current.TryFindResource("Icon.FileText") as Geometry, "PrescriptionsView"),
                new MenuItemModel("فرم نسخه", Application.Current.TryFindResource("Icon.FilePlus") as Geometry, "PrescriptionFormWindow"),
                new MenuItemModel("جزئیات نسخه", Application.Current.TryFindResource("Icon.FileSearch") as Geometry, "PrescriptionDetailWindow"),
                new MenuItemModel("نسخه الکترونیک", Application.Current.TryFindResource("Icon.Send") as Geometry, "EPrescriptionView"),
            }));
            MenuGroups.Add(new MenuGroup("داروخانه", new[]
            {
                new MenuItemModel("داروخانه", Application.Current.TryFindResource("Icon.Pill") as Geometry, "PharmacyView"),
                new MenuItemModel("فرم قلم دارویی", Application.Current.TryFindResource("Icon.PackagePlus") as Geometry, "PharmacyItemFormWindow"),
                new MenuItemModel("ورود کالا", Application.Current.TryFindResource("Icon.PackagePlus") as Geometry, "StockInFormWindow"),
                new MenuItemModel("انبارگردانی", Application.Current.TryFindResource("Icon.ClipboardCheck") as Geometry, "StocktakingView"),
            }));
            MenuGroups.Add(new MenuGroup("آزمایشگاه", new[]
            {
                new MenuItemModel("آزمایشگاه", Application.Current.TryFindResource("Icon.FlaskConical") as Geometry, "LabView"),
                new MenuItemModel("درخواست آزمایش", Application.Current.TryFindResource("Icon.TestTube") as Geometry, "LabOrderFormWindow"),
                new MenuItemModel("ثبت نتیجه آزمایش", Application.Current.TryFindResource("Icon.FileCheck") as Geometry, "LabResultWindow"),
            }));
            MenuGroups.Add(new MenuGroup("تصویربرداری", new[]
            {
                new MenuItemModel("تصویربرداری", Application.Current.TryFindResource("Icon.ScanLine") as Geometry, "ImagingView"),
                new MenuItemModel("نمایشگر تصاویر پزشکی", Application.Current.TryFindResource("Icon.Monitor") as Geometry, "DicomViewerWindow"),
            }));
            MenuGroups.Add(new MenuGroup("مالی", new[]
            {
                new MenuItemModel("صورتحساب", Application.Current.TryFindResource("Icon.Receipt") as Geometry, "BillingView"),
                new MenuItemModel("فرم صورتحساب", Application.Current.TryFindResource("Icon.FilePlus") as Geometry, "InvoiceFormWindow"),
                new MenuItemModel("حسابداری", Application.Current.TryFindResource("Icon.Calculator") as Geometry, "AccountingView"),
                new MenuItemModel("جزئیات سند حسابداری", Application.Current.TryFindResource("Icon.FileSpreadsheet") as Geometry, "JournalEntryDetailWindow"),
                new MenuItemModel("تعرفه خدمات", Application.Current.TryFindResource("Icon.Tags") as Geometry, "TariffFormWindow"),
                new MenuItemModel("فرم شرکت بیمه", Application.Current.TryFindResource("Icon.Building2") as Geometry, "CompanyFormWindow"),
                new MenuItemModel("بیمه و مطالبات", Application.Current.TryFindResource("Icon.ShieldCheck") as Geometry, "InsuranceView"),
                new MenuItemModel("حقوق و دستمزد", Application.Current.TryFindResource("Icon.Banknote") as Geometry, "PayrollView"),
                new MenuItemModel("فیش حقوقی", Application.Current.TryFindResource("Icon.FileText") as Geometry, "PayslipWindow"),
                new MenuItemModel("گزارش کارمزد", Application.Current.TryFindResource("Icon.Handshake") as Geometry, "CommissionReportWindow"),
                new MenuItemModel("عملکرد پزشکان", Application.Current.TryFindResource("Icon.TrendingUp") as Geometry, "DoctorPerformanceWindow"),
            }));
            MenuGroups.Add(new MenuGroup("منابع انسانی", new[]
            {
                new MenuItemModel("پرسنل", Application.Current.TryFindResource("Icon.Users") as Geometry, "PersonnelView"),
                new MenuItemModel("فرم کارمند", Application.Current.TryFindResource("Icon.UserPlus") as Geometry, "EmployeeFormWindow"),
                new MenuItemModel("ویرایش حقوق", Application.Current.TryFindResource("Icon.Banknote") as Geometry, "EmployeeSalaryEditWindow"),
                new MenuItemModel("حضور و غیاب", Application.Current.TryFindResource("Icon.Fingerprint") as Geometry, "AttendanceView"),
                new MenuItemModel("پزشکان", Application.Current.TryFindResource("Icon.Stethoscope") as Geometry, "DoctorsView"),
                new MenuItemModel("فرم پزشک", Application.Current.TryFindResource("Icon.Stethoscope") as Geometry, "DoctorFormWindow"),
                new MenuItemModel("کاربران", Application.Current.TryFindResource("Icon.UserCog") as Geometry, "UsersView"),
                new MenuItemModel("فرم کاربر", Application.Current.TryFindResource("Icon.UserCog") as Geometry, "UserFormWindow"),
                new MenuItemModel("نقش‌ها و مجوزها", Application.Current.TryFindResource("Icon.ShieldCheck") as Geometry, "RolePermissionView"),
                new MenuItemModel("مدیریت سیستم", Application.Current.TryFindResource("Icon.Settings2") as Geometry, "AdminView"),
            }));
            MenuGroups.Add(new MenuGroup("سازمانی", new[]
            {
                new MenuItemModel("شعب", Application.Current.TryFindResource("Icon.Building2") as Geometry, "BranchesView"),
                new MenuItemModel("سامانه سپاس", Application.Current.TryFindResource("Icon.Link2") as Geometry, "SepasView"),
            }));
            MenuGroups.Add(new MenuGroup("ارتباطات", new[]
            {
                new MenuItemModel("پیامک", Application.Current.TryFindResource("Icon.MessageSquare") as Geometry, "SmsView"),
                new MenuItemModel("نظرسنجی", Application.Current.TryFindResource("Icon.Star") as Geometry, "SurveyView"),
            }));
            MenuGroups.Add(new MenuGroup("گزارشات", new[]
            {
                new MenuItemModel("گزارشات", Application.Current.TryFindResource("Icon.ChartColumn") as Geometry, "ReportsView"),
                new MenuItemModel("مرورگر گزارشات", Application.Current.TryFindResource("Icon.FileSearch") as Geometry, "ReportBrowserView"),
                new MenuItemModel("گزارش‌ساز پویا", Application.Current.TryFindResource("Icon.Wand") as Geometry, "AdhocReportWindow"),
                new MenuItemModel("گزارش حسابرسی", Application.Current.TryFindResource("Icon.History") as Geometry, "AuditLogView"),
            }));
            MenuGroups.Add(new MenuGroup("سیستم", new[]
            {
                new MenuItemModel("پشتیبان‌گیری", Application.Current.TryFindResource("Icon.DatabaseBackup") as Geometry, "BackupView"),
                new MenuItemModel("وضعیت لایسنس", Application.Current.TryFindResource("Icon.ShieldCheck") as Geometry, "LicenseStatusView"),
                new MenuItemModel("فعال‌سازی لایسنس", Application.Current.TryFindResource("Icon.KeyRound") as Geometry, "LicenseActivationWindow"),
                new MenuItemModel("انتخاب رنگ", Application.Current.TryFindResource("Icon.Palette") as Geometry, "ColorPickerWindow"),
                new MenuItemModel("دیالوگ یکپارچه", Application.Current.TryFindResource("Icon.MessageSquare") as Geometry, "ThemedDialog"),
            }));

        /// <summary>نامِ برنامه</summary>
        public string AppName => "سهند کلینیک‌پرو";

        /// <summary>تعویضِ تمِ روشن/تاریک</summary>
        public IRelayCommand ToggleThemeCommand => new RelayCommand(ToggleTheme);

        /// <summary>تعویضِ زبان فارسی/انگلیسی</summary>
        public IRelayCommand ToggleLanguageCommand => new RelayCommand(ToggleLanguage);

        /// <summary>نمایشِ اعلان‌ها</summary>
        public IRelayCommand NotificationsCommand => new RelayCommand(ShowNotifications);

        /// <summary>گروه‌های منوی کناری</summary>
        public ObservableCollection<MenuGroup> MenuGroups { get; } = new();

        /// <summary>صفحه‌ی در حال نمایش</summary>
        public object? CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }
        private object? _currentPage;

        /// <summary>متنِ نوارِ وضعیت</summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }
        private string _statusText = "آماده";

        /// <summary>تعویضِ تم</summary>
        private void ToggleTheme()
            => ShowSuccess("تم تغییر کرد");

        /// <summary>تعویضِ زبان</summary>
        private void ToggleLanguage()
            => ShowSuccess("زبان تغییر کرد");

        /// <summary>نمایشِ اعلان‌ها</summary>
        private void ShowNotifications()
            => ShowSuccess("اعلان‌ها");
    }
}
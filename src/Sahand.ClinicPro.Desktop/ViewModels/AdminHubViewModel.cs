using CommunityToolkit.Mvvm.ComponentModel;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// مرکز یکپارچهٔ «مدیریت سامانه» — بازطراحی تأییدشدهٔ مالک: یازده آیتم
/// پراکندهٔ منوی کناری (که چندتایشان با تب‌های صفحهٔ مدیریت تکراری بودند)
/// به یک آیتم و یک صفحهٔ تب‌دار تبدیل شد. هیچ صفحه‌ای حذف نشده است؛ هر تب
/// همان صفحهٔ قبلی با همان ویومدل و همان دسترسی‌هاست — فقط جای رسیدن به
/// آن‌ها یکی شده است.
/// </summary>
public partial class AdminHubViewModel : ViewModelBase
{
    /// <summary>عنوان صفحه</summary>
    public override string Title => "مدیریت سامانه";

    /// <summary>ساخت مرکز مدیریت با همهٔ ویومدل‌های مدیریتی موجود</summary>
    public AdminHubViewModel(
        SettingsViewModel settings,
        ComplementaryViewModel complementary,
        UsersViewModel users,
        RolesViewModel roles,
        ServiceCatalogViewModel serviceCatalog,
        RoomsManagerViewModel rooms,
        BranchesViewModel branches,
        BackupViewModel backup,
        AdminViewModel admin,
        AuditLogViewModel audit,
        LicenseStatusViewModel license)
    {
        Settings = settings;
        Complementary = complementary;
        Users = users;
        Roles = roles;
        ServiceCatalog = serviceCatalog;
        Rooms = rooms;
        Branches = branches;
        Backup = backup;
        Admin = admin;
        Audit = audit;
        License = license;
        // تب «پشتیبان‌گیری و آرشیو» صفحهٔ مدیریت قدیمی را روی زبانهٔ پشتیبان باز می‌کند
        Admin.TabIndex = 2;
    }

    /// <summary>تب تنظیمات درمانگاه</summary>
    public SettingsViewModel Settings { get; }

    /// <summary>تب چاپ و قالب‌ها (استودیو قالب و نظرسنجی)</summary>
    public ComplementaryViewModel Complementary { get; }

    /// <summary>تب کاربران</summary>
    public UsersViewModel Users { get; }

    /// <summary>تب نقش‌ها و دسترسی</summary>
    public RolesViewModel Roles { get; }

    /// <summary>تب خدمات و تعرفه</summary>
    public ServiceCatalogViewModel ServiceCatalog { get; }

    /// <summary>تب اتاق‌ها</summary>
    public RoomsManagerViewModel Rooms { get; }

    /// <summary>تب شعبه‌ها</summary>
    public BranchesViewModel Branches { get; }

    /// <summary>تب پشتیبان‌گیری (صفحهٔ اختصاصی)</summary>
    public BackupViewModel Backup { get; }

    /// <summary>صفحهٔ مدیریت قدیمی — برای زبانهٔ آرشیو سالانه در تب پشتیبان‌گیری و آرشیو</summary>
    public AdminViewModel Admin { get; }

    /// <summary>تب لاگ عملیات</summary>
    public AuditLogViewModel Audit { get; }

    /// <summary>تب لایسنس</summary>
    public LicenseStatusViewModel License { get; }

    /// <summary>تب انتخاب‌شدهٔ مرکز مدیریت</summary>
    [ObservableProperty]
    private int _selectedTabIndex;
}

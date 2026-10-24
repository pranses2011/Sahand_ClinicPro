namespace Sahand.ClinicPro.Domain.Entities.Security;

/// <summary>
/// نام‌های فارسی نقش‌های سیستمی — منبع یگانهٔ نام نقش‌ها در سراسر سامانه.
/// نام نقش از این پس فارسی است (نمایش در فهرست کاربران، فرم کاربر و صفحهٔ
/// نقش‌ها بدون لایهٔ ترجمه) و بررسی‌هایی مثل «مدیر سامانه» به همین ثابت‌ها تکیه می‌کنند.
/// نگاشت نام‌های قدیمی انگلیسی برای مهاجرت خودکار دیتابیس‌های موجود نگه داشته شده است.
/// </summary>
public static class SystemRoles
{
    /// <summary>مدیر سامانه — دسترسی کامل</summary>
    public const string Admin = "مدیر سامانه";
    /// <summary>مدیر درمانگاه</summary>
    public const string Manager = "مدیر درمانگاه";
    /// <summary>پزشک معالج</summary>
    public const string Doctor = "پزشک";
    /// <summary>منشی پذیرش و نوبت‌دهی</summary>
    public const string Receptionist = "پذیرش";
    /// <summary>حسابدار و مسئول مالی</summary>
    public const string Accountant = "حسابدار";
    /// <summary>پرستار</summary>
    public const string Nurse = "پرستار";
    /// <summary>مسئول داروخانه</summary>
    public const string Pharmacist = "داروساز";
    /// <summary>کارشناس آزمایشگاه</summary>
    public const string LabTechnician = "کارشناس آزمایشگاه";

    /// <summary>نام قدیمی انگلیسی ← نام فارسی فعلی (مهاجرت دیتابیس‌های موجود)</summary>
    public static readonly IReadOnlyDictionary<string, string> LegacyToPersian =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Admin"] = Admin,
            ["Manager"] = Manager,
            ["Doctor"] = Doctor,
            ["Receptionist"] = Receptionist,
            ["Accountant"] = Accountant,
            ["Nurse"] = Nurse,
            ["Pharmacist"] = Pharmacist,
            ["LabTechnician"] = LabTechnician,
        };
}

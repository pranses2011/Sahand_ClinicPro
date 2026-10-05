namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// برند دستگاه حضور و غیاب — تعیین‌کننده پروتکل ارتباطی درایور
/// </summary>
public enum DeviceBrand : byte
{
    /// <summary>زدکاتیکو — پروتکل PULL SDK روی TCP/4370</summary>
    ZKTeco = 1,

    /// <summary>هایک‌ویژن — پروتکل ISAPI روی HTTP</summary>
    Hikvision = 2,

    /// <summary>سوپرما — پروتکل BioStar2</summary>
    Suprema = 3,

    /// <summary>آنویز — پروتکل Wiegand/UDP</summary>
    Anviz = 4,

    /// <summary>دستگاه عمومی — خروجی CSV/HTTP Webhook</summary>
    Generic = 5,
}

/// <summary>
/// وضعیت اتصال دستگاه از نگاه سرور
/// </summary>
public enum DeviceStatus : byte
{
    /// <summary>متصل و سالم</summary>
    Online = 1,

    /// <summary>خارج از دسترس</summary>
    Offline = 2,

    /// <summary>در حال همگام‌سازی لاگ‌ها</summary>
    Syncing = 3,

    /// <summary>خطای پایدار — نیازمند بررسی</summary>
    Error = 4,

    /// <summary>غیرفعال‌شده توسط اپراتور</summary>
    Disabled = 5,
}

/// <summary>
/// نوع ضربه (Punch) ثبت‌شده روی دستگاه
/// </summary>
public enum PunchState : byte
{
    /// <summary>نامشخص — نیازمند تطبیق با شیفت</summary>
    Undefined = 0,

    /// <summary>ورود</summary>
    CheckIn = 1,

    /// <summary>خروج</summary>
    CheckOut = 2,

    /// <summary>خروج استراحت</summary>
    BreakOut = 3,

    /// <summary>بازگشت از استراحت</summary>
    BreakIn = 4,

    /// <summary>ورود اضافه‌کاری</summary>
    OvertimeIn = 5,

    /// <summary>خروج اضافه‌کاری</summary>
    OvertimeOut = 6,
}

/// <summary>
/// وضعیت پردازش لاگ خام دستگاه
/// </summary>
public enum RawLogStatus : byte
{
    /// <summary>در انتظار تطبیق با شیفت و پرسنل</summary>
    Pending = 0,

    /// <summary>تبدیل به رکورد حضور شد</summary>
    Processed = 1,

    /// <summary>نادیده گرفته‌شده (تکراری/خارج از شیفت)</summary>
    Ignored = 2,

    /// <summary>خطای پردازش — نگاشت پرسنل یافت نشد</summary>
    Error = 3,
}

/// <summary>
/// الگوی تکرار تخصیص شیفت
/// </summary>
public enum ShiftAssignmentRecurrence : byte
{
    /// <summary>شیفت ثابت روزانه (کل بازه)</summary>
    Fixed = 1,

    /// <summary>چرخشی بر اساس روزهای هفته</summary>
    Weekly = 2,

    /// <summary>سفارشی — فقط روزهای منتخب هفته</summary>
    CustomDays = 3,
}

/// <summary>
/// نوع تعطیلی
/// </summary>
public enum HolidayType : byte
{
    /// <summary>رسمی — تعطیل کامل درمانگاه</summary>
    Official = 1,

    /// <summary>سازمانی — نیمه‌کار یا سفارشی</summary>
    Custom = 2,

    /// <summary>پوشش پزشک روی-call</summary>
    OnCall = 3,
}

/// <summary>
/// وضعیت درخواست اصلاح کارت حضور
/// </summary>
public enum CorrectionStatus : byte
{
    /// <summary>در انتظار تأیید سرپرست</summary>
    Pending = 1,

    /// <summary>تأیید و اعمال شد</summary>
    Approved = 2,

    /// <summary>رد شد</summary>
    Rejected = 3,
}

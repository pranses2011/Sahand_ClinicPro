# -*- coding: utf-8 -*-
"""گام ۲ — بخش ۳C: موجودیت‌های گروه ۷ تا ۱۲ (RBAC، ارتباطات، زیرساخت، لایسنس، دولتی، گزارش)"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from step02_lib import E, P, N, C, entity_file, write

EN = "Sahand.ClinicPro.Domain.Enums"

# ═══════════ گروه ۷: پرسنل و RBAC ═══════════

entity_file(E("EmployeeAttendance", "Personnel", "EmployeeAttendance",
    "حضور و غیاب پرسنل - ورود و خروج روزانه",
    [
        P("EmployeeId", "long", "شناسه کارمند"),
        P("AttendanceDate", "DateOnly", "تاریخ"),
        P("CheckIn", "TimeOnly?", "ساعت ورود"),
        P("CheckOut", "TimeOnly?", "ساعت خروج"),
        P("ShiftId", "long?", "شناسه شیفت"),
        P("Status", "AttendanceStatus", "وضعیت حضور"),
        P("WorkedHours", "decimal", "ساعات کارکرد", "0m"),
        P("OvertimeHours", "decimal", "ساعات اضافه‌کاری", "0m"),
        N("Employee", "Employee?", "کارمند"),
        N("Shift", "Shift?", "شیفت"),
    ],
    extra_usings=[f"using {EN};"]), "Personnel")

entity_file(E("EmployeeSalary", "Personnel", "EmployeeSalaries",
    "فیش حقوقی - محاسبه ماهانه حقوق پرسنل",
    [
        P("EmployeeId", "long", "شناسه کارمند"),
        P("PeriodYear", "int", "سال دوره"),
        P("PeriodMonth", "int", "ماه دوره"),
        P("BaseSalary", "decimal", "حقوق پایه", "0m"),
        P("Overtime", "decimal", "اضافه‌کاری", "0m"),
        P("Bonus", "decimal", "پاداش", "0m"),
        P("Deductions", "decimal", "کسورات", "0m"),
        P("Insurance", "decimal", "بیمه", "0m"),
        P("Tax", "decimal", "مالیات", "0m"),
        P("NetAmount", "decimal", "خالص پرداختی", "0m"),
        P("PaidAt", "DateTime?", "زمان پرداخت"),
        P("Status", "SalaryStatus", "وضعیت"),
        N("Employee", "Employee?", "کارمند"),
    ],
    extra_usings=[f"using {EN};"]), "Personnel")

entity_file(E("EmployeeLeave", "Personnel", "EmployeeLeaves",
    "مرخصی پرسنل - درخواست و تأیید",
    [
        P("EmployeeId", "long", "شناسه کارمند"),
        P("LeaveType", "LeaveType", "نوع مرخصی"),
        P("FromDate", "DateOnly?", "از تاریخ"),
        P("ToDate", "DateOnly?", "تا تاریخ"),
        P("Days", "decimal", "تعداد روز", "0m"),
        P("Reason", "string?", "دلیل"),
        P("Status", "LeaveStatus", "وضعیت"),
        P("ApprovedBy", "long?", "شناسه تأییدکننده"),
        N("Employee", "Employee?", "کارمند"),
    ],
    extra_usings=[f"using {EN};"]), "Personnel")

entity_file(E("PersonnelContract", "Personnel", "Contracts",
    "قرارداد پرسنلی - سند رسمی همکاری",
    [
        P("EmployeeId", "long", "شناسه کارمند"),
        P("ContractNumber", "string?", "شماره قرارداد - یکتا"),
        P("ContractType", "ContractType", "نوع قرارداد"),
        P("StartDate", "DateOnly?", "شروع قرارداد"),
        P("EndDate", "DateOnly?", "پایان قرارداد"),
        P("Salary", "decimal", "حقوق قراردادی", "0m"),
        P("Terms", "string?", "شرایط قرارداد"),
        P("DocumentPath", "string?", "مسیر فایل قرارداد"),
        P("Status", "ContractStatus", "وضعیت"),
        N("Employee", "Employee?", "کارمند"),
    ],
    extra_usings=[f"using {EN};"]), "Personnel")

# ─── RBAC ───

entity_file(E("User", "Security", "Users",
    "کاربر سیستم - حساب ورود پرسنل به نرم‌افزار",
    [
        P("EmployeeId", "long?", "شناسه پرسنل مرتبط"),
        P("Username", "string", "نام کاربری - یکتا"),
        P("Email", "string", "ایمیل - یکتا"),
        P("PasswordHash", "string", "هش رمز عبور با BCrypt - ضریب ۱۲"),
        P("SecurityStamp", "string?", "مهر امنیتی برای ابطال توکن‌ها"),
        P("TwoFactorEnabled", "bool", "ورود دومرحله‌ای فعال", "false"),
        P("TwoFactorSecret", "string?", "کلید دومرحله‌ای"),
        P("PhoneNumber", "string?", "موبایل کاربر"),
        P("EmailConfirmed", "bool", "ایمیل تأیید شده", "false"),
        P("PhoneConfirmed", "bool", "موبایل تأیید شده", "false"),
        P("FailedLoginAttempts", "int", "شمارش تلاش ناموفق ورود", "0"),
        P("LockoutEnd", "DateTime?", "پایان قفل حساب - ۳۰ دقیقه بعد از ۵ تلاش ناموفق"),
        P("LastLoginAt", "DateTime?", "آخرین ورود موفق"),
        P("LastLoginIp", "string?", "IP آخرین ورود"),
        P("PasswordChangedAt", "DateTime?", "آخرین تغییر رمز"),
        P("MustChangePassword", "bool", "باید رمز را عوض کند", "false"),
        P("IsActive", "bool", "فعال", "true"),
        N("Employee", "Employee?", "پرسنل مرتبط"),
        C("Roles", "UserRole", "نقش‌های کاربر"),
        C("RefreshTokens", "RefreshToken", "توکن‌های تازه‌سازی"),
        C("Sessions", "UserSession", "نشست‌های فعال"),
        C("Notifications", "Notification", "اعلان‌های کاربر"),
    ],
    remarks=["رمز عبور هرگز به‌صورت متن ساده ذخیره نمی‌شود",
             "پس از پنج تلاش ناموفق حساب به مدت سی دقیقه قفل می‌شود"]), "Security")

entity_file(E("Role", "Security", "Roles",
    "نقش - دسته‌بندی دسترسی کاربران",
    [
        P("Name", "string", "نام نقش - یکتا"),
        P("NormalizedName", "string?", "نام نرمال‌شده برای جستجو"),
        P("Description", "string?", "توضیح نقش"),
        P("IsSystem", "bool", "نقش سیستمی غیرقابل حذف", "false"),
        P("IsActive", "bool", "فعال", "true"),
        C("Users", "UserRole", "کاربران این نقش"),
        C("Permissions", "RolePermission", "دسترسی‌های این نقش"),
    ]), "Security")

entity_file(E("Permission", "Security", "Permissions",
    "دسترسی - مجوز اتمی بر اساس ماژول و عملیات",
    [
        P("Name", "string", "نام یکتای دسترسی مثل patients.create"),
        P("Category", "string?", "دسته‌بندی"),
        P("Module", "string?", "نام ماژول"),
        P("Action", "string?", "نوع عملیات مثل view یا create"),
        P("Description", "string?", "توضیح فارسی دسترسی"),
        C("Roles", "RolePermission", "نقش‌های دارای این دسترسی"),
    ]), "Security")

write("Security/UserRole.cs", """
using System;

namespace Sahand.ClinicPro.Domain.Entities.Security;

/// <summary>
/// رابطه چند‌به‌چند کاربر و نقش - کلید مرکب
/// </summary>
public class UserRole
{
    /// <summary>شناسه کاربر</summary>
    public long UserId { get; set; }

    /// <summary>شناسه نقش</summary>
    public long RoleId { get; set; }

    /// <summary>زمان تخصیص نقش</summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>شناسه کاربر تخصیص‌دهنده</summary>
    public long? AssignedBy { get; set; }

    /// <summary>کاربر مرتبط</summary>
    public User? User { get; set; }

    /// <summary>نقش مرتبط</summary>
    public Role? Role { get; set; }
}
""")

write("Security/RolePermission.cs", """
namespace Sahand.ClinicPro.Domain.Entities.Security;

/// <summary>
/// رابطه چند‌به‌چند نقش و دسترسی - کلید مرکب
/// </summary>
public class RolePermission
{
    /// <summary>شناسه نقش</summary>
    public long RoleId { get; set; }

    /// <summary>شناسه دسترسی</summary>
    public long PermissionId { get; set; }

    /// <summary>نقش مرتبط</summary>
    public Role? Role { get; set; }

    /// <summary>دسترسی مرتبط</summary>
    public Permission? Permission { get; set; }
}
""")

entity_file(E("RefreshToken", "Security", "RefreshTokens",
    "توکن تازه‌سازی - چرخه عمر توکن بدون ورود مجدد",
    [
        P("UserId", "long", "شناسه کاربر"),
        P("Token", "string", "مقدار توکن - یکتا و هش‌شده"),
        P("JwtId", "string?", "شناسه توکن JWT مرتبط"),
        P("IsRevoked", "bool", "ابطال شده", "false"),
        P("CreatedAt", "DateTime", "زمان ایجاد"),
        P("ExpiresAt", "DateTime", "زمان انقضا"),
        P("DeviceInfo", "string?", "اطلاعات دستگاه"),
        P("IpAddress", "string?", "نشانی IP"),
        N("User", "User?", "کاربر صاحب توکن"),
    ],
    inherits="BaseEntity", interfaces=()), "Security")

entity_file(E("UserSession", "Security", "UserSessions",
    "نشست کاربر - ردیابی دستگاه‌های متصل",
    [
        P("UserId", "long", "شناسه کاربر"),
        P("SessionToken", "string?", "توکن نشست"),
        P("LoginAt", "DateTime?", "زمان ورود"),
        P("LogoutAt", "DateTime?", "زمان خروج"),
        P("LastActivityAt", "DateTime?", "آخرین فعالیت"),
        P("IpAddress", "string?", "نشانی IP"),
        P("UserAgent", "string?", "عامل کاربر مرورگر"),
        P("DeviceType", "DeviceType", "نوع دستگاه"),
        P("IsActive", "bool", "نشست فعال", "true"),
        N("User", "User?", "کاربر"),
    ],
    extra_usings=[f"using {EN};"]), "Security")

entity_file(E("PasswordHistory", "Security", "PasswordHistory",
    "تاریخچه رمز عبور - جلوگیری از تکرار رمزهای قبلی",
    [
        P("UserId", "long", "شناسه کاربر"),
        P("PasswordHash", "string", "هش رمز پیشین"),
        P("ChangedAt", "DateTime?", "زمان تغییر"),
        N("User", "User?", "کاربر"),
    ],
    interfaces=()), "Security")

entity_file(E("UserModulePermission", "Security", "UserModulePermissions",
    "دسترسی ماژولی کاربر - سطح دسترسی دقیق‌تر از نقش",
    [
        P("UserId", "long", "شناسه کاربر"),
        P("ModuleId", "long", "شناسه ماژول"),
        P("CanView", "bool", "مجوز مشاهده", "false"),
        P("CanAdd", "bool", "مجوز افزودن", "false"),
        P("CanEdit", "bool", "مجوز ویرایش", "false"),
        P("CanDelete", "bool", "مجوز حذف", "false"),
        P("CanPrint", "bool", "مجوز چاپ", "false"),
        P("CanExport", "bool", "مجوز خروجی", "false"),
        N("User", "User?", "کاربر"),
        N("Module", "SystemModule?", "ماژول"),
    ]), "Security")

# ═══════════ گروه ۸: ارتباطات ═══════════

entity_file(E("SmsMessage", "Communication", "SmsMessages",
    "پیامک ارسالی - رکورد کامل همراه وضعیت تحویل",
    [
        P("Recipient", "string", "شماره گیرنده - ۱۱ رقم"),
        P("RecipientType", "SmsRecipientType", "نوع گیرنده"),
        P("RecipientId", "long?", "شناسه رکورد گیرنده مثل بیمار"),
        P("Message", "string?", "متن پیامک"),
        P("TemplateId", "long?", "شناسه قالب استفاده‌شده"),
        P("Provider", "string?", "نام پنل ارسال‌کننده"),
        P("ProviderMessageId", "string?", "شناسه پیام در پنل"),
        P("Status", "SmsStatus", "وضعیت ارسال"),
        P("SentAt", "DateTime?", "زمان ارسال"),
        P("DeliveredAt", "DateTime?", "زمان تحویل به گوشی"),
        P("Cost", "decimal", "هزینه ارسال", "0m"),
        P("ErrorMessage", "string?", "خطای ارسال"),
        P("MessageType", "SmsMessageType", "نوع پیام"),
        P("ReferenceType", "SmsReferenceType?", "منشأ پیام"),
        P("ReferenceId", "long?", "شناسه رکورد منشأ"),
        N("Template", "SmsTemplate?", "قالب پیامک"),
    ],
    extra_usings=[f"using {EN};"]), "Communication")

entity_file(E("SmsTemplate", "Communication", "SmsTemplates",
    "قالب پیامک - الگوی آماده با متغیرها",
    [
        P("Name", "string", "نام قالب"),
        P("Code", "string?", "کد یکتای قالب"),
        P("TemplateType", "SmsTemplateType", "نوع قالب"),
        P("Content", "string?", "متن قالب با متغیرهایی مثل نام بیمار"),
        P("Variables", "string?", "تعریف متغیرها - JSON"),
        P("IsActive", "bool", "فعال", "true"),
        C("Messages", "SmsMessage", "پیامک‌های ارسال‌شده با این قالب"),
    ],
    extra_usings=[f"using {EN};"]), "Communication")

entity_file(E("SmsProvider", "Communication", "SmsProviders",
    "پنل پیامک - تنظیمات ده پنل ایرانی با احراز رمزنگاری‌شده",
    [
        P("ProviderName", "string", "نام پنل مثل کاوه‌نگار"),
        P("ProviderCode", "string?", "کد یکتای پنل"),
        P("ApiUrl", "string?", "نشانی API"),
        P("ApiKeyEncrypted", "byte[]?", "کلید API رمزنگاری‌شده با AES-256"),
        P("Username", "string?", "نام کاربری پنل"),
        P("PasswordEncrypted", "byte[]?", "رمز عبور رمزنگاری‌شده"),
        P("SenderNumber", "string?", "شماره فرستنده"),
        P("Priority", "int", "اولویت در زنجیره جایگزینی", "1"),
        P("Balance", "decimal", "موجودی اعتبار", "0m"),
        P("IsDefault", "bool", "پنل پیش‌فرض", "false"),
        P("IsActive", "bool", "فعال", "true"),
    ]), "Communication")

entity_file(E("SmsQueueItem", "Communication", "SmsQueue",
    "صف ارسال پیامک - پردازش پس‌زمینه با Hangfire",
    [
        P("Recipient", "string", "شماره گیرنده"),
        P("Message", "string?", "متن پیام"),
        P("ScheduledAt", "DateTime?", "زمان برنامه‌ریزی‌شده ارسال"),
        P("ProcessedAt", "DateTime?", "زمان پردازش"),
        P("Attempts", "int", "تعداد تلاش", "0"),
        P("Status", "SmsStatus", "وضعیت در صف"),
        P("Priority", "byte", "اولویت صف"),
    ],
    extra_usings=[f"using {EN};"]), "Communication")

entity_file(E("PhoneCall", "Communication", "PhoneCalls",
    "تماس تلفنی - ثبت تماس‌های مرتبط با بیماران",
    [
        P("CallerNumber", "string?", "شماره تماس‌گیرنده"),
        P("PatientId", "long?", "شناسه بیمار مرتبط"),
        P("UserId", "long?", "شناسه کاربر ثبت‌کننده"),
        P("Direction", "CallDirection", "جهت تماس"),
        P("CallStart", "DateTime?", "شروع تماس"),
        P("CallEnd", "DateTime?", "پایان تماس"),
        P("Duration", "int?", "مدت به ثانیه"),
        P("Result", "CallResult", "نتیجه تماس"),
        P("Notes", "string?", "خلاصه گفتگو"),
        P("RecordingPath", "string?", "مسیر فایل صوت"),
        N("Patient", "Patient?", "بیمار مرتبط"),
    ],
    extra_usings=[f"using {EN};"]), "Communication")

entity_file(E("Survey", "Communication", "Surveys",
    "نظرسنجی - پرسشنامه رضایت بیماران",
    [
        P("Title", "string", "عنوان نظرسنجی"),
        P("Description", "string?", "توضیحات"),
        P("SurveyType", "SurveyType", "نوع نظرسنجی"),
        P("Questions", "string?", "سوالات - JSON"),
        P("IsActive", "bool", "فعال", "true"),
        C("Responses", "SurveyResponse", "پاسخ‌های نظرسنجی"),
    ],
    extra_usings=[f"using {EN};"]), "Communication")

entity_file(E("SurveyResponse", "Communication", "SurveyResponses",
    "پاسخ نظرسنجی - ثبت امتیاز و نظرات بیمار",
    [
        P("SurveyId", "long", "شناسه نظرسنجی"),
        P("PatientId", "long?", "شناسه بیمار"),
        P("VisitId", "long?", "شناسه ویزیت مرتبط"),
        P("DoctorId", "long?", "شناسه پزشک مورد نظر"),
        P("Rating", "byte?", "امتیاز یک تا پنج"),
        P("Answers", "string?", "پاسخ‌ها - JSON"),
        P("Comments", "string?", "نظرات آزاد"),
        P("SubmittedAt", "DateTime?", "زمان ثبت"),
        N("Survey", "Survey?", "نظرسنجی والد"),
    ]), "Communication")

entity_file(E("Notification", "Communication", "Notifications",
    "اعلان درون‌برنامه‌ای - اعلان‌های سیستم و کسب‌وکار",
    [
        P("UserId", "long", "شناسه کاربر گیرنده"),
        P("Title", "string", "عنوان اعلان"),
        P("Message", "string?", "متن اعلان"),
        P("Type", "NotificationType", "نوع اعلان"),
        P("Priority", "NotificationPriority", "اولویت"),
        P("Link", "string?", "پیوند مقصد در صورت کلیک"),
        P("IsRead", "bool", "خوانده شده", "false"),
        P("ReadAt", "DateTime?", "زمان خواندن"),
        N("User", "User?", "کاربر گیرنده"),
    ],
    extra_usings=[f"using {EN};"]), "Communication")

# ═══════════ گروه ۹: زیرساخت ═══════════

entity_file(E("Document", "Infrastructure", "Documents",
    "سند - فایل پیوست هر رکورد سیستم",
    [
        P("Title", "string", "عنوان سند"),
        P("Description", "string?", "توضیحات"),
        P("CategoryId", "long?", "شناسه دسته سند"),
        P("ReferenceType", "DocumentReferenceType", "نوع رکورد مرجع"),
        P("ReferenceId", "long?", "شناسه رکورد مرجع"),
        P("FileName", "string?", "نام فایل"),
        P("FilePath", "string?", "مسیر ذخیره‌سازی"),
        P("FileType", "string?", "پسوند فایل"),
        P("FileSize", "long?", "حجم به بایت"),
        P("MimeType", "string?", "نوع MIME"),
        P("Hash", "string?", "هش فایل برای بررسی یکپارچگی"),
        P("IsEncrypted", "bool", "رمزنگاری‌شده", "false"),
        P("Tags", "string?", "برچسب‌ها"),
        P("UploadedBy", "long?", "شناسه بارگذار"),
        P("UploadedAt", "DateTime?", "زمان بارگذاری"),
        N("Category", "DocumentCategory?", "دسته سند"),
    ],
    extra_usings=[f"using {EN};"]), "Infrastructure")

entity_file(E("DocumentCategory", "Infrastructure", "DocumentCategories",
    "دسته سند - طبقه‌بندی درختی فایل‌ها",
    [
        P("Name", "string", "نام دسته"),
        P("Code", "string?", "کد یکتا"),
        P("ParentCategoryId", "long?", "شناسه دسته والد"),
        P("IconPath", "string?", "آیکون"),
        N("Parent", "DocumentCategory?", "دسته والد"),
        C("Children", "DocumentCategory", "دسته‌های فرزند"),
        C("Documents", "Document", "اسناد این دسته"),
    ]), "Infrastructure")

entity_file(E("BackupHistory", "Infrastructure", "BackupHistory",
    "تاریخچه پشتیبان‌گیری - سه‌سطحی: کامل و افزایشی و لاگ",
    [
        P("BackupType", "BackupType", "نوع پشتیبان"),
        P("FilePath", "string?", "مسیر فایل پشتیبان"),
        P("FileSize", "long?", "حجم به بایت"),
        P("StartedAt", "DateTime?", "شروع عملیات"),
        P("CompletedAt", "DateTime?", "پایان عملیات"),
        P("Status", "BackupStatus", "وضعیت"),
        P("IsAutomatic", "bool", "خودکار توسط زمان‌بند", "true"),
        P("StorageType", "StorageType", "محل ذخیره"),
        P("ErrorMessage", "string?", "خطای احتمالی"),
        P("PerformedBy", "long?", "شناسه اجراکننده"),
    ],
    extra_usings=[f"using {EN};"]), "Infrastructure")

entity_file(E("AuditLog", "Infrastructure", "AuditLogs",
    "لاگ عملیات - ثبت تمام تغییرات داده‌ها با مقادیر قبل و بعد",
    [
        P("UserId", "long?", "شناسه کاربر عملیات"),
        P("Username", "string?", "نام کاربری در لحظه عملیات"),
        P("ActionType", "AuditActionType", "نوع عملیات"),
        P("EntityName", "string?", "نام موجودیت تغییر‌یافته"),
        P("EntityId", "long?", "شناسه رکورد"),
        P("OldValues", "string?", "مقادیر قبلی - JSON"),
        P("NewValues", "string?", "مقادیر جدید - JSON"),
        P("ChangedFields", "string?", "فهرست فیلدهای تغییر‌یافته"),
        P("IpAddress", "string?", "نشانی IP"),
        P("UserAgent", "string?", "عامل کاربر"),
        P("Timestamp", "DateTime", "زمان عملیات"),
        P("Module", "string?", "ماژول عملیات"),
        P("Severity", "AuditSeverity", "شدت رخداد"),
    ],
    interfaces=(),
    extra_usings=[f"using {EN};"]), "Infrastructure")

entity_file(E("ErrorLog", "Infrastructure", "ErrorLogs",
    "لاگ خطای سیستم - برای پایش سلامت نرم‌افزار",
    [
        P("Timestamp", "DateTime", "زمان خطا"),
        P("Level", "ErrorLogLevel", "سطح خطا"),
        P("Source", "string?", "محل وقوع خطا"),
        P("Message", "string?", "پیام خطا"),
        P("Exception", "string?", "متن استثنا"),
        P("StackTrace", "string?", "ردی پشته"),
        P("UserId", "long?", "شناسه کاربر در لحظه خطا"),
        P("RequestUrl", "string?", "نشانی درخواست"),
        P("Resolved", "bool", "رسیدگی شده", "false"),
    ],
    interfaces=(),
    extra_usings=[f"using {EN};"]), "Infrastructure")

entity_file(E("SystemSetting", "Infrastructure", "SystemSettings",
    "تنظیمات سیستم - کلید و مقدار با پشتیبانی رمزنگاری",
    [
        P("Category", "string?", "دسته تنظیم مثل عمومی یا مالی"),
        P("Key", "string", "کلید تنظیم - یکتا"),
        P("Value", "string?", "مقدار"),
        P("DataType", "SettingDataType", "نوع داده"),
        P("Description", "string?", "توضیح"),
        P("IsEncrypted", "bool", "مقدار رمزنگاری‌شده", "false"),
        P("IsSystem", "bool", "تنظیم سیستمی غیرقابل حذف", "false"),
    ],
    extra_usings=[f"using {EN};"]), "Infrastructure")

entity_file(E("ClinicInfo", "Infrastructure", "ClinicInfo",
    "اطلاعات درمانگاه - مشخصات هویتی برای سربرگ چاپ",
    [
        P("ClinicName", "string", "نام درمانگاه"),
        P("ClinicNameEn", "string?", "نام انگلیسی"),
        P("LogoPath", "string?", "مسیر لوگو"),
        P("LicenseNumber", "string?", "شماره مجوز"),
        P("EconomicCode", "string?", "کد اقتصادی"),
        P("NationalId", "string?", "شناسه ملی"),
        P("Address", "string?", "نشانی"),
        P("Phone", "string?", "تلفن"),
        P("Fax", "string?", "فکس"),
        P("Email", "string?", "ایمیل"),
        P("Website", "string?", "وب‌سایت"),
        P("ManagerName", "string?", "نام مدیر"),
        P("FoundationDate", "DateOnly?", "تاریخ تأسیس"),
    ]), "Infrastructure")

entity_file(E("SystemModule", "Infrastructure", "Modules",
    "ماژول سیستم - فهرست ماژول‌ها با وضعیت فعال‌سازی لایسنس",
    [
        P("Name", "string", "نام ماژول"),
        P("Code", "string?", "کد یکتای ماژول"),
        P("Category", "string?", "دسته ماژول"),
        P("Description", "string?", "توضیحات"),
        P("IconPath", "string?", "آیکون"),
        P("SortOrder", "int", "ترتیب نمایش", "0"),
        P("IsCore", "bool", "ماژول اصلی - بدون نیاز به لایسنس", "false"),
        P("IsEnabled", "bool", "فعال‌شده با لایسنس", "false"),
        P("Version", "string?", "نسخه ماژول"),
        C("UserPermissions", "UserModulePermission", "دسترسی‌های کاربر به این ماژول"),
    ]), "Infrastructure")

entity_file(E("PerformanceMetric", "Infrastructure", "PerformanceMetrics",
    "سنجه عملکرد - نمونه‌برداری دوره‌ای سلامت سیستم",
    [
        P("MetricType", "string?", "نوع سنجه مثل CPU"),
        P("MetricName", "string?", "نام سنجه"),
        P("Value", "decimal", "مقدار اندازه‌گیری", "0m"),
        P("Unit", "string?", "واحد"),
        P("Timestamp", "DateTime", "زمان نمونه"),
        P("Context", "string?", "بافت اضافی - JSON"),
    ],
    interfaces=()), "Infrastructure")

# ═══════════ گروه ۱۰: لایسنس سمت کلاینت ═══════════

entity_file(E("ClinicLicense", "Licensing", "ClinicLicense",
    "لایسنس محلی درمانگاه - رکورد رمزنگاری‌شده با امضای RSA-2048",
    [
        P("LicenseKey", "string", "کلید لایسنس"),
        P("ActivationCode", "string?", "کد فعال‌سازی"),
        P("LicenseType", "LicenseType", "نوع لایسنس - اشتراکی یا دائمی"),
        P("MaxUsers", "int", "حداکثر کاربر همزمان", "1"),
        P("MaxDoctors", "int", "حداکثر پزشک", "1"),
        P("MaxPatients", "int", "حداکثر بیمار", "1000"),
        P("MaxBranches", "int", "حداکثر شعبه", "1"),
        P("EnabledModules", "string?", "ماژول‌های فعال - JSON رمزنگاری‌شده"),
        P("IssuedDate", "DateOnly?", "تاریخ صدور"),
        P("ExpiryDate", "DateOnly?", "تاریخ انقضا - تهی یعنی دائمی"),
        P("HardwareFingerprint", "string?", "اثر انگشت سخت‌افزاری - SHA-256 از شش مؤلفه"),
        P("SignatureData", "byte[]?", "امضای دیجیتال RSA-2048 با SHA256"),
        P("LastValidatedAt", "DateTime?", "آخرین اعتبارسنجی موفق"),
        P("IsOfflineActivation", "bool", "فعال‌سازی آفلاین", "false"),
    ],
    remarks=["اعتبارسنجی سه‌مرحله‌ای: اثر انگشت سخت‌افزار و انقضا و امضای RSA",
             "heartbeat هر ۲۴ ساعت با عطف‌بندی ۳۰ روز آفلاین"],
    extra_usings=[f"using {EN};"]), "Licensing")

entity_file(E("LicenseCheckLog", "Licensing", "LicenseCheckLog",
    "لاگ اعتبارسنجی لایسنس - هر بررسی روزانه ثبت می‌شود",
    [
        P("CheckDate", "DateTime?", "زمان بررسی"),
        P("Result", "LicenseValidationResult", "نتیجه"),
        P("IsOnlineCheck", "bool", "بررسی آنلاین بود", "false"),
        P("Message", "string?", "پیام نتیجه"),
    ],
    extra_usings=[f"using {EN};"]), "Licensing")

# ═══════════ گروه ۱۱: یکپارچه‌سازی دولتی ═══════════

entity_file(E("SepasTransaction", "Integration", "SepasTransactions",
    "تراکنش سپاس - ثبت ویزیت در سامانه پوشش سلامت همگا",
    [
        P("PatientId", "long", "شناسه بیمار"),
        P("VisitId", "long?", "شناسه ویزیت"),
        P("TransactionType", "SepasTransactionType", "نوع تراکنش"),
        P("SepasReferenceId", "string?", "شناسه ارجاع سپاس"),
        P("RequestData", "string?", "درخواست خام - JSON"),
        P("ResponseData", "string?", "پاسخ خام - JSON"),
        P("Status", "SyncStatus", "وضعیت همگام‌سازی"),
        P("ErrorMessage", "string?", "خطای احتمالی"),
        P("SentAt", "DateTime?", "زمان ارسال"),
        P("RespondedAt", "DateTime?", "زمان پاسخ"),
        P("RetryCount", "int", "تعداد بازتلاش", "0"),
        N("Patient", "Patient?", "بیمار"),
    ],
    extra_usings=[f"using {EN};"]), "Integration")

entity_file(E("ElectronicPrescription", "Integration", "ElectronicPrescriptions",
    "نسخه الکترونیک - رکورد ارسال نسخه به سامانه سلامت یا تأمین",
    [
        P("PrescriptionId", "long", "شناسه نسخه محلی"),
        P("SystemType", "InsuranceSystemType", "سامانه مقصد - سلامت یا تأمین"),
        P("TrackingCode", "string?", "کد رهگیری - یکتا"),
        P("RegistrationCode", "string?", "کد ثبت در سامانه"),
        P("Status", "ElectronicPrescriptionStatus", "وضعیت"),
        P("Response", "string?", "پاسخ خام سامانه"),
        P("SentAt", "DateTime?", "زمان ارسال"),
        P("IsSuccess", "bool", "ارسال موفق", "false"),
        P("ErrorCode", "string?", "کد خطای سامانه"),
        N("Prescription", "Prescription?", "نسخه مرتبط"),
    ],
    extra_usings=[f"using {EN};"]), "Integration")

entity_file(E("SibSystemRecord", "Integration", "SibSystemRecords",
    "رکورد سیب - همگام‌سازی با سامانه سطح یک بهداشت",
    [
        P("PatientId", "long", "شناسه بیمار"),
        P("SibRecordId", "string?", "شناسه رکورد در سیب"),
        P("ServiceType", "SibServiceType", "نوع خدمت"),
        P("ReferralType", "ReferralType?", "نوع ارجاع"),
        P("Data", "string?", "داده خام - JSON"),
        P("SyncStatus", "SyncStatus", "وضعیت همگام‌سازی"),
        P("SyncedAt", "DateTime?", "زمان همگام‌سازی"),
        N("Patient", "Patient?", "بیمار"),
    ],
    extra_usings=[f"using {EN};"]), "Integration")

entity_file(E("ApiCallLog", "Integration", "ApiCallLogs",
    "لاگ فراخوانی API - تمام تماس‌های خارجی با پایش زمان",
    [
        P("ServiceName", "string?", "نام سرویس مثل سپاس"),
        P("Endpoint", "string?", "نشانی فراخوانی‌شده"),
        P("Method", "string?", "فعل HTTP"),
        P("RequestBody", "string?", "بدنه درخواست"),
        P("ResponseBody", "string?", "بدنه پاسخ"),
        P("StatusCode", "int?", "کد وضعیت HTTP"),
        P("Duration", "int?", "مدت به میلی‌ثانیه"),
        P("CalledAt", "DateTime?", "زمان فراخوانی"),
        P("IsSuccess", "bool", "موفق", "false"),
        P("UserId", "long?", "شناسه کاربر آغازگر"),
    ],
    interfaces=()), "Integration")

entity_file(E("ApiCredential", "Integration", "ApiCredentials",
    "اعتبارسنجی سرویس خارجی - رمزنگاری‌شده با AES-256",
    [
        P("ServiceName", "string", "نام سرویس - یکتا"),
        P("Username", "string?", "نام کاربری"),
        P("PasswordEncrypted", "byte[]?", "رمز عبور رمزنگاری‌شده"),
        P("ApiKeyEncrypted", "byte[]?", "کلید API رمزنگاری‌شده"),
        P("Token", "string?", "توکن دسترسی"),
        P("TokenExpiresAt", "DateTime?", "انقضای توکن"),
        P("Endpoint", "string?", "نشانی پایه سرویس"),
        P("IsActive", "bool", "فعال", "true"),
    ]), "Integration")

entity_file(E("ReferralRecord", "Integration", "ReferralSystem",
    "ارجاع - داخلی یا بین‌مرکزی با کد ارجاع",
    [
        P("PatientId", "long", "شناسه بیمار"),
        P("ReferringDoctorId", "long?", "شناسه پزشک ارجاع‌دهنده"),
        P("ReferralCode", "string?", "کد ارجاع - یکتا"),
        P("ReferralType", "ReferralType", "نوع ارجاع"),
        P("DestinationType", "string?", "نوع مقصد ارجاع"),
        P("Reason", "string?", "دلیل ارجاع"),
        P("Status", "ReferralStatus", "وضعیت ارجاع"),
        P("ValidUntil", "DateOnly?", "معتبر تا"),
        N("Patient", "Patient?", "بیمار"),
    ],
    extra_usings=[f"using {EN};"]), "Integration")

# ═══════════ گروه ۱۲: گزارش‌ها ═══════════

entity_file(E("ReportDefinition", "Reporting", "ReportDefinitions",
    "تعریف گزارش - قالب فایل گزارش یا کوئری SQL",
    [
        P("ReportName", "string", "نام گزارش"),
        P("Code", "string?", "کد یکتای گزارش"),
        P("Category", "string?", "دسته گزارش"),
        P("Description", "string?", "توضیحات"),
        P("ReportFile", "byte[]?", "فایل قالب گزارش"),
        P("SqlQuery", "string?", "کوئری منبع داده"),
        P("Parameters", "string?", "پارامترهای گزارش - JSON"),
        P("RequiredPermission", "string?", "دسترسی موردنیاز اجرا"),
        C("Executions", "ReportExecution", "سوابق اجرا"),
        C("Schedules", "ReportSchedule", "زمان‌بندی‌های گزارش"),
    ]), "Reporting")

entity_file(E("ReportExecution", "Reporting", "ReportExecutions",
    "اجرا گزارش - رکورد هر بار تولید گزارش",
    [
        P("ReportId", "long", "شناسه گزارش"),
        P("UserId", "long", "شناسه کاربر اجراکننده"),
        P("ExecutedAt", "DateTime?", "زمان اجرا"),
        P("Parameters", "string?", "پارامترهای استفاده‌شده - JSON"),
        P("Duration", "int?", "مدت اجرا به میلی‌ثانیه"),
        P("ExportFormat", "string?", "قالب خروجی مثل PDF"),
        P("RowCount", "int?", "تعداد سطرهای نتیجه"),
        N("Report", "ReportDefinition?", "گزارش اجراشده"),
    ]), "Reporting")

entity_file(E("ReportSchedule", "Reporting", "ReportSchedules",
    "زمان‌بندی گزارش - تولید خودکار دوره‌ای",
    [
        P("ReportId", "long", "شناسه گزارش"),
        P("Frequency", "ReportFrequency", "دوره تکرار"),
        P("ScheduleTime", "TimeOnly?", "ساعت اجرا"),
        P("Recipients", "string?", "گیرندگان خروجی"),
        P("NextRun", "DateTime?", "اجرای بعدی"),
        P("LastRun", "DateTime?", "اجرای قبلی"),
        P("IsActive", "bool", "فعال", "true"),
        N("Report", "ReportDefinition?", "گزارش والد"),
    ],
    extra_usings=[f"using {EN};"]), "Reporting")

print("✅ بخش ۳C: موجودیت‌های گروه ۷ تا ۱۲ — کامل شد")

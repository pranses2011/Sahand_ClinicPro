namespace Sahand.ClinicPro.Application.Features.Reports.Adhoc;

/// <summary>
/// برچسب‌های فارسی شمارش‌های لیست سفید گزارش‌ساز رایگانه — چرخه سیزدهم (درخواست ۴-۳).
/// گزینه‌های هر فیلد شمارشی در طرح لیست سفید باید دقیقاً با خروجی این توابع یکی باشد
/// تا فیلتر «مساوی» و گروه‌بندی کار کند.
/// </summary>
public static class PersianEnum
{
    /// <summary>جنسیت</summary>
    public static string Gender(Domain.Enums.Gender g) => g switch
    {
        Domain.Enums.Gender.Male => "مرد",
        Domain.Enums.Gender.Female => "زن",
        _ => g.ToString(),
    };

    /// <summary>وضعیت تأهل</summary>
    public static string? Marital(Domain.Enums.MaritalStatus? m) => m switch
    {
        Domain.Enums.MaritalStatus.Single => "مجرد",
        Domain.Enums.MaritalStatus.Married => "متأهل",
        Domain.Enums.MaritalStatus.Widowed => "همسر فوت‌شده",
        Domain.Enums.MaritalStatus.Divorced => "طلاق‌گرفته",
        null => null,
        _ => m.ToString(),
    };

    /// <summary>نوع بیمار</summary>
    public static string PatientType(Domain.Enums.PatientType t) => t switch
    {
        Domain.Enums.PatientType.Normal => "عادی",
        Domain.Enums.PatientType.Vip => "ویژه",
        Domain.Enums.PatientType.Staff => "پرسنل",
        _ => t.ToString(),
    };

    /// <summary>وضعیت نوبت</summary>
    public static string AppointmentStatus(Domain.Enums.AppointmentStatus s) => s switch
    {
        Domain.Enums.AppointmentStatus.Booked => "رزرو شده",
        Domain.Enums.AppointmentStatus.Confirmed => "تأیید شده",
        Domain.Enums.AppointmentStatus.Arrived => "حاضر",
        Domain.Enums.AppointmentStatus.InProgress => "در حال ویزیت",
        Domain.Enums.AppointmentStatus.Completed => "تکمیل شده",
        Domain.Enums.AppointmentStatus.Cancelled => "لغو شده",
        Domain.Enums.AppointmentStatus.NoShow => "عدم مراجعه",
        _ => s.ToString(),
    };

    /// <summary>نوع نوبت</summary>
    public static string AppointmentType(Domain.Enums.AppointmentType t) => t switch
    {
        Domain.Enums.AppointmentType.Initial => "اولیه",
        Domain.Enums.AppointmentType.FollowUp => "پیگیری",
        Domain.Enums.AppointmentType.Emergency => "اورژانس",
        _ => t.ToString(),
    };

    /// <summary>منبع نوبت</summary>
    public static string AppointmentSource(Domain.Enums.AppointmentSource s) => s switch
    {
        Domain.Enums.AppointmentSource.InPerson => "حضوری",
        Domain.Enums.AppointmentSource.Phone => "تلفنی",
        Domain.Enums.AppointmentSource.Online => "آنلاین",
        _ => s.ToString(),
    };

    /// <summary>اولویت</summary>
    public static string Priority(Domain.Enums.Priority p) => p switch
    {
        Domain.Enums.Priority.Emergency => "اورژانس",
        Domain.Enums.Priority.High => "فوری",
        Domain.Enums.Priority.Normal => "عادی",
        _ => p.ToString(),
    };

    /// <summary>نوع ویزیت</summary>
    public static string VisitType(Domain.Enums.VisitType t) => t switch
    {
        Domain.Enums.VisitType.Initial => "اولیه",
        Domain.Enums.VisitType.FollowUp => "پیگیری",
        Domain.Enums.VisitType.Emergency => "اورژانس",
        Domain.Enums.VisitType.Consultation => "مشاوره",
        _ => t.ToString(),
    };

    /// <summary>وضعیت ویزیت</summary>
    public static string VisitStatus(Domain.Enums.VisitStatus s) => s switch
    {
        Domain.Enums.VisitStatus.Started => "شروع‌شده",
        Domain.Enums.VisitStatus.InProgress => "در حال ویزیت",
        Domain.Enums.VisitStatus.Completed => "تکمیل شده",
        Domain.Enums.VisitStatus.Draft => "پیش‌نویس",
        _ => s.ToString(),
    };

    /// <summary>نوع فاکتور</summary>
    public static string InvoiceType(Domain.Enums.InvoiceType t) => t switch
    {
        Domain.Enums.InvoiceType.Visit => "ویزیت",
        Domain.Enums.InvoiceType.Lab => "آزمایش",
        Domain.Enums.InvoiceType.Imaging => "تصویربرداری",
        Domain.Enums.InvoiceType.Pharmacy => "داروخانه",
        Domain.Enums.InvoiceType.Dental => "دندانپزشکی",
        Domain.Enums.InvoiceType.Physiotherapy => "فیزیوتراپی",
        Domain.Enums.InvoiceType.Other => "سایر",
        _ => t.ToString(),
    };

    /// <summary>وضعیت فاکتور</summary>
    public static string InvoiceStatus(Domain.Enums.InvoiceStatus s) => s switch
    {
        Domain.Enums.InvoiceStatus.Draft => "پیش‌نویس",
        Domain.Enums.InvoiceStatus.Issued => "صادره",
        Domain.Enums.InvoiceStatus.PartiallyPaid => "تسویه‌نشده",
        Domain.Enums.InvoiceStatus.Paid => "تسویه‌شده",
        Domain.Enums.InvoiceStatus.Cancelled => "لغو شده",
        Domain.Enums.InvoiceStatus.Refunded => "برگشتی",
        _ => s.ToString(),
    };

    /// <summary>وضعیت پرداخت فاکتور</summary>
    public static string PaymentStatus(Domain.Enums.PaymentStatus s) => s switch
    {
        Domain.Enums.PaymentStatus.Unpaid => "پرداخت‌نشده",
        Domain.Enums.PaymentStatus.PartiallyPaid => "تسویه‌نشده",
        Domain.Enums.PaymentStatus.Paid => "تسویه‌شده",
        Domain.Enums.PaymentStatus.Overpaid => "پرداخت اضافه",
        _ => s.ToString(),
    };

    /// <summary>روش پرداخت</summary>
    public static string PaymentMethod(Domain.Enums.PaymentMethod m) => m switch
    {
        Domain.Enums.PaymentMethod.Cash => "نقدی",
        Domain.Enums.PaymentMethod.Card => "کارت",
        Domain.Enums.PaymentMethod.Online => "آنلاین",
        Domain.Enums.PaymentMethod.Cheque => "چک",
        Domain.Enums.PaymentMethod.BankTransfer => "حواله بانکی",
        Domain.Enums.PaymentMethod.Insurance => "بیمه",
        _ => m.ToString(),
    };

    /// <summary>وضعیت رکورد پرداخت</summary>
    public static string PaymentRecordStatus(Domain.Enums.PaymentRecordStatus s) => s switch
    {
        Domain.Enums.PaymentRecordStatus.Pending => "در انتظار",
        Domain.Enums.PaymentRecordStatus.Completed => "تکمیل شده",
        Domain.Enums.PaymentRecordStatus.Failed => "ناموفق",
        Domain.Enums.PaymentRecordStatus.Refunded => "برگشتی",
        _ => s.ToString(),
    };

    /// <summary>نوع نسخه</summary>
    public static string PrescriptionType(Domain.Enums.PrescriptionType t) => t switch
    {
        Domain.Enums.PrescriptionType.Medication => "دارویی",
        Domain.Enums.PrescriptionType.Paraclinical => "پاراکلینیک",
        Domain.Enums.PrescriptionType.Combined => "ترکیبی",
        _ => t.ToString(),
    };

    /// <summary>وضعیت نسخه</summary>
    public static string PrescriptionStatus(Domain.Enums.PrescriptionStatus s) => s switch
    {
        Domain.Enums.PrescriptionStatus.Issued => "صادره",
        Domain.Enums.PrescriptionStatus.SentToInsurance => "ارسال به بیمه",
        Domain.Enums.PrescriptionStatus.InsuranceApproved => "تأیید بیمه",
        Domain.Enums.PrescriptionStatus.Delivered => "تحویل‌شده",
        Domain.Enums.PrescriptionStatus.Cancelled => "لغو شده",
        Domain.Enums.PrescriptionStatus.Expired => "منقضی",
        _ => s.ToString(),
    };

    /// <summary>وضعیت درخواست آزمایش</summary>
    public static string LabOrderStatus(Domain.Enums.LabOrderStatus s) => s switch
    {
        Domain.Enums.LabOrderStatus.Ordered => "درخواست‌شده",
        Domain.Enums.LabOrderStatus.SampleCollected => "نمونه‌گیری‌شده",
        Domain.Enums.LabOrderStatus.InProgress => "در حال انجام",
        Domain.Enums.LabOrderStatus.ResultReady => "آماده",
        Domain.Enums.LabOrderStatus.Verified => "تأییدشده",
        Domain.Enums.LabOrderStatus.Delivered => "تحویل‌شده",
        Domain.Enums.LabOrderStatus.Cancelled => "لغو شده",
        _ => s.ToString(),
    };
}

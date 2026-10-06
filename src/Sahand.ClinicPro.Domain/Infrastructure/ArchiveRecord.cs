namespace Sahand.ClinicPro.Domain.Entities.Infrastructure;

/// <summary>
/// رکورد آرشیو سالانه — چرخه سیزدهم (درخواست ۷-۷).
/// داده‌های قدیمی‌تر از سال انتخابی از جداول عملیاتی (نوبت/ویزیت/پذیرش/
/// فاکتور/پرداخت/نسخه/آزمایش) به این جدول منتقل می‌شوند: ستون‌های کلیدی
/// ایندکس‌شده + تصویر کامل ردیف به‌صورت JSON — بانک عملیاتی سبک می‌ماند
/// و پرونده قدیمی بیمار از مرورگر آرشیو قابل جستجو است.
/// </summary>
public class ArchiveRecord : AuditableEntity, ISoftDeletable
{
    /// <summary>نام جدول مبدأ — مثل Appointments</summary>
    public string SourceTable { get; set; } = null!;

    /// <summary>شناسه ردیف در جدول مبدأ</summary>
    public long SourceId { get; set; }

    /// <summary>شناسه بیمار — برای جستجوی پرونده قدیمی</summary>
    public long? PatientId { get; set; }

    /// <summary>نام بیمار هنگام آرشیو — برای جستجو بدون join</summary>
    public string? PatientName { get; set; }

    /// <summary>تاریخ کسب‌وکارِ ردیف اصلی</summary>
    public DateTime RecordDate { get; set; }

    /// <summary>سال شمسی رکورد — پیش‌محرک گزارش‌های آرشیو</summary>
    public int YearJalali { get; set; }

    /// <summary>خلاصه‌سطر نمایشی در مرورگر — مثل «نوبت رزرو شده»</summary>
    public string? SummaryLine { get; set; }

    /// <summary>تصویر کامل ردیف به‌صورت JSON</summary>
    public string Payload { get; set; } = null!;

    /// <summary>شناسه کاربر آرشیوکننده</summary>
    public long? ArchivedBy { get; set; }

    /// <summary>زمان آرشیو</summary>
    public DateTime ArchivedAt { get; set; }
}

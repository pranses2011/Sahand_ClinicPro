using System;

namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// موجودیت قابل ردگیری — فیلدهای حسابرسی و حذف نرم و چندشعبه‌ای را اضافه می‌کند
/// فیلدهای ایجاد و ویرایش توسط AuditableInterceptor به‌صورت خودکار پر می‌شوند
/// </summary>
public abstract class AuditableEntity : BaseEntity, ISoftDeletable
{
    /// <summary>تاریخ و ساعت ایجاد رکورد (UTC)</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>شناسه کاربر ایجادکننده</summary>
    public long? CreatedBy { get; set; }

    /// <summary>تاریخ و ساعت آخرین ویرایش (UTC)</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>شناسه کاربر ویرایش‌کننده</summary>
    public long? UpdatedBy { get; set; }

    /// <summary>آیا رکورد حذف (نرم) شده است</summary>
    public bool IsDeleted { get; set; }

    /// <summary>تاریخ حذف نرم</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>شناسه کاربر حذف‌کننده</summary>
    public long? DeletedBy { get; set; }

    /// <summary>شناسه شعبه برای چندشعبه‌ای آینده</summary>
    public long? BranchId { get; set; }

    /// <summary>نسخه ردیف برای کنترل همزمانی خوش‌بینانه (Concurrency Token)</summary>
    public byte[]? RowVersion { get; set; }
}

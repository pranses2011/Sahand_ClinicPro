using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Infrastructure;

/// <summary>
/// تاریخچه پشتیبان‌گیری - سه‌سطحی: کامل و افزایشی و لاگ
/// </summary>
public class BackupHistory : AuditableEntity, ISoftDeletable
{
    /// <summary>نوع پشتیبان</summary>
    public BackupType BackupType { get; set; }

    /// <summary>مسیر فایل پشتیبان</summary>
    public string? FilePath { get; set; }

    /// <summary>حجم به بایت</summary>
    public long? FileSize { get; set; }

    /// <summary>شروع عملیات</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>پایان عملیات</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>وضعیت</summary>
    public BackupStatus Status { get; set; }

    /// <summary>خودکار توسط زمان‌بند</summary>
    public bool IsAutomatic { get; set; } = true;

    /// <summary>محل ذخیره</summary>
    public StorageType StorageType { get; set; }

    /// <summary>خطای احتمالی</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>شناسه اجراکننده</summary>
    public long? PerformedBy { get; set; }
}

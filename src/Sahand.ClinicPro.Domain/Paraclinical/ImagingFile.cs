namespace Sahand.ClinicPro.Domain.Entities.Paraclinical;

/// <summary>
/// فایل تصویربرداری - DICOM یا تصویر معمولی
/// </summary>
public class ImagingFile : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه درخواست تصویربرداری</summary>
    public long ImagingOrderId { get; set; }

    /// <summary>نام فایل</summary>
    public string? FileName { get; set; }

    /// <summary>مسیر ذخیره‌سازی</summary>
    public string? FilePath { get; set; }

    /// <summary>نوع فایل</summary>
    public string? FileType { get; set; }

    /// <summary>حجم فایل به بایت</summary>
    public long? FileSize { get; set; }

    /// <summary>فرمت DICOM</summary>
    public bool IsDicom { get; set; } = false;

    /// <summary>مسیر پیش‌نمایش</summary>
    public string? ThumbnailPath { get; set; }

    /// <summary>زمان بارگذاری</summary>
    public DateTime? UploadedAt { get; set; }

    /// <summary>درخواست والد</summary>
    public ImagingOrder? ImagingOrder { get; set; }
}

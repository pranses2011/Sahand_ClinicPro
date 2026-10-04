using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Infrastructure;

/// <summary>
/// سند - فایل پیوست هر رکورد سیستم
/// </summary>
public class Document : AuditableEntity, ISoftDeletable
{
    /// <summary>عنوان سند</summary>
    public string Title { get; set; } = null!;

    /// <summary>توضیحات</summary>
    public string? Description { get; set; }

    /// <summary>شناسه دسته سند</summary>
    public long? CategoryId { get; set; }

    /// <summary>نوع رکورد مرجع</summary>
    public DocumentReferenceType ReferenceType { get; set; }

    /// <summary>شناسه رکورد مرجع</summary>
    public long? ReferenceId { get; set; }

    /// <summary>نام فایل</summary>
    public string? FileName { get; set; }

    /// <summary>مسیر ذخیره‌سازی</summary>
    public string? FilePath { get; set; }

    /// <summary>پسوند فایل</summary>
    public string? FileType { get; set; }

    /// <summary>حجم به بایت</summary>
    public long? FileSize { get; set; }

    /// <summary>نوع MIME</summary>
    public string? MimeType { get; set; }

    /// <summary>هش فایل برای بررسی یکپارچگی</summary>
    public string? Hash { get; set; }

    /// <summary>رمزنگاری‌شده</summary>
    public bool IsEncrypted { get; set; } = false;

    /// <summary>برچسب‌ها</summary>
    public string? Tags { get; set; }

    /// <summary>شناسه بارگذار</summary>
    public long? UploadedBy { get; set; }

    /// <summary>زمان بارگذاری</summary>
    public DateTime? UploadedAt { get; set; }

    /// <summary>دسته سند</summary>
    public DocumentCategory? Category { get; set; }
}

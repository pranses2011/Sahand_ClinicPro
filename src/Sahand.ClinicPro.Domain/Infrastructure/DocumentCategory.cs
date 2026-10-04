namespace Sahand.ClinicPro.Domain.Entities.Infrastructure;

/// <summary>
/// دسته سند - طبقه‌بندی درختی فایل‌ها
/// </summary>
public class DocumentCategory : AuditableEntity, ISoftDeletable
{
    /// <summary>نام دسته</summary>
    public string Name { get; set; } = null!;

    /// <summary>کد یکتا</summary>
    public string? Code { get; set; }

    /// <summary>شناسه دسته والد</summary>
    public long? ParentCategoryId { get; set; }

    /// <summary>آیکون</summary>
    public string? IconPath { get; set; }

    /// <summary>دسته والد</summary>
    public DocumentCategory? Parent { get; set; }

    /// <summary>دسته‌های فرزند</summary>
    public ICollection<DocumentCategory> Children { get; set; } = new List<DocumentCategory>();

    /// <summary>اسناد این دسته</summary>
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}

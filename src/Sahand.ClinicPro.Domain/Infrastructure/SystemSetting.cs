using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Infrastructure;

/// <summary>
/// تنظیمات سیستم - کلید و مقدار با پشتیبانی رمزنگاری
/// </summary>
public class SystemSetting : AuditableEntity, ISoftDeletable
{
    /// <summary>دسته تنظیم مثل عمومی یا مالی</summary>
    public string? Category { get; set; }

    /// <summary>کلید تنظیم - یکتا</summary>
    public string Key { get; set; } = null!;

    /// <summary>مقدار</summary>
    public string? Value { get; set; }

    /// <summary>نوع داده</summary>
    public SettingDataType DataType { get; set; }

    /// <summary>توضیح</summary>
    public string? Description { get; set; }

    /// <summary>مقدار رمزنگاری‌شده</summary>
    public bool IsEncrypted { get; set; } = false;

    /// <summary>تنظیم سیستمی غیرقابل حذف</summary>
    public bool IsSystem { get; set; } = false;
}

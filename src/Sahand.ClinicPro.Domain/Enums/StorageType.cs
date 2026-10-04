namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// محل ذخیره‌سازی
/// </summary>
public enum StorageType : byte
{
    /// <summary>محلی</summary>
    Local = 1,
    /// <summary>شبکه</summary>
    NetworkShare = 2,
    /// <summary>ابر</summary>
    Cloud = 3,
}

namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع پشتیبان‌گیری
/// </summary>
public enum BackupType : byte
{
    /// <summary>کامل</summary>
    Full = 1,
    /// <summary>افزایشی</summary>
    Differential = 2,
    /// <summary>لاگ تراکنش</summary>
    Log = 3,
}

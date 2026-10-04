namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع داده تنظیمات
/// </summary>
public enum SettingDataType : byte
{
    /// <summary>متن</summary>
    String = 1,
    /// <summary>عدد</summary>
    Number = 2,
    /// <summary>منطقی</summary>
    Boolean = 3,
    /// <summary>تاریخ</summary>
    Date = 4,
    /// <summary>جیسون</summary>
    Json = 5,
}

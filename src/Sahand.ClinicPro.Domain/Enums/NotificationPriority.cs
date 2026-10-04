namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// اولویت اعلان
/// </summary>
public enum NotificationPriority : byte
{
    /// <summary>کم</summary>
    Low = 1,
    /// <summary>عادی</summary>
    Normal = 2,
    /// <summary>زیاد</summary>
    High = 3,
    /// <summary>بحرانی</summary>
    Critical = 4,
}

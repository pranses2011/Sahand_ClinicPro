namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// جهت تماس
/// </summary>
public enum CallDirection : byte
{
    /// <summary>ورودی</summary>
    Inbound = 1,
    /// <summary>خروجی</summary>
    Outbound = 2,
}

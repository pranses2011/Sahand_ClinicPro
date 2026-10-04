namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// شدت رخداد حسابرسی
/// </summary>
public enum AuditSeverity : byte
{
    /// <summary>اطلاع</summary>
    Info = 1,
    /// <summary>هشدار</summary>
    Warning = 2,
    /// <summary>بحرانی</summary>
    Critical = 3,
}

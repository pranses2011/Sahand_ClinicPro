namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع مرخصی
/// </summary>
public enum LeaveType : byte
{
    /// <summary>استحقاقی</summary>
    Vacation = 1,
    /// <summary>استعلاجی</summary>
    SickLeave = 2,
    /// <summary>بدون حقوق</summary>
    Unpaid = 3,
    /// <summary>مأموریت</summary>
    Mission = 4,
}

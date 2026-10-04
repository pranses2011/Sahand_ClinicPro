namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت قرارداد
/// </summary>
public enum ContractStatus : byte
{
    /// <summary>فعال</summary>
    Active = 1,
    /// <summary>منقضی</summary>
    Expired = 2,
    /// <summary>فسخ شده</summary>
    Terminated = 3,
}

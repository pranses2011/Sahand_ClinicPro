namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع ارجاع
/// </summary>
public enum ReferralType : byte
{
    /// <summary>وارده</summary>
    Inbound = 1,
    /// <summary>صادره</summary>
    Outbound = 2,
}

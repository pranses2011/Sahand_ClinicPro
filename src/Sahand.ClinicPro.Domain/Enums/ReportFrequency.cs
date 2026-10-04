namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// دوره تکرار گزارش
/// </summary>
public enum ReportFrequency : byte
{
    /// <summary>روزانه</summary>
    Daily = 1,
    /// <summary>هفتگی</summary>
    Weekly = 2,
    /// <summary>ماهانه</summary>
    Monthly = 3,
    /// <summary>فصلی</summary>
    Quarterly = 4,
    /// <summary>سالانه</summary>
    Yearly = 5,
}

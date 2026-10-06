namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع قاعده تسویه بیمه — درصدی یا اسمی (چرخه ۱۳، درخواست ۲-۱)
/// </summary>
public enum FractionmentRuleType : byte
{
    /// <summary>درصدی — سهم بیمه درصدی از مبلغ است</summary>
    Percentage = 1,

    /// <summary>اسمی — سهم بیمه مبلغ ثابت ریالی است (قراردادهای خدامت‌به‌خدمت)</summary>
    Nominal = 2
}

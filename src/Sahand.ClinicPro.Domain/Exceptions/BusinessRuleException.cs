namespace Sahand.ClinicPro.Domain.Exceptions;

/// <summary>
/// نقض قاعده کسب‌وکار (مثال: ظرفیت نوبت تکمیل است)
/// </summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message, "BUSINESS_RULE_VIOLATION") { }
}

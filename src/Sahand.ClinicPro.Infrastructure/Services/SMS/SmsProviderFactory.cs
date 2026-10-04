using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.SMS;

/// <summary>
/// کارخانه پنل‌های پیامک — زنجیره جایگزینی بر اساس اولویت
/// اگر پنل اصلی خطا داد پنل بعدی به‌صورت خودکار امتحان می‌شود
/// </summary>
public class SmsProviderFactory
{
    private readonly IEnumerable<ISmsProvider> _providers;

    /// <summary>سازنده با تزریق همه پنل‌های ثبت‌شده</summary>
    public SmsProviderFactory(IEnumerable<ISmsProvider> providers)
    {
        _providers = providers.OrderBy(p => p.Priority);
    }

    /// <summary>زنجیره پنل‌ها به ترتیب اولویت</summary>
    public IReadOnlyList<ISmsProvider> GetChain() => _providers.ToList();

    /// <summary>پنل پیش‌فرض — اولین زنجیره</summary>
    public ISmsProvider? GetDefault() => _providers.FirstOrDefault();

    /// <summary>یافتن پنل بر اساس کد</summary>
    public ISmsProvider? GetByCode(string code) =>
        _providers.FirstOrDefault(p =>
            string.Equals(p.ProviderCode, code, StringComparison.OrdinalIgnoreCase));
}

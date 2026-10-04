namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// قرارداد یک پنل پیامک — هر ده پنل ایرانی این را پیاده می‌کند
/// </summary>
public interface ISmsProvider
{
    /// <summary>کد یکتای پنل مثل KAVENEGAR</summary>
    string ProviderCode { get; }

    /// <summary>اولویت در زنجیره جایگزینی — عدد کوچک‌تر زودتر</summary>
    int Priority { get; }

    /// <summary>ارسال پیامک خام از طریق API پنل</summary>
    Task<SmsSendResult> SendRawAsync(string recipient, string message,
        CancellationToken ct = default);

    /// <summary>بررسی موجودی اعتبار پنل</summary>
    Task<decimal> GetBalanceAsync(CancellationToken ct = default);
}

namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>درخواست استعلام اعتبار بیمه</summary>
/// <param name="PatientId">شناسه بیمار</param>
/// <param name="InsuranceCompanyId">شناسه شرکت بیمه</param>
/// <param name="NationalCode">کد ملی بیمار</param>
/// <param name="InsuranceNumber">شماره بیمه — اختیاری</param>
public record InsuranceEligibilityRequest(
    long PatientId,
    long InsuranceCompanyId,
    string NationalCode,
    string? InsuranceNumber = null);

/// <summary>نتیجه استعلام اعتبار بیمه</summary>
/// <param name="IsEligible">بیمه معتبر و پوشش‌دار است</param>
/// <param name="WasOnline">استعلام از سامانه بیمه برخط پاسخ گرفت</param>
/// <param name="Message">پیام فارسی نتیجه</param>
/// <param name="ApprovalCode">کد تأیید بیمه در استعلام موفق</param>
/// <param name="ValidUntil">اعتبار پوشش تا</param>
public record InsuranceEligibilityResult(
    bool IsEligible,
    bool WasOnline,
    string Message,
    string? ApprovalCode = null,
    DateOnly? ValidUntil = null);

/// <summary>
/// قرارداد سرویس استعلام برخط اعتبار بیمه — در لحظه پذیرش فراخوانده می‌شود
/// </summary>
/// <remarks>
/// پیاده‌سازی زیرساخت با سه تلاش و کش ۳۰ دقیقه‌ای کار می‌کند و در قطع ارتباط
/// به حالت برون‌خط (بررسی قرارداد محلی) سقوط می‌کند.
/// </remarks>
public interface IInsuranceEligibilityService
{
    /// <summary>استعلام اعتبار پوشش بیمه بیمار</summary>
    Task<InsuranceEligibilityResult> CheckAsync(InsuranceEligibilityRequest request,
        CancellationToken ct = default);
}
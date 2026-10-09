using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر لایسنس — وضعیت و فعال‌سازی برخط و برون‌خط
/// مسیرها: /api/v1/license — بدون نیاز به احراز هویت برای فعال‌سازی
/// چرخه هجدهم (گزارش دوازدهم مالک): نشانی سرور لایسنس قابل خواندن/تغییر،
/// فایل درخواست آفلاین (.req) قابل دریافت و اطلاعات پشتیبانی همیشه موجود است.
/// </summary>
[ApiController]
[Route("api/v1/license")]
public class LicenseController : ApiBaseController
{
    private readonly ILicenseService _license;

    /// <summary>سازنده با تزریق سرویس لایسنس</summary>
    public LicenseController(ILicenseService license) => _license = license;

    /// <summary>وضعیت فعلی لایسنس — ماژول‌ها، گنجایش‌ها و اطلاعات پشتیبانی</summary>
    [HttpGet("status")]
    [AllowAnonymous]
    // عمدتاً عمومی برای همه کاربران (ممیزی چرخه ۱۲)؛ چرخه هجدهم: AllowAnonymous شد تا
    // پنجرهٔ فعال‌سازی پیش از ورود هم وضعیت را ببیند — داده‌ها فقط کلید/انقضا/ماژول‌هاست
    public async Task<IActionResult> GetStatus()
    {
        var status = await _license.GetStatusAsync();
        var support = await _license.GetSupportInfoAsync();
        var response = new
        {
            isValid = status.IsValid,
            licenseKey = status.LicenseKey,
            expiryDate = status.ExpiryDate,
            maxUsers = status.MaxUsers,
            maxDoctors = status.MaxDoctors,
            maxPatients = status.MaxPatients,
            enabledModules = status.EnabledModules,
            supportInfo = support.HasData ? new
            {
                name = support.Name,
                company = support.Company,
                phone = support.Phone,
                mobile = support.Mobile,
                email = support.Email,
                website = support.Website,
                source = support.Source
            } : null,
            message = status.Message
        };
        return Ok(ApiResponse<object>.Success(response, status.Message));
    }

    /// <summary>
    /// اطلاعات پشتیبانی محصول — چرخه هجدهم (گزارش دوازدهم):
    /// همیشه محتوا دارد — اول مقادیر ذخیره‌شده در دیتابیس، در نبود آن
    /// تازه‌سازی best-effort از سرور لایسنس (action=support) و در نبود هر دو،
    /// مقادیر پیش‌فرض محصول. «از دفعات بعد بروزرسانی از سرور لایسنس» همین‌جاست.
    /// </summary>
    [HttpGet("support")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSupportInfo(bool refresh = false)
    {
        var support = refresh
            ? await _license.RefreshSupportInfoAsync()
            : await _license.GetSupportInfoAsync();
        var response = new
        {
            name = support.Name,
            company = support.Company,
            phone = support.Phone,
            mobile = support.Mobile,
            email = support.Email,
            website = support.Website,
            hasData = support.HasData,
            source = support.Source
        };
        return Ok(ApiResponse<object>.Success(response, "اطلاعات پشتیبانی"));
    }

    /// <summary>
    /// نشانی سرور لایسنس — چرخه هجدهم: صفحهٔ فعال‌سازی این مقدار را در
    /// فیلد «سرور لایسنس» نشان می‌دهد و کاربر می‌تواند عوضش کند.
    /// </summary>
    [HttpGet("settings")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSettings()
    {
        var portalUrl = await _license.GetPortalUrlAsync();
        var response = new { portalUrl };
        return Ok(ApiResponse<object>.Success(response, "تنظیمات سرور لایسنس"));
    }

    /// <summary>
    /// ذخیرهٔ نشانی سرور لایسنس واردشده از صفحهٔ فعال‌سازی —
    /// ریشهٔ نشانی هم پذیرفته می‌شود و به api.php نرمال می‌شود.
    /// </summary>
    [HttpPut("settings")]
    [AllowAnonymous]
    public async Task<IActionResult> SaveSettings([FromBody] SaveLicenseSettingsRequest request)
    {
        var ok = await _license.SetPortalUrlAsync(request.PortalUrl ?? string.Empty);
        if (!ok)
            return BadRequest(ApiResponse<object>.Failure(
                "نشانی سرور لایسنس نامعتبر است — باید http/https باشد (مثل https://clinic-license.ea-fixer.ir)"));
        var portalUrl = await _license.GetPortalUrlAsync();
        return Ok(ApiResponse<object>.Success(new { portalUrl }, "نشانی سرور لایسنس ذخیره شد"));
    }

    /// <summary>
    /// درخواست فعال‌سازی آفلاین (فایل .req) — چرخه هجدهم (گزارش دوازدهم):
    /// کلاینت این پاسخ را با پسوند .req ذخیره می‌کند و برای فروشنده می‌فرستد؛
    /// فروشنده در پنل، محتوای همین فایل را می‌پذیرد و فایل .lic برمی‌گرداند.
    /// </summary>
    /// <param name="licenseKey">کلید اختیاری — اگر هستوید در فایل درج می‌شود</param>
    [HttpGet("activation-request")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActivationRequest([FromQuery] string? licenseKey)
    {
        var request = await _license.BuildActivationRequestAsync(licenseKey);
        return Ok(ApiResponse<object>.Success(request, "درخواست فعال‌سازی آفلاین ساخته شد"));
    }

    /// <summary>فعال‌سازی برخط با کلید لایسنس</summary>
    [HttpPost("activate")]
    [AllowAnonymous]
    public async Task<IActionResult> ActivateOnline([FromBody] ActivateRequest request)
    {
        var status = await _license.ActivateOnlineAsync(request.LicenseKey);
        if (!status.IsValid)
            return BadRequest(ApiResponse<object>.Failure(status.Message));
        return Ok(ApiResponse<object>.SuccessMessage("لایسنس با موفقیت فعال شد"));
    }

    /// <summary>فعال‌سازی برون‌خط از محتوای فایل lic</summary>
    [HttpPost("activate-offline")]
    [AllowAnonymous]
    public async Task<IActionResult> ActivateOffline([FromBody] ActivateOfflineRequest request)
    {
        var status = await _license.ActivateOfflineAsync(request.LicenseFileContent);
        if (!status.IsValid)
            return BadRequest(ApiResponse<object>.Failure(status.Message));
        return Ok(ApiResponse<object>.SuccessMessage("لایسنس برون‌خط فعال شد"));
    }

    /// <summary>
    /// تازه‌سازی لایسنس از سرور لایسنس — چرخهٔ سی‌ودوم (گزارش مالک):
    /// «ماژول جدید در پنل لایسنس فعال کردم ولی هنوز غیرفعال نشان می‌دهد» —
    /// فهرست ماژول‌ها/گنجایش‌ها و اطلاعات درمانگاه فقط با تپ قلب یا فعال‌سازی
    /// مجدد به‌روز می‌شدند. این اندپوینت همان تپ قلب (HeartbeatAsync) را در
    /// لحظه اجرا می‌کند: حضور به سرور لایسنس اعلام، محدودیت‌ها/ماژول‌ها/اطلاعات
    /// درمانگاه تازه و پیام‌های پشتیبانی تحویل می‌شوند؛ پاسخ، وضعیت تازه است.
    /// در قطعی شبکه، وضعیت محلی فعلی (مهلت آفلاین) بی‌خطا برمی‌گردد.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    // عمومی مثل وضعیت — پیش از ورود هم (پنجرهٔ فعال‌سازی) قابل فراخوان است
    public async Task<IActionResult> RefreshFromServer()
    {
        var status = await _license.HeartbeatAsync();
        var response = new
        {
            isValid = status.IsValid,
            licenseKey = status.LicenseKey,
            expiryDate = status.ExpiryDate,
            maxUsers = status.MaxUsers,
            maxDoctors = status.MaxDoctors,
            maxPatients = status.MaxPatients,
            enabledModules = status.EnabledModules,
            message = status.Message
        };
        return Ok(ApiResponse<object>.Success(response, status.Message));
    }

    /// <summary>
    /// پیام‌های مستقیم پشتیبانی به داشبورد درمانگاه — چرخه دوازدهم:
    /// با هر تپ‌قلب موفق از سرور لایسنس (پنل PHP) دریافت و در تنظیمات محلی
    /// ذخیره می‌شوند؛ داشبورد دسکتاپ اینجا می‌خواند و در کارت «پیام‌های پشتیبانی» می‌نمایاند
    /// </summary>
    [HttpGet("messages")]
    [Authorize]
    // عمدتاً عمومی برای همه کاربران واردشده (ممیزی چرخه ۱۲): پیام‌های پشتیبانی روی داشبورد همه کاربران
    public async Task<IActionResult> GetSupportMessages()
    {
        var messages = await _license.GetSupportMessagesAsync();
        var response = messages.Select(m => new
        {
            id = m.Id,
            title = m.Title,
            body = m.Body,
            level = m.Level,
            date = m.Date,
            levelIcon = m.LevelIcon,
            levelBrush = m.LevelBrush
        });
        return Ok(ApiResponse<object>.Success(response, $"{messages.Count} پیام پشتیبانی"));
    }

    /// <summary>درخواست فعال‌سازی برخط</summary>
    public record ActivateRequest(string LicenseKey);

    /// <summary>درخواست فعال‌سازی برون‌خط</summary>
    public record ActivateOfflineRequest(string LicenseFileContent);

    /// <summary>بدنهٔ ذخیرهٔ تنظیمات سرور لایسنس — چرخه هجدهم</summary>
    public record SaveLicenseSettingsRequest(string? PortalUrl);
}

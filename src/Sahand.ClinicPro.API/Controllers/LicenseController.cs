using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر لایسنس — وضعیت و فعال‌سازی برخط و برون‌خط
/// مسیرها: /api/v1/license — بدون نیاز به احراز هویت برای فعال‌سازی
/// </summary>
[ApiController]
[Route("api/v1/license")]
public class LicenseController : ApiBaseController
{
    private readonly ILicenseService _license;

    /// <summary>سازنده با تزریق سرویس لایسنس</summary>
    public LicenseController(ILicenseService license) => _license = license;

    /// <summary>وضعیت فعلی لایسنس — ماژول‌ها و گنجایش‌ها</summary>
    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _license.GetStatusAsync();
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

    /// <summary>درخواست فعال‌سازی برخط</summary>
    public record ActivateRequest(string LicenseKey);

    /// <summary>درخواست فعال‌سازی برون‌خط</summary>
    public record ActivateOfflineRequest(string LicenseFileContent);
}

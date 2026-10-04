using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر یکپارچه‌سازی دولتی — سپاس و نسخه الکترونیک
/// مسیرها: /api/v1/government
/// </summary>
[ApiController]
[Route("api/v1/government")]
[Authorize]
public class GovernmentController : ApiBaseController
{
    private readonly ISepasService _sepas;
    private readonly IElectronicPrescriptionService _ePrescription;

    /// <summary>سازنده با تزریق سرویس‌ها</summary>
    public GovernmentController(ISepasService sepas,
        IElectronicPrescriptionService ePrescription)
    {
        _sepas = sepas;
        _ePrescription = ePrescription;
    }

    /// <summary>ثبت ویزیت در سامانه سپاس</summary>
    /// <param name="visitId">شناسه ویزیت</param>
    [HttpPost("sepas/encounters/{visitId:long}")]
    [HasPermission("appointments.edit")]
    public async Task<IActionResult> RegisterEncounter(long visitId)
    {
        var result = await _sepas.RegisterEncounterAsync(visitId);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Failure(result.Error ?? "ثبت ناموفق بود"));
        return Ok(ApiResponse<object>.SuccessMessage(
            $"ویزیت با شناسه ارجاع {result.ReferenceId} در سپاس ثبت شد"));
    }

    /// <summary>ارسال نسخه به سامانه نسخه الکترونیک</summary>
    /// <param name="prescriptionId">شناسه نسخه</param>
    [HttpPost("eprescription/{prescriptionId:long}")]
    [HasPermission("prescriptions.edit")]
    public async Task<IActionResult> SendPrescription(long prescriptionId)
    {
        var result = await _ePrescription.SendAsync(prescriptionId);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Failure(result.Error ?? "ارسال ناموفق بود"));
        return Ok(ApiResponse<object>.SuccessMessage(
            $"نسخه با کد رهگیری {result.ReferenceId} ارسال شد"));
    }
}

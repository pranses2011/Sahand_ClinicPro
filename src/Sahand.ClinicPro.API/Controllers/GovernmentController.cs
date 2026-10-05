using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Integration;
using Sahand.ClinicPro.Domain.Interfaces;

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
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق سرویس‌ها و واحد کار (گام U18)</summary>
    public GovernmentController(ISepasService sepas,
        IElectronicPrescriptionService ePrescription, IUnitOfWork unitOfWork)
    {
        _sepas = sepas;
        _ePrescription = ePrescription;
        _unitOfWork = unitOfWork;
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

    /* ═══════════════════════════════════════════════════════════
       گام U18 — نمای‌های مانیتورینگ (فرم‌های بخش ۵ پرامپت)
       ═══════════════════════════════════════════════════════════ */

    /// <summary>آمار کلی سپاس — تراکنش‌ها به تفکیک وضعیت همگام‌سازی</summary>
    [HttpGet("sepas/stats")]
    [HasPermission("appointments.view")]
    public async Task<IActionResult> GetSepasStats()
    {
        var transactions = _unitOfWork.Repository<SepasTransaction>().QueryableNoTracking;
        var total = await transactions.CountAsync();
        var synced = await transactions.CountAsync(t => t.Status == Domain.Enums.SyncStatus.Synced);
        var pending = await transactions.CountAsync(t => t.Status == Domain.Enums.SyncStatus.Pending);
        var failed = await transactions.CountAsync(t => t.Status == Domain.Enums.SyncStatus.Failed);
        var todayCount = await transactions.CountAsync(t => t.SentAt >= DateTime.UtcNow.Date);

        return Ok(ApiResponse<object>.Success(new
        {
            total, synced, pending, failed, todayCount,
            lastSyncAt = await transactions
                .Where(t => t.RespondedAt != null)
                .MaxAsync(t => (DateTime?)t.RespondedAt!)
        }));
    }

    /// <summary>فهرست تراکنش‌های سپاس — تب ۲ فرم سپاس</summary>
    [HttpGet("sepas/transactions")]
    [HasPermission("appointments.view")]
    public async Task<IActionResult> GetSepasTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _unitOfWork.Repository<SepasTransaction>().QueryableNoTracking
            .OrderByDescending(t => t.SentAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new
            {
                t.Id,
                patient = t.Patient != null ? t.Patient.FirstName + " " + t.Patient.LastName : "—",
                type = t.TransactionType.ToString(),
                referenceId = t.SepasReferenceId,
                status = t.Status.ToString(),
                t.RetryCount,
                sentAt = t.SentAt,
                respondedAt = t.RespondedAt,
                error = t.ErrorMessage
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new { items, totalCount = total, page, pageSize }));
    }

    /// <summary>بازارسال تراکنش ناموفق سپاس</summary>
    [HttpPost("sepas/transactions/{id:long}/retry")]
    [HasPermission("appointments.edit")]
    public async Task<IActionResult> RetrySepasTransaction(long id)
    {
        var transaction = await _unitOfWork.Repository<SepasTransaction>().QueryableTracking
            .FirstOrDefaultAsync(t => t.Id == id);
        if (transaction is null)
            return BadRequest(ApiResponse<object>.Failure("تراکنش یافت نشد"));
        if (transaction.VisitId is null)
            return BadRequest(ApiResponse<object>.Failure("این تراکنش ویزیت مرتبط ندارد"));

        var result = await _sepas.RegisterEncounterAsync(transaction.VisitId.Value);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Failure(result.Error ?? "بازارسال ناموفق بود"));

        transaction.Status = Domain.Enums.SyncStatus.Synced;
        transaction.RetryCount++;
        transaction.RespondedAt = DateTime.UtcNow;
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("تراکنش با موفقیت بازارسال شد"));
    }

    /// <summary>فهرست نسخه‌های الکترونیک — تب پایش</summary>
    [HttpGet("eprescription/list")]
    [HasPermission("prescriptions.view")]
    public async Task<IActionResult> GetEPrescriptions([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _unitOfWork.Repository<ElectronicPrescription>().QueryableNoTracking
            .OrderByDescending(e => e.SentAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.PrescriptionId,
                trackingCode = e.TrackingCode,
                status = e.Status.ToString(),
                e.IsSuccess,
                errorCode = e.ErrorCode,
                sentAt = e.SentAt
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new { items, totalCount = total, page, pageSize }));
    }

    /// <summary>آمار نسخه الکترونیک — کارت‌های تب آمار</summary>
    [HttpGet("eprescription/stats")]
    [HasPermission("prescriptions.view")]
    public async Task<IActionResult> GetEPrescriptionStats()
    {
        var all = _unitOfWork.Repository<ElectronicPrescription>().QueryableNoTracking;
        var total = await all.CountAsync();
        var success = await all.CountAsync(e => e.IsSuccess);
        var todayCount = await all.CountAsync(e => e.SentAt >= DateTime.UtcNow.Date);

        return Ok(ApiResponse<object>.Success(new
        {
            total,
            success,
            failed = total - success,
            todayCount,
            successRate = total == 0 ? 0 : Math.Round(success * 100.0 / total, 1)
        }));
    }

    /// <summary>بازارسال نسخه ناموفق به سامانه نسخه الکترونیک</summary>
    [HttpPost("eprescription/{id:long}/resend")]
    [HasPermission("prescriptions.edit")]
    public async Task<IActionResult> ResendEPrescription(long id)
    {
        var record = await _unitOfWork.Repository<ElectronicPrescription>().QueryableTracking
            .FirstOrDefaultAsync(e => e.Id == id);
        if (record is null)
            return BadRequest(ApiResponse<object>.Failure("رکورد یافت نشد"));

        var result = await _ePrescription.SendAsync(record.PrescriptionId);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Failure(result.Error ?? "بازارسال ناموفق بود"));

        record.IsSuccess = true;
        record.TrackingCode = result.ReferenceId;
        record.SentAt = DateTime.UtcNow;
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage($"نسخه با کد رهگیری {result.ReferenceId} بازارسال شد"));
    }

    /// <summary>لاگ فراخوانی API سامانه‌های دولتی — تب ۵ با Request/Response</summary>
    [HttpGet("api-call-logs")]
    [HasPermission("auditlogs.view")]
    public async Task<IActionResult> GetApiCallLogs([FromQuery] string? service,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _unitOfWork.Repository<ApiCallLog>().QueryableNoTracking;
        if (!string.IsNullOrWhiteSpace(service))
            query = query.Where(l => l.ServiceName == service);
        query = query.OrderByDescending(l => l.CalledAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new
            {
                l.Id,
                l.ServiceName,
                l.Endpoint,
                l.Method,
                l.StatusCode,
                l.Duration,
                l.CalledAt,
                l.IsSuccess,
                requestBody = l.RequestBody,
                responseBody = l.ResponseBody
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new { items, totalCount = total, page, pageSize }));
    }
}

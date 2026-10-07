using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Infrastructure.Services.Archive;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر آرشیو سالانه — چرخه سیزدهم (درخواست ۷-۷).
/// پیش‌نمایش شمارشی، اجرای انتقال به ArchiveRecords و مرورگر جستجوی پرونده قدیمی.
/// مسیرها: /api/v1/admin/archive
/// چرخه بیستم (گزارش چهاردهم مالک): قالب‌های مسیر با پیشوند ~/ مطلق شدند تا
/// به مسیر پایهٔ api/v1/[controller] الحاق نشوند (وگرنه 404 برمی‌گشتند).
/// </summary>
[ApiController]
[Authorize]
public class ArchiveController : ApiBaseController
{
    private readonly ArchiveService _archive;
    private readonly ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق سرویس آرشیو و کاربر فعلی</summary>
    public ArchiveController(ArchiveService archive, ICurrentUserService currentUser)
    {
        _archive = archive;
        _currentUser = currentUser;
    }

    /// <summary>پیش‌نمایش آرشیو سال شمسی — شمارش ردیف‌های قابل انتقال و مسدودشده بدون تغییر داده</summary>
    /// <param name="year">سال شمسی — ردیف‌های قدیمی‌تر از آغاز این سال آرشیو می‌شوند</param>
    [HttpGet("~/api/v1/admin/archive/preview")]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> Preview([FromQuery] int year)
    {
        if (year is < 1300 or > 1600)
            return BadRequest(ApiResponse<string>.Failure("سال شمسی نامعتبر است"));
        var items = await _archive.PreviewAsync(year);
        return Ok(ApiResponse<object>.Success(new
        {
            year,
            cutoff = ArchiveService.CutoffOf(year),
            totalCandidates = items.Sum(i => i.Candidates),
            totalBlocked = items.Sum(i => i.Blocked),
            items,
        }));
    }

    /// <summary>اجرای آرشیو — انتقال ردیف‌های قدیمی به ArchiveRecords</summary>
    /// <remarks>
    /// پیش‌فرض حذف نرم امن (ردیف از دید برنامه حذف می‌شود ولی فیزیکی می‌ماند)؛
    /// با hardPurge=true حذف فیزیکی بلافاصله اجرا می‌شود — برای پاک‌سازی برنامه‌ای
    /// همین کار با اسکریپت نگهداشت docs/guides/archive-maintenance.sql هم ممکن است.
    /// </remarks>
    [HttpPost("~/api/v1/admin/archive/run")]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> Run([FromBody] ArchiveRunRequest request, CancellationToken ct)
    {
        if (request.Year is < 1300 or > 1600)
            return BadRequest(ApiResponse<string>.Failure("سال شمسی نامعتبر است"));
        if (request.Year >= Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToJalali(DateTime.UtcNow).Year)
            return BadRequest(ApiResponse<string>.Failure("فقط سال‌های گذشته قابل آرشیو هستند"));

        var report = await _archive.RunAsync(request.Year, _currentUser.UserId, request.HardPurge, ct);
        return Ok(ApiResponse<object>.Success(new
        {
            report.YearJalali,
            cutoff = report.Cutoff,
            report.TotalArchived,
            items = report.Items,
        }));
    }

    /// <summary>مرورگر آرشیو — جستجوی پرونده قدیمی بیمار</summary>
    /// <param name="patientId">شناسه بیمار</param>
    /// <param name="term">عبارت جستجو در نام بیمار یا شرح رکورد</param>
    /// <param name="sourceTable">فیلتر جدول مبدأ</param>
    [HttpGet("~/api/v1/admin/archive/records")]
    [HasPermission("settings.view")]
    public async Task<IActionResult> Records([FromQuery] long? patientId, [FromQuery] string? term,
        [FromQuery] string? sourceTable, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        var rows = await _archive.SearchAsync(patientId, term, sourceTable, take, ct);
        return Ok(ApiResponse<object>.Success(rows));
    }

    /// <summary>تصویر JSON کامل یک رکورد آرشیو</summary>
    [HttpGet("~/api/v1/admin/archive/records/{id:long}/payload")]
    [HasPermission("settings.view")]
    public async Task<IActionResult> Payload(long id, CancellationToken ct)
    {
        var payload = await _archive.GetPayloadAsync(id, ct);
        if (payload is null)
            return NotFound(ApiResponse<string>.Failure("رکورد آرشیو یافت نشد"));
        return Ok(ApiResponse<object>.Success(payload));
    }

    /// <summary>فهرست جداول آرشیوشونده با عنوان فارسی</summary>
    [HttpGet("~/api/v1/admin/archive/tables")]
    [HasPermission("settings.view")]
    public IActionResult Tables()
        => Ok(ApiResponse<object>.Success(ArchiveService.TableTitles));
}

/// <summary>درخواست اجرای آرشیو</summary>
public class ArchiveRunRequest
{
    /// <summary>سال شمسی آرشیو</summary>
    public int Year { get; set; }

    /// <summary>حذف فیزیکی پس از آرشیو؟ — پیش‌فرض حذف نرم امن</summary>
    public bool HardPurge { get; set; }
}

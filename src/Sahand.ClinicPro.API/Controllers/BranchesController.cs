using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر شعبه‌ها — گام U17 چندشعبه‌ای (فرم بخش ۵ پرامپت):
/// فهرست/ثبت/ویرایش/حذف شعبه‌ها + انتخابگر TopBar دسکتاپ با هدر X-Branch-Id
/// </summary>
[ApiController]
[Route("api/v1/branches")]
[Authorize]
public class BranchesController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public BranchesController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>فهرست شعبه‌های فعال</summary>
    [HttpGet]
    [HasPermission("settings.view")]
    public async Task<IActionResult> GetBranches()
    {
        var branches = await _unitOfWork.Repository<Branch>().QueryableNoTracking
            .Where(b => !b.IsDeleted)
            .OrderBy(b => b.Id)
            .Select(b => new BranchDto(
                b.Id, b.BranchName, b.BranchCode, b.City, b.Province,
                b.Phone, b.IsMain, b.IsActive))
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(branches));
    }

    /// <summary>ثبت شعبه جدید</summary>
    [HttpPost]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> CreateBranch([FromBody] SaveBranchRequest request)
    {
        var duplicate = await _unitOfWork.Repository<Branch>().QueryableNoTracking
            .AnyAsync(b => b.BranchCode == request.Code && request.Code != null);
        if (duplicate)
            return BadRequest(ApiResponse<object>.Failure("شعبه‌ای با این کد وجود دارد"));

        var branch = new Branch
        {
            BranchName = request.Name.Trim(),
            BranchCode = request.Code,
            Address = request.Address,
            City = request.City,
            Province = request.Province,
            Phone = request.Phone,
            IsMain = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<Branch>().AddAsync(branch);
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.Success(new { branchId = branch.Id }, "شعبه ایجاد شد"));
    }

    /// <summary>ویرایش شعبه</summary>
    [HttpPut("{id:long}")]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> UpdateBranch(long id, [FromBody] SaveBranchRequest request)
    {
        var branch = await _unitOfWork.Repository<Branch>().QueryableTracking
            .FirstOrDefaultAsync(b => b.Id == id);
        if (branch is null)
            return BadRequest(ApiResponse<object>.Failure("شعبه یافت نشد"));

        branch.BranchName = request.Name.Trim();
        branch.BranchCode = request.Code;
        branch.Address = request.Address;
        branch.City = request.City;
        branch.Province = request.Province;
        branch.Phone = request.Phone;
        branch.IsActive = request.IsActive;

        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("شعبه به‌روزرسانی شد"));
    }

    /// <summary>تعیین شعبه اصلی — فقط یکی می‌تواند اصلی باشد</summary>
    [HttpPost("{id:long}/set-main")]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> SetMain(long id)
    {
        var branch = await _unitOfWork.Repository<Branch>().QueryableTracking
            .FirstOrDefaultAsync(b => b.Id == id);
        if (branch is null)
            return BadRequest(ApiResponse<object>.Failure("شعبه یافت نشد"));

        var all = await _unitOfWork.Repository<Branch>().QueryableTracking
            .Where(b => b.IsMain && b.Id != id).ToListAsync();
        foreach (var b in all) b.IsMain = false;
        branch.IsMain = true;

        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage($"«{branch.BranchName}» شعبه اصلی شد"));
    }

    /// <summary>حذف نرم شعبه — شعبه اصلی قابل حذف نیست</summary>
    [HttpDelete("{id:long}")]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> DeleteBranch(long id)
    {
        var branch = await _unitOfWork.Repository<Branch>().QueryableTracking
            .FirstOrDefaultAsync(b => b.Id == id);
        if (branch is null)
            return BadRequest(ApiResponse<object>.Failure("شعبه یافت نشد"));
        if (branch.IsMain)
            return BadRequest(ApiResponse<object>.Failure("شعبه اصلی قابل حذف نیست"));

        branch.IsDeleted = true;
        branch.DeletedAt = DateTime.UtcNow;
        branch.IsActive = false;
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("شعبه حذف شد"));
    }

    /* ─── مدل‌ها ─── */

    /// <summary>ردیف شعبه</summary>
    public record BranchDto(long Id, string Name, string? Code, string? City,
        string? Province, string? Phone, bool IsMain, bool IsActive);

    /// <summary>درخواست ذخیره شعبه</summary>
    public record SaveBranchRequest(string Name, string? Code, string? Address,
        string? City, string? Province, string? Phone, bool IsActive);
}

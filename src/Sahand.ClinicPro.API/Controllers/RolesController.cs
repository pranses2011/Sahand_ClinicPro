using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Security;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/* ─── مدل‌های مشترک نقش‌ها و مجوزها ─── */

/// <summary>ردیف مجوز</summary>
public record PermissionDto(long Id, string Name, string? Module, string? Action, string? Description);

/// <summary>گروه مجوزهای ماژول</summary>
public record PermissionGroupDto(string ModuleTitle, List<PermissionDto> Permissions);

/// <summary>ردیف نقش</summary>
public record RoleListItemDto(long Id, string Name, string? Description, bool IsSystem, int UserCount, int PermissionCount);

/// <summary>درخواست ثبت نقش</summary>
public record CreateRoleRequest(string Name, string? Description);

/// <summary>درخواست ذخیره ماتریس دسترسی</summary>
public record SavePermissionsRequest(List<long>? PermissionIds);

/// <summary>
/// کنترلر نقش‌ها و دسترسی‌ها — فرم ۲۵ پرامپت UI بخش ۴
/// ماتریس دسترسی ۲۴ گروهی با انتخاب همه/هیچ و ذخیره یکجا
/// مسیرها: /api/v1/roles و /api/v1/permissions
/// </summary>
[ApiController]
[Route("api/v1/roles")]
[Authorize]
public class RolesController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public RolesController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>فهرست نقش‌ها با تعداد کاربران هر نقش</summary>
    [HttpGet]
    [HasPermission("roles.view")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _unitOfWork.Repository<Role>().QueryableNoTracking
            .OrderBy(r => r.Id)
            .Select(r => new RoleListItemDto(r.Id, r.Name, r.Description, r.IsSystem,
                r.Users.Count, r.Permissions.Count))
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(roles));
    }

    /// <summary>ثبت نقش جدید</summary>
    [HttpPost]
    [HasPermission("roles.create")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var duplicate = await _unitOfWork.Repository<Role>().QueryableNoTracking
            .AnyAsync(r => r.Name == request.Name);
        if (duplicate)
            return BadRequest(ApiResponse<object>.Failure("نقشی با این نام وجود دارد"));

        var role = new Role
        {
            Name = request.Name.Trim(),
            NormalizedName = request.Name.Trim().ToUpperInvariant(),
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<Role>().AddAsync(role);
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.Success(new { roleId = role.Id }, "نقش ایجاد شد"));
    }

    /// <summary>دسترسی‌های نقش — شناسه‌های مجوز فعلی</summary>
    [HttpGet("{id:long}/permissions")]
    [HasPermission("roles.view")]
    public async Task<IActionResult> GetRolePermissions(long id)
    {
        var exists = await _unitOfWork.Repository<Role>().QueryableNoTracking.AnyAsync(r => r.Id == id);
        if (!exists)
            return BadRequest(ApiResponse<object>.Failure("نقش یافت نشد"));

        var permissionIds = await _unitOfWork.Repository<Role>().QueryableNoTracking
            .Where(r => r.Id == id)
            .SelectMany(r => r.Permissions.Select(rp => rp.PermissionId))
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(permissionIds));
    }

    /// <summary>ذخیره ماتریس دسترسی نقش — جایگزینی کامل مجموعه مجوزها</summary>
    [HttpPut("{id:long}/permissions")]
    [HasPermission("roles.edit")]
    public async Task<IActionResult> SaveRolePermissions(long id, [FromBody] SavePermissionsRequest request)
    {
        var role = await _unitOfWork.Repository<Role>().QueryableTracking
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (role is null)
            return BadRequest(ApiResponse<object>.Failure("نقش یافت نشد"));

        // نقش سیستمی مدیر — برای جلوگیری از قفل‌شدن سامانه محافظت می‌شود
        if (role.IsSystem && (request.PermissionIds is null || request.PermissionIds.Count == 0))
            return BadRequest(ApiResponse<object>.Failure("نقش سیستمی نمی‌بدون دسترسی بماند"));

        // اعتبارسنجی وجود مجوزها
        var validIds = request.PermissionIds is null
            ? new List<long>()
            : await _unitOfWork.Repository<Permission>().QueryableNoTracking
                .Where(p => request.PermissionIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

        // جایگزینی کامل — حذف قدیمی‌ها و افزودن جدیدها
        role.Permissions.Clear();
        foreach (var pid in validIds)
            role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = pid });

        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage(
            $"دسترسی‌های نقش «{role.Name}» ذخیره شد ({validIds.Count} مجوز)"));
    }

    /// <summary>حذف نرم نقش غیرسیستمی — با کنترل کاربران فعال</summary>
    [HttpDelete("{id:long}")]
    [HasPermission("roles.delete")]
    public async Task<IActionResult> DeleteRole(long id)
    {
        var role = await _unitOfWork.Repository<Role>().QueryableTracking
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (role is null)
            return BadRequest(ApiResponse<object>.Failure("نقش یافت نشد"));
        if (role.IsSystem)
            return BadRequest(ApiResponse<object>.Failure("نقش سیستمی قابل حذف نیست"));
        if (role.Users.Count > 0)
            return BadRequest(ApiResponse<object>.Failure(
                $"این نقش به {role.Users.Count} کاربر اختصاص دارد — ابتدا کاربران را منتقل کنید"));

        role.IsDeleted = true;
        role.DeletedAt = DateTime.UtcNow;
        role.IsActive = false;
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("نقش حذف شد"));
    }
}

/// <summary>
/// کنترلر فهرست مجوزها — کاتالوگ ماتریس دسترسی گروه‌بندی‌شده بر اساس ماژول
/// مسیر: /api/v1/permissions
/// </summary>
[ApiController]
[Route("api/v1/permissions")]
[Authorize]
public class PermissionsController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public PermissionsController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>نام فارسی ماژول‌ها برای نمایش ماتریس</summary>
    private static readonly Dictionary<string, string> ModuleTitles = new()
    {
        ["patients"] = "بیماران", ["doctors"] = "پزشکان", ["appointments"] = "نوبت‌دهی",
        ["receptions"] = "پذیرش", ["visits"] = "ویزیت", ["prescriptions"] = "نسخه‌نویسی",
        ["invoices"] = "صندوق و فاکتور", ["payments"] = "پرداخت‌ها", ["accounting"] = "حسابداری",
        ["insurance"] = "بیمه", ["lab"] = "آزمایشگاه", ["imaging"] = "تصویربرداری",
        ["dental"] = "دندانپزشکی", ["eye"] = "چشم‌پزشکی", ["physiotherapy"] = "فیزیوتراپی",
        ["pharmacy"] = "داروخانه", ["sms"] = "پیامک", ["reports"] = "گزارش‌ها",
        ["users"] = "کاربران", ["roles"] = "نقش‌ها", ["settings"] = "تنظیمات",
        ["backup"] = "بکاپ", ["auditlogs"] = "لاگ عملیات", ["license"] = "لایسنس",
        ["dashboard"] = "داشبورد"
    };

    /// <summary>نام فارسی عملیات برای نمایش ماتریس</summary>
    private static readonly Dictionary<string, string> ActionTitles = new()
    {
        ["view"] = "مشاهده", ["create"] = "ایجاد", ["edit"] = "ویرایش",
        ["delete"] = "حذف", ["print"] = "چاپ", ["export"] = "خروجی"
    };

    /// <summary>همه مجوزها گروه‌بندی‌شده بر اساس ماژول — برای ماتریس ۲۴ گروهی</summary>
    [HttpGet]
    [HasPermission("roles.view")]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await _unitOfWork.Repository<Permission>().QueryableNoTracking
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Module).ThenBy(p => p.Id)
            .Select(p => new PermissionDto(p.Id, p.Name, p.Module, p.Action, p.Description))
            .ToListAsync();

        var groups = permissions
            .GroupBy(p => p.Module ?? "سایر")
            .Select(g => new PermissionGroupDto(
                ModuleTitles.TryGetValue(g.Key, out var title) ? title : g.Key,
                g.ToList()))
            .ToList();

        return Ok(ApiResponse<object>.Success(groups));
    }

}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Security;
using Sahand.ClinicPro.Domain.Interfaces;
using System.Security.Cryptography;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر مدیریت کاربران — فرم ۲۴ پرامپت UI بخش ۴
/// فهرست با فیلتر، ثبت/ویرایش دو‌تبی، ریست رمز، قفل/آزادسازی و غیرفعال‌سازی
/// مسیرها: /api/v1/users
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _hasher;

    /// <summary>سازنده با تزریق واحد کار و هشکننده رمز</summary>
    public UsersController(IUnitOfWork unitOfWork, IPasswordHasher hasher)
    {
        _unitOfWork = unitOfWork;
        _hasher = hasher;
    }

    /// <summary>فهرست کاربران — جستجو و فیلتر نقش و وضعیت با صفحه‌بندی</summary>
    [HttpGet]
    [HasPermission("users.view")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? role,
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        IQueryable<User> query = _unitOfWork.Repository<User>().QueryableNoTracking
            .Include(u => u.Employee)
            .Include(u => u.Roles).ThenInclude(r => r.Role);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search) ||
                (u.Employee != null && (u.Employee.FirstName + " " + u.Employee.LastName).Contains(search)));

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Roles.Any(r => r.Role != null && r.Role.Name == role));

        // وضعیت: active فعال | locked قفل | inactive غیرفعال
        query = status switch
        {
            "active" => query.Where(u => u.IsActive && (u.LockoutEnd == null || u.LockoutEnd <= DateTime.UtcNow)),
            "locked" => query.Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTime.UtcNow),
            "inactive" => query.Where(u => !u.IsActive),
            _ => query
        };

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UserListItemDto(
                u.Id,
                u.Username,
                u.Email,
                u.Employee == null ? null : u.Employee.FirstName + " " + u.Employee.LastName,
                u.Roles.Where(r => r.Role != null).Select(r => r.Role!.Name).ToList(),
                u.LastLoginAt,
                u.LockoutEnd,
                u.IsActive,
                u.MustChangePassword))
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new PagedResult<UserListItemDto>(items, total, page, pageSize)));
    }

    /// <summary>بررسی یکتایی نام کاربری — اعتبارسنجی برخط فرم</summary>
    [HttpGet("check-username")]
    [HasPermission("users.view")]
    public async Task<IActionResult> CheckUsername([FromQuery] string username, [FromQuery] long? excludeId)
    {
        var exists = await _unitOfWork.Repository<User>().QueryableNoTracking
            .AnyAsync(u => u.Username == username && (excludeId == null || u.Id != excludeId));
        return Ok(ApiResponse<object>.Success(new { available = !exists }));
    }

    /// <summary>ثبت کاربر جدید — با هش BCrypt و نقش‌های انتخابی</summary>
    [HttpPost]
    [HasPermission("users.create")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // اعتبارسنجی‌ها — نام کاربری یکتا و رمز مطابق سیاست
        var duplicate = await _unitOfWork.Repository<User>().QueryableNoTracking
            .AnyAsync(u => u.Username == request.Username);
        if (duplicate)
            return BadRequest(ApiResponse<object>.Failure("این نام کاربری قبلاً استفاده شده است"));

        if (!_hasher.MeetsPolicy(request.Password, out var policyError))
            return BadRequest(ApiResponse<object>.Failure(policyError));

        var user = new User
        {
            Username = request.Username.Trim(),
            Email = (request.Email ?? $"{request.Username}@clinic.local").Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            PhoneNumber = request.Phone,
            EmployeeId = request.EmployeeId,
            MustChangePassword = request.MustChangePassword,
            TwoFactorEnabled = request.TwoFactorEnabled,
            IsActive = request.IsActive,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            PasswordChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await AttachRolesAsync(user, request.RoleIds);
        _unitOfWork.Repository<User>().AddAsync(user);
        await _unitOfWork.SaveEntitiesAsync();

        return Ok(ApiResponse<object>.SuccessMessage($"کاربر {user.Username} ایجاد شد"));
    }

    /// <summary>ویرایش کاربر — ایمیل/موبایل/نقش‌ها/وضعیت</summary>
    [HttpPut("{id:long}")]
    [HasPermission("users.edit")]
    public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserRequest request)
    {
        var user = await _unitOfWork.Repository<User>().QueryableTracking
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return BadRequest(ApiResponse<object>.Failure("کاربر یافت نشد"));

        if (!string.IsNullOrWhiteSpace(request.Email))
            user.Email = request.Email.Trim();
        user.PhoneNumber = request.Phone;
        user.EmployeeId = request.EmployeeId;
        user.MustChangePassword = request.MustChangePassword;
        user.TwoFactorEnabled = request.TwoFactorEnabled;
        user.IsActive = request.IsActive;

        // جایگزینی نقش‌ها
        user.Roles.Clear();
        await AttachRolesAsync(user, request.RoleIds);

        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("کاربر به‌روزرسانی شد"));
    }

    /// <summary>ریست رمز عبور — تولید رمز تصادفی قوی و بازگشت برای نمایش یک‌باره</summary>
    [HttpPost("{id:long}/reset-password")]
    [HasPermission("users.edit")]
    public async Task<IActionResult> ResetPassword(long id)
    {
        var user = await _unitOfWork.Repository<User>().QueryableTracking.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return BadRequest(ApiResponse<object>.Failure("کاربر یافت نشد"));

        // رمز تصادفی ۱۲ کاراکتری با حروف بزرگ/کوچک/رقم/علامت — مطابق سیاست
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%-_*+?";
        var random = RandomNumberGenerator.Create();
        string Next(string set)
        {
            var bytes = new byte[1];
            random.GetBytes(bytes);
            return set[bytes[0] % set.Length].ToString();
        }
        var chars = new List<string> { Next(upper), Next(lower), Next(digits), Next(symbols) };
        var all = upper + lower + digits + symbols;
        while (chars.Count < 12) chars.Add(Next(all));
        var password = string.Concat(chars.OrderBy(_ => Guid.NewGuid()));

        user.PasswordHash = _hasher.Hash(password);
        user.MustChangePassword = true;
        user.PasswordChangedAt = DateTime.UtcNow;
        user.SecurityStamp = Guid.NewGuid().ToString("N"); // ابطال نشست‌های قبلی
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.Success(new { newPassword = password },
            "رمز جدید تولید شد — در اولین ورود باید تغییر کند"));
    }

    /// <summary>قفل حساب کاربر — ۳۰ دقیقه</summary>
    [HttpPost("{id:long}/lock")]
    [HasPermission("users.edit")]
    public async Task<IActionResult> LockUser(long id)
    {
        var user = await _unitOfWork.Repository<User>().QueryableTracking.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return BadRequest(ApiResponse<object>.Failure("کاربر یافت نشد"));

        user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
        user.FailedLoginAttempts = 5;
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("حساب کاربر به مدت ۳۰ دقیقه قفل شد"));
    }

    /// <summary>آزادسازی حساب کاربر قفل‌شده</summary>
    [HttpPost("{id:long}/unlock")]
    [HasPermission("users.edit")]
    public async Task<IActionResult> UnlockUser(long id)
    {
        var user = await _unitOfWork.Repository<User>().QueryableTracking.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return BadRequest(ApiResponse<object>.Failure("کاربر یافت نشد"));

        user.LockoutEnd = null;
        user.FailedLoginAttempts = 0;
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("حساب کاربر آزاد شد"));
    }

    /// <summary>غیرفعال‌سازی (حذف نرم) کاربر</summary>
    [HttpDelete("{id:long}")]
    [HasPermission("users.delete")]
    public async Task<IActionResult> DeactivateUser(long id)
    {
        var user = await _unitOfWork.Repository<User>().QueryableTracking.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return BadRequest(ApiResponse<object>.Failure("کاربر یافت نشد"));

        user.IsActive = false;
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = user.Id; // توسط خود کاربر فعلی — AuditableInterceptor دقت می‌کند
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("کاربر غیرفعال شد"));
    }

    /// <summary>پیوست کردن نقش‌ها به کاربر بر اساس شناسه‌ها</summary>
    private async Task AttachRolesAsync(User user, List<long>? roleIds)
    {
        if (roleIds is null || roleIds.Count == 0) return;
        var roles = await _unitOfWork.Repository<Role>().QueryableTracking
            .Where(r => roleIds.Contains(r.Id)).ToListAsync();
        foreach (var role in roles)
            user.Roles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
    }

    /* ─── مدل‌های درخواست/پاسخ ─── */

    /// <summary>ردیف کاربر در فهرست</summary>
    public record UserListItemDto(long Id, string Username, string? Email, string? FullName,
        List<string> Roles, DateTime? LastLoginAt, DateTime? LockoutEnd, bool IsActive, bool MustChangePassword);

    /// <summary>نتیجه صفحه‌بندی‌شده</summary>
    public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);

    /// <summary>درخواست ثبت کاربر</summary>
    public record CreateUserRequest(string Username, string? Email, string Password, string? Phone,
        long? EmployeeId, List<long>? RoleIds, bool MustChangePassword, bool TwoFactorEnabled, bool IsActive);

    /// <summary>درخواست ویرایش کاربر</summary>
    public record UpdateUserRequest(string? Email, string? Phone, long? EmployeeId,
        List<long>? RoleIds, bool MustChangePassword, bool TwoFactorEnabled, bool IsActive);
}

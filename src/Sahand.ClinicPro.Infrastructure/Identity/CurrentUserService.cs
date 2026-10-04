using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Identity;

/// <summary>
/// سرویس کاربر فعلی — از HttpContext ادعاهای کاربر احراز هویت‌شده را می‌خواند
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>سازنده با تزریق دستیاب HttpContext</summary>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>شناسه کاربر فعلی از ادعای NameIdentifier</summary>
    public long? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(value, out var id) ? id : null;
        }
    }

    /// <summary>نام کاربری فعلی</summary>
    public string? Username =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

    /// <summary>آیا کاربر احراز هویت شده است</summary>
    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    /// <summary>نشانی IP درخواست جاری</summary>
    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Sahand.ClinicPro.API.Filters;

/// <summary>
/// نیاز دسترسی — بررسی ادعای permission در توکن
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>نام دسترسی مثل patients.view</summary>
    public string Permission { get; }

    /// <summary>ساخت نیاز با نام دسترسی</summary>
    public PermissionRequirement(string permission) => Permission = permission;
}

/// <summary>
/// گرداننده بررسی دسترسی — نقش ادمین همه دسترسی‌ها را دارد
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <summary>بررسی ادعای permission یا نقش Admin</summary>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim("permission", requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

/// <summary>
/// تأمین‌کننده سیاست دسترسی — سیاست‌های PERMISSION:x را پویا می‌سازد
/// </summary>
public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    /// <summary>پیشوند شناسایی سیاست‌های دسترسی</summary>
    public const string PolicyPrefix = "PERMISSION:";

    /// <summary>سازنده با گزینه‌های سیاست</summary>
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    /// <summary>ساخت سیاست برای هر دسترسی به‌صورت پویا</summary>
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[PolicyPrefix.Length..];
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
        }

        return await base.GetPolicyAsync(policyName);
    }
}

/// <summary>
/// صفت اعمال دسترسی روی اکشن یا کنترلر — مثل [HasPermission("patients.view")]
/// </summary>
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    /// <summary>ساخت صفت با نام دسترسی</summary>
    public HasPermissionAttribute(string permission)
        : base($"{PermissionPolicyProvider.PolicyPrefix}{permission}") { }
}

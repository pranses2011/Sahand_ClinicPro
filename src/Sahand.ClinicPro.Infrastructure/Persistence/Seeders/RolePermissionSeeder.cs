using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// بذر دسترسی‌های پیش‌فرض نقش‌ها — هر نقش فارسی از ابتدا مجموعهٔ دسترسی
/// متناسب با وظیفه‌اش را دارد تا مدیر مجبور نباشد ماتریس را از صفر بسازد.
/// قاعدهٔ ایمنی: فقط نقشی که «هیچ» دسترسی‌ای ندارد پر می‌شود؛ ماتریس
/// دست‌کاری‌شدهٔ مدیر درمانگاه هرگز با اجرای دوبارهٔ بذر بازنویسی نمی‌شود.
/// </summary>
public class RolePermissionSeeder : IDataSeeder
{
    private readonly ClinicProDbContext _context;

    /// <summary>ترتیب اجرا — بعد از نقش‌ها (۱۰) و مجوزها (۲۰)</summary>
    public int Order => 25;

    /// <summary>سازنده با تزریق کانتکست</summary>
    public RolePermissionSeeder(ClinicProDbContext context) => _context = context;

    // ماژول ← عملیات مجاز؛ "*" یعنی همهٔ عملیات آن ماژول
    private static readonly Dictionary<string, Dictionary<string, string[]>> Defaults = new()
    {
        [SystemRoles.Manager] = new()
        {
            ["*"] = new[] { "*" },
        },
        [SystemRoles.Doctor] = new()
        {
            ["dashboard"] = new[] { "view" },
            ["patients"] = new[] { "view", "create", "edit", "print" },
            ["doctors"] = new[] { "view" },
            ["appointments"] = new[] { "view", "edit" },
            ["receptions"] = new[] { "view" },
            ["visits"] = new[] { "*" },
            ["prescriptions"] = new[] { "*" },
            ["lab"] = new[] { "view", "create", "print" },
            ["imaging"] = new[] { "view", "create", "print" },
            ["dental"] = new[] { "view", "create", "edit", "print" },
            ["eye"] = new[] { "view", "create", "edit", "print" },
            ["physiotherapy"] = new[] { "view", "create", "edit", "print" },
            ["insurance"] = new[] { "view" },
            ["reports"] = new[] { "view", "print" },
            ["attendance"] = new[] { "view" },
        },
        [SystemRoles.Receptionist] = new()
        {
            ["dashboard"] = new[] { "view" },
            ["patients"] = new[] { "view", "create", "edit", "print" },
            ["doctors"] = new[] { "view" },
            ["appointments"] = new[] { "view", "create", "edit", "delete", "print" },
            ["receptions"] = new[] { "view", "create", "edit", "delete", "print" },
            ["visits"] = new[] { "view" },
            ["prescriptions"] = new[] { "view", "print" },
            ["invoices"] = new[] { "view", "create", "edit", "print" },
            ["payments"] = new[] { "view", "create", "print" },
            ["insurance"] = new[] { "view" },
            ["lab"] = new[] { "view" },
            ["imaging"] = new[] { "view" },
            ["sms"] = new[] { "view", "create" },
            ["reports"] = new[] { "view", "print" },
            ["attendance"] = new[] { "view" },
        },
        [SystemRoles.Accountant] = new()
        {
            ["dashboard"] = new[] { "view" },
            ["patients"] = new[] { "view" },
            ["doctors"] = new[] { "view" },
            ["receptions"] = new[] { "view", "print" },
            ["invoices"] = new[] { "*" },
            ["payments"] = new[] { "*" },
            ["accounting"] = new[] { "*" },
            ["insurance"] = new[] { "view", "edit" },
            ["pharmacy"] = new[] { "view" },
            ["reports"] = new[] { "*" },
            ["backup"] = new[] { "view", "create" },
            ["auditlogs"] = new[] { "view" },
            ["attendance"] = new[] { "view", "export" },
        },
        [SystemRoles.Nurse] = new()
        {
            ["dashboard"] = new[] { "view" },
            ["patients"] = new[] { "view", "edit" },
            ["doctors"] = new[] { "view" },
            ["appointments"] = new[] { "view" },
            ["receptions"] = new[] { "view" },
            ["visits"] = new[] { "view", "edit" },
            ["prescriptions"] = new[] { "view" },
            ["dental"] = new[] { "view", "edit" },
            ["eye"] = new[] { "view", "edit" },
            ["physiotherapy"] = new[] { "view", "edit" },
            ["attendance"] = new[] { "view" },
        },
        [SystemRoles.Pharmacist] = new()
        {
            ["dashboard"] = new[] { "view" },
            ["patients"] = new[] { "view" },
            ["doctors"] = new[] { "view" },
            ["prescriptions"] = new[] { "view", "print" },
            ["pharmacy"] = new[] { "*" },
            ["invoices"] = new[] { "view", "print" },
            ["payments"] = new[] { "view" },
            ["reports"] = new[] { "view", "print", "export" },
            ["attendance"] = new[] { "view" },
        },
        [SystemRoles.LabTechnician] = new()
        {
            ["dashboard"] = new[] { "view" },
            ["patients"] = new[] { "view" },
            ["doctors"] = new[] { "view" },
            ["appointments"] = new[] { "view" },
            ["receptions"] = new[] { "view" },
            ["lab"] = new[] { "*" },
            ["imaging"] = new[] { "view" },
            ["reports"] = new[] { "view", "print" },
            ["attendance"] = new[] { "view" },
        },
    };

    // دسترسی‌های ویژه (خارج از الگوی ماژول×عملیات) برای نقش‌های مرتبط
    private static readonly Dictionary<string, string[]> Specials = new()
    {
        [SystemRoles.Doctor] = new[] { "emr.advanced" },
        [SystemRoles.Receptionist] = new[] { "sms.bulksend", "surveys.view" },
        [SystemRoles.Accountant] = new[] { "payroll.manage" },
        [SystemRoles.Pharmacist] = new[] { "pharmacy.stocktake" },
        [SystemRoles.Manager] = new[]
        {
            "sms.bulksend", "notifications.manage", "emr.advanced", "payroll.manage",
            "attendance.devices", "attendance.shifts", "attendance.approve",
            "pharmacy.stocktake", "surveys.manage", "surveys.view"
        },
    };

    /// <summary>پرکردن دسترسی پیش‌فرض نقش‌های بدون دسترسی</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _context.Permissions.AsNoTracking().ToListAsync(cancellationToken);
        var roles = await _context.Roles.ToListAsync(cancellationToken);

        foreach (var role in roles)
        {
            // مدیر سامانه ذاتاً همهٔ دسترسی‌ها را دارد (میانبر PermissionAuthorization هم هست)
            // با این حال ماتریس او هم کامل پر می‌شود تا در صفحهٔ نقش‌ها قابل مشاهده باشد.
            var isAdmin = role.Name == SystemRoles.Admin;
            if (!Defaults.TryGetValue(role.Name, out var modules) && !isAdmin)
                continue;

            var hasAny = await _context.RolePermissions.AnyAsync(rp => rp.RoleId == role.Id, cancellationToken);
            if (hasAny)
                continue; // ماتریس موجود (پیش‌فرض قبلی یا تنظیم مدیر) دست نمی‌خورد

            IEnumerable<Permission> granted;
            if (isAdmin)
            {
                granted = permissions;
            }
            else
            {
                var allModules = modules!.ContainsKey("*");
                granted = permissions.Where(p =>
                {
                    if (p.Module is not null && Specials.TryGetValue(role.Name, out var sp) && sp.Contains(p.Name))
                        return true;
                    if (p.Module is null || p.Action is null) return false;
                    if (allModules) return true;
                    return modules.TryGetValue(p.Module, out var actions)
                           && (actions.Contains("*") || actions.Contains(p.Action));
                });
            }

            foreach (var permission in granted)
                _context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

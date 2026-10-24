using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// بذر کاربر مدیر — ادمین پیش‌فرض با رمز BCrypt
/// نام کاربری: admin و رمز عبور: Admin@123 — باید پس از نخستین ورود تغییر کند
/// </summary>
public class AdminSeeder : IDataSeeder
{
    private readonly ClinicProDbContext _context;

    /// <summary>ترتیب اجرای بذر</summary>
    public int Order => 30;

    /// <summary>سازنده با تزریق کانتکست</summary>
    public AdminSeeder(ClinicProDbContext context) => _context = context;

    /// <summary>ایجاد کاربر مدیر در صورت نبود</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var adminExists = await _context.Users.AnyAsync(u => u.Username == "admin", cancellationToken);
        if (adminExists) return;

        var adminRole = await _context.Roles.FirstAsync(r => r.Name == SystemRoles.Admin, cancellationToken);

        var admin = new User
        {
            Username = "admin",
            Email = "admin@clinic.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 12),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            EmailConfirmed = true,
            MustChangePassword = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        admin.Roles.Add(new UserRole { RoleId = adminRole.Id, AssignedAt = DateTime.UtcNow });

        _context.Users.Add(admin);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

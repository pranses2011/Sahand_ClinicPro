using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// بذر نقش‌های سیستمی — هشت نقش پایه با نام فارسی.
/// دیتابیس‌های قدیمی: نقش انگلیسی موجود درجا به فارسی تغییرنام می‌یابد تا
/// کاربران و دسترسی‌های فعلی حفظ شوند (حذف/ساخت مجدد انجام نمی‌شود).
/// </summary>
public class RoleSeeder : IDataSeeder
{
    private readonly ClinicProDbContext _context;

    /// <summary>ترتیب اجرای بذر</summary>
    public int Order => 10;

    /// <summary>سازنده با تزریق کانتکست</summary>
    public RoleSeeder(ClinicProDbContext context) => _context = context;

    /// <summary>ایجاد/فارسی‌سازی نقش‌های پایه</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var roles = new (string Name, string Description)[]
        {
            (SystemRoles.Admin, "مدیر سامانه با دسترسی کامل به همهٔ بخش‌ها"),
            (SystemRoles.Doctor, "پزشک معالج — ویزیت، نسخه و پروندهٔ بالینی"),
            (SystemRoles.Receptionist, "منشی پذیرش و نوبت‌دهی"),
            (SystemRoles.Accountant, "حسابدار و مسئول مالی"),
            (SystemRoles.Nurse, "پرستار و دستیار درمان"),
            (SystemRoles.Pharmacist, "مسئول داروخانه و انبار دارو"),
            (SystemRoles.LabTechnician, "کارشناس آزمایشگاه"),
            (SystemRoles.Manager, "مدیر درمانگاه — نظارت و مدیریت عملیاتی"),
        };

        foreach (var (name, description) in roles)
        {
            // مهاجرت نام قدیمی انگلیسی به فارسی (حفظ کاربران/دسترسی‌های موجود)
            if (SystemRoles.LegacyToPersian.FirstOrDefault(kv => kv.Value == name) is { Key: not null } legacy
                && legacy.Key != name)
            {
                var old = await _context.Roles.FirstOrDefaultAsync(r => r.Name == legacy.Key, cancellationToken);
                var persianExists = await _context.Roles.AnyAsync(r => r.Name == name, cancellationToken);
                if (old is not null && !persianExists)
                {
                    old.Name = name;
                    old.NormalizedName = name.ToUpperInvariant();
                    old.Description = description;
                    old.IsSystem = name == SystemRoles.Admin;
                }
            }

            var exists = await _context.Roles.AnyAsync(r => r.Name == name, cancellationToken);
            if (!exists)
            {
                _context.Roles.Add(new Role
                {
                    Name = name,
                    NormalizedName = name.ToUpperInvariant(),
                    Description = description,
                    IsSystem = name == SystemRoles.Admin,
                    IsActive = true
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

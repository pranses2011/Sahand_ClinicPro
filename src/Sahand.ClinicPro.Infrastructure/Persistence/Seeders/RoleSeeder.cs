using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// بذر نقش‌های سیستمی — هشت نقش پایه سامانه
/// </summary>
public class RoleSeeder : IDataSeeder
{
    private readonly ClinicProDbContext _context;

    /// <summary>ترتیب اجرای بذر</summary>
    public int Order => 10;

    /// <summary>سازنده با تزریق کانتکست</summary>
    public RoleSeeder(ClinicProDbContext context) => _context = context;

    /// <summary>ایجاد نقش‌های پایه در صورت نبود</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var roles = new (string Name, string Description)[]
        {
            ("Admin", "مدیر سامانه با دسترسی کامل"),
            ("Doctor", "پزشک معالج"),
            ("Receptionist", "منشی پذیرش و نوبت‌دهی"),
            ("Accountant", "حسابدار و مسئول مالی"),
            ("Nurse", "پرستار"),
            ("Pharmacist", "مسئول داروخانه"),
            ("LabTechnician", "کارشناس آزمایشگاه"),
            ("Manager", "مدیر درمانگاه"),
        };

        foreach (var (name, description) in roles)
        {
            var exists = await _context.Roles.AnyAsync(r => r.Name == name, cancellationToken);
            if (!exists)
            {
                _context.Roles.Add(new Role
                {
                    Name = name,
                    NormalizedName = name.ToUpperInvariant(),
                    Description = description,
                    IsSystem = name == "Admin",
                    IsActive = true
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// بذر نقش‌های سیستمی — هشت نقش پایه با نام فارسی.
/// دیتابیس‌های قدیمی: نقش انگلیسی موجود درجا به فارسی تغییرنام می‌یابد تا
/// کاربران و دسترسی‌های فعلی حفظ شوند (حذف/ساخت مجدد انجام نمی‌شود).
/// </summary>
/// <remarks>
/// ═══ چرخهٔ ۸۳ — ریشهٔ خطای نصب سرور ۲.۲۴.۰ ═══
/// «Cannot insert duplicate key row in object 'dbo.Roles' ... (مدیر سامانه)»
/// نسخهٔ قبل همهٔ تغییرنام‌ها را در حافظه انجام می‌داد و فقط در انتهای حلقه
/// یک SaveChanges می‌زد؛ اما کوئری‌های «آیا نقش فارسی وجود دارد؟» دیتابیس را
/// می‌خواندند و تغییرنامِ هنوز ذخیره‌نشده را نمی‌دیدند، پس برای همان نقش یک
/// ردیف تازه هم ساخته می‌شد و ذخیرهٔ نهایی هم UPDATE و هم INSERT یک نام را
/// می‌فرستاد. اکنون هر نقش جداگانه و بلافاصله ذخیره می‌شود و تصمیم‌ها بر
/// اساس فهرستِ یک‌بارهٔ خوانده‌شده از دیتابیس گرفته می‌شوند.
/// اگر هر دو نسخه (انگلیسی و فارسی) وجود داشته باشند، کاربران و دسترسی‌های
/// نسخهٔ قدیمی به نقش فارسی منتقل و نسخهٔ قدیمی با نام یکتا حذف نرم می‌شود.
/// </remarks>
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

        // فهرست یک‌بارهٔ نقش‌ها — تصمیم همهٔ شاخه‌ها بر اساس همین تصویر است
        // تا تغییرنامِ ذخیره‌نشده، چشم بذر را کور نکند (ریشهٔ خطای چرخهٔ ۸۳).
        var existing = await _context.Roles.ToListAsync(cancellationToken);

        foreach (var (name, description) in roles)
        {
            var persian = existing.FirstOrDefault(
                r => string.Equals(r.Name, name, StringComparison.Ordinal));
            var legacyName = SystemRoles.LegacyToPersian
                .FirstOrDefault(kv => kv.Value == name).Key;
            var legacy = legacyName is null || legacyName == name
                ? null
                : existing.FirstOrDefault(
                    r => string.Equals(r.Name, legacyName, StringComparison.OrdinalIgnoreCase));

            if (persian is not null && legacy is not null)
            {
                // هر دو نسخه هست: ادغام قدیمی در فارسی و حذف نرم نسخهٔ قدیمی
                await MergeLegacyIntoPersianAsync(legacy, persian, cancellationToken);
                existing.Remove(legacy);
                continue;
            }

            if (persian is null && legacy is not null)
            {
                // فقط نسخهٔ انگلیسی هست: همان ردیف درجا فارسی می‌شود
                legacy.Name = name;
                legacy.NormalizedName = name.ToUpperInvariant();
                legacy.Description = description;
                legacy.IsSystem = name == SystemRoles.Admin;
                legacy.IsActive = true;
                // ذخیرهٔ فوری همین نقش — پیش از هر تصمیم بعدی (رفع ریشهٔ خطا)
                await _context.SaveChangesAsync(cancellationToken);
                continue;
            }

            if (persian is null)
            {
                // هیچ نسخه‌ای نیست: ساخت نقش تازه و ذخیرهٔ فوری
                var role = new Role
                {
                    Name = name,
                    NormalizedName = name.ToUpperInvariant(),
                    Description = description,
                    IsSystem = name == SystemRoles.Admin,
                    IsActive = true
                };
                _context.Roles.Add(role);
                existing.Add(role);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// ادغام نقش قدیمی (انگلیسی) در نقش فارسی موجود: کاربران و دسترسی‌هایی
    /// که نقش فارسی ندارد منتقل می‌شوند، سپس نقش قدیمی با نامی یکتا حذف نرم
    /// می‌شود تا ایندکس یکتای نام آزاد بماند و سابقهٔ حسابرسی گم نشود.
    /// </summary>
    private async Task MergeLegacyIntoPersianAsync(Role legacy, Role persian,
        CancellationToken cancellationToken)
    {
        var legacyUsers = await _context.UserRoles
            .Where(ur => ur.RoleId == legacy.Id).ToListAsync(cancellationToken);
        var persianUserIds = (await _context.UserRoles
            .Where(ur => ur.RoleId == persian.Id).Select(ur => ur.UserId)
            .ToListAsync(cancellationToken)).ToHashSet();
        foreach (var userRole in legacyUsers)
        {
            if (persianUserIds.Contains(userRole.UserId))
                _context.UserRoles.Remove(userRole); // کاربر هر دو نقش را دارد
            else
                userRole.RoleId = persian.Id;
        }

        var legacyPermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == legacy.Id).ToListAsync(cancellationToken);
        var persianPermissionIds = (await _context.RolePermissions
            .Where(rp => rp.RoleId == persian.Id).Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken)).ToHashSet();
        foreach (var rolePermission in legacyPermissions)
        {
            if (persianPermissionIds.Contains(rolePermission.PermissionId))
                _context.RolePermissions.Remove(rolePermission);
            else
                rolePermission.RoleId = persian.Id;
        }

        legacy.Name = $"{legacy.Name} (قدیمی {legacy.Id})";
        legacy.NormalizedName = legacy.Name.ToUpperInvariant();
        legacy.IsActive = false;
        legacy.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

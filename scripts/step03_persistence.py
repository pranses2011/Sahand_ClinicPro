# -*- coding: utf-8 -*-
"""گام ۳ — بخش ۲: Repository + UnitOfWork + Interceptors + Seeders + DI"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
INFRA = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Infrastructure"))


def write(rel, content):
    full = os.path.join(INFRA, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════ Repository عمومی ═══════════
write("Persistence/Repositories/Repository.cs", """
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Repositories;

/// <summary>
/// پیاده‌سازی عمومی ریپازیتوری — کار رویClinicProDbContext
/// </summary>
/// <typeparam name="T">نوع موجودیت</typeparam>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    /// <summary>کانتکست دیتابیس</summary>
    protected readonly ClinicProDbContext Context;

    /// <summary>مجموعه داخلی موجودیت</summary>
    protected readonly DbSet<T> DbSet;

    /// <summary>سازنده با تزریق کانتکست</summary>
    public Repository(ClinicProDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    /// <summary>کوئری بدون ردیابی — برای عملیات فقطخواندنی</summary>
    public IQueryable<T> QueryableNoTracking => DbSet.AsNoTracking();

    /// <summary>کوئری با ردیابی تغییرات — برای ویرایش</summary>
    public IQueryable<T> QueryableTracking => DbSet;

    /// <summary>یافتن بر اساس کلید اصلی</summary>
    public async Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await DbSet.FindAsync(new object[] { id }, cancellationToken);

    /// <summary>یافتن اولین رکورد منطبق با شرط</summary>
    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(predicate, cancellationToken);

    /// <summary>بررسی وجود رکورد با شرط</summary>
    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(predicate, cancellationToken);

    /// <summary>شمارش رکوردهای منطبق</summary>
    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate is null
            ? await DbSet.CountAsync(cancellationToken)
            : await DbSet.CountAsync(predicate, cancellationToken);

    /// <summary>دریافت فهرست رکوردها با فیلتر اختیاری</summary>
    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();
        if (predicate is not null)
            query = query.Where(predicate);
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>افزودن موجودیت جدید</summary>
    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(entity, cancellationToken);
        return entry.Entity;
    }

    /// <summary>ثبت برای ویرایش</summary>
    public void Update(T entity) => DbSet.Update(entity);

    /// <summary>ثبت برای حذف نرم — توسط SoftDeleteInterceptor به ویرایش تبدیل می‌شود</summary>
    public void Remove(T entity) => DbSet.Remove(entity);
}
""")

# ═══════════ ریپازیتوری‌های تخصصی ═══════════
write("Persistence/Repositories/PatientRepository.cs", """
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Repositories;

/// <summary>
/// ریپازیتوری تخصصی بیماران
/// </summary>
public class PatientRepository : Repository<Patient>, IPatientRepository
{
    /// <summary>سازنده با تزریق کانتکست</summary>
    public PatientRepository(ClinicProDbContext context) : base(context) { }

    /// <summary>یافتن بیمار بر اساس کد ملی</summary>
    public async Task<Patient?> GetByNationalCodeAsync(string nationalCode, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(p => p.NationalCode == nationalCode, cancellationToken);

    /// <summary>یافتن بیمار بر اساس شماره پرونده</summary>
    public async Task<Patient?> GetByMedicalRecordNoAsync(string medicalRecordNo, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(p => p.MedicalRecordNo == medicalRecordNo, cancellationToken);

    /// <summary>آیا بیماری با این کد ملی ثبت شده است</summary>
    public async Task<bool> ExistsByNationalCodeAsync(string nationalCode, CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(p => p.NationalCode == nationalCode, cancellationToken);
}
""")

write("Persistence/Repositories/DoctorRepository.cs", """
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Repositories;

/// <summary>
/// ریپازیتوری تخصصی پزشکان
/// </summary>
public class DoctorRepository : Repository<Doctor>, IDoctorRepository
{
    /// <summary>سازنده با تزریق کانتکست</summary>
    public DoctorRepository(ClinicProDbContext context) : base(context) { }

    /// <summary>یافتن پزشک بر اساس کد نظام پزشکی</summary>
    public async Task<Doctor?> GetByNezamCodeAsync(string nezamCode, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(d => d.NezamPezeshkiCode == nezamCode, cancellationToken);
}
""")

write("Persistence/Repositories/AppointmentRepository.cs", """
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Repositories;

/// <summary>
/// ریپازیتوری تخصصی نوبت‌ها — کنترل ظرفیت پزشک در هر روز
/// </summary>
public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
{
    /// <summary>سازنده با تزریق کانتکست</summary>
    public AppointmentRepository(ClinicProDbContext context) : base(context) { }

    /// <summary>شمارش نوبت‌های فعال یک پزشک در یک تاریخ</summary>
    public async Task<int> CountActiveByDoctorAndDateAsync(long doctorId, DateOnly date, CancellationToken cancellationToken = default)
        => await DbSet.CountAsync(
            a => a.DoctorId == doctorId
                 && a.AppointmentDate == date
                 && a.Status != AppointmentStatus.Cancelled
                 && a.Status != AppointmentStatus.NoShow,
            cancellationToken);
}
""")

write("Persistence/Repositories/LicenseRepository.cs", """
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Licensing;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Repositories;

/// <summary>
/// ریپازیتوری لایسنس سمت کلاینت
/// </summary>
public class LicenseRepository : Repository<ClinicLicense>, ILicenseRepository
{
    /// <summary>سازنده با تزریق کانتکست</summary>
    public LicenseRepository(ClinicProDbContext context) : base(context) { }

    /// <summary>دریافت تنها رکورد لایسنس فعال درمانگاه</summary>
    public async Task<ClinicLicense?> GetActiveAsync(CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(cancellationToken);
}
""")

# ═══════════ UnitOfWork ═══════════
write("Persistence/UnitOfWork.cs", """
using System.Collections.Concurrent;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Events;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Persistence;

/// <summary>
/// واحد کار — ذخیره تغییرات + انتشار رویدادهای دامنه از طریق MediatR
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ClinicProDbContext _context;
    private readonly IMediator _mediator;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    /// <summary>سازنده با تزریق کانتکست و میانجی</summary>
    public UnitOfWork(ClinicProDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    /// <summary>دریافت ریپازیتوری موجودیت — کش‌شده بر اساس نوع</summary>
    public IRepository<T> Repository<T>() where T : BaseEntity
        => (IRepository<T>)_repositories.GetOrAdd(
            typeof(T),
            _ => Activator.CreateInstance(
                typeof(Repository<>).MakeGenericType(typeof(T)),
                _context) ?? throw new InvalidOperationException("ساخت ریپازیتوری ناموفق بود"));

    /// <summary>ذخیره تغییرات و انتشار رویدادهای دامنه</summary>
    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        await DispatchDomainEventsAsync(cancellationToken);
        return true;
    }

    /// <summary>انتشار رویدادهای دامنه معلق همه موجودیت‌های ردیابی‌شده</summary>
    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var entitiesWithEvents = _context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToList();
            entity.ClearDomainEvents();
            foreach (var domainEvent in events)
            {
                // پوشش رویداد دامنه در نوتیفیکیشن MediatR با بازتاب نوع
                var notification = (INotification)Activator.CreateInstance(
                    typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()),
                    domainEvent)!;
                await _mediator.Publish(notification, cancellationToken);
            }
        }
    }

    /// <summary>آزادسازی منابع کانتکست</summary>
    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
""")

# ═══════════ Interceptors ═══════════
write("Persistence/Interceptors/AuditableEntityInterceptor.cs", """
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Interceptors;

/// <summary>
/// اینترسپتور حسابرسی — پر کردن خودکار فیلدهای Created و Updated
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق سرویس کاربر فعلی</summary>
    public AuditableEntityInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    /// <summary>رهگیری ذخیره‌سازی همگام</summary>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>رهگیری ذخیره‌سازی ناهمگام</summary>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>پر کردن فیلدهای حسابرسی بر اساس وضعیت موجودیت</summary>
    private void ApplyAuditFields(DbContext? context)
    {
        if (context is null) return;
        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }
    }
}
""")

write("Persistence/Interceptors/SoftDeleteInterceptor.cs", """
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Interceptors;

/// <summary>
/// اینترسپتور حذف نرم — حذف واقعی به ویرایش با پرچم IsDeleted تبدیل می‌شود
/// هیچ رکوردی به‌طور فیزیکی حذف نمی‌شود
/// </summary>
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق سرویس کاربر فعلی</summary>
    public SoftDeleteInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    /// <summary>رهگیری ذخیره‌سازی همگام</summary>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>رهگیری ذخیره‌سازی ناهمگام</summary>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>تبدیل حذف فیزیکی به حذف نرم</summary>
    private void ApplySoftDelete(DbContext? context)
    {
        if (context is null) return;
        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted) continue;

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;

            // ثبت شناسه حذف‌کننده اگر موجودیت پشتیبانی کند
            var prop = entry.Entity.GetType().GetProperty(nameof(ISoftDeletable.DeletedBy));
            prop?.SetValue(entry.Entity, userId);
        }
    }
}
""")

# ═══════════ Identity: CurrentUserService ═══════════
write("Identity/CurrentUserService.cs", """
using System.Security.Claims;
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
""")

# ═══════════ Seeders ═══════════
write("Persistence/Seeders/IDataSeeder.cs", """
namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// قرارداد بذرافشان داده اولیه — همه بذرها هنگام راه‌اندازی به ترتیب اجرا می‌شوند
/// </summary>
public interface IDataSeeder
{
    /// <summary>ترتیب اجرای بذر</summary>
    int Order { get; }

    /// <summary>اجرای بذرافشان — باید تکرارپذیر باشد</summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
""")

write("Persistence/Seeders/RoleSeeder.cs", """
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
""")

write("Persistence/Seeders/PermissionSeeder.cs", """
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// بذر دسترسی‌ها — مجوزهای اتمی بر اساس ماژول و عملیات
/// </summary>
public class PermissionSeeder : IDataSeeder
{
    private readonly ClinicProDbContext _context;

    /// <summary>ترتیب اجرای بذر</summary>
    public int Order => 20;

    /// <summary>سازنده با تزریق کانتکست</summary>
    public PermissionSeeder(ClinicProDbContext context) => _context = context;

    /// <summary>ایجاد دسترسی‌های ماژول‌های اصلی</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var modules = new[]
        {
            "patients", "doctors", "appointments", "receptions", "visits", "prescriptions",
            "invoices", "payments", "accounting", "insurance", "lab", "imaging", "dental",
            "eye", "physiotherapy", "pharmacy", "sms", "reports", "users", "roles",
            "settings", "backup", "auditlogs", "license", "dashboard"
        };
        var actions = new[] { "view", "create", "edit", "delete", "print", "export" };
        var persianActions = new Dictionary<string, string>
        {
            ["view"] = "مشاهده", ["create"] = "ایجاد", ["edit"] = "ویرایش",
            ["delete"] = "حذف", ["print"] = "چاپ", ["export"] = "خروجی"
        };

        foreach (var module in modules)
        {
            foreach (var action in actions)
            {
                var name = $"{module}.{action}";
                var exists = await _context.Permissions.AnyAsync(p => p.Name == name, cancellationToken);
                if (!exists)
                {
                    _context.Permissions.Add(new Permission
                    {
                        Name = name,
                        Module = module,
                        Action = action,
                        Category = "ماژول اصلی",
                        Description = $"{persianActions[action]} ماژول {module}"
                    });
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
""")

write("Persistence/Seeders/AdminSeeder.cs", """
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

        var adminRole = await _context.Roles.FirstAsync(r => r.Name == "Admin", cancellationToken);

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
""")

write("Persistence/Seeders/BasicDataSeeder.cs", """
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// بذر داده‌های پایه — تخصص‌ها، کدهای ICD-10 پرکاربرد، بیمه‌ها، ماژول‌ها و تنظیمات
/// </summary>
public class BasicDataSeeder : IDataSeeder
{
    private readonly ClinicProDbContext _context;

    /// <summary>ترتیب اجرای بذر</summary>
    public int Order => 40;

    /// <summary>سازنده با تزریق کانتکست</summary>
    public BasicDataSeeder(ClinicProDbContext context) => _context = context;

    /// <summary>ایجاد داده‌های پایه در صورت نبود</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedSpecialtiesAsync(cancellationToken);
        await SeedIcd10Async(cancellationToken);
        await SeedInsuranceCompaniesAsync(cancellationToken);
        await SeedModulesAsync(cancellationToken);
        await SeedSmsTemplatesAsync(cancellationToken);
        await SeedClinicInfoAsync(cancellationToken);
        await SeedFiscalYearAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>بذر تخصص‌های پزشکی</summary>
    private async Task SeedSpecialtiesAsync(CancellationToken ct)
    {
        if (await _context.Specialties.AnyAsync(ct)) return;
        var list = new (string Fa, string En, string Code)[]
        {
            ("پزشکی عمومی", "General Practice", "GP"),
            ("داخلی", "Internal Medicine", "INT"),
            ("قلب و عروق", "Cardiology", "CAR"),
            ("اطفال", "Pediatrics", "PED"),
            ("زنان و زایمان", "Obstetrics and Gynecology", "OBG"),
            ("جراحی عمومی", "General Surgery", "SUR"),
            ("ارتوپدی", "Orthopedics", "ORT"),
            ("پوست و مو", "Dermatology", "DER"),
            ("چشم‌پزشکی", "Ophthalmology", "OPH"),
            ("گوش حلق بینی", "Otolaryngology", "ENT"),
            ("روان‌پزشکی", "Psychiatry", "PSY"),
            ("دندانپزشکی", "Dentistry", "DENT"),
            ("فیزیوتراپی", "Physiotherapy", "PHY"),
            ("رادیولوژی", "Radiology", "RAD"),
        };
        foreach (var (fa, en, code) in list)
        {
            _context.Specialties.Add(new Specialty { Name = fa, NameEn = en, Code = code, IsActive = true });
        }
    }

    /// <summary>بذر کدهای ICD-10 پرکاربرد</summary>
    private async Task SeedIcd10Async(CancellationToken ct)
    {
        if (await _context.ICD10Codes.AnyAsync(ct)) return;
        var list = new (string Code, string En, string Fa)[]
        {
            ("A09", "Infectious gastroenteritis and colitis", "اسهال و گاستروانتریت عفونی"),
            ("B54", "Malaria, unspecified", "مالاریای نامشخص"),
            ("E11", "Type 2 diabetes mellitus", "دیابت نوع دو"),
            ("E03", "Other hypothyroidism", "کم‌کاری تیروئید"),
            ("E78", "Disorders of lipoprotein metabolism", "اختلال متابولیسم لیپوپروتئین"),
            ("G43", "Migraine", "میگرن"),
            ("H10", "Acute conjunctivitis", "ملتحیه حاد"),
            ("H52", "Disorders of accommodation and refraction", "اختلالات انکسار چشم"),
            ("I10", "Essential hypertension", "فشار خون بالا"),
            ("I50", "Heart failure", "نارسایی قلب"),
            ("J06", "Acute upper respiratory infection", "عفونتهای حاد دستگاه تنفس فوقانی"),
            ("J20", "Acute bronchitis", "برونشیت حاد"),
            ("J45", "Asthma", "آسم"),
            ("K29", "Gastritis and duodenitis", "گاستریت و دوودنیت"),
            ("K35", "Acute appendicitis", "آپاندیسیت حاد"),
            ("L50", "Urticaria", "کهیر"),
            ("M17", "Gonarthrosis", "آرتروز زانو"),
            ("M54", "Dorsalgia", "درد پشت"),
            ("N39", "Urinary tract infection", "عفونت مجاری ادراری"),
            ("O80", "Encounter for full-term uncomplicated delivery", "زایمان طبیعی بدون عارضه"),
            ("R05", "Cough", "سرفه"),
            ("R51", "Headache", "سردرد"),
            ("Z00", "General medical examination", "معاینه عمومی پزشکی"),
        };
        foreach (var (code, en, fa) in list)
        {
            _context.ICD10Codes.Add(new Icd10Code { Code = code, Description = en, DescriptionFa = fa });
        }
    }

    /// <summary>بذر شرکت‌های بیمه</summary>
    private async Task SeedInsuranceCompaniesAsync(CancellationToken ct)
    {
        if (await _context.InsuranceCompanies.AnyAsync(ct)) return;
        var list = new (string Name, string Code, InsuranceType Type)[]
        {
            ("تأمین اجتماعی", "TAMIN", InsuranceType.Public),
            ("سلامت", "SALAMAT", InsuranceType.Salamat),
            ("نیروهای مسلح", "ARTESH", InsuranceType.Artesh),
            ("خدمات درمانی آزاد", "AZAD", InsuranceType.Private),
            ("بیمه ایران", "IRAN", InsuranceType.Private),
            ("بیمه آسیا", "ASIA", InsuranceType.Private),
            ("بیمه دانا", "DANA", InsuranceType.Private),
            ("بیمه پاسارگاد", "PASARGAD", InsuranceType.Supplementary),
        };
        foreach (var (name, code, type) in list)
        {
            _context.InsuranceCompanies.Add(new InsuranceCompany
            {
                Name = name, Code = code, Type = type, IsActive = true
            });
        }
    }

    /// <summary>بذر ماژول‌های سامانه</summary>
    private async Task SeedModulesAsync(CancellationToken ct)
    {
        if (await _context.Modules.AnyAsync(ct)) return;
        var list = new (string Name, string Code, bool Core)[]
        {
            ("پذیرش و نوبت‌دهی", "Appointments", true),
            ("پرونده بیمار", "Patients", true),
            ("ویزیت و معاینه", "Visits", true),
            ("نسخه‌نویسی", "Prescriptions", true),
            ("مالی و صندوق", "Billing", true),
            ("حسابداری دوبل", "Accounting", false),
            ("آزمایشگاه", "Laboratory", false),
            ("تصویربرداری", "Imaging", false),
            ("دندانپزشکی", "Dental", false),
            ("چشم‌پزشکی", "EyeCare", false),
            ("فیزیوتراپی", "Physiotherapy", false),
            ("داروخانه", "Pharmacy", false),
            ("سامانه پیامک", "Sms", false),
            ("گزارش‌ساز", "Reports", false),
            ("منابع انسانی", "HR", false),
            ("یکپارچه‌سازی دولتی", "GovIntegration", false),
            ("نسخه الکترونیک", "EPrescription", false),
            ("بیمه و مطالبات", "Insurance", false),
            ("مدیریت کاربران", "Users", true),
            ("تنظیمات سامانه", "Settings", true),
            ("پشتیبان‌گیری", "Backup", true),
            ("لاگ عملیات", "Audit", true),
            ("داشبورد مدیریت", "Dashboard", true),
        };
        var order = 1;
        foreach (var (name, code, core) in list)
        {
            _context.Modules.Add(new SystemModule
            {
                Name = name, Code = code, IsCore = core, IsEnabled = core,
                SortOrder = order++, Version = "1.0.0"
            });
        }
    }

    /// <summary>بذر قالب‌های پیامک آماده</summary>
    private async Task SeedSmsTemplatesAsync(CancellationToken ct)
    {
        if (await _context.SmsTemplates.AnyAsync(ct)) return;
        var list = new (string Name, string Code, SmsTemplateType Type, string Content)[]
        {
            ("یادآوری نوبت فردا", "APPT_REMINDER", SmsTemplateType.Reminder,
             "{patient_name} عزیز، نوبت شما با {doctor_name} در تاریخ {date} ساعت {time} در درمانگاه سهند ثبت شده است. کد پیگیری: {code}"),
            ("تأیید ثبت نوبت", "APPT_CONFIRM", SmsTemplateType.Confirmation,
             "{patient_name} عزیز، نوبت شما با موفقیت ثبت شد. لغو نوبت تا ۲۴ ساعت قبل امکان‌پذیر است."),
            ("آماده شدن نتیجه آزمایش", "LAB_READY", SmsTemplateType.Result,
             "{patient_name} عزیز، نتیجه آزمایش شما آماده دریافت است."),
            ("رسید پرداخت", "PAY_RECEIPT", SmsTemplateType.Receipt,
             "{patient_name} عزیز، پرداخت شما به مبلغ {amount} ریال با شماره رسید {ref_no} ثبت شد."),
            ("خوش‌آمدگویی بیمار جدید", "PATIENT_WELCOME", SmsTemplateType.Custom,
             "{patient_name} عزیز، به درمانگاه سهند خوش آمدید. شماره پرونده شما: {mrn}"),
        };
        foreach (var (name, code, type, content) in list)
        {
            _context.SmsTemplates.Add(new SmsTemplate
            {
                Name = name, Code = code, TemplateType = type, Content = content, IsActive = true
            });
        }
    }

    /// <summary>بذر اطلاعات پایه درمانگاه</summary>
    private async Task SeedClinicInfoAsync(CancellationToken ct)
    {
        if (await _context.ClinicInfo.AnyAsync(ct)) return;
        _context.ClinicInfo.Add(new ClinicInfo
        {
            ClinicName = "درمانگاه سهند",
            Phone = "04112345678",
            Email = "info@sahandclinic.ir",
            Address = "تبریز، خیابان آبرسان"
        });
    }

    /// <summary>بذر سال مالی جاری</summary>
    private async Task SeedFiscalYearAsync(CancellationToken ct)
    {
        if (await _context.FiscalYears.AnyAsync(ct)) return;
        var now = DateTime.UtcNow;
        _context.FiscalYears.Add(new FiscalYear
        {
            Year = now.Year,
            StartDate = new DateOnly(now.Year, 1, 1),
            EndDate = new DateOnly(now.Year, 12, 31),
            IsActive = true,
            IsClosed = false
        });
    }
}
""")

write("Persistence/Seeders/DataSeederRunner.cs", """
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// اجراکننده بذرها — همه IDataSeederها را به ترتیب اجرا می‌کند
/// خطای هر بذر ثبت می‌شود اما راه‌اندازی برنامه را متوقف نمی‌کند
/// </summary>
public static class DataSeederRunner
{
    /// <summary>اجرای همه بذرهای ثبت‌شده به ترتیب Order</summary>
    public static async Task RunAllAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ClinicProDbContext>>();
        var sp = scope.ServiceProvider;

        // ClinicProDbContext صرفاً برای گرفتن نوع لاگر استفاده شده است
        var seeders = sp.GetServices<IDataSeeder>().OrderBy(s => s.Order).ToList();

        foreach (var seeder in seeders)
        {
            try
            {
                await seeder.SeedAsync(cancellationToken);
                logger.LogInformation("بذرافشان {Seeder} با موفقیت اجرا شد", seeder.GetType().Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "خطا در اجرای بذرافشان {Seeder}", seeder.GetType().Name);
            }
        }
    }
}
""")

write("Persistence/Seeders/AccountingAccountSeeder.cs", """
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Accounting;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// بذر سرفصل‌های حسابداری — ساختار چهارسطحی پایه (کدینگ ملی مبنا)
/// </summary>
public class AccountingAccountSeeder : IDataSeeder
{
    private readonly ClinicProDbContext _context;

    /// <summary>ترتیب اجرای بذر</summary>
    public int Order => 50;

    /// <summary>سازنده با تزریق کانتکست</summary>
    public AccountingAccountSeeder(ClinicProDbContext context) => _context = context;

    /// <summary>ایجاد ساختار پایه سرفصل‌ها</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.AccountingAccounts.AnyAsync(cancellationToken)) return;

        var roots = new (string Code, string Name, AccountType Type, AccountNature Nature)[]
        {
            ("1", "دارایی‌ها", AccountType.Asset, AccountNature.Debit),
            ("2", "بدهی‌ها", AccountType.Liability, AccountNature.Credit),
            ("3", "حقوق صاحبان سهام", AccountType.Equity, AccountNature.Credit),
            ("4", "درآمدها", AccountType.Revenue, AccountNature.Credit),
            ("5", "هزینه‌ها", AccountType.Expense, AccountNature.Debit),
        };

        foreach (var (code, name, type, nature) in roots)
        {
            _context.AccountingAccounts.Add(new AccountingAccount
            {
                AccountCode = code,
                AccountName = name,
                AccountType = type,
                Level = AccountLevel.Group,
                Nature = nature,
                IsDetail = false,
                IsActive = true
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
""")

# ═══════════ DependencyInjection ═══════════
write("DependencyInjection.cs", """
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Entities.Licensing;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Identity;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Sahand.ClinicPro.Infrastructure.Persistence.Interceptors;
using Sahand.ClinicPro.Infrastructure.Persistence.Repositories;
using Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

namespace Sahand.ClinicPro.Infrastructure;

/// <summary>
/// نقطه ورود ثبت سرویس‌های لایه زیرساخت در کانتینر تزریق وابستگی
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// ثبت DbContext اصلی با SQL Server، اینترسپتورها، ریپازیتوری‌ها و بذرها
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // رشته اتصال — از appsettings یا متغیر محیطی
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("رشته اتصال DefaultConnection یافت نشد");

        // DbContext با اینترسپتورهای حسابرسی و حذف نرم
        services.AddDbContext<ClinicProDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(3);
                sql.CommandTimeout(60);
            });
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>());
        });

        // اینترسپتورها و سرویس کاربر فعلی
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // واحد کار و ریپازیتوری‌ها
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<ILicenseRepository, LicenseRepository>();

        // بذرهای داده اولیه
        services.AddTransient<IDataSeeder, RoleSeeder>();
        services.AddTransient<IDataSeeder, PermissionSeeder>();
        services.AddTransient<IDataSeeder, AdminSeeder>();
        services.AddTransient<IDataSeeder, BasicDataSeeder>();
        services.AddTransient<IDataSeeder, AccountingAccountSeeder>();

        return services;
    }

    /// <summary>
    /// اجرای بذرها پس از راه‌اندازی — خطاها ثبت می‌شوند اما برنامه را متوقف نمی‌کنند
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        try
        {
            await DataSeederRunner.RunAllAsync(serviceProvider);
        }
        catch
        {
            // در محیط بدون دیتابیس (تست یا CI) بی‌صدا عبور می‌کنیم —
            // خطای واقعی توسط لاگر داخل RunAllAsync ثبت شده است
        }
    }
}
""")

print("✅ بخش ۲ گام ۳ کامل شد")

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

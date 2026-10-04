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

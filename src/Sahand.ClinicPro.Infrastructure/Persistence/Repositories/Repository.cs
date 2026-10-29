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

    /// <summary>تغییر قطعی یک فیلد ردیابی‌شده، حتی اگر مقدار پیشین برابر باشد.</summary>
    public void MarkPropertyModified<TProperty>(T entity, Expression<Func<T, TProperty>> property)
        => Context.Entry(entity).Property(property).IsModified = true;

    /// <summary>ثبت برای حذف نرم — توسط SoftDeleteInterceptor به ویرایش تبدیل می‌شود</summary>
    public void Remove(T entity) => DbSet.Remove(entity);
}

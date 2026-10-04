using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Interfaces;

/// <summary>
/// ریپازیتوری عمومی — قرارداد دسترسی به داده موجودیت‌ها
/// </summary>
/// <typeparam name="T">نوع موجودیت</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>دسترسی به کوئری‌پذیر برای سناریوهای پیشرفته (بدون ردیابی)</summary>
    IQueryable<T> QueryableNoTracking { get; }

    /// <summary>دسترسی به کوئری‌پذیر با ردیابی تغییرات</summary>
    IQueryable<T> QueryableTracking { get; }

    /// <summary>یافتن بر اساس کلید اصلی</summary>
    Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>یافتن بر اساس شرط</summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>آیا حداقل یک رکورد با شرط وجود دارد</summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>شمارش رکوردهای منطبق با شرط</summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>دریافت همه رکوردها (با فیلتر اختیاری)</summary>
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>افزودن موجودیت جدید</summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>ثبت موجودیت برای ویرایش</summary>
    void Update(T entity);

    /// <summary>ثبت موجودیت برای حذف نرم (توسط اینترسپتور انجام می‌شود)</summary>
    void Remove(T entity);
}

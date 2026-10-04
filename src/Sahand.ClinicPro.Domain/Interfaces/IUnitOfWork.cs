using System;
using System.Threading;
using System.Threading.Tasks;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Interfaces;

/// <summary>
/// واحد کار — هماهنگ‌سازی ذخیره‌سازی و انتشار رویدادهای دامنه
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>دریافت ریپازیتوری موجودیت موردنظر</summary>
    IRepository<T> Repository<T>() where T : BaseEntity;

    /// <summary>ذخیره تغییرات و انتشار رویدادهای دامنه — تراکنش توسط Pipeline مدیریت می‌شود</summary>
    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}

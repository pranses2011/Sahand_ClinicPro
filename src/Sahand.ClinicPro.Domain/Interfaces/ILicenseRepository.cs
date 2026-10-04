using System.Threading;
using System.Threading.Tasks;
using Sahand.ClinicPro.Domain.Entities.Licensing;

namespace Sahand.ClinicPro.Domain.Interfaces;

/// <summary>
/// ریپازیتوری لایسنس سمت کلاینت — خواندن رکورد رمزنگاری‌شده لایسنس محلی
/// </summary>
public interface ILicenseRepository : IRepository<ClinicLicense>
{
    /// <summary>دریافت تنها رکورد لایسنس فعال درمانگاه</summary>
    Task<ClinicLicense?> GetActiveAsync(CancellationToken cancellationToken = default);
}

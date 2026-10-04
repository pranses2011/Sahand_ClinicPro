using System.Threading;
using System.Threading.Tasks;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Domain.Interfaces;

/// <summary>
/// ریپازیتوری تخصصی پزشکان
/// </summary>
public interface IDoctorRepository : IRepository<Doctor>
{
    /// <summary>یافتن پزشک بر اساس کد نظام پزشکی</summary>
    Task<Doctor?> GetByNezamCodeAsync(string nezamCode, CancellationToken cancellationToken = default);
}

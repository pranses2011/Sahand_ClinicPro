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

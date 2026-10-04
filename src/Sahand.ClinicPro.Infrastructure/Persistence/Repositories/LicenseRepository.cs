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

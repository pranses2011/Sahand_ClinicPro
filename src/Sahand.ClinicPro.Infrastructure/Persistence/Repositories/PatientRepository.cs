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

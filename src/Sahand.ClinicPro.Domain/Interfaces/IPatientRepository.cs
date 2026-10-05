using System.Threading;
using System.Threading.Tasks;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Domain.Interfaces;

/// <summary>
/// ریپازیتوری تخصصی بیماران — متدهای خاص ماژول بیمار
/// </summary>
public interface IPatientRepository : IRepository<Patient>
{
    /// <summary>یافتن بیمار بر اساس کد ملی</summary>
    Task<Patient?> GetByNationalCodeAsync(string nationalCode, CancellationToken cancellationToken = default);

    /// <summary>یافتن بیمار بر اساس شماره پرونده</summary>
    Task<Patient?> GetByMedicalRecordNoAsync(string medicalRecordNo, CancellationToken cancellationToken = default);

    /// <summary>آیا بیماری با این کد ملی ثبت شده است</summary>
    Task<bool> ExistsByNationalCodeAsync(string nationalCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// آیا بیمار «دیگری» با این کد ملی ثبت شده است — برای ویرایش؛
    /// کد ملی خود بیمار مورد بررسی، استثنا می‌شود
    /// </summary>
    /// <param name="nationalCode">کد ملی ۱۰ رقمی</param>
    /// <param name="excludePatientId">شناسه بیمار در حال ویرایش</param>
    /// <param name="cancellationToken">توکن لغو</param>
    Task<bool> ExistsByNationalCodeAsync(string nationalCode, long excludePatientId, CancellationToken cancellationToken = default);
}

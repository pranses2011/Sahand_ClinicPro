using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Application.Features.Patients.Commands.UpdatePatient;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست‌های یکپارچگی اعتبارسنجی کد ملی در ویرایش بیمار — نسخه ۱.۶.۰
/// سه سناریوی کلیدی: کد جعلی رد، کد تکراریِ بیمار دیگر رد، حفظ کد خود بیمار مجاز
/// </summary>
public class UpdatePatientNationalCodeTests
{
    /// <summary>ساخت UnitOfWork روی دیتابیس درون‌حافظه‌ای</summary>
    private static (UnitOfWork UoW, ClinicProDbContext Context) CreateUnitOfWork()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"UpdNcDb_{Guid.NewGuid():N}")
            .Options;

        var context = new ClinicProDbContext(options);
        var mediator = new Mock<MediatR.IMediator>().Object;

        return (new UnitOfWork(context, mediator), context);
    }

    /// <summary>کد ملی معتبر — رقم کنترلی صحیح</summary>
    private const string ValidCodeA = "0012345679";

    /// <summary>کد ملی معتبر دوم</summary>
    private const string ValidCodeB = "0076409627";

    /// <summary>درج دو بیمار پایه</summary>
    private static async Task<(long IdA, long IdB)> SeedPatientsAsync(ClinicProDbContext context)
    {
        var a = new Patient
        {
            NationalCode = ValidCodeA,
            MedicalRecordNo = "MR-1001",
            FirstName = "علی",
            LastName = "رضایی",
            Mobile = "09121234567",
            RegistrationDate = DateTime.UtcNow,
            IsActive = true,
        };
        var b = new Patient
        {
            NationalCode = ValidCodeB,
            MedicalRecordNo = "MR-1002",
            FirstName = "مریم",
            LastName = "احمدی",
            Mobile = "09351234567",
            RegistrationDate = DateTime.UtcNow,
            IsActive = true,
        };
        context.Patients.AddRange(a, b);
        await context.SaveChangesAsync();
        return (a.Id, b.Id);
    }

    /// <summary>درخواست ویرایش معتبر نمونه</summary>
    private static UpdatePatientCommand EditCommand(long id, string nationalCode) =>
        new(id, new UpdatePatientDto
        {
            NationalCode = nationalCode,
            FirstName = "علی",
            LastName = "رضایی‌نژاد",
            Mobile = "09121234567",
        });

    [Fact]
    public async Task Validator_InvalidCheckDigit_Should_Fail()
    {
        // آماده‌سازی: بیمار پایه با کد معتبر
        var (uow, context) = CreateUnitOfWork();
        var (idA, _) = await SeedPatientsAsync(context);
        var validator = new UpdatePatientCommandValidator(new PatientRepositoryProxy(uow));

        // عملیات: ویرایش با کد جعلی (رقم کنترلی نادرست)
        var result = await validator.ValidateAsync(EditCommand(idA, "1234567890"));

        // نتیجه: رد به دلیل کد ملی نامعتبر
        result.IsValid.Should().BeFalse(because: "رقم کنترلی کد ملی نادرست است");
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("کد ملی"));
    }

    [Fact]
    public async Task Validator_DuplicateCodeOfAnotherPatient_Should_Fail()
    {
        var (uow, context) = CreateUnitOfWork();
        var (idA, _) = await SeedPatientsAsync(context);
        var validator = new UpdatePatientCommandValidator(new PatientRepositoryProxy(uow));

        // عملیات: بیمار الف می‌خواهد کد ملی بیمار ب را بگیرد
        var result = await validator.ValidateAsync(EditCommand(idA, ValidCodeB));

        // نتیجه: رد به دلیل تکراری بودن برای بیمار دیگر
        result.IsValid.Should().BeFalse(because: "کد ملی به بیمار دیگری تعلق دارد");
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("قبلاً ثبت شده"));
    }

    [Fact]
    public async Task Validator_OwnUnchangedCode_Should_Pass()
    {
        var (uow, context) = CreateUnitOfWork();
        var (idA, _) = await SeedPatientsAsync(context);
        var validator = new UpdatePatientCommandValidator(new PatientRepositoryProxy(uow));

        // عملیات: ویرایش با همان کد ملی خود بیمار
        var result = await validator.ValidateAsync(EditCommand(idA, ValidCodeA));

        // نتیجه: مجاز — استثنای خود بیمار اعمال شده است
        result.IsValid.Should().BeTrue(because: "کد ملی خود بیمار نباید تکراری شمرده شود");
    }

    [Fact]
    public async Task Handler_ValidNewCode_Should_Apply_Change()
    {
        var (uow, context) = CreateUnitOfWork();
        var (idA, _) = await SeedPatientsAsync(context);
        var handler = new UpdatePatientCommandHandler(uow,
            new Mock<Microsoft.Extensions.Logging.ILogger<UpdatePatientCommandHandler>>().Object);

        // عملیات: تغییر معتبر نام خانوادگی همراه با همان کد ملی
        var command = EditCommand(idA, ValidCodeA);
        var result = await handler.Handle(command, CancellationToken.None);

        // نتیجه: موفق و اعمال هر دو فیلد
        result.IsSuccess.Should().BeTrue();
        var saved = await context.Patients.FirstAsync(p => p.Id == idA);
        saved.NationalCode.Should().Be(ValidCodeA);
        saved.LastName.Should().Be("رضایی‌نژاد");
    }

    /// <summary>پروکسی IPatientRepository برای Validator — رفتار واقعی روی درون‌حافظه‌ای</summary>
    private sealed class PatientRepositoryProxy : Domain.Interfaces.IPatientRepository
    {
        private readonly UnitOfWork _uow;

        public PatientRepositoryProxy(UnitOfWork uow) => _uow = uow;

        public Task<Domain.Entities.People.Patient?> GetByNationalCodeAsync(
            string nationalCode, CancellationToken ct = default)
            => _uow.Repository<Domain.Entities.People.Patient>()
                .FirstOrDefaultAsync(p => p.NationalCode == nationalCode, ct);

        public Task<Domain.Entities.People.Patient?> GetByMedicalRecordNoAsync(
            string medicalRecordNo, CancellationToken ct = default)
            => _uow.Repository<Domain.Entities.People.Patient>()
                .FirstOrDefaultAsync(p => p.MedicalRecordNo == medicalRecordNo, ct);

        public Task<bool> ExistsByNationalCodeAsync(
            string nationalCode, CancellationToken ct = default)
            => _uow.Repository<Domain.Entities.People.Patient>()
                .AnyAsync(p => p.NationalCode == nationalCode, ct);

        public Task<bool> ExistsByNationalCodeAsync(
            string nationalCode, long excludePatientId, CancellationToken ct = default)
            => _uow.Repository<Domain.Entities.People.Patient>()
                .AnyAsync(p => p.NationalCode == nationalCode && p.Id != excludePatientId, ct);

        // ── اعضای عمومی Repository<T> — واگذاری به UnitOfWork ──
        public IQueryable<Domain.Entities.People.Patient> QueryableNoTracking
            => _uow.Repository<Domain.Entities.People.Patient>().QueryableNoTracking;
        public IQueryable<Domain.Entities.People.Patient> QueryableTracking
            => _uow.Repository<Domain.Entities.People.Patient>().QueryableTracking;

        public Task<Domain.Entities.People.Patient?> GetByIdAsync(long id, CancellationToken ct = default)
            => _uow.Repository<Domain.Entities.People.Patient>().GetByIdAsync(id, ct);

        public Task<Domain.Entities.People.Patient?> FirstOrDefaultAsync(
            System.Linq.Expressions.Expression<Func<Domain.Entities.People.Patient, bool>> p, CancellationToken ct = default)
            => _uow.Repository<Domain.Entities.People.Patient>().FirstOrDefaultAsync(p, ct);

        public Task<bool> AnyAsync(
            System.Linq.Expressions.Expression<Func<Domain.Entities.People.Patient, bool>> p, CancellationToken ct = default)
            => _uow.Repository<Domain.Entities.People.Patient>().AnyAsync(p, ct);

        public Task<int> CountAsync(
            System.Linq.Expressions.Expression<Func<Domain.Entities.People.Patient, bool>>? p = null, CancellationToken ct = default)
            => _uow.Repository<Domain.Entities.People.Patient>().CountAsync(p, ct);

        public Task<IReadOnlyList<Domain.Entities.People.Patient>> ListAsync(
            System.Linq.Expressions.Expression<Func<Domain.Entities.People.Patient, bool>>? p = null, CancellationToken ct = default)
            => _uow.Repository<Domain.Entities.People.Patient>().ListAsync(p, ct);

        public Task<Domain.Entities.People.Patient> AddAsync(
            Domain.Entities.People.Patient entity, CancellationToken ct = default)
            => _uow.Repository<Domain.Entities.People.Patient>().AddAsync(entity, ct);

        public void Update(Domain.Entities.People.Patient entity)
            => _uow.Repository<Domain.Entities.People.Patient>().Update(entity);

        public void Remove(Domain.Entities.People.Patient entity)
            => _uow.Repository<Domain.Entities.People.Patient>().Remove(entity);
    }
}

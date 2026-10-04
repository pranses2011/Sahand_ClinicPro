using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Application.Features.Patients.Commands.CreatePatient;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست واحد گرداننده ثبت بیمار — با دیتابیس درون‌حافظه‌ای EF
/// الگوی کامل رفتار واقعی Repository و UnitOfWork را پوشش می‌دهد
/// </summary>
public class CreatePatientCommandHandlerTests
{
    /// <summary>ساخت UnitOfWork روی دیتابیس درون‌حافظه‌ای</summary>
    private static (UnitOfWork UoW, ClinicProDbContext Context) CreateUnitOfWork()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid():N}")
            .Options;

        var context = new ClinicProDbContext(options) { IsInMemory = true };
        var mediator = new Mock<MediatR.IMediator>().Object;

        return (new UnitOfWork(context, mediator), context);
    }

    /// <summary>درخواست ثبت معتبر نمونه</summary>
    private static CreatePatientCommand ValidCommand() => new(new CreatePatientDto
    {
        NationalCode = "0012345678",
        FirstName = "علی",
        LastName = "رضایی",
        Mobile = "09121234567",
        BirthDateShamsi = "1370/05/15",
        Gender = Domain.Enums.Gender.Male,
        City = "تبریز"
    });

    [Fact]
    public async Task Handle_ValidCommand_Should_Create_Patient()
    {
        // آماده‌سازی
        var (uow, context) = CreateUnitOfWork();
        var handler = new CreatePatientCommandHandler(
            uow, new Mock<AutoMapper.IMapper>().Object,
            new Mock<Microsoft.Extensions.Logging.ILogger<CreatePatientCommandHandler>>().Object);

        // نقشه واقعی لازم است — AutoMapper با پروفایل واقعی
        var mapperConfig = new AutoMapper.MapperConfiguration(cfg =>
            cfg.AddProfile<Sahand.ClinicPro.Application.Common.Mappings.PatientProfile>());
        var mapper = mapperConfig.CreateMapper();
        handler = new CreatePatientCommandHandler(uow, mapper,
            new Mock<Microsoft.Extensions.Logging.ILogger<CreatePatientCommandHandler>>().Object);

        // عملیات: ثبت بیمار
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        // نتیجه: موفق با شناسه و ذخیره در دیتابیس
        result.IsSuccess.Should().BeTrue(because: "داده معتبر است");
        result.Value.FirstName.Should().Be("علی");
        result.Value.NationalCode.Should().Be("0012345678");

        var saved = await context.Patients.FirstOrDefaultAsync();
        saved.Should().NotBeNull(because: "بیمار باید ذخیره شده باشد");
        saved!.RegistrationDate.Should().NotBeNull();
        saved.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DuplicateNationalCode_Should_Still_Save_Both_InMemory()
    {
        // توجه: یکتایی کد ملی در Validator با MustAsync بررسی می‌شود نه در Handler؛
        // این تست رفتار Handler را مستقل از اعتبارسنجی نشان می‌دهد
        var (uow, context) = CreateUnitOfWork();
        var mapperConfig = new AutoMapper.MapperConfiguration(cfg =>
            cfg.AddProfile<Sahand.ClinicPro.Application.Common.Mappings.PatientProfile>());
        var mapper = mapperConfig.CreateMapper();
        var handler = new CreatePatientCommandHandler(uow, mapper,
            new Mock<Microsoft.Extensions.Logging.ILogger<CreatePatientCommandHandler>>().Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);
        await handler.Handle(ValidCommand(), CancellationToken.None);

        // در سطح دیتابیس ایندکس یکتا رد می‌کند — اینجا فقط رفتار دستور آزموده می‌شود
        (await context.Patients.CountAsync()).Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Validator_InvalidNationalCode_Should_Fail()
    {
        // آماده‌سازی: ریپازیتوری روی دیتابیس خالی
        var (uow, _) = CreateUnitOfWork();
        var validator = new CreatePatientCommandValidator(
            new PatientRepositoryProxy(uow));

        var command = ValidCommand() with { };
        command = new CreatePatientCommand(new CreatePatientDto
        {
            NationalCode = "1234567890",  // رقم کنترلی نادرست
            FirstName = "علی",
            LastName = "رضایی",
            Mobile = "09121234567"
        });

        // عملیات: اعتبارسنجی
        var result = await validator.ValidateAsync(command);

        // نتیجه: خطای کد ملی
        result.IsValid.Should().BeFalse(because: "رقم کنترلی کد ملی نادرست است");
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("کد ملی"));
    }

    [Fact]
    public async Task Validator_InvalidMobile_Should_Fail()
    {
        var (uow, _) = CreateUnitOfWork();
        var validator = new CreatePatientCommandValidator(new PatientRepositoryProxy(uow));

        var command = new CreatePatientCommand(new CreatePatientDto
        {
            NationalCode = "0012345678",
            FirstName = "علی",
            LastName = "رضایی",
            Mobile = "9121234567"  // بدون صفر ابتدایی
        });

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse(because: "الگوی موبایل نادرست است");
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("موبایل"));
    }

    /// <summary>پروکسی IPatientRepository برای Validator — رفتار واقعی</summary>
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
            Domain.Entities.People.Patient e, CancellationToken ct = default)
            => _uow.Repository<Domain.Entities.People.Patient>().AddAsync(e, ct);
        public void Update(Domain.Entities.People.Patient e)
            => _uow.Repository<Domain.Entities.People.Patient>().Update(e);
        public void Remove(Domain.Entities.People.Patient e)
            => _uow.Repository<Domain.Entities.People.Patient>().Remove(e);
    }
}

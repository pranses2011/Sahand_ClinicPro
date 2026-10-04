# -*- coding: utf-8 -*-
"""گام ۷ — ماژول Patients: DTO + CQRS کامل (Create/Update/Delete/GetAll/GetById/Search) + نگاشت + رمزنگاری شفاف"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
APP = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Application"))
INFRA = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Infrastructure"))


def write(rel, content, base=APP):
    full = os.path.join(base, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════ DTOها ═══════════
write("DTOs/Patients/PatientDtos.cs", """
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Patients;

/// <summary>
/// آیتم فهرست بیماران — سبک برای شبکه و گرید WPF
/// </summary>
public class PatientListDto
{
    /// <summary>شناسه بیمار</summary>
    public long Id { get; set; }

    /// <summary>شماره پرونده پزشکی</summary>
    public string MedicalRecordNo { get; set; } = string.Empty;

    /// <summary>نام</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>نام خانوادگی</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>کد ملی</summary>
    public string NationalCode { get; set; } = string.Empty;

    /// <summary>جنسیت</summary>
    public Gender Gender { get; set; }

    /// <summary>موبایل</summary>
    public string Mobile { get; set; } = string.Empty;

    /// <summary>نام بیمه اصلی</summary>
    public string? InsuranceName { get; set; }

    /// <summary>تاریخ آخرین مراجعه شمسی</summary>
    public string? LastVisitDateShamsi { get; set; }
}

/// <summary>
/// جزئیات کامل بیمار برای صفحه پرونده
/// </summary>
public class PatientDetailDto
{
    /// <summary>شناسه بیمار</summary>
    public long Id { get; set; }

    /// <summary>شماره پرونده پزشکی</summary>
    public string MedicalRecordNo { get; set; } = string.Empty;

    /// <summary>کد ملی</summary>
    public string NationalCode { get; set; } = string.Empty;

    /// <summary>نام</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>نام خانوادگی</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>نام پدر</summary>
    public string? FatherName { get; set; }

    /// <summary>جنسیت</summary>
    public Gender Gender { get; set; }

    /// <summary>تاریخ تولد شمسی</summary>
    public string? BirthDateShamsi { get; set; }

    /// <summary>سن محاسبه‌شده</summary>
    public int? Age { get; set; }

    /// <summary>گروه خونی</summary>
    public string? BloodType { get; set; }

    /// <summary>موبایل</summary>
    public string Mobile { get; set; } = string.Empty;

    /// <summary>تلفن ثابت</summary>
    public string? Phone { get; set; }

    /// <summary>ایمیل</summary>
    public string? Email { get; set; }

    /// <summary>آدرس</summary>
    public string? Address { get; set; }

    /// <summary>شهر</summary>
    public string? City { get; set; }

    /// <summary>استان</summary>
    public string? Province { get; set; }

    /// <summary>بیمه اصلی — شناسه</summary>
    public long? InsuranceCompanyId { get; set; }

    /// <summary>نام بیمه اصلی</summary>
    public string? InsuranceName { get; set; }

    /// <summary>شماره دفترچه بیمه</summary>
    public string? InsuranceNumber { get; set; }

    /// <summary>بیمه تکمیلی — شناسه</summary>
    public long? SupplementaryInsuranceId { get; set; }

    /// <summary>آلرژی‌ها</summary>
    public string? Allergies { get; set; }

    /// <summary>بیماری‌های مزمن</summary>
    public string? ChronicDiseases { get; set; }

    /// <summary>داروهای فعلی</summary>
    public string? CurrentMedications { get; set; }

    /// <summary>تاریخ ثبت‌نام شمسی</summary>
    public string? RegistrationDateShamsi { get; set; }
}
""")

write("DTOs/Patients/CreatePatientDto.cs", """
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Patients;

/// <summary>
/// درخواست ثبت بیمار جدید
/// شماره پرونده به‌صورت خودکار با سکانس MRN تولید می‌شود
/// </summary>
public class CreatePatientDto
{
    /// <summary>کد ملی ۱۰ رقمی — یکتا و اعتبارسنجی‌شده با رقم کنترلی</summary>
    public string NationalCode { get; set; } = string.Empty;

    /// <summary>نام</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>نام خانوادگی</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>نام پدر</summary>
    public string? FatherName { get; set; }

    /// <summary>جنسیت</summary>
    public Gender Gender { get; set; }

    /// <summary>تاریخ تولد شمسی — فرمت 1358/04/15</summary>
    public string? BirthDateShamsi { get; set; }

    /// <summary>گروه خونی</summary>
    public string? BloodType { get; set; }

    /// <summary>موبایل — الگوی 09XXXXXXXXX</summary>
    public string Mobile { get; set; } = string.Empty;

    /// <summary>تلفن ثابت</summary>
    public string? Phone { get; set; }

    /// <summary>ایمیل</summary>
    public string? Email { get; set; }

    /// <summary>آدرس — با AES-256 رمزنگاری می‌شود</summary>
    public string? Address { get; set; }

    /// <summary>شهر</summary>
    public string? City { get; set; }

    /// <summary>استان</summary>
    public string? Province { get; set; }

    /// <summary>بیمه اصلی</summary>
    public long? InsuranceCompanyId { get; set; }

    /// <summary>شماره دفترچه بیمه</summary>
    public string? InsuranceNumber { get; set; }

    /// <summary>بیمه تکمیلی</summary>
    public long? SupplementaryInsuranceId { get; set; }

    /// <summary>شغل</summary>
    public string? Occupation { get; set; }
}

/// <summary>
/// درخواست ویرایش بیمار
/// </summary>
public class UpdatePatientDto
{
    /// <summary>نام</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>نام خانوادگی</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>نام پدر</summary>
    public string? FatherName { get; set; }

    /// <summary>موبایل</summary>
    public string Mobile { get; set; } = string.Empty;

    /// <summary>تلفن ثابت</summary>
    public string? Phone { get; set; }

    /// <summary>ایمیل</summary>
    public string? Email { get; set; }

    /// <summary>آدرس</summary>
    public string? Address { get; set; }

    /// <summary>شهر</summary>
    public string? City { get; set; }

    /// <summary>استان</summary>
    public string? Province { get; set; }

    /// <summary>بیمه اصلی</summary>
    public long? InsuranceCompanyId { get; set; }

    /// <summary>شماره دفترچه بیمه</summary>
    public string? InsuranceNumber { get; set; }

    /// <summary>بیمه تکمیلی</summary>
    public long? SupplementaryInsuranceId { get; set; }
}
""")

# ═══════════ Create ═══════════
write("Features/Patients/Commands/CreatePatient/CreatePatientCommand.cs", """
using MediatR;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.CreatePatient;

/// <summary>
/// فرمان ثبت بیمار جدید — شماره پرونده خودکار و رویداد پیامک خوش‌آمدگویی
/// </summary>
/// <param name="Dto">داده‌های بیمار جدید</param>
public record CreatePatientCommand(CreatePatientDto Dto) : IRequest<Result<PatientListDto>>;
""")

write("Features/Patients/Commands/CreatePatient/CreatePatientCommandValidator.cs", """
using FluentValidation;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.CreatePatient;

/// <summary>
/// اعتبارسنج ثبت بیمار — کد ملی با رقم کنترلی و یکتایی غیرهمگام
/// </summary>
public class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    /// <summary>قواعد اعتبارسنجی با پیام فارسی</summary>
    public CreatePatientCommandValidator(IPatientRepository patientRepository)
    {
        RuleFor(x => x.Dto.NationalCode)
            .NotEmpty().WithMessage("کد ملی الزامی است")
            .Length(10).WithMessage("کد ملی باید دقیقاً ۱۰ رقم باشد")
            .Must(NationalCodeValidator.IsValid).WithMessage(NationalCodeValidator.InvalidMessage)
            .MustAsync(async (code, ct) => !await patientRepository.ExistsByNationalCodeAsync(code, ct))
                .WithMessage("بیماری با این کد ملی قبلاً ثبت شده است");

        RuleFor(x => x.Dto.FirstName)
            .NotEmpty().WithMessage("نام الزامی است")
            .MaximumLength(50).WithMessage("نام حداکثر ۵۰ کاراکتر است");

        RuleFor(x => x.Dto.LastName)
            .NotEmpty().WithMessage("نام خانوادگی الزامی است")
            .MaximumLength(50).WithMessage("نام خانوادگی حداکثر ۵۰ کاراکتر است");

        RuleFor(x => x.Dto.Mobile)
            .NotEmpty().WithMessage("شماره موبایل الزامی است")
            .Matches("^09\\\\d{9}$").WithMessage("شماره موبایل معتبر نیست — الگوی 09XXXXXXXXX");

        RuleFor(x => x.Dto.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Dto.Email))
            .WithMessage("ایمیل معتبر نیست");

        RuleFor(x => x.Dto.BirthDateShamsi)
            .Must(ShamsiValidationExtensions.IsValidShamsi)
            .When(x => !string.IsNullOrEmpty(x.Dto.BirthDateShamsi))
            .WithMessage("تاریخ تولد شمسی نامعتبر است — الگوی 1403/07/12");
    }
}
""")

# کمکی برای اعتبارسنجی تاریخ شمسی
write("Common/Helpers/ShamsiValidationExtensions.cs", """
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Application.Common.Helpers;

/// <summary>
/// متدهای الحاقی اعتبارسنجی تاریخ شمسی
/// </summary>
public static class ShamsiValidationExtensions
{
    /// <summary>آیا رشته تاریخ شمسی معتبر و قابل تبدیل است</summary>
    public static bool IsNotNullShamsi(this PersianDateHelper helper, string? shamsi)
        => PersianDateHelper.ToGregorian(shamsi) is not null;

    /// <summary>آیا رشته تاریخ شمسی معتبر است — امضای مستقیم برای FluentValidation</summary>
    public static bool IsValidShamsi(string? shamsi)
        => string.IsNullOrEmpty(shamsi) || PersianDateHelper.ToGregorian(shamsi) is not null;
}
""")

write("Features/Patients/Commands/CreatePatient/CreatePatientCommandHandler.cs", """
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Events;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.CreatePatient;

/// <summary>
/// گرداننده ثبت بیمار — تولید شماره پرونده با سکانس و تاریخ شمسی موازی و رویداد دامنه
/// </summary>
public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, Result<PatientListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreatePatientCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار و نقشه و لاگر</summary>
    public CreatePatientCommandHandler(IUnitOfWork unitOfWork, IMapper mapper,
        ILogger<CreatePatientCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>اجرای فرمان ثبت بیمار</summary>
    public async Task<Result<PatientListDto>> Handle(CreatePatientCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var patient = _mapper.Map<Patient>(dto);

        // تاریخ شمسی موازی — الزام مستندات دیتابیس
        patient.BirthDateShamsi = dto.BirthDateShamsi;
        if (PersianDateHelper.ToGregorian(dto.BirthDateShamsi) is { } birth)
            patient.BirthDate = DateOnly.FromDateTime(birth);

        patient.RegistrationDate = DateTime.UtcNow;
        patient.IsActive = true;

        var repo = _unitOfWork.Repository<Patient>();
        await repo.AddAsync(patient, cancellationToken);

        // رویداد دامنه — پیامک خوش‌آمدگویی پس از ذخیره ارسال می‌شود
        patient.AddDomainEvent(new PatientCreatedEvent(patient.Id, "در انتظار", dto.Mobile));

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("بیمار جدید ثبت شد — کد ملی {NationalCode} پرونده {Mrn}",
            dto.NationalCode, patient.MedicalRecordNo);

        return Result<PatientListDto>.Success(new PatientListDto
        {
            Id = patient.Id,
            MedicalRecordNo = patient.MedicalRecordNo,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            NationalCode = patient.NationalCode,
            Gender = patient.Gender,
            Mobile = patient.Mobile,
            InsuranceName = null,
            LastVisitDateShamsi = null
        });
    }
}
""")

# ═══════════ Update ═══════════
write("Features/Patients/Commands/UpdatePatient/UpdatePatientCommand.cs", """
using MediatR;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.UpdatePatient;

/// <summary>
/// فرمان ویرایش اطلاعات بیمار
/// </summary>
/// <param name="Id">شناسه بیمار</param>
/// <param name="Dto">داده‌های ویرایش</param>
public record UpdatePatientCommand(long Id, UpdatePatientDto Dto) : IRequest<Result>;
""")

write("Features/Patients/Commands/UpdatePatient/UpdatePatientCommandValidator.cs", """
using FluentValidation;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.UpdatePatient;

/// <summary>
/// اعتبارسنج ویرایش بیمار
/// </summary>
public class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public UpdatePatientCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("شناسه بیمار نامعتبر است");

        RuleFor(x => x.Dto.FirstName)
            .NotEmpty().WithMessage("نام الزامی است")
            .MaximumLength(50).WithMessage("نام حداکثر ۵۰ کاراکتر است");

        RuleFor(x => x.Dto.LastName)
            .NotEmpty().WithMessage("نام خانوادگی الزامی است")
            .MaximumLength(50).WithMessage("نام خانوادگی حداکثر ۵۰ کاراکتر است");

        RuleFor(x => x.Dto.Mobile)
            .NotEmpty().WithMessage("شماره موبایل الزامی است")
            .Matches("^09\\\\d{9}$").WithMessage("شماره موبایل معتبر نیست");
    }
}
""")

write("Features/Patients/Commands/UpdatePatient/UpdatePatientCommandHandler.cs", """
using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.UpdatePatient;

/// <summary>
/// گرداننده ویرایش بیمار — بارگذاری و اعمال تغییرات
/// </summary>
public class UpdatePatientCommandHandler : IRequestHandler<UpdatePatientCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePatientCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public UpdatePatientCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdatePatientCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>اجرای فرمان ویرایش</summary>
    public async Task<Result> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Patient>();
        var patient = await repo.GetByIdAsync(request.Id, cancellationToken);

        if (patient is null)
            return Result.Failure(Error.NotFound("بیمار", request.Id));

        var dto = request.Dto;
        patient.FirstName = dto.FirstName;
        patient.LastName = dto.LastName;
        patient.FatherName = dto.FatherName;
        patient.Mobile = dto.Mobile;
        patient.Phone = dto.Phone;
        patient.Email = dto.Email;
        patient.Address = dto.Address;
        patient.City = dto.City;
        patient.Province = dto.Province;
        patient.InsuranceCompanyId = dto.InsuranceCompanyId;
        patient.InsuranceNumber = dto.InsuranceNumber;
        patient.SupplementaryInsuranceId = dto.SupplementaryInsuranceId;

        repo.Update(patient);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("اطلاعات بیمار {PatientId} ویرایش شد", request.Id);
        return Result.Success();
    }
}
""")

# ═══════════ Delete (نرم) ═══════════
write("Features/Patients/Commands/DeletePatient/DeletePatientCommand.cs", """
using MediatR;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.DeletePatient;

/// <summary>
/// فرمان حذف نرم بیمار — رکورد فیزیکی حذف نمی‌شود
/// </summary>
/// <param name="Id">شناسه بیمار</param>
public record DeletePatientCommand(long Id) : IRequest<Result>;
""")

write("Features/Patients/Commands/DeletePatient/DeletePatientCommandHandler.cs", """
using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.DeletePatient;

/// <summary>
/// گرداننده حذف نرم بیمار — توسط SoftDeleteInterceptor به ویرایش تبدیل می‌شود
/// </summary>
public class DeletePatientCommandHandler : IRequestHandler<DeletePatientCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeletePatientCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public DeletePatientCommandHandler(IUnitOfWork unitOfWork, ILogger<DeletePatientCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>اجرای فرمان حذف نرم</summary>
    public async Task<Result> Handle(DeletePatientCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Patient>();
        var patient = await repo.GetByIdAsync(request.Id, cancellationToken);

        if (patient is null)
            return Result.Failure(Error.NotFound("بیمار", request.Id));

        repo.Remove(patient); // حذف فیزیکی → اینترسپتور → IsDeleted=true
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("بیمار {PatientId} حذف نرم شد", request.Id);
        return Result.Success();
    }
}
""")

# ═══════════ Queries ═══════════
write("Features/Patients/Queries/GetPatientById/GetPatientByIdQuery.cs", """
using MediatR;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Patients.Queries.GetPatientById;

/// <summary>
/// پرس‌وجوی دریافت جزئیات کامل بیمار
/// </summary>
/// <param name="Id">شناسه بیمار</param>
public record GetPatientByIdQuery(long Id) : IRequest<Result<PatientDetailDto>>;
""")

write("Features/Patients/Queries/GetPatientById/GetPatientByIdQueryHandler.cs", """
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Queries.GetPatientById;

/// <summary>
/// گرداننده دریافت جزئیات بیمار — همراه بیمه و سن محاسبه‌شده
/// </summary>
public class GetPatientByIdQueryHandler : IRequestHandler<GetPatientByIdQuery, Result<PatientDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    /// <summary>سازنده با تزریق واحد کار و نقشه</summary>
    public GetPatientByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<PatientDetailDto>> Handle(GetPatientByIdQuery request,
        CancellationToken cancellationToken)
    {
        var patient = await _unitOfWork.Repository<Patient>()
            .QueryableNoTracking
            .Include(p => p.InsuranceCompany)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (patient is null)
            return Result<PatientDetailDto>.Failure(Error.NotFound("بیمار", request.Id));

        var dto = _mapper.Map<PatientDetailDto>(patient);
        dto.InsuranceName = patient.InsuranceCompany?.Name;
        dto.Age = patient.BirthDate.HasValue
            ? PersianDateHelper.CalculateAge(patient.BirthDate.Value.ToDateTime(TimeOnly.MinValue))
            : null;

        return Result<PatientDetailDto>.Success(dto);
    }
}
""")

write("Features/Patients/Queries/GetAllPatients/GetAllPatientsQuery.cs", """
using MediatR;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Patients.Queries.GetAllPatients;

/// <summary>
/// پرس‌وجوی فهرست صفحه‌بندی‌شده بیماران
/// </summary>
/// <param name="Page">پارامترهای صفحه‌بندی و مرتب‌سازی و جستجو</param>
public record GetAllPatientsQuery(PagedRequest? Page = null) : IRequest<Result<PagedResponse<PatientListDto>>>;
""")

write("Features/Patients/Queries/GetAllPatients/GetAllPatientsQueryHandler.cs", """
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Queries.GetAllPatients;

/// <summary>
/// گرداننده فهرست بیماران — صفحه‌بندی سروری با پروجکشن سبک
/// </summary>
public class GetAllPatientsQueryHandler : IRequestHandler<GetAllPatientsQuery, Result<PagedResponse<PatientListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    /// <summary>سازنده با تزریق واحد کار و نقشه</summary>
    public GetAllPatientsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>اجرای پرس‌وجو با صفحه‌بندی</summary>
    public async Task<Result<PagedResponse<PatientListDto>>> Handle(GetAllPatientsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page ?? new PagedRequest();
        var query = _unitOfWork.Repository<Patient>().QueryableNoTracking;

        // جستجوی عمومی روی نام و کد ملی و موبایل و شماره پرونده
        if (!string.IsNullOrWhiteSpace(page.Search))
        {
            var term = page.Search.Trim();
            query = query.Where(p =>
                p.FirstName.Contains(term) ||
                p.LastName.Contains(term) ||
                p.NationalCode.Contains(term) ||
                p.Mobile.Contains(term) ||
                p.MedicalRecordNo.Contains(term));
        }

        // شمارش کل
        var totalCount = await query.CountAsync(cancellationToken);

        // مرتب‌سازی پیش‌فرض: جدیدترین ثبت‌نام
        query = query.OrderByDescending(p => p.Id);

        var items = await query
            .Skip((page.PageNumber - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<PatientListDto>>(items);

        return Result<PagedResponse<PatientListDto>>.Success(
            PagedResponse<PatientListDto>.Create(dtos, page.PageNumber, page.PageSize, totalCount));
    }
}
""")

write("Features/Patients/Queries/SearchPatients/SearchPatientsQuery.cs", """
using MediatR;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Patients.Queries.SearchPatients;

/// <summary>
/// پرس‌وجوی جستجوی سریع بیماران — برای فیلد جستجوی سراسری WPF
/// </summary>
/// <param name="SearchTerm">عبارت جستجو در نام و کد ملی و موبایل و پرونده</param>
/// <param name="MaxResults">حداکثر نتایج — پیش‌فرض ۲۰</param>
public record SearchPatientsQuery(string SearchTerm, int MaxResults = 20)
    : IRequest<Result<List<PatientListDto>>>;
""")

write("Features/Patients/Queries/SearchPatients/SearchPatientsQueryHandler.cs", """
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Queries.SearchPatients;

/// <summary>
/// گرداننده جستجوی سریع بیمار — سبک برای تکمیل خودکار
/// </summary>
public class SearchPatientsQueryHandler : IRequestHandler<SearchPatientsQuery, Result<List<PatientListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    /// <summary>سازنده با تزریق واحد کار و نقشه</summary>
    public SearchPatientsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>اجرای جستجوی سریع</summary>
    public async Task<Result<List<PatientListDto>>> Handle(SearchPatientsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
            return Result<List<PatientListDto>>.Success(new List<PatientListDto>());

        var term = request.SearchTerm.Trim();

        var patients = await _unitOfWork.Repository<Patient>()
            .QueryableNoTracking
            .Where(p => p.FirstName.Contains(term)
                        || p.LastName.Contains(term)
                        || p.NationalCode.Contains(term)
                        || p.Mobile.Contains(term)
                        || p.MedicalRecordNo.Contains(term))
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Take(request.MaxResults)
            .ToListAsync(cancellationToken);

        return Result<List<PatientListDto>>.Success(_mapper.Map<List<PatientListDto>>(patients));
    }
}
""")

# ═══════════ نگاشت AutoMapper ═══════════
write("Common/Mappings/PatientProfile.cs", """
using AutoMapper;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Application.Common.Mappings;

/// <summary>
/// پروفایل نگاشت ماژول بیماران
/// </summary>
public class PatientProfile : Profile
{
    /// <summary>ثبت نقشه‌های بیمار</summary>
    public PatientProfile()
    {
        // موجودیت به فهرست
        CreateMap<Patient, PatientListDto>()
            .ForMember(d => d.InsuranceName, o => o.MapFrom(s => s.InsuranceCompany != null ? s.InsuranceCompany.Name : null))
            .ForMember(d => d.LastVisitDateShamsi, o => o.Ignore());

        // موجودیت به جزئیات
        CreateMap<Patient, PatientDetailDto>()
            .ForMember(d => d.Age, o => o.Ignore())
            .ForMember(d => d.InsuranceName, o => o.Ignore())
            .ForMember(d => d.RegistrationDateShamsi, o => o.Ignore());

        // DTO ورودی به موجودیت — پرونده و حسابرسی خودکار است
        CreateMap<CreatePatientDto, Patient>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.MedicalRecordNo, o => o.Ignore())
            .ForMember(d => d.MedicalRecordNo, o => o.Ignore());
    }
}
""")

print("✅ گام ۷ — ماژول Patients کامل شد")

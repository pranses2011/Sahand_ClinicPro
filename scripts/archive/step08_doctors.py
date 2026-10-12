# -*- coding: utf-8 -*-
"""گام ۸ — ماژول Doctors: DTO + CQRS + نگاشت"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
APP = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Application"))


def write(rel, content):
    full = os.path.join(APP, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


write("DTOs/Doctors/DoctorDtos.cs", """
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Doctors;

/// <summary>
/// آیتم فهرست پزشکان
/// </summary>
public class DoctorListDto
{
    /// <summary>شناسه پزشک</summary>
    public long Id { get; set; }

    /// <summary>نام کامل</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>کد نظام پزشکی</summary>
    public string? NezamPezeshkiCode { get; set; }

    /// <summary>نام تخصص</summary>
    public string? SpecialtyName { get; set; }

    /// <summary>حق ویزیت به ریال</summary>
    public decimal ConsultationFee { get; set; }

    /// <summary>مدت هر ویزیت به دقیقه</summary>
    public int VisitDuration { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// جزئیات پزشک
/// </summary>
public class DoctorDetailDto
{
    /// <summary>شناسه پزشک</summary>
    public long Id { get; set; }

    /// <summary>شناسه پرسنل مرتبط</summary>
    public long? EmployeeId { get; set; }

    /// <summary>شماره پروانه پزشکی</summary>
    public string? MedicalLicenseNo { get; set; }

    /// <summary>کد نظام پزشکی</summary>
    public string? NezamPezeshkiCode { get; set; }

    /// <summary>تخصص اصلی — شناسه</summary>
    public long? SpecialtyId { get; set; }

    /// <summary>نام تخصص اصلی</summary>
    public string? SpecialtyName { get; set; }

    /// <summary>فوق‌تخصص — شناسه</summary>
    public long? SubSpecialtyId { get; set; }

    /// <summary>مرتبه علمی</summary>
    public string? AcademicRank { get; set; }

    /// <summary>بیوگرافی</summary>
    public string? Biography { get; set; }

    /// <summary>حق ویزیت به ریال</summary>
    public decimal ConsultationFee { get; set; }

    /// <summary>مدت هر ویزیت به دقیقه</summary>
    public int VisitDuration { get; set; }

    /// <summary>حداکثر بیمار در هر شیفت</summary>
    public int? MaxPatientsPerShift { get; set; }

    /// <summary>درصد سهم پزشک</summary>
    public decimal DoctorPercentage { get; set; }

    /// <summary>نوبت‌دهی آنلاین فعال</summary>
    public bool OnlineBookingEnabled { get; set; }

    /// <summary>پذیرش بیمار جدید</summary>
    public bool AcceptsNewPatients { get; set; }

    /// <summary>امتیاز</summary>
    public decimal Rating { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// درخواست ثبت یا ویرایش پزشک
/// </summary>
public class SaveDoctorDto
{
    /// <summary>شناسه پرسنل مرتبط</summary>
    public long? EmployeeId { get; set; }

    /// <summary>شماره پروانه پزشکی</summary>
    public string? MedicalLicenseNo { get; set; }

    /// <summary>کد نظام پزشکی</summary>
    public string? NezamPezeshkiCode { get; set; }

    /// <summary>تخصص اصلی</summary>
    public long? SpecialtyId { get; set; }

    /// <summary>فوق‌تخصص</summary>
    public long? SubSpecialtyId { get; set; }

    /// <summary>مرتبه علمی</summary>
    public string? AcademicRank { get; set; }

    /// <summary>بیوگرافی</summary>
    public string? Biography { get; set; }

    /// <summary>حق ویزیت به ریال</summary>
    public decimal ConsultationFee { get; set; }

    /// <summary>مدت هر ویزیت به دقیقه</summary>
    public int VisitDuration { get; set; } = 15;

    /// <summary>حداکثر بیمار در هر شیفت</summary>
    public int? MaxPatientsPerShift { get; set; }

    /// <summary>درصد سهم پزشک</summary>
    public decimal DoctorPercentage { get; set; }

    /// <summary>نوبت‌دهی آنلاین فعال</summary>
    public bool OnlineBookingEnabled { get; set; } = true;

    /// <summary>پذیرش بیمار جدید</summary>
    public bool AcceptsNewPatients { get; set; } = true;
}
""")

# ─── Commands ───
write("Features/Doctors/Commands/CreateDoctor/CreateDoctorCommand.cs", """
using MediatR;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Doctors.Commands.CreateDoctor;

/// <summary>
/// فرمان ثبت پزشک جدید
/// </summary>
/// <param name="Dto">داده‌های پزشک</param>
public record CreateDoctorCommand(SaveDoctorDto Dto) : IRequest<Result<long>>;

/// <summary>
/// اعتبارسنج ثبت پزشک
/// </summary>
public class CreateDoctorCommandValidator : AbstractValidator<CreateDoctorCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateDoctorCommandValidator()
    {
        RuleFor(x => x.Dto.ConsultationFee)
            .GreaterThanOrEqualTo(0).WithMessage("حق ویزیت نمی‌تواند منفی باشد");
        RuleFor(x => x.Dto.VisitDuration)
            .InclusiveBetween(5, 120).WithMessage("مدت ویزیت باید بین ۵ تا ۱۲۰ دقیقه باشد");
        RuleFor(x => x.Dto.DoctorPercentage)
            .InclusiveBetween(0, 100).WithMessage("درصد سهم پزشک باید بین صفر تا صد باشد");
    }
}

/// <summary>
/// گرداننده ثبت پزشک
/// </summary>
public class CreateDoctorCommandHandler : IRequestHandler<CreateDoctorCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateDoctorCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای فرمان ثبت</summary>
    public async Task<Result<long>> Handle(CreateDoctorCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var doctor = new Doctor
        {
            EmployeeId = dto.EmployeeId,
            MedicalLicenseNo = dto.MedicalLicenseNo,
            NezamPezeshkiCode = dto.NezamPezeshkiCode,
            SpecialtyId = dto.SpecialtyId,
            SubSpecialtyId = dto.SubSpecialtyId,
            AcademicRank = dto.AcademicRank,
            Biography = dto.Biography,
            ConsultationFee = dto.ConsultationFee,
            VisitDuration = dto.VisitDuration,
            MaxPatientsPerShift = dto.MaxPatientsPerShift,
            DoctorPercentage = dto.DoctorPercentage,
            OnlineBookingEnabled = dto.OnlineBookingEnabled,
            AcceptsNewPatients = dto.AcceptsNewPatients,
            IsActive = true
        };

        await _unitOfWork.Repository<Doctor>().AddAsync(doctor, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<long>.Success(doctor.Id);
    }
}

// ── واگذاری usingها به فایل اصلی — برای جلوگیری از تکرار
""")

# بعد از تعریف بالا، فایل واقعی باید بازنویسی شود با usingهای کامل
write("Features/Doctors/Commands/CreateDoctor/CreateDoctorCommand.cs", """
using FluentValidation;
using MediatR;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Commands.CreateDoctor;

/// <summary>
/// فرمان ثبت پزشک جدید
/// </summary>
/// <param name="Dto">داده‌های پزشک</param>
public record CreateDoctorCommand(SaveDoctorDto Dto) : IRequest<Result<long>>;

/// <summary>
/// اعتبارسنج ثبت پزشک — با پیام فارسی
/// </summary>
public class CreateDoctorCommandValidator : AbstractValidator<CreateDoctorCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateDoctorCommandValidator()
    {
        RuleFor(x => x.Dto.ConsultationFee)
            .GreaterThanOrEqualTo(0).WithMessage("حق ویزیت نمی‌تواند منفی باشد");
        RuleFor(x => x.Dto.VisitDuration)
            .InclusiveBetween(5, 120).WithMessage("مدت ویزیت باید بین ۵ تا ۱۲۰ دقیقه باشد");
        RuleFor(x => x.Dto.DoctorPercentage)
            .InclusiveBetween(0, 100).WithMessage("درصد سهم پزشک باید بین صفر تا صد باشد");
    }
}

/// <summary>
/// گرداننده ثبت پزشک
/// </summary>
public class CreateDoctorCommandHandler : IRequestHandler<CreateDoctorCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateDoctorCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای فرمان ثبت</summary>
    public async Task<Result<long>> Handle(CreateDoctorCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var doctor = new Doctor
        {
            EmployeeId = dto.EmployeeId,
            MedicalLicenseNo = dto.MedicalLicenseNo,
            NezamPezeshkiCode = dto.NezamPezeshkiCode,
            SpecialtyId = dto.SpecialtyId,
            SubSpecialtyId = dto.SubSpecialtyId,
            AcademicRank = dto.AcademicRank,
            Biography = dto.Biography,
            ConsultationFee = dto.ConsultationFee,
            VisitDuration = dto.VisitDuration,
            MaxPatientsPerShift = dto.MaxPatientsPerShift,
            DoctorPercentage = dto.DoctorPercentage,
            OnlineBookingEnabled = dto.OnlineBookingEnabled,
            AcceptsNewPatients = dto.AcceptsNewPatients,
            IsActive = true
        };

        await _unitOfWork.Repository<Doctor>().AddAsync(doctor, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<long>.Success(doctor.Id);
    }
}
""")

write("Features/Doctors/Commands/UpdateDoctor/UpdateDoctorCommand.cs", """
using FluentValidation;
using MediatR;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Commands.UpdateDoctor;

/// <summary>
/// فرمان ویرایش پزشک
/// </summary>
/// <param name="Id">شناسه پزشک</param>
/// <param name="Dto">داده‌های ویرایش</param>
public record UpdateDoctorCommand(long Id, SaveDoctorDto Dto) : IRequest<Result>;

/// <summary>
/// اعتبارسنج ویرایش پزشک
/// </summary>
public class UpdateDoctorCommandValidator : AbstractValidator<UpdateDoctorCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public UpdateDoctorCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("شناسه پزشک نامعتبر است");
        RuleFor(x => x.Dto.VisitDuration)
            .InclusiveBetween(5, 120).WithMessage("مدت ویزیت باید بین ۵ تا ۱۲۰ دقیقه باشد");
    }
}

/// <summary>
/// گرداننده ویرایش پزشک
/// </summary>
public class UpdateDoctorCommandHandler : IRequestHandler<UpdateDoctorCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public UpdateDoctorCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای فرمان ویرایش</summary>
    public async Task<Result> Handle(UpdateDoctorCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Doctor>();
        var doctor = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (doctor is null)
            return Result.Failure(Error.NotFound("پزشک", request.Id));

        var dto = request.Dto;
        doctor.EmployeeId = dto.EmployeeId;
        doctor.MedicalLicenseNo = dto.MedicalLicenseNo;
        doctor.NezamPezeshkiCode = dto.NezamPezeshkiCode;
        doctor.SpecialtyId = dto.SpecialtyId;
        doctor.SubSpecialtyId = dto.SubSpecialtyId;
        doctor.AcademicRank = dto.AcademicRank;
        doctor.Biography = dto.Biography;
        doctor.ConsultationFee = dto.ConsultationFee;
        doctor.VisitDuration = dto.VisitDuration;
        doctor.MaxPatientsPerShift = dto.MaxPatientsPerShift;
        doctor.DoctorPercentage = dto.DoctorPercentage;
        doctor.OnlineBookingEnabled = dto.OnlineBookingEnabled;
        doctor.AcceptsNewPatients = dto.AcceptsNewPatients;

        repo.Update(doctor);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}
""")

write("Features/Doctors/Commands/DeleteDoctor/DeleteDoctorCommand.cs", """
using MediatR;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Commands.DeleteDoctor;

/// <summary>
/// فرمان حذف نرم پزشک
/// </summary>
/// <param name="Id">شناسه پزشک</param>
public record DeleteDoctorCommand(long Id) : IRequest<Result>;

/// <summary>
/// گرداننده حذف نرم پزشک
/// </summary>
public class DeleteDoctorCommandHandler : IRequestHandler<DeleteDoctorCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public DeleteDoctorCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای فرمان حذف نرم</summary>
    public async Task<Result> Handle(DeleteDoctorCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Doctor>();
        var doctor = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (doctor is null)
            return Result.Failure(Error.NotFound("پزشک", request.Id));

        repo.Remove(doctor);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}
""")

# ─── Queries ───
write("Features/Doctors/Queries/GetDoctors/GetDoctorsQuery.cs", """
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Queries.GetDoctors;

/// <summary>
/// پرس‌وجوی فهرست پزشکان با صفحه‌بندی و جستجو
/// </summary>
/// <param name="Page">پارامترهای صفحه‌بندی</param>
/// <param name="SpecialtyId">فیلتر تخصص — اختیاری</param>
/// <param name="OnlyActive">فقط پزشکان فعال</param>
public record GetDoctorsQuery(PagedRequest? Page = null, long? SpecialtyId = null, bool OnlyActive = true)
    : IRequest<Result<PagedResponse<DoctorListDto>>>;

/// <summary>
/// گرداننده فهرست پزشکان
/// </summary>
public class GetDoctorsQueryHandler : IRequestHandler<GetDoctorsQuery, Result<PagedResponse<DoctorListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    /// <summary>سازنده با تزریق واحد کار و نقشه</summary>
    public GetDoctorsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<PagedResponse<DoctorListDto>>> Handle(GetDoctorsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page ?? new PagedRequest();
        IQueryable<Doctor> query = _unitOfWork.Repository<Doctor>().QueryableNoTracking
            .Include(d => d.Specialty)
            .Include(d => d.Employee);

        if (request.OnlyActive)
            query = query.Where(d => d.IsActive);
        if (request.SpecialtyId.HasValue)
            query = query.Where(d => d.SpecialtyId == request.SpecialtyId.Value);
        if (!string.IsNullOrWhiteSpace(page.Search))
        {
            var term = page.Search.Trim();
            query = query.Where(d =>
                (d.Employee != null && d.Employee.FirstName.Contains(term)) ||
                (d.Employee != null && d.Employee.LastName.Contains(term)) ||
                d.NezamPezeshkiCode != null && d.NezamPezeshkiCode.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(d => d.Id)
            .Skip((page.PageNumber - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(d => new DoctorListDto
        {
            Id = d.Id,
            FullName = d.Employee is not null ? $"{d.Employee.FirstName} {d.Employee.LastName}" : $"پزشک {d.Id}",
            NezamPezeshkiCode = d.NezamPezeshkiCode,
            SpecialtyName = d.Specialty?.Name,
            ConsultationFee = d.ConsultationFee,
            VisitDuration = d.VisitDuration,
            IsActive = d.IsActive
        }).ToList();

        return Result<PagedResponse<DoctorListDto>>.Success(
            PagedResponse<DoctorListDto>.Create(dtos, page.PageNumber, page.PageSize, totalCount));
    }
}
""")

write("Features/Doctors/Queries/GetDoctorById/GetDoctorByIdQuery.cs", """
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Queries.GetDoctorById;

/// <summary>
/// پرس‌وجوی جزئیات یک پزشک
/// </summary>
/// <param name="Id">شناسه پزشک</param>
public record GetDoctorByIdQuery(long Id) : IRequest<Result<DoctorDetailDto>>;

/// <summary>
/// گرداننده جزئیات پزشک
/// </summary>
public class GetDoctorByIdQueryHandler : IRequestHandler<GetDoctorByIdQuery, Result<DoctorDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    /// <summary>سازنده با تزریق واحد کار و نقشه</summary>
    public GetDoctorByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<DoctorDetailDto>> Handle(GetDoctorByIdQuery request,
        CancellationToken cancellationToken)
    {
        var doctor = await _unitOfWork.Repository<Doctor>()
            .QueryableNoTracking
            .Include(d => d.Specialty)
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (doctor is null)
            return Result<DoctorDetailDto>.Failure(Error.NotFound("پزشک", request.Id));

        var dto = _mapper.Map<DoctorDetailDto>(doctor);
        dto.SpecialtyName = doctor.Specialty?.Name;
        return Result<DoctorDetailDto>.Success(dto);
    }
}
""")

write("Common/Mappings/DoctorProfile.cs", """
using AutoMapper;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Application.Common.Mappings;

/// <summary>
/// پروفایل نگاشت ماژول پزشکان
/// </summary>
public class DoctorProfile : Profile
{
    /// <summary>ثبت نقشه‌های پزشک</summary>
    public DoctorProfile()
    {
        CreateMap<Doctor, DoctorDetailDto>()
            .ForMember(d => d.SpecialtyName, o => o.Ignore());
    }
}
""")

print("✅ گام ۸ — ماژول Doctors کامل شد")

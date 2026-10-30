using Sahand.ClinicPro.Application.Common.Mappings;
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
    private readonly PatientMapper _mapper;
    private readonly ILogger<CreatePatientCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار و نقشه و لاگر</summary>
    public CreatePatientCommandHandler(IUnitOfWork unitOfWork, PatientMapper mapper,
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

        var patient = _mapper.ToPatient(dto);

        // تاریخ شمسی موازی — الزام مستندات دیتابیس
        patient.BirthDateShamsi = dto.BirthDateShamsi;
        if (PersianDateHelper.ToGregorian(dto.BirthDateShamsi) is { } birth)
            patient.BirthDate = DateOnly.FromDateTime(birth);

        // چرخه بیست‌ویکم (گزارش پانزدهم مالک): فیلدهای تازه‌افزودهٔ DTO نیز
        // ذخیره می‌شوند — تأهل (متن فارسی→enum)، کد پستی، تماس اضطراری، نوع بیمه
        patient.MaritalStatus = MaritalStatusTextMapper.FromFaText(dto.MaritalStatus);

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
            NationalCode = patient.NationalCode ?? string.Empty,
            Gender = patient.Gender,
            Mobile = patient.Mobile,
            InsuranceName = null,
            LastVisitDateShamsi = null
        });
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.CreateUnidentifiedPatient;

/// <summary>
/// گرداننده ساخت پروندهٔ موقت بیمار ناشناس — کد موقت با الگوی
/// UNK-{تاریخ شمسی فشرده}-{ردیف سه‌رقمی روزانه} تولید می‌شود تا هم
/// یکتا باشد و هم روی قبض و صف قابل خواندن/ردیابی باشد. بیمار بلافاصله
/// قابل پذیرش است و بعداً از پرونده «تکمیل هویت» می‌شود.
/// </summary>
public class CreateUnidentifiedPatientCommandHandler
    : IRequestHandler<CreateUnidentifiedPatientCommand, Result<PatientListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateUnidentifiedPatientCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار و لاگر</summary>
    public CreateUnidentifiedPatientCommandHandler(IUnitOfWork unitOfWork,
        ILogger<CreateUnidentifiedPatientCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>اجرای فرمان — ساخت بیمار ناشناس و تولید کد موقت یکتا</summary>
    public async Task<Result<PatientListDto>> Handle(CreateUnidentifiedPatientCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var repo = _unitOfWork.Repository<Patient>();

        // پیشوند روزانه بر مبنای تاریخ شمسی رسمی ایران — 14050518
        var iranNow = PersianDateHelper.IranNow();
        var datePart = PersianDateHelper.ToShamsi(iranNow).Replace("/", string.Empty);
        var prefix = $"UNK-{datePart}-";

        // ردیف روزانه: شمارش پرونده‌های موقت امروز + ۱ (ایندکس یکتا هم نگهبان نهایی است)
        var todayCount = await repo.QueryableNoTracking
            .CountAsync(p => p.TemporaryCode != null && p.TemporaryCode.StartsWith(prefix),
                cancellationToken);
        var temporaryCode = prefix + (todayCount + 1).ToString("000");

        var patient = new Patient
        {
            FirstName = "بیمار",
            LastName = "ناشناس",
            NationalCode = null,
            Mobile = string.Empty,
            Gender = dto.Gender == 2 ? Gender.Female : Gender.Male,
            IsUnidentified = true,
            TemporaryCode = temporaryCode,
            RegistrationDate = DateTime.UtcNow,
            IsActive = true
        };

        // سن تقریبی اختیاری — فقط برای برآورد تاریخ تولد تا تکمیل هویت
        if (dto.ApproximateAge is > 0 and < 130)
        {
            var approxBirth = iranNow.AddYears(-dto.ApproximateAge.Value);
            patient.BirthDate = DateOnly.FromDateTime(approxBirth);
            patient.BirthDateShamsi = PersianDateHelper.ToShamsi(approxBirth);
        }

        await repo.AddAsync(patient, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("پروندهٔ موقت بیمار ناشناس ساخته شد — کد موقت {Code} پرونده {Mrn}",
            temporaryCode, patient.MedicalRecordNo);

        return Result<PatientListDto>.Success(new PatientListDto
        {
            Id = patient.Id,
            MedicalRecordNo = patient.MedicalRecordNo,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            NationalCode = string.Empty,
            Gender = patient.Gender,
            Mobile = patient.Mobile,
            IsUnidentified = true,
            TemporaryCode = temporaryCode
        });
    }
}

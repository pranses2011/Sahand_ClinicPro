using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.UpdatePatient;

/// <summary>
/// گرداننده ویرایش بیمار — بارگذاری و اعمال تغییرات
/// چرخه بیست‌ویکم (گزارش پانزدهم مالک): پیش‌تر فقط ۱۳ فیلد پایه اعمال می‌شد و
/// جنسیت، تاریخ تولد، گروه خونی، تأهل، کد پستی، شغل، تماس اضطراری، نوع بیمه و
/// سوابق پزشکی هرگز در دیتابیس به‌روزرسانی نمی‌شدند — به همین دلیل هنگام بازکردن
/// فرم ویرایش این فیلدها خالی دیده می‌شدند. اکنون همهٔ فیلدها اعمال می‌شوند.
/// </summary>
public class UpdatePatientCommandHandler : IRequestHandler<UpdatePatientCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePatientCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار و لاگر</summary>
    public UpdatePatientCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdatePatientCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>اجرای فرمان ویرایش بیمار — اعمال کامل تمام فیلدهای قابل ویرایش</summary>
    public async Task<Result> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Patient>();
        var patient = await repo.GetByIdAsync(request.Id, cancellationToken);

        if (patient is null)
            return Result.Failure(Error.NotFound("بیمار", request.Id));

        var dto = request.Dto;

        // ── فیلدهای هویتی و تماسی ──
        patient.NationalCode = dto.NationalCode;
        patient.FirstName = dto.FirstName;
        patient.LastName = dto.LastName;
        patient.FatherName = dto.FatherName;
        patient.Mobile = dto.Mobile;
        patient.Phone = dto.Phone;
        patient.Email = dto.Email;

        // ── فیلدهای پیشین‌جا‌افتاده (چرخه بیست‌ویکم) ──
        // جنسیت: enum از عدد ۱/۲ که دسکتاپ می‌فرستد
        patient.Gender = dto.Gender;

        // تاریخ تولد شمسی + معادل میلادی موازی — الزام مستندات دیتابیس
        patient.BirthDateShamsi = dto.BirthDateShamsi;
        patient.BirthDate = PersianDateHelper.ToGregorian(dto.BirthDateShamsi) is { } birth
            ? DateOnly.FromDateTime(birth)
            : null;

        patient.BloodType = dto.BloodType;
        // تأهل: دسکتاپ نام فارسی می‌فرستد — با نگاشتگر امن به enum تبدیل می‌شود
        patient.MaritalStatus = MaritalStatusTextMapper.FromFaText(dto.MaritalStatus);

        // ── نشانی و شغلی ──
        patient.Address = dto.Address;
        patient.City = dto.City;
        patient.Province = dto.Province;
        patient.PostalCode = dto.PostalCode;
        patient.Occupation = dto.Occupation;

        // ── تماس اضطراری ──
        patient.EmergencyContactName = dto.EmergencyContactName;
        patient.EmergencyContactPhone = dto.EmergencyContactPhone;
        patient.EmergencyContactRelation = dto.EmergencyContactRelation;

        // ── بیمه ──
        patient.InsuranceCompanyId = dto.InsuranceCompanyId;
        patient.InsuranceNumber = dto.InsuranceNumber;
        patient.InsuranceType = dto.InsuranceType;
        patient.SupplementaryInsuranceId = dto.SupplementaryInsuranceId;

        // ── سوابق پزشکی (آرایه JSON) ──
        patient.Allergies = dto.Allergies;
        patient.ChronicDiseases = dto.ChronicDiseases;
        patient.CurrentMedications = dto.CurrentMedications;

        repo.Update(patient);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("اطلاعات بیمار {PatientId} ویرایش شد (ویرایش کامل چرخه بیست‌ویکم)", request.Id);
        return Result.Success();
    }
}

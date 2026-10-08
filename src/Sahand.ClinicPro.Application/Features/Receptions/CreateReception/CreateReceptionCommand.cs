using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Receptions.Commands.CreateReception;

/// <summary>
/// فرمان پذیرش بیمار — از نوبت یا مراجعه حضوری بدون نوبت
/// </summary>
/// <param name="Dto">داده پذیرش</param>
/// <param name="ReceptionistId">شناسه منشی پذیرش‌کننده — از کاربر فعلی پر می‌شود</param>
public record CreateReceptionCommand(CreateReceptionDto Dto, long? ReceptionistId = null)
    : IRequest<Result<long>>;

/// <summary>اعتبارسنج پذیرش</summary>
public class CreateReceptionCommandValidator : AbstractValidator<CreateReceptionCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateReceptionCommandValidator()
    {
        RuleFor(x => x.Dto.PatientId).GreaterThan(0).WithMessage("انتخاب بیمار الزامی است");
        RuleFor(x => x.Dto.DoctorId).GreaterThan(0).WithMessage("انتخاب پزشک الزامی است");
    }
}

/// <summary>گرداننده ثبت پذیرش — ورود بیمار به جریان ویزیت روز</summary>
public class CreateReceptionCommandHandler : IRequestHandler<CreateReceptionCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateReceptionCommandHandler> _logger;
    private readonly IRealtimeNotifier _realtime;

    /// <summary>سازنده با تزریق واحد کار — و اتصال بلادرنگ صف (گام R1)</summary>
    public CreateReceptionCommandHandler(IUnitOfWork unitOfWork,
        ILogger<CreateReceptionCommandHandler> logger, IRealtimeNotifier realtime)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _realtime = realtime;
    }

    /// <summary>اجرای فرمان پذیرش</summary>
    public async Task<Result<long>> Handle(CreateReceptionCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(dto.PatientId, cancellationToken);
        if (patient is null)
            return Result<long>.Failure(Error.NotFound("بیمار", dto.PatientId));

        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(dto.DoctorId, cancellationToken);
        if (doctor is null)
            return Result<long>.Failure(Error.NotFound("پزشک", dto.DoctorId));

        var now = DateTime.UtcNow;
        var reception = new Reception
        {
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            AppointmentId = dto.AppointmentId,
            // چرخه بیست‌ویکم (گزارش پانزدهم مالک): بیمه پذیرش — سه حالت:
            //   ▪ 0  → «آزاد» صریح از کشوی پذیرش → بدون بیمه ذخیره می‌شود (null)
            //   ▪ >0 → همان شرکت بیمهٔ انتخاب‌شدهٔ کاربر (مثلاً آسیا) ذخیره می‌شود
            //   ▪ null (نبود کلید در درخواست) → سازگاری قدیم: بیمهٔ پروندهٔ بیمار
            // ریشهٔ «آسیا انتخاب شد ولی آزاد ثبت شد» همین‌جا بود: پیش‌تر مقدار تهیِ
            // ارسالی بی‌شرط با بیمهٔ پرونده (که تهی بود) جایگزین می‌شد.
            InsuranceCompanyId = dto.InsuranceCompanyId switch
            {
                0 => null,
                > 0 => dto.InsuranceCompanyId,
                _ => patient.InsuranceCompanyId
            },
            ReferralCode = dto.ReferralCode,
            ReceptionDate = now,
            ReceptionDateShamsi = PersianDateHelper.ToShamsi(now),
            Status = ReceptionStatus.Admitted,
            ReceptionistId = request.ReceptionistId,
            EnterTime = now,
            Notes = dto.Notes
        };

        await _unitOfWork.Repository<Reception>().AddAsync(reception, cancellationToken);

        // چرخه بیست‌وسوم (گزارش شانزدهم مالک): پذیرش خودش یک «مراجعه» است —
        // ستون «آخرین مراجعه» فهرست بیماران باید با پذیرش هم تازه شود (نه فقط با ثبت ویزیت)
        if (patient.LastVisitDate is null || patient.LastVisitDate < now)
            patient.LastVisitDate = now;

        // اگر از نوبت است — وضعیت نوبت به حاضر تغییر می‌کند
        if (dto.AppointmentId.HasValue)
        {
            var appointment = await _unitOfWork.Repository<Appointment>()
                .GetByIdAsync(dto.AppointmentId.Value, cancellationToken);
            if (appointment is not null)
                appointment.Status = AppointmentStatus.Arrived;
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        // پخش بلادرنگ — ورود بیمار جدید به صف برای همه مانیتورهای شعبه (گام R1)
        await _realtime.QueueChangedAsync(reason: "enqueue", cancellationToken: cancellationToken);

        _logger.LogInformation("پذیرش {Number} برای بیمار {PatientId} ثبت شد",
            reception.ReceptionNumber, dto.PatientId);

        return Result<long>.Success(reception.Id);
    }
}

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Paraclinical;

/// <summary>درخواست تصویربرداری</summary>
public record CreateImagingOrderDto(
    long PatientId, long? ReferringDoctorId, ImagingModality Modality,
    string? BodyPart, Priority Priority = Priority.Normal, string? ClinicalInfo = null);

/// <summary>گزارش تصویربرداری — ماژول حرفه‌ای (نسخهٔ ۲ تأییدشده): جزئیات ساخت‌یافته (JSON: دوز/مقایسه/قالب/اندازه‌گیری/ایمنی/اطلاع بحرانی) و پرچم نهایی‌بودن</summary>
public record ImagingReportDto(
    long OrderId, string? Technique, string? Findings, string? Impression,
    string? Recommendation, string? ReportDetails = null, bool IsFinal = true);

/// <summary>فرمان ثبت درخواست تصویربرداری</summary>
public record CreateImagingOrderCommand(CreateImagingOrderDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج درخواست تصویربرداری</summary>
public class CreateImagingOrderCommandValidator : AbstractValidator<CreateImagingOrderCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateImagingOrderCommandValidator()
    {
        RuleFor(x => x.Dto.PatientId).GreaterThan(0).WithMessage("شناسه بیمار الزامی است");
        RuleFor(x => x.Dto.BodyPart).NotEmpty().MaximumLength(200).WithMessage("ناحیه بدن الزامی و حداکثر ۲۰۰ نویسه است");
        RuleFor(x => x.Dto.Modality).IsInEnum().WithMessage("نوع تصویربرداری نامعتبر است");
        RuleFor(x => x.Dto.Priority).IsInEnum().WithMessage("اولویت نامعتبر است");
    }
}

/// <summary>گرداننده ثبت درخواست تصویربرداری</summary>
public class CreateImagingOrderCommandHandler : IRequestHandler<CreateImagingOrderCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateImagingOrderCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت درخواست</summary>
    public async Task<Result<long>> Handle(CreateImagingOrderCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        if (string.IsNullOrWhiteSpace(dto.BodyPart) || dto.BodyPart.Trim().Length > 200 ||
            !Enum.IsDefined(dto.Modality) || !Enum.IsDefined(dto.Priority))
            return Result<long>.Failure(new Error("INVALID_IMAGING_ORDER", "نوع، ناحیه یا اولویت تصویربرداری معتبر نیست"));
        if (!await _unitOfWork.Repository<Domain.Entities.People.Patient>()
                .AnyAsync(p => p.Id == dto.PatientId && !p.IsDeleted, cancellationToken))
            return Result<long>.Failure(Error.NotFound("بیمار", dto.PatientId));
        if (dto.ReferringDoctorId.HasValue && !await _unitOfWork.Repository<Domain.Entities.People.Doctor>()
                .AnyAsync(d => d.Id == dto.ReferringDoctorId.Value && !d.IsDeleted, cancellationToken))
            return Result<long>.Failure(Error.NotFound("پزشک", dto.ReferringDoctorId.Value));

        var order = new ImagingOrder
        {
            PatientId = dto.PatientId,
            ReferringDoctorId = dto.ReferringDoctorId,
            Modality = dto.Modality,
            BodyPart = dto.BodyPart.Trim(),
            Priority = dto.Priority,
            ClinicalInfo = dto.ClinicalInfo,
            Status = ImagingOrderStatus.Ordered
        };

        await _unitOfWork.Repository<ImagingOrder>().AddAsync(order, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(order.Id);
    }
}

/// <summary>فرمان ثبت گزارش رادیولوژی — تکمیل پرونده تصویربرداری</summary>
public record SubmitImagingReportCommand(ImagingReportDto Dto) : IRequest<Result>;

/// <summary>گرداننده ثبت گزارش تصویربرداری</summary>
public class SubmitImagingReportCommandHandler : IRequestHandler<SubmitImagingReportCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public SubmitImagingReportCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت گزارش</summary>
    public async Task<Result> Handle(SubmitImagingReportCommand request, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Repository<ImagingOrder>()
            .GetByIdAsync(request.Dto.OrderId, cancellationToken);

        if (order is null)
            return Result.Failure(Error.NotFound("درخواست تصویربرداری", request.Dto.OrderId));

        // گزارش نهایی و لغوشده نباید با پیش‌نویس یا الحاقیهٔ فاقد تاریخچه
        // بازنویسی شوند. مسیر الحاقیهٔ واقعی هنوز پیاده‌سازی نشده است.
        if (order.Status is ImagingOrderStatus.Reported or ImagingOrderStatus.Cancelled)
            return Result.Failure(new Error("IMAGING_REPORT_LOCKED", "گزارش نهایی یا درخواست لغوشده قابل بازنویسی نیست"));
        if (request.Dto.IsFinal && string.IsNullOrWhiteSpace(request.Dto.Findings) && string.IsNullOrWhiteSpace(request.Dto.Impression))
            return Result.Failure(new Error("IMAGING_REPORT_EMPTY", "برای نهایی‌کردن گزارش، یافته یا نتیجه‌گیری لازم است"));

        order.Technique = request.Dto.Technique;
        order.Findings = request.Dto.Findings;
        order.Impression = request.Dto.Impression;
        order.Recommendation = request.Dto.Recommendation;
        order.ReportDetails = request.Dto.ReportDetails;
        if (request.Dto.IsFinal)
        {
            order.Status = ImagingOrderStatus.Reported;
            order.ReportedAt = DateTime.UtcNow;
            order.PerformedAt ??= DateTime.UtcNow;
        }
        else if (order.Status is ImagingOrderStatus.Ordered or ImagingOrderStatus.Scheduled)
        {
            // پیش‌نویس/گزارش اولیه: مطالعه انجام‌شده محسوب می‌شود ولی هنوز نهایی نیست
            order.Status = ImagingOrderStatus.Performed;
            order.PerformedAt ??= DateTime.UtcNow;
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>فرمان تغییر وضعیت گردش کار درخواست تصویربرداری (شروع انجام/انجام‌شده/لغو)</summary>
public record UpdateImagingOrderStatusCommand(long OrderId, ImagingOrderStatus Status) : IRequest<Result>;

/// <summary>گرداننده تغییر وضعیت درخواست تصویربرداری</summary>
public class UpdateImagingOrderStatusCommandHandler : IRequestHandler<UpdateImagingOrderStatusCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public UpdateImagingOrderStatusCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای تغییر وضعیت — زمان انجام هنگام «انجام‌شده» ثبت می‌شود</summary>
    public async Task<Result> Handle(UpdateImagingOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Repository<ImagingOrder>()
            .GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure(Error.NotFound("درخواست تصویربرداری", request.OrderId));

        // از بازگشت گزارش نهایی یا درخواست لغوشده به صف انجام جلوگیری شود.
        // گزارش نهایی فقط از مسیر ثبت گزارش ایجاد می‌شود، نه تغییر وضعیت صرف.
        var allowed = (order.Status, request.Status) switch
        {
            (ImagingOrderStatus.Ordered, ImagingOrderStatus.Scheduled or ImagingOrderStatus.Performed or ImagingOrderStatus.Cancelled) => true,
            (ImagingOrderStatus.Scheduled, ImagingOrderStatus.Performed or ImagingOrderStatus.Cancelled) => true,
            (ImagingOrderStatus.Scheduled, ImagingOrderStatus.Scheduled) => true,
            (ImagingOrderStatus.Performed, ImagingOrderStatus.Performed) => true,
            (ImagingOrderStatus.Cancelled, ImagingOrderStatus.Cancelled) => true,
            _ => false
        };
        if (!allowed)
            return Result.Failure(new Error("INVALID_IMAGING_TRANSITION", "تغییر وضعیت این درخواست مجاز نیست؛ صف را تازه‌سازی کنید"));
        order.Status = request.Status;
        if (request.Status == ImagingOrderStatus.Performed)
            order.PerformedAt ??= DateTime.UtcNow;
        if (request.Status == ImagingOrderStatus.Scheduled)
            order.ScheduledAt ??= DateTime.UtcNow;

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>فرمان ثبت طرح درمان دندانپزشکی</summary>
public record CreateDentalPlanCommand(
    long PatientId, long? DentistId, decimal TotalCost,
    int EstimatedSessions, string? PlanDetails) : IRequest<Result<long>>;

/// <summary>گرداننده ثبت طرح درمان دندانپزشکی</summary>
public class CreateDentalPlanCommandHandler : IRequestHandler<CreateDentalPlanCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateDentalPlanCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت طرح درمان</summary>
    public async Task<Result<long>> Handle(CreateDentalPlanCommand request, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Repository<Domain.Entities.People.Patient>()
                .AnyAsync(p => p.Id == request.PatientId && !p.IsDeleted, cancellationToken))
            return Result<long>.Failure(Error.NotFound("بیمار", request.PatientId));
        if (request.TotalCost < 0 || request.EstimatedSessions < 0)
            return Result<long>.Failure(new Error("INVALID_DENTAL_PLAN", "مبلغ و تعداد جلسات نمی‌توانند منفی باشند"));
        var plan = new DentalTreatmentPlan
        {
            PatientId = request.PatientId,
            DentistId = request.DentistId,
            CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalCost = request.TotalCost,
            EstimatedSessions = request.EstimatedSessions,
            PlanDetails = request.PlanDetails,
            Status = TreatmentPlanStatus.Proposed
        };

        await _unitOfWork.Repository<DentalTreatmentPlan>().AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(plan.Id);
    }
}

/// <summary>
/// فرمان ثبت جلسه فیزیوتراپی — ماژول حرفه‌ای فیزیوتراپی (طرح تأییدشدهٔ مالک):
/// «یادداشت پیشرفت» ساخت‌یافته (JSON نقشهٔ بدن و شدت درد ناحیه‌ها، پرچم‌های
/// قرمز، ارزیابی ساخت‌یافته، پروتکل فعال، تمرین خانگی و اهداف) هم‌اکنون
/// همراه جلسه ذخیره می‌شود تا سابقه و نمودار روند دوره از روی آن ساخته شود.
/// </summary>
public record RecordPhysioSessionCommand(
    long PatientId, long? TherapistId, int SessionNumber,
    string? Treatments, int? Duration, byte? PainScore,
    string? ProgressNote = null) : IRequest<Result<long>>;

/// <summary>گرداننده ثبت جلسه فیزیوتراپی</summary>
public class RecordPhysioSessionCommandHandler : IRequestHandler<RecordPhysioSessionCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public RecordPhysioSessionCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت جلسه</summary>
    public async Task<Result<long>> Handle(RecordPhysioSessionCommand request, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Repository<Domain.Entities.People.Patient>()
                .AnyAsync(p => p.Id == request.PatientId && !p.IsDeleted, cancellationToken))
            return Result<long>.Failure(Error.NotFound("بیمار", request.PatientId));
        if (request.SessionNumber < 1 || request.PainScore > 10 || request.Duration is <= 0)
            return Result<long>.Failure(new Error("INVALID_PHYSIO_SESSION", "شماره جلسه، مدت یا شدت درد معتبر نیست"));
        var session = new PhysiotherapySession
        {
            PatientId = request.PatientId,
            TherapistId = request.TherapistId,
            SessionNumber = request.SessionNumber,
            SessionDate = DateTime.UtcNow,
            Treatments = request.Treatments,
            Duration = request.Duration,
            PainScore = request.PainScore,
            ProgressNote = request.ProgressNote,
            Status = SessionStatus.Completed
        };

        await _unitOfWork.Repository<PhysiotherapySession>().AddAsync(session, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(session.Id);
    }
}

/// <summary>
/// فرمان ثبت معاینه چشم — چرخهٔ بیست‌وهشتم: فیلدهای اندازه‌گیری که کلاینت
/// ارسال می‌کرد ولی این فرمان نداشت (IOP چپ/راست، حدت بینایی، فاصلهٔ
/// مردمک، دید رنگی) اضافه شدند؛ پیش‌تر این مقادیر در محل binding دور
/// ریخته می‌شدند و در سابقهٔ بیمار ذخیره نمی‌شدند.
/// </summary>
public record SaveEyeExamCommand(
    long PatientId, long? VisitId,
    decimal? RightSphere, decimal? RightCylinder, int? RightAxis,
    decimal? LeftSphere, decimal? LeftCylinder, int? LeftAxis,
    int? RightIop, int? LeftIop,
    string? RightVisualAcuity, string? LeftVisualAcuity,
    decimal? PupilDistance, string? ColorVision,
    string? Findings, string? EyewearPrescription,
    string? VisualField = null) : IRequest<Result<long>>;

/// <summary>گرداننده ثبت معاینه چشم</summary>
public class SaveEyeExamCommandHandler : IRequestHandler<SaveEyeExamCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public SaveEyeExamCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت معاینه چشم — همهٔ اندازه‌ها در رکورد بیمار می‌نشیند</summary>
    public async Task<Result<long>> Handle(SaveEyeExamCommand request, CancellationToken cancellationToken)
    {
        var patientExists = await _unitOfWork.Repository<Domain.Entities.People.Patient>()
            .AnyAsync(p => p.Id == request.PatientId && !p.IsDeleted, cancellationToken);
        if (!patientExists)
            return Result<long>.Failure(Error.NotFound("بیمار", request.PatientId));
        if (request.VisitId.HasValue && !await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>()
                .AnyAsync(v => v.Id == request.VisitId.Value && v.PatientId == request.PatientId && !v.IsDeleted, cancellationToken))
            return Result<long>.Failure(new Error("VISIT_PATIENT_MISMATCH", "ویزیت معتبر و متعلق به این بیمار یافت نشد"));
        var exam = new EyeExamRecord
        {
            PatientId = request.PatientId,
            VisitId = request.VisitId,
            RightSphere = request.RightSphere,
            RightCylinder = request.RightCylinder,
            RightAxis = request.RightAxis,
            LeftSphere = request.LeftSphere,
            LeftCylinder = request.LeftCylinder,
            LeftAxis = request.LeftAxis,
            // چرخهٔ بیست‌وهشتم — پوشش کامل اندازه‌ها
            RightIop = request.RightIop,
            LeftIop = request.LeftIop,
            RightVisualAcuity = request.RightVisualAcuity,
            LeftVisualAcuity = request.LeftVisualAcuity,
            PupilDistance = request.PupilDistance,
            VisualField = request.VisualField,
            ColorVision = request.ColorVision,
            Findings = request.Findings,
            EyewearPrescription = request.EyewearPrescription
        };

        await _unitOfWork.Repository<EyeExamRecord>().AddAsync(exam, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(exam.Id);
    }
}

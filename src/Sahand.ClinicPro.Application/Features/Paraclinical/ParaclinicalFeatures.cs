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

/// <summary>گزارش تصویربرداری</summary>
public record ImagingReportDto(
    long OrderId, string? Technique, string? Findings, string? Impression,
    string? Recommendation);

/// <summary>فرمان ثبت درخواست تصویربرداری</summary>
public record CreateImagingOrderCommand(CreateImagingOrderDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج درخواست تصویربرداری</summary>
public class CreateImagingOrderCommandValidator : AbstractValidator<CreateImagingOrderCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateImagingOrderCommandValidator()
    {
        RuleFor(x => x.Dto.PatientId).GreaterThan(0).WithMessage("شناسه بیمار الزامی است");
        RuleFor(x => x.Dto.BodyPart).NotEmpty().WithMessage("ناحیه بدن الزامی است");
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

        var order = new ImagingOrder
        {
            PatientId = dto.PatientId,
            ReferringDoctorId = dto.ReferringDoctorId,
            Modality = dto.Modality,
            BodyPart = dto.BodyPart,
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

        order.Technique = request.Dto.Technique;
        order.Findings = request.Dto.Findings;
        order.Impression = request.Dto.Impression;
        order.Recommendation = request.Dto.Recommendation;
        order.Status = ImagingOrderStatus.Reported;
        order.ReportedAt = DateTime.UtcNow;

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

/// <summary>فرمان ثبت جلسه فیزیوتراپی</summary>
public record RecordPhysioSessionCommand(
    long PatientId, long? TherapistId, int SessionNumber,
    string? Treatments, int? Duration, byte? PainScore) : IRequest<Result<long>>;

/// <summary>گرداننده ثبت جلسه فیزیوتراپی</summary>
public class RecordPhysioSessionCommandHandler : IRequestHandler<RecordPhysioSessionCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public RecordPhysioSessionCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت جلسه</summary>
    public async Task<Result<long>> Handle(RecordPhysioSessionCommand request, CancellationToken cancellationToken)
    {
        var session = new PhysiotherapySession
        {
            PatientId = request.PatientId,
            TherapistId = request.TherapistId,
            SessionNumber = request.SessionNumber,
            SessionDate = DateTime.UtcNow,
            Treatments = request.Treatments,
            Duration = request.Duration,
            PainScore = request.PainScore,
            Status = SessionStatus.Completed
        };

        await _unitOfWork.Repository<PhysiotherapySession>().AddAsync(session, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(session.Id);
    }
}

/// <summary>فرمان ثبت معاینه چشم</summary>
public record SaveEyeExamCommand(
    long PatientId, long? VisitId,
    decimal? RightSphere, decimal? RightCylinder, int? RightAxis,
    decimal? LeftSphere, decimal? LeftCylinder, int? LeftAxis,
    string? Findings, string? EyewearPrescription) : IRequest<Result<long>>;

/// <summary>گرداننده ثبت معاینه چشم</summary>
public class SaveEyeExamCommandHandler : IRequestHandler<SaveEyeExamCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public SaveEyeExamCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت معاینه چشم</summary>
    public async Task<Result<long>> Handle(SaveEyeExamCommand request, CancellationToken cancellationToken)
    {
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
            Findings = request.Findings,
            EyewearPrescription = request.EyewearPrescription
        };

        await _unitOfWork.Repository<EyeExamRecord>().AddAsync(exam, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(exam.Id);
    }
}

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Prescriptions;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Events;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Prescriptions.Commands.CreatePrescription;

/// <summary>
/// فرمان صدور نسخه — با بررسی تداخل دارویی و اعتبارسنجی اقلام
/// </summary>
/// <param name="Dto">داده نسخه</param>
public record CreatePrescriptionCommand(CreatePrescriptionDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج صدور نسخه</summary>
public class CreatePrescriptionCommandValidator : AbstractValidator<CreatePrescriptionCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreatePrescriptionCommandValidator()
    {
        RuleFor(x => x.Dto.PatientId).GreaterThan(0).WithMessage("شناسه بیمار الزامی است");
        RuleFor(x => x.Dto.DoctorId).GreaterThan(0).WithMessage("شناسه پزشک الزامی است");
        RuleFor(x => x.Dto.Items)
            .NotEmpty().WithMessage("نسخه باید حداقل یک قلم دارو داشته باشد");
        RuleForEach(x => x.Dto.Items).SetValidator(new PrescriptionItemDtoValidator());
    }
}

/// <summary>اعتبارسنج قلم نسخه</summary>
public class PrescriptionItemDtoValidator : AbstractValidator<PrescriptionItemDto>
{
    /// <summary>قواعد اعتبارسنجی قلم</summary>
    public PrescriptionItemDtoValidator()
    {
        RuleFor(x => x.MedicationId).GreaterThan(0).WithMessage("انتخاب دارو الزامی است");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("تعداد باید بیشتر از صفر باشد");
        RuleFor(x => x.Dosage).MaximumLength(100).WithMessage("دوز حداکثر ۱۰۰ کاراکتر است");
    }
}

/// <summary>گرداننده صدور نسخه — بررسی تداخل شدید قبل از ثبت</summary>
public class CreatePrescriptionCommandHandler : IRequestHandler<CreatePrescriptionCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreatePrescriptionCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreatePrescriptionCommandHandler(IUnitOfWork unitOfWork,
        ILogger<CreatePrescriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>اجرای صدور نسخه</summary>
    public async Task<Result<long>> Handle(CreatePrescriptionCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        // بررسی تداخل دارویی جفت‌ها — تداخل ممنوع رد می‌شود
        var medIds = dto.Items.Select(i => i.MedicationId).Distinct().ToList();
        var dangerousInteractions = await _unitOfWork.Repository<DrugInteraction>()
            .QueryableNoTracking
            .Where(x => medIds.Contains(x.Medication1Id) && medIds.Contains(x.Medication2Id))
            .ToListAsync(cancellationToken);

        if (dangerousInteractions.Any(x => x.Severity == DrugInteractionSeverity.Contraindicated))
        {
            var interaction = dangerousInteractions
                .First(x => x.Severity == DrugInteractionSeverity.Contraindicated);
            return Result<long>.Failure(new Error("DRUG_CONTRAINDICATION",
                $"تداخل ممنوع مصرف بین داروهای نسخه شناسایی شد — {interaction.Description}"));
        }

        var now = DateTime.UtcNow;
        var prescription = new Prescription
        {
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            VisitId = dto.VisitId,
            PrescriptionDate = now,
            Type = dto.Type,
            IsElectronic = dto.IsElectronic,
            InsuranceSystemType = dto.InsuranceSystemType,
            IsNarcotic = dto.IsNarcotic,
            Status = PrescriptionStatus.Issued,
            Notes = dto.Notes,
            ValidUntil = DateOnly.FromDateTime(now.AddDays(dto.IsNarcotic ? 3 : 30))
        };

        foreach (var item in dto.Items)
        {
            prescription.Items.Add(new PrescriptionItem
            {
                MedicationId = item.MedicationId,
                OrderIndex = item.OrderIndex,
                Dosage = item.Dosage,
                Frequency = item.Frequency,
                Duration = item.Duration,
                Route = item.Route,
                Quantity = item.Quantity,
                Unit = item.Unit,
                Instructions = item.Instructions,
                IsSos = item.IsSos
            });
        }

        await _unitOfWork.Repository<Prescription>().AddAsync(prescription, cancellationToken);

        prescription.AddDomainEvent(new PrescriptionCreatedEvent(
            prescription.Id, dto.PatientId, dto.DoctorId, dto.IsElectronic));

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("نسخه {Code} با {Count} قلم صادر شد",
            prescription.PrescriptionCode, dto.Items.Count);

        return Result<long>.Success(prescription.Id);
    }
}

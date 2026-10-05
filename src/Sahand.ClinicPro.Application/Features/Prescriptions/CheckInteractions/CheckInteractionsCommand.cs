using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Application.DTOs.Prescriptions;

namespace Sahand.ClinicPro.Application.Features.Prescriptions.CheckInteractions;

/// <summary>بررسی تداخل دارویی — داروهای نسخه + داروهای فعلی بیمار + آلرژی‌ها</summary>
public record CheckInteractionsCommand(CheckInteractionsRequest Request)
    : IRequest<Result<CheckInteractionsResult>>;

/// <summary>گرداننده بررسی تداخل — الگوریتم ۳ پرامپت</summary>
public class CheckInteractionsCommandHandler
    : IRequestHandler<CheckInteractionsCommand, Result<CheckInteractionsResult>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public CheckInteractionsCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<CheckInteractionsResult>> Handle(CheckInteractionsCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;
        var result = new List<InteractionDto>();

        // همه شناسه‌های دارو — نسخه فعلی + داروهای فعلی بیمار
        var patient = await _unitOfWork.Repository<Patient>().QueryableNoTracking
            .FirstOrDefaultAsync(p => p.Id == dto.PatientId, cancellationToken);

        var medIds = dto.MedicationIds.Distinct().ToList();
        var meds = await _unitOfWork.Repository<Medication>().QueryableNoTracking
            .Where(m => medIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.GenericNameFa ?? m.GenericName, cancellationToken);

        // ۱) تداخل دارو-دارو از جدول مرجع
        var pairs = await _unitOfWork.Repository<DrugInteraction>().QueryableNoTracking
            .Where(d => dto.MedicationIds.Contains(d.Medication1Id) &&
                        dto.MedicationIds.Contains(d.Medication2Id))
            .ToListAsync(cancellationToken);
        foreach (var pair in pairs)
        {
            result.Add(new InteractionDto(
                meds.GetValueOrDefault(pair.Medication1Id, "؟"),
                meds.GetValueOrDefault(pair.Medication2Id, "؟"),
                (int)pair.Severity,
                SeverityText(pair.Severity),
                SeverityColor(pair.Severity),
                pair.Mechanism,
                pair.Recommendation,
                pair.Description,
                false));
        }

        // ۲) تداخل با آلرژی‌های بیمار — سطح قرمز تیره BLOCK
        if (patient?.Allergies is { Length: > 0 })
        {
            List<string>? allergies = null;
            try { allergies = JsonSerializer.Deserialize<List<string>>(patient.Allergies); }
            catch { allergies = new List<string> { patient.Allergies }; }

            if (allergies is { Count: > 0 })
            {
                foreach (var medId in dto.MedicationIds)
                {
                    var name = meds.GetValueOrDefault(medId, string.Empty);
                    if (string.IsNullOrEmpty(name)) continue;
                    var matched = allergies.FirstOrDefault(a =>
                        !string.IsNullOrWhiteSpace(a) &&
                        name.Contains(a.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (matched is not null)
                    {
                        result.Add(new InteractionDto(
                            name, $"آلرژی بیمار: {matched}",
                            4, "ممنوع مصرف — آلرژی", "#B71C1C",
                            "بیمار به این ماده حساسیت دارویی دارد",
                            "حذف دارو از نسخه الزامی است — در صورت اصرار پزشک تأیید صریح لازم است",
                            $"تداخل با آلرژی ثبت‌شده بیمار ({matched})",
                            true));
                    }
                }
            }
        }

        var hasAllergy = result.Any(r => r.IsAllergyConflict);
        var hasSevere = result.Any(r => r.Severity >= 3);

        return Result<CheckInteractionsResult>.Success(
            new CheckInteractionsResult(result, hasSevere, hasAllergy));
    }

    /// <summary>متن شدت</summary>
    private static string SeverityText(Domain.Enums.DrugInteractionSeverity severity) => severity switch
    {
        Domain.Enums.DrugInteractionSeverity.Minor => "خفیف",
        Domain.Enums.DrugInteractionSeverity.Moderate => "متوسط",
        Domain.Enums.DrugInteractionSeverity.Major => "شدید",
        Domain.Enums.DrugInteractionSeverity.Contraindicated => "ممنوع مصرف",
        _ => "نامشخص"
    };

    /// <summary>رنگ سطح</summary>
    private static string SeverityColor(Domain.Enums.DrugInteractionSeverity severity) => severity switch
    {
        Domain.Enums.DrugInteractionSeverity.Minor => "#FFF9C4",
        Domain.Enums.DrugInteractionSeverity.Moderate => "#FFF3E0",
        Domain.Enums.DrugInteractionSeverity.Major => "#FFEBEE",
        Domain.Enums.DrugInteractionSeverity.Contraindicated => "#B71C1C",
        _ => "#ECEFF1"
    };
}

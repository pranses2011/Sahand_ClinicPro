using MediatR;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.MergePatients;

/// <summary>
/// فرمان ادغام پروندهٔ موقت بیمار ناشناس در پروندهٔ اصلی موجود —
/// همهٔ سوابق بالینی/مالی بیمار موقت به پروندهٔ اصلی منتقل و پروندهٔ
/// موقت حذف نرم می‌شود تا هیچ سابقه‌ای از دست نرود
/// </summary>
public record MergePatientsCommand(long SourcePatientId, long TargetPatientId)
    : IRequest<Result<bool>>;

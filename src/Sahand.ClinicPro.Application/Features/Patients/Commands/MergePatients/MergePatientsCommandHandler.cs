using System.Reflection;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Integration;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.MergePatients;

/// <summary>
/// گرداننده ادغام پرونده‌ها — PatientId همهٔ موجودیت‌های وابسته (پذیرش،
/// نوبت، ویزیت، فاکتور، آزمایش، تصویربرداری، دندان، چشم، فیزیوتراپی و…)
/// از پروندهٔ موقت به پروندهٔ اصلی منتقل می‌شود و پروندهٔ موقت حذف نرم
/// می‌گردد؛ کد موقت روی پروندهٔ موقت برای ردیابی سوابق باقی می‌ماند.
/// </summary>
public class MergePatientsCommandHandler : IRequestHandler<MergePatientsCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public MergePatientsCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>انواع موجودیت‌های دارای PatientId که باید منتقل شوند</summary>
    private static readonly Type[] PatientLinkedTypes =
    {
        typeof(Reception), typeof(Appointment), typeof(WaitlistEntry),
        typeof(Visit), typeof(Prescription), typeof(VitalSign),
        typeof(GrowthMeasurement), typeof(FamilyHistory), typeof(ChronicCarePlan),
        typeof(Invoice), typeof(Payment), typeof(InsuranceInquiry),
        typeof(PatientHistory),
        typeof(LabOrder), typeof(ImagingOrder), typeof(DentalRecord),
        typeof(DentalTreatmentPlan), typeof(EyeExamRecord), typeof(PhysiotherapySession),
        typeof(ReferralRecord), typeof(SepasTransaction), typeof(SibSystemRecord)
    };

    /// <summary>اجرای فرمان ادغام</summary>
    public async Task<Result<bool>> Handle(MergePatientsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.SourcePatientId == request.TargetPatientId)
            return Result<bool>.Failure(
                new Error("Patient.MergeSameRecord", "پروندهٔ مبدأ و مقصد ادغام یکسان است"));

        var repo = _unitOfWork.Repository<Patient>();
        var source = await repo.QueryableTracking
            .FirstOrDefaultAsync(p => p.Id == request.SourcePatientId, cancellationToken);
        if (source is null)
            return Result<bool>.Failure(Error.NotFound("بیمار", request.SourcePatientId));

        var targetExists = await repo.QueryableNoTracking
            .AnyAsync(p => p.Id == request.TargetPatientId, cancellationToken);
        if (!targetExists)
            return Result<bool>.Failure(Error.NotFound("بیمار", request.TargetPatientId));

        if (!source.IsUnidentified)
            return Result<bool>.Failure(new Error("Patient.MergeSourceNotTemporary",
                "فقط پروندهٔ موقت بیمار ناشناس قابل ادغام در پروندهٔ اصلی است"));

        // انتقال همهٔ سوابق وابسته به پروندهٔ اصلی
        var moveMethod = typeof(MergePatientsCommandHandler)
            .GetMethod(nameof(MoveRecordsAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;
        foreach (var type in PatientLinkedTypes)
        {
            var task = (Task)moveMethod.MakeGenericMethod(type)
                .Invoke(this, new object[] { request.SourcePatientId, request.TargetPatientId, cancellationToken })!;
            await task;
        }

        // حذف نرم پروندهٔ موقت — کد موقت برای ردیابی در سوابق می‌ماند
        source.IsDeleted = true;
        source.DeletedAt = DateTime.UtcNow;
        source.IsActive = false;

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    /// <summary>انتقال رکوردهای یک نوع موجودیت از بیمار مبدأ به مقصد</summary>
    private async Task MoveRecordsAsync<T>(long fromPatientId, long toPatientId,
        CancellationToken cancellationToken) where T : BaseEntity
    {
        var records = await _unitOfWork.Repository<T>()
            .QueryableTracking
            .Where(x => EF.Property<long>(x, "PatientId") == fromPatientId)
            .ToListAsync(cancellationToken);
        if (records.Count == 0) return;

        var property = typeof(T).GetProperty("PatientId")!;
        foreach (var record in records)
            property.SetValue(record, toPatientId);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Visits.CallNextPatient;

/// <summary>خروجی فراخوان — شناسه ویزیت ساخته‌شده</summary>
public record CallNextResult(long VisitId, long ReceptionId, long PatientId);

/// <summary>فرمان فراخوان بیمار بعدی از صف — فرم ۱۳</summary>
public record CallNextPatientCommand(long? DoctorId) : IRequest<Result<CallNextResult>>;

/// <summary>گرداننده فراخوان</summary>
public class CallNextPatientCommandHandler : IRequestHandler<CallNextPatientCommand, Result<CallNextResult>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public CallNextPatientCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — اولین پذیرش در صف → ویزیت در حال انجام</summary>
    public async Task<Result<CallNextResult>> Handle(CallNextPatientCommand request,
        CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var next = await _unitOfWork.Repository<Reception>().QueryableTracking
            .Where(r => r.ReceptionDate >= today && r.ReceptionDate < tomorrow &&
                        r.Status == ReceptionStatus.InQueue &&
                        (request.DoctorId == null || r.DoctorId == request.DoctorId))
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (next is null)
            return Result<CallNextResult>.Failure(new Error("QUEUE.EMPTY", "بیماری در صف انتظار نیست"));

        // هر ویزیت در جریان برای همین پزشک؟ پایان خودکار طبق «بیمار بعدی» فرم ۱۱
        var inProgress = await _unitOfWork.Repository<Visit>().QueryableTracking
            .Where(v => v.VisitDate >= today && v.VisitDate < tomorrow &&
                        v.Status == VisitStatus.InProgress &&
                        (request.DoctorId == null || v.DoctorId == request.DoctorId))
            .ToListAsync(cancellationToken);
        foreach (var v in inProgress)
        {
            v.Status = VisitStatus.Completed;
            v.EndTime = DateTime.UtcNow;
            v.Duration = v.StartTime.HasValue ? (int)(DateTime.UtcNow - v.StartTime.Value).TotalMinutes : null;
            _unitOfWork.Repository<Visit>().Update(v);
        }

        var visit = new Visit
        {
            PatientId = next.PatientId,
            DoctorId = next.DoctorId,
            ReceptionId = next.Id,
            VisitDate = DateTime.UtcNow,
            VisitDateShamsi = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(DateTime.UtcNow),
            VisitType = VisitType.Initial,
            Status = VisitStatus.InProgress,
            StartTime = DateTime.UtcNow,
            VisitFee = 0m,
            ChiefComplaint = next.Notes
        };
        await _unitOfWork.Repository<Visit>().AddAsync(visit, cancellationToken);

        next.Status = ReceptionStatus.InVisit;
        _unitOfWork.Repository<Reception>().Update(next);

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<CallNextResult>.Success(new CallNextResult(visit.Id, next.Id, next.PatientId));
    }
}

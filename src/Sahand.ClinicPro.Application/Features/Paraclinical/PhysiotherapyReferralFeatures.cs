using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Paraclinical;

/// <summary>درخواست پایدار و قابل تکرار همان ارجاع.</summary>
public record CreatePhysiotherapyReferralCommand(Guid RequestId, long VisitId, long PatientId, long DoctorId, string? Note) : IRequest<Result<long>>;

public class CreatePhysiotherapyReferralCommandHandler : IRequestHandler<CreatePhysiotherapyReferralCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;
    public CreatePhysiotherapyReferralCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Result<long>> Handle(CreatePhysiotherapyReferralCommand request, CancellationToken cancellationToken)
    {
        var note = request.Note?.Trim();
        if (request.RequestId == Guid.Empty || request.VisitId <= 0 || request.PatientId <= 0 || request.DoctorId <= 0 ||
            string.IsNullOrWhiteSpace(note) || note.Length > 2000)
            return Result<long>.Failure(new Error("PHYSIO.REFERRAL_INVALID", "شرح ارجاع، ویزیت و هویت بیمار و پزشک الزامی است؛ شرح حداکثر ۲۰۰۰ نویسه است."));
        var repo = _unitOfWork.Repository<PhysiotherapyReferral>();
        var existing = await repo.QueryableNoTracking.FirstOrDefaultAsync(x => x.RequestId == request.RequestId, cancellationToken);
        if (existing is not null)
            return existing.VisitId == request.VisitId && existing.PatientId == request.PatientId && existing.DoctorId == request.DoctorId && existing.Note == note
                ? Result<long>.Success(existing.Id)
                : Result<long>.Failure(new Error("PHYSIO.REFERRAL_KEY_CONFLICT", "شناسه درخواست پیش‌تر برای ارجاع دیگری استفاده شده است."));
        var visit = await _unitOfWork.Repository<Visit>().GetByIdAsync(request.VisitId, cancellationToken);
        var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(request.PatientId, cancellationToken);
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(request.DoctorId, cancellationToken);
        if (visit is null || visit.IsDeleted || patient is null || patient.IsDeleted || doctor is null || doctor.IsDeleted ||
            visit.PatientId != request.PatientId || visit.DoctorId != request.DoctorId)
            return Result<long>.Failure(new Error("PHYSIO.REFERRAL_MISMATCH", "ویزیت با بیمار و پزشک ارجاع مطابقت ندارد."));
        var referral = new PhysiotherapyReferral { RequestId = request.RequestId, VisitId = visit.Id,
            PatientId = patient.Id, DoctorId = doctor.Id, Note = note, CreatedAt = DateTime.UtcNow };
        await repo.AddAsync(referral, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(referral.Id);
    }
}

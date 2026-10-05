using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Prescriptions.SubmitElectronic;

/// <summary>خروجی ارسال الکترونیک</summary>
public record SubmitElectronicResult(long PrescriptionId, string? TrackingCode, string? Error);

/// <summary>فرمان ارسال نسخه به سامانه نسخه الکترونیک — فرم ۱۲</summary>
public record SubmitElectronicCommand(long PrescriptionId) : IRequest<Result<SubmitElectronicResult>>;

/// <summary>گرداننده ارسال الکترونیک</summary>
public class SubmitElectronicCommandHandler
    : IRequestHandler<SubmitElectronicCommand, Result<SubmitElectronicResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IElectronicPrescriptionService _eprescription;

    /// <summary>سازنده</summary>
    public SubmitElectronicCommandHandler(IUnitOfWork unitOfWork,
        IElectronicPrescriptionService eprescription)
    {
        _unitOfWork = unitOfWork;
        _eprescription = eprescription;
    }

    /// <summary>اجرا — ارسال و ثبت کد رهگیری</summary>
    public async Task<Result<SubmitElectronicResult>> Handle(SubmitElectronicCommand request,
        CancellationToken cancellationToken)
    {
        var prescription = await _unitOfWork.Repository<Prescription>()
            .QueryableTracking
            .Include(p => p.Items).ThenInclude(i => i.Medication)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
            return Result<SubmitElectronicResult>.Failure(Error.NotFound("نسخه", request.PrescriptionId));

        var gov = await _eprescription.SendAsync(request.PrescriptionId, cancellationToken);
        if (!gov.Success || string.IsNullOrWhiteSpace(gov.ReferenceId))
            return Result<SubmitElectronicResult>.Failure(
                new Error("EPRESCRIPTION.FAILED", gov.Error ?? "سامانه نسخه الکترونیک پاسخ نداد"));

        prescription.IsElectronic = true;
        prescription.ElectronicTrackingCode = gov.ReferenceId;
        prescription.Status = PrescriptionStatus.SentToInsurance;
        _unitOfWork.Repository<Prescription>().Update(prescription);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<SubmitElectronicResult>.Success(
            new SubmitElectronicResult(prescription.Id, gov.ReferenceId, null));
    }
}

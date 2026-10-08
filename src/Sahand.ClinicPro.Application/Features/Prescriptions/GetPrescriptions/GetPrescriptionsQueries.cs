using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.DTOs.Prescriptions;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Prescriptions.Queries;

/// <summary>
/// پرس‌وجوی نسخه‌های یک ویزیت
/// </summary>
/// <param name="VisitId">شناسه ویزیت</param>
public record GetVisitPrescriptionsQuery(long VisitId) : IRequest<Result<List<PrescriptionListDto>>>;

/// <summary>گرداننده نسخه‌های ویزیت</summary>
public class GetVisitPrescriptionsQueryHandler
    : IRequestHandler<GetVisitPrescriptionsQuery, Result<List<PrescriptionListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetVisitPrescriptionsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<PrescriptionListDto>>> Handle(GetVisitPrescriptionsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<Domain.Entities.Clinical.Prescription>()
            .QueryableNoTracking
            .Include(p => p.Patient)
            .Include(p => p.Doctor).ThenInclude(d => d!.Employee)
            .Where(p => p.VisitId == request.VisitId)
            .OrderByDescending(p => p.PrescriptionDate)
            .Select(p => new PrescriptionListDto(
                p.Id,
                p.PrescriptionCode,
                p.Patient != null ? p.Patient.FirstName + " " + p.Patient.LastName : "",
                p.Doctor != null && p.Doctor.Employee != null
                    ? p.Doctor.Employee.FirstName + " " + p.Doctor.Employee.LastName : "پزشک",
                p.PrescriptionDate.HasValue
                    ? Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(p.PrescriptionDate.Value)
                    : string.Empty,
                p.Type, p.Status, p.IsElectronic))
            .ToListAsync(cancellationToken);

        return Result<List<PrescriptionListDto>>.Success(items);
    }
}

/// <summary>
/// پرس‌وجوی جزئیات کامل نسخه با اقلام
/// </summary>
/// <param name="Id">شناسه نسخه</param>
public record GetPrescriptionByIdQuery(long Id) : IRequest<Result<PrescriptionDetailDto>>;

/// <summary>گرداننده جزئیات نسخه</summary>
public class GetPrescriptionByIdQueryHandler
    : IRequestHandler<GetPrescriptionByIdQuery, Result<PrescriptionDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetPrescriptionByIdQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<PrescriptionDetailDto>> Handle(GetPrescriptionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var p = await _unitOfWork.Repository<Domain.Entities.Clinical.Prescription>()
            .QueryableNoTracking
            // چرخه بیست‌وسوم (گزارش شانزدهم مالک): Include بیمار و پزشک برای
            // PatientName/DoctorName در جزئیات (چاپ مجدد و هدر پنجرهٔ جزئیات)
            .Include(x => x.Patient)
            .Include(x => x.Doctor).ThenInclude(d => d!.Employee)
            .Include(x => x.Items).ThenInclude(i => i.Medication)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (p is null)
            return Result<PrescriptionDetailDto>.Failure(Error.NotFound("نسخه", request.Id));

        var dto = new PrescriptionDetailDto(
            p.Id, p.PrescriptionCode, p.PatientId,
            // نام بیمار و پزشک — برای چاپ مجدد و هدر پنجرهٔ جزئیات (چرخه ۲۳)
            p.Patient != null ? p.Patient.FirstName + " " + p.Patient.LastName : "",
            p.DoctorId,
            p.Doctor != null && p.Doctor.Employee != null
                ? p.Doctor.Employee.FirstName + " " + p.Doctor.Employee.LastName : "پزشک",
            p.VisitId,
            p.PrescriptionDate.HasValue
                ? Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(p.PrescriptionDate.Value)
                : string.Empty,
            p.Type, p.Status, p.IsElectronic, p.IsNarcotic, p.Notes,
            p.Items.OrderBy(i => i.OrderIndex)
                .Select(i => new PrescriptionItemDetailDto(
                    i.MedicationId,
                    i.Medication != null ? i.Medication.GenericNameFa ?? i.Medication.GenericName : "",
                    i.OrderIndex ?? 0, i.Dosage, i.Frequency, i.Duration,
                    i.Quantity, i.Unit, i.Instructions))
                .ToList());

        return Result<PrescriptionDetailDto>.Success(dto);
    }
}

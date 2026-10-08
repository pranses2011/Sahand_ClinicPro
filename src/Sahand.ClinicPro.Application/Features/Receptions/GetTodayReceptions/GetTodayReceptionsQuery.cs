using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Receptions.Queries.GetTodayReceptions;

/// <summary>
/// پرس‌وجوی پذیرش‌های امروز — صفحه پذیرش فعال
/// </summary>
/// <param name="DoctorId">فیلتر پزشک — اختیاری</param>
public record GetTodayReceptionsQuery(long? DoctorId = null) : IRequest<Result<List<ReceptionListDto>>>;

/// <summary>گرداننده پذیرش‌های روز</summary>
public class GetTodayReceptionsQueryHandler
    : IRequestHandler<GetTodayReceptionsQuery, Result<List<ReceptionListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetTodayReceptionsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<ReceptionListDto>>> Handle(GetTodayReceptionsQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        IQueryable<Reception> query = _unitOfWork.Repository<Reception>()
            .QueryableNoTracking
            .Include(r => r.Patient)
            .Include(r => r.Doctor).ThenInclude(d => d!.Employee)
            .Include(r => r.InsuranceCompany)
            .Where(r => DateOnly.FromDateTime(r.ReceptionDate) == today);

        if (request.DoctorId.HasValue)
            query = query.Where(r => r.DoctorId == request.DoctorId.Value);

        var items = await query
            .OrderBy(r => r.ReceptionDate)
            .Select(r => new ReceptionListDto(
                r.Id,
                r.ReceptionNumber,
                r.Patient != null ? r.Patient.FirstName + " " + r.Patient.LastName : "",
                // چرخه ۲۴ — پذیرش خدمتی: به‌جای پزشک، نام خدمت نمایش داده می‌شود
                r.Doctor != null && r.Doctor.Employee != null
                    ? r.Doctor.Employee.FirstName + " " + r.Doctor.Employee.LastName
                    : (r.ServiceName ?? "خدمت درمانی"),
                r.ReceptionDate.ToString("HH:mm"),
                r.Status,
                r.InsuranceCompany != null ? r.InsuranceCompany.Name : null,
                r.WaitingTime))
            .ToListAsync(cancellationToken);

        return Result<List<ReceptionListDto>>.Success(items);
    }
}

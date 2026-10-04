using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Reports;

/// <summary>درآمد روزانه یک بازه</summary>
public record DailyRevenueReportDto(
    string Date, string DateShamsi, int InvoiceCount,
    decimal Total, decimal Paid, decimal Remaining);

/// <summary>پرس‌وجوی درآمد روزانه بازه — نمودار خطی داشبورد مالی</summary>
public record GetDailyRevenueQuery(int Days = 30) : IRequest<Result<List<DailyRevenueReportDto>>>;

/// <summary>گرداننده درآمد روزانه</summary>
public class GetDailyRevenueQueryHandler
    : IRequestHandler<GetDailyRevenueQuery, Result<List<DailyRevenueReportDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetDailyRevenueQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<DailyRevenueReportDto>>> Handle(GetDailyRevenueQuery request,
        CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.Date.AddDays(-request.Days);

        var rows = await _unitOfWork.Repository<Invoice>()
            .QueryableNoTracking
            .Where(i => i.InvoiceDate >= from && i.Status != InvoiceStatus.Cancelled)
            .Select(i => new { i.InvoiceDate, i.TotalAmount, i.PaidAmount })
            .ToListAsync(cancellationToken);

        var result = rows
            .GroupBy(r => DateOnly.FromDateTime(r.InvoiceDate))
            .Select(g => new DailyRevenueReportDto(
                g.Key.ToString("yyyy-MM-dd"),
                Common.Helpers.PersianDateHelper.ToShamsi(g.Key.ToDateTime(TimeOnly.MinValue)),
                g.Count(),
                g.Sum(x => x.TotalAmount),
                g.Sum(x => x.PaidAmount),
                g.Sum(x => x.TotalAmount - x.PaidAmount)))
            .OrderBy(d => d.Date)
            .ToList();

        return Result<List<DailyRevenueReportDto>>.Success(result);
    }
}

/// <summary>عملکرد پزشک</summary>
public record DoctorPerformanceDto(
    long DoctorId, string DoctorName, int VisitsCount,
    decimal TotalRevenue, decimal DoctorShare);

/// <summary>پرس‌وجوی عملکرد پزشکان بازه</summary>
public record GetDoctorPerformanceQuery(int Days = 30) : IRequest<Result<List<DoctorPerformanceDto>>>;

/// <summary>گرداننده عملکرد پزشکان</summary>
public class GetDoctorPerformanceQueryHandler
    : IRequestHandler<GetDoctorPerformanceQuery, Result<List<DoctorPerformanceDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetDoctorPerformanceQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو — شمارش ویزیت‌ها و جمع سهم پزشک</summary>
    public async Task<Result<List<DoctorPerformanceDto>>> Handle(GetDoctorPerformanceQuery request,
        CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.Date.AddDays(-request.Days);

        var performance = await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>()
            .QueryableNoTracking
            .Where(v => v.VisitDate >= from && v.Status == Domain.Enums.VisitStatus.Completed)
            .GroupBy(v => new { v.DoctorId })
            .Select(g => new { DoctorId = g.Key.DoctorId, Visits = g.Count() })
            .ToListAsync(cancellationToken);

        var shares = await _unitOfWork.Repository<InvoiceItem>()
            .QueryableNoTracking
            .Where(i => i.Invoice != null && i.Invoice.InvoiceDate >= from
                     && i.DoctorId != null)
            .GroupBy(i => i.DoctorId!.Value)
            .Select(g => new
            {
                DoctorId = g.Key,
                Revenue = g.Sum(x => x.TotalPrice),
                Share = g.Sum(x => x.DoctorShareAmount)
            })
            .ToDictionaryAsync(x => x.DoctorId, cancellationToken);

        var doctors = await _unitOfWork.Repository<Doctor>()
            .QueryableNoTracking.Include(d => d.Employee)
            .ToListAsync(cancellationToken);

        var result = doctors.Select(d => new DoctorPerformanceDto(
            d.Id,
            d.Employee is not null ? $"{d.Employee.FirstName} {d.Employee.LastName}" : $"پزشک {d.Id}",
            performance.FirstOrDefault(p => p.DoctorId == d.Id)?.Visits ?? 0,
            shares.TryGetValue(d.Id, out var s) ? s.Revenue : 0,
            shares.TryGetValue(d.Id, out var s2) ? s2.Share : 0)).ToList();

        return Result<List<DoctorPerformanceDto>>.Success(result);
    }
}

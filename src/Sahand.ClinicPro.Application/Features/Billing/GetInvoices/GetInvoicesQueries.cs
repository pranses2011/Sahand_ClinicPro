using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Billing.Queries;

/// <summary>پرس‌وجوی فهرست فاکتورها با صفحه‌بندی</summary>
public record GetInvoicesQuery(PagedRequest? Page = null, long? PatientId = null,
    PaymentStatus? Status = null) : IRequest<Result<PagedResponse<InvoiceListDto>>>;

/// <summary>گرداننده فهرست فاکتورها</summary>
public class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, Result<PagedResponse<InvoiceListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetInvoicesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<PagedResponse<InvoiceListDto>>> Handle(GetInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page ?? new PagedRequest();
        IQueryable<Invoice> query = _unitOfWork.Repository<Invoice>()
            .QueryableNoTracking.Include(i => i.Patient);

        if (request.PatientId.HasValue)
            query = query.Where(i => i.PatientId == request.PatientId.Value);
        if (request.Status.HasValue)
            query = query.Where(i => i.PaymentStatus == request.Status.Value);
        if (!string.IsNullOrWhiteSpace(page.Search))
        {
            var term = page.Search.Trim();
            var isId = long.TryParse(term, out var searchedId) && searchedId > 0;
            query = query.Where(i =>
                (isId && i.Id == searchedId) ||
                (i.InvoiceNumber != null && i.InvoiceNumber.Contains(term)) ||
                (i.Patient != null && i.Patient.LastName.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page.PageNumber - 1) * page.PageSize)
            .Take(page.PageSize)
            .Select(i => new InvoiceListDto(
                i.Id, i.InvoiceNumber,
                i.Patient != null ? i.Patient.FirstName + " " + i.Patient.LastName : "",
                i.InvoiceDateShamsi ?? string.Empty,
                i.TotalAmount, i.PaidAmount, i.RemainingAmount,
                i.PaymentStatus, i.Status))
            .ToListAsync(cancellationToken);

        return Result<PagedResponse<InvoiceListDto>>.Success(
            PagedResponse<InvoiceListDto>.Create(items, page.PageNumber, page.PageSize, total));
    }
}

/// <summary>پرس‌وجوی جزئیات فاکتور با اقلام</summary>
public record GetInvoiceByIdQuery(long Id) : IRequest<Result<InvoiceDetailDto>>;

/// <summary>گرداننده جزئیات فاکتور</summary>
public class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetInvoiceByIdQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<InvoiceDetailDto>> Handle(GetInvoiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var i = await _unitOfWork.Repository<Invoice>()
            .QueryableNoTracking
            .Include(x => x.Patient)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (i is null)
            return Result<InvoiceDetailDto>.Failure(Error.NotFound("فاکتور", request.Id));

        var dto = new InvoiceDetailDto(
            i.Id, i.InvoiceNumber, i.PatientId,
            i.Patient != null ? i.Patient.FirstName + " " + i.Patient.LastName : "",
            i.InvoiceDateShamsi ?? string.Empty,
            i.InvoiceType, i.SubTotal, i.DiscountAmount, i.Tax,
            i.TotalAmount, i.PaidAmount, i.RemainingAmount,
            i.PaymentStatus, i.Status,
            i.Items.Select(it => new InvoiceItemDetailDto(
                it.ServiceId, it.Description, it.Quantity, it.UnitPrice,
                it.DiscountAmount, it.PatientShare, it.InsuranceShare, it.TotalPrice)).ToList());

        return Result<InvoiceDetailDto>.Success(dto);
    }
}

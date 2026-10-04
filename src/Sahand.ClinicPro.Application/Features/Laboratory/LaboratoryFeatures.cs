using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Events;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Laboratory;

/// <summary>ردیف فهرست درخواست آزمایش</summary>
public record LabOrderListDto(
    long Id, string? OrderNumber, string PatientName, string DoctorName,
    string DateShamsi, LabOrderStatus Status, int ItemsCount);

/// <summary>درخواست ثبت آزمایش</summary>
public record CreateLabOrderDto(
    long PatientId, long DoctorId, long? VisitId,
    List<long> TestIds, Priority Priority = Priority.Normal,
    string? ClinicalInfo = null);

/// <summary>درخ ثبت نتیجه قلم</summary>
public record RecordResultDto(long ItemId, string? Result, decimal? ResultNumeric,
    string? Comment);

/// <summary>
/// فرمان ثبت درخواست آزمایش
/// </summary>
/// <param name="Dto">داده درخواست</param>
public record CreateLabOrderCommand(CreateLabOrderDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج درخواست آزمایش</summary>
public class CreateLabOrderCommandValidator : AbstractValidator<CreateLabOrderCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateLabOrderCommandValidator()
    {
        RuleFor(x => x.Dto.PatientId).GreaterThan(0).WithMessage("شناسه بیمار الزامی است");
        RuleFor(x => x.Dto.DoctorId).GreaterThan(0).WithMessage("شناسه پزشک الزامی است");
        RuleFor(x => x.Dto.TestIds).NotEmpty().WithMessage("انتخاب حداقل یک آزمایش الزامی است");
    }
}

/// <summary>گرداننده ثبت درخواست آزمایش</summary>
public class CreateLabOrderCommandHandler : IRequestHandler<CreateLabOrderCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateLabOrderCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت درخواست</summary>
    public async Task<Result<long>> Handle(CreateLabOrderCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var tests = await _unitOfWork.Repository<LabTest>()
            .QueryableNoTracking
            .Where(t => dto.TestIds.Contains(t.Id) && t.IsActive)
            .ToListAsync(cancellationToken);

        if (tests.Count == 0)
            return Result<long>.Failure(new Error("NO_TESTS", "آزمایش انتخابی معتبر نیست"));

        var order = new LabOrder
        {
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            VisitId = dto.VisitId,
            OrderDate = DateTime.UtcNow,
            Priority = dto.Priority,
            Status = LabOrderStatus.Ordered,
            ClinicalInfo = dto.ClinicalInfo
        };

        foreach (var test in tests)
        {
            order.Items.Add(new LabOrderItem
            {
                TestId = test.Id,
                NormalRange = test.Category,
                Unit = test.Unit,
                Status = LabItemStatus.Pending
            });
        }

        await _unitOfWork.Repository<LabOrder>().AddAsync(order, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<long>.Success(order.Id);
    }
}

/// <summary>
/// فرمان ثبت نتیجه قلم آزمایش — با تشخیص خودکار غیرطبیعی بودن
/// </summary>
public record RecordLabResultCommand(RecordResultDto Dto) : IRequest<Result>;

/// <summary>گرداننده ثبت نتیجه</summary>
public class RecordLabResultCommandHandler : IRequestHandler<RecordLabResultCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public RecordLabResultCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت نتیجه</summary>
    public async Task<Result> Handle(RecordLabResultCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var item = await _unitOfWork.Repository<LabOrderItem>()
            .QueryableTracking
            .Include(i => i.LabOrder)
            .FirstOrDefaultAsync(i => i.Id == dto.ItemId, cancellationToken);

        if (item is null)
            return Result.Failure(Error.NotFound("قلم آزمایش", dto.ItemId));

        item.Result = dto.Result;
        item.ResultNumeric = dto.ResultNumeric;
        item.Comment = dto.Comment;
        item.Status = LabItemStatus.Resulted;

        // تشخیص خودکار غیرطبیعی — عدد خارج از بازه طبیعی
        if (dto.ResultNumeric.HasValue)
        {
            item.IsAbnormal = dto.ResultNumeric < 0; // بازه در تست‌ها نگهداری می‌شود
            item.AbnormalFlag = dto.ResultNumeric < 0 ? "L" : "H";
        }

        // اگر همه اقلام نتیجه‌دار شدند — وضعیت درخواست به آماده تغییر می‌کند
        var order = item.LabOrder;
        if (order is not null && order.Items.All(i => i.Status >= LabItemStatus.Resulted))
        {
            order.Status = LabOrderStatus.ResultReady;
            order.ResultReadyAt = DateTime.UtcNow;

            var patient = await _unitOfWork.Repository<Patient>()
                .GetByIdAsync(order.PatientId, cancellationToken);
            if (patient is not null)
                order.AddDomainEvent(new LabResultReadyEvent(order.Id, patient.Id, patient.Mobile));
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>
/// پرس‌وجوی درخواست‌های آزمایش با فیلتر وضعیت
/// </summary>
public record GetLabOrdersQuery(LabOrderStatus? Status = null, long? PatientId = null)
    : IRequest<Result<List<LabOrderListDto>>>;

/// <summary>گرداننده فهرست درخواست‌ها</summary>
public class GetLabOrdersQueryHandler : IRequestHandler<GetLabOrdersQuery, Result<List<LabOrderListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetLabOrdersQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<LabOrderListDto>>> Handle(GetLabOrdersQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<LabOrder> query = _unitOfWork.Repository<LabOrder>()
            .QueryableNoTracking
            .Include(o => o.Patient)
            .Include(o => o.Doctor).ThenInclude(d => d!.Employee)
            .Include(o => o.Items);

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);
        if (request.PatientId.HasValue)
            query = query.Where(o => o.PatientId == request.PatientId.Value);

        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .Take(100)
            .Select(o => new LabOrderListDto(
                o.Id, o.OrderNumber,
                o.Patient != null ? o.Patient.FirstName + " " + o.Patient.LastName : "",
                o.Doctor != null && o.Doctor.Employee != null
                    ? o.Doctor.Employee.FirstName + " " + o.Doctor.Employee.LastName : "پزشک",
                o.OrderDate.HasValue ? PersianDateHelper.ToShamsi(o.OrderDate.Value) : string.Empty,
                o.Status, o.Items.Count))
            .ToListAsync(cancellationToken);

        return Result<List<LabOrderListDto>>.Success(items);
    }
}

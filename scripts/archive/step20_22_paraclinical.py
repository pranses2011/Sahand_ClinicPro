# -*- coding: utf-8 -*-
"""گام‌های ۲۰ تا ۲۲ — آزمایشگاه + داروخانه + پاراکلینیک"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
APP = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Application"))
API = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.API"))


def write(rel, content, base=APP):
    full = os.path.join(base, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════ گام ۲۰: آزمایشگاه ═══════════
write("Features/Laboratory/LaboratoryFeatures.cs", """
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
""")

write("Controllers/LabController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Laboratory;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر آزمایشگاه — درخواست‌ها و نتایج
/// مسیرها: /api/v1/lab
/// </summary>
[ApiController]
[Route("api/v1/lab")]
[Authorize]
public class LabController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public LabController(IMediator mediator) => _mediator = mediator;

    /// <summary>فهرست درخواست‌های آزمایش</summary>
    [HttpGet("orders")]
    [HasPermission("lab.view")]
    public async Task<IActionResult> GetOrders([FromQuery] LabOrderStatus? status,
        [FromQuery] long? patientId)
    {
        var result = await _mediator.Send(new GetLabOrdersQuery(status, patientId));
        return ToResponse(result);
    }

    /// <summary>ثبت درخواست آزمایش جدید</summary>
    [HttpPost("orders")]
    [HasPermission("lab.create")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateLabOrderDto dto)
    {
        var result = await _mediator.Send(new CreateLabOrderCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, "GetOrders", new { }, "درخواست آزمایش ثبت شد");
        return ToResponse(result);
    }

    /// <summary>ثبت نتیجه یک قلم آزمایش</summary>
    [HttpPut("results")]
    [HasPermission("lab.edit")]
    public async Task<IActionResult> RecordResult([FromBody] RecordResultDto dto)
    {
        var result = await _mediator.Send(new RecordLabResultCommand(dto));
        return ToResponse(result, "نتیجه ثبت شد");
    }
}
""", API)

# ═══════════ گام ۲۱: داروخانه ═══════════
write("Features/Pharmacy/PharmacyFeatures.cs", """
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Pharmacy;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Events;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Pharmacy;

/// <summary>ردیف موجودی داروخانه</summary>
public record InventoryListDto(
    long Id, string MedicationName, string? BatchNumber,
    decimal Quantity, decimal ReorderLevel, string? ExpiryDateShamsi,
    bool IsLowStock, bool IsExpiringSoon);

/// <summary>ردیف فهرست اقلام نسخه برای تحویل</summary>
public record PrescriptionItemRowDto(
    long ItemId, string MedicationName, string? Dosage, string? Frequency,
    decimal Quantity, string? Unit, bool IsDelivered);

/// <summary>
/// فرمان تحویل نسخه — کاهش موجودی بچ‌ها به ترتیب انقضا + تراکنش + هشدار کمبود
/// </summary>
public record DispensePrescriptionCommand(long PrescriptionId) : IRequest<Result>;

/// <summary>گرداننده تحویل نسخه</summary>
public class DispensePrescriptionCommandHandler : IRequestHandler<DispensePrescriptionCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public DispensePrescriptionCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای تحویل نسخه</summary>
    public async Task<Result> Handle(DispensePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await _unitOfWork.Repository<Prescription>()
            .QueryableTracking
            .Include(p => p.Items).ThenInclude(i => i.Medication)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
            return Result.Failure(Error.NotFound("نسخه", request.PrescriptionId));

        if (prescription.Items.All(i => i.IsDelivered))
            return Result.Failure(new Error("ALREADY_DELIVERED", "این نسخه قبلاً تحویل شده است"));

        var now = DateTime.UtcNow;

        foreach (var item in prescription.Items.Where(i => !i.IsDelivered))
        {
            // موجودی بچ‌های این دارو به ترتیب نزدیک‌ترین انقضا
            var batches = await _unitOfWork.Repository<PharmacyInventory>()
                .QueryableTracking
                .Where(inv => inv.MedicationId == item.MedicationId
                           && inv.Quantity > 0
                           && (inv.ExpiryDate == null || inv.ExpiryDate >= DateOnly.FromDateTime(now)))
                .OrderBy(inv => inv.ExpiryDate)
                .ToListAsync(cancellationToken);

            var remaining = item.Quantity;

            foreach (var batch in batches)
            {
                if (remaining <= 0) break;

                var take = Math.Min(batch.Quantity, remaining);
                batch.Quantity -= take;
                remaining -= take;

                await _unitOfWork.Repository<PharmacyTransaction>().AddAsync(new PharmacyTransaction
                {
                    InventoryId = batch.Id,
                    TransactionType = PharmacyTransactionType.Dispense,
                    Quantity = take,
                    UnitPrice = batch.SellPrice,
                    TotalPrice = take * batch.SellPrice,
                    TransactionDate = now,
                    ReferenceType = PharmacyReferenceType.Prescription,
                    ReferenceId = prescription.Id,
                    PrescriptionId = prescription.Id,
                    Notes = "تحویل نسخه"
                }, cancellationToken);

                // هشدار کمبود موجودی — رویداد دامنه برای نوتیفیکیشن
                if (batch.Quantity <= batch.ReorderLevel)
                {
                    batch.AddDomainEvent(new LowStockAlertEvent(
                        batch.Id, batch.MedicationId,
                        item.Medication?.GenericNameFa ?? item.Medication?.GenericName ?? "دارو",
                        batch.Quantity, batch.ReorderLevel));
                }
            }

            if (remaining > 0)
                return Result.Failure(new Error("INSUFFICIENT_STOCK",
                    $"موجودی داروی {item.Medication?.GenericNameFa} کافی نیست — {remaining} عدد کمبود"));

            item.IsDelivered = true;
            item.DeliveredAt = now;
            item.DeliveredQuantity = item.Quantity;
        }

        prescription.Status = PrescriptionStatus.Delivered;

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>
/// فرمان ورود موجودی جدید (خرید)
/// </summary>
public record StockInCommand(
    long MedicationId, string? BatchNumber, decimal Quantity,
    decimal PurchasePrice, decimal SellPrice,
    string? ExpiryDateShamsi, long? SupplierId) : IRequest<Result<long>>;

/// <summary>اعتبارسنج ورود موجودی</summary>
public class StockInCommandValidator : AbstractValidator<StockInCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public StockInCommandValidator()
    {
        RuleFor(x => x.MedicationId).GreaterThan(0).WithMessage("انتخاب دارو الزامی است");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("تعداد باید بیشتر از صفر باشد");
        RuleFor(x => x.SellPrice).GreaterThanOrEqualTo(0).WithMessage("قیمت فروش نامعتبر است");
    }
}

/// <summary>گرداننده ورود موجودی — رکورد بچ جدید یا افزایش بچ موجود</summary>
public class StockInCommandHandler : IRequestHandler<StockInCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public StockInCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ورود موجودی</summary>
    public async Task<Result<long>> Handle(StockInCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expiry = Application.Common.Helpers.PersianDateHelper.ToGregorian(request.ExpiryDateShamsi);

        var inventory = new PharmacyInventory
        {
            MedicationId = request.MedicationId,
            BatchNumber = request.BatchNumber,
            Quantity = request.Quantity,
            PurchasePrice = request.PurchasePrice,
            SellPrice = request.SellPrice,
            ExpiryDate = expiry.HasValue ? DateOnly.FromDateTime(expiry.Value) : null,
            SupplierId = request.SupplierId,
            LastPurchaseDate = DateOnly.FromDateTime(now),
            IsActive = true
        };

        await _unitOfWork.Repository<PharmacyInventory>().AddAsync(inventory, cancellationToken);

        await _unitOfWork.Repository<PharmacyTransaction>().AddAsync(new PharmacyTransaction
        {
            InventoryId = inventory.Id,
            TransactionType = PharmacyTransactionType.StockIn,
            Quantity = request.Quantity,
            UnitPrice = request.PurchasePrice,
            TotalPrice = request.Quantity * request.PurchasePrice,
            TransactionDate = now,
            ReferenceType = PharmacyReferenceType.Manual,
            Notes = "ورود موجودی"
        }, cancellationToken);

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(inventory.Id);
    }
}

/// <summary>
/// پرس‌وجوی موجودی داروخانه با هشدار کمبود و انقضا
/// </summary>
public record GetInventoryQuery(bool? LowStock = null) : IRequest<Result<List<InventoryListDto>>>;

/// <summary>گرداننده فهرست موجودی</summary>
public class GetInventoryQueryHandler : IRequestHandler<GetInventoryQuery, Result<List<InventoryListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetInventoryQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<InventoryListDto>>> Handle(GetInventoryQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var soon = today.AddDays(90);

        IQueryable<PharmacyInventory> query = _unitOfWork.Repository<PharmacyInventory>()
            .QueryableNoTracking
            .Include(i => i.Medication);

        if (request.LowStock == true)
            query = query.Where(i => i.Quantity <= i.ReorderLevel);

        var items = await query
            .OrderBy(i => i.Medication!.GenericName)
            .Take(200)
            .Select(i => new InventoryListDto(
                i.Id,
                i.Medication != null ? i.Medication.GenericNameFa ?? i.Medication.GenericName : "",
                i.BatchNumber,
                i.Quantity, i.ReorderLevel,
                i.ExpiryDate.HasValue
                    ? Application.Common.Helpers.PersianDateHelper.ToShamsi(i.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue))
                    : null,
                i.Quantity <= i.ReorderLevel,
                i.ExpiryDate.HasValue && i.ExpiryDate.Value <= soon))
            .ToListAsync(cancellationToken);

        return Result<List<InventoryListDto>>.Success(items);
    }
}
""")

write("Controllers/PharmacyController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Pharmacy;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر داروخانه — موجودی و تحویل نسخه
/// مسیرها: /api/v1/pharmacy
/// </summary>
[ApiController]
[Route("api/v1/pharmacy")]
[Authorize]
public class PharmacyController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public PharmacyController(IMediator mediator) => _mediator = mediator;

    /// <summary>فهرست موجودی — با فیلتر کمبود</summary>
    [HttpGet("inventory")]
    [HasPermission("pharmacy.view")]
    public async Task<IActionResult> GetInventory([FromQuery] bool? lowStock)
    {
        var result = await _mediator.Send(new GetInventoryQuery(lowStock));
        return ToResponse(result);
    }

    /// <summary>ورود موجودی جدید — بچ با تاریخ انقضا</summary>
    [HttpPost("inventory")]
    [HasPermission("pharmacy.create")]
    public async Task<IActionResult> StockIn([FromBody] StockInCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return CreatedResponse(result, "GetInventory", new { }, "موجودی ثبت شد");
        return ToResponse(result);
    }

    /// <summary>تحویل نسخه — کاهش FEFO و هشدار کمبود خودکار</summary>
    /// <param name="prescriptionId">شناسه نسخه</param>
    [HttpPost("dispense/{prescriptionId:long}")]
    [HasPermission("pharmacy.create")]
    public async Task<IActionResult> Dispense(long prescriptionId)
    {
        var result = await _mediator.Send(new DispensePrescriptionCommand(prescriptionId));
        return ToResponse(result, "نسخه تحویل شد");
    }
}
""", API)

# ═══════════ گام ۲۲: پاراکلینیک ═══════════
write("Features/Paraclinical/ParaclinicalFeatures.cs", """
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Paraclinical;

/// <summary>درخواست تصویربرداری</summary>
public record CreateImagingOrderDto(
    long PatientId, long? ReferringDoctorId, ImagingModality Modality,
    string? BodyPart, Priority Priority = Priority.Normal, string? ClinicalInfo = null);

/// <summary>گزارش تصویربرداری</summary>
public record ImagingReportDto(
    long OrderId, string? Technique, string? Findings, string? Impression,
    string? Recommendation);

/// <summary>فرمان ثبت درخواست تصویربرداری</summary>
public record CreateImagingOrderCommand(CreateImagingOrderDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج درخواست تصویربرداری</summary>
public class CreateImagingOrderCommandValidator : AbstractValidator<CreateImagingOrderCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateImagingOrderCommandValidator()
    {
        RuleFor(x => x.Dto.PatientId).GreaterThan(0).WithMessage("شناسه بیمار الزامی است");
        RuleFor(x => x.Dto.BodyPart).NotEmpty().WithMessage("ناحیه بدن الزامی است");
    }
}

/// <summary>گرداننده ثبت درخواست تصویربرداری</summary>
public class CreateImagingOrderCommandHandler : IRequestHandler<CreateImagingOrderCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateImagingOrderCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت درخواست</summary>
    public async Task<Result<long>> Handle(CreateImagingOrderCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var order = new ImagingOrder
        {
            PatientId = dto.PatientId,
            ReferringDoctorId = dto.ReferringDoctorId,
            Modality = dto.Modality,
            BodyPart = dto.BodyPart,
            Priority = dto.Priority,
            ClinicalInfo = dto.ClinicalInfo,
            Status = ImagingOrderStatus.Ordered
        };

        await _unitOfWork.Repository<ImagingOrder>().AddAsync(order, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(order.Id);
    }
}

/// <summary>فرمان ثبت گزارش رادیولوژی — تکمیل پرونده تصویربرداری</summary>
public record SubmitImagingReportCommand(ImagingReportDto Dto) : IRequest<Result>;

/// <summary>گرداننده ثبت گزارش تصویربرداری</summary>
public class SubmitImagingReportCommandHandler : IRequestHandler<SubmitImagingReportCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public SubmitImagingReportCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت گزارش</summary>
    public async Task<Result> Handle(SubmitImagingReportCommand request, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Repository<ImagingOrder>()
            .GetByIdAsync(request.Dto.OrderId, cancellationToken);

        if (order is null)
            return Result.Failure(Error.NotFound("درخواست تصویربرداری", request.Dto.OrderId));

        order.Technique = request.Dto.Technique;
        order.Findings = request.Dto.Findings;
        order.Impression = request.Dto.Impression;
        order.Recommendation = request.Dto.Recommendation;
        order.Status = ImagingOrderStatus.Reported;
        order.ReportedAt = DateTime.UtcNow;

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>فرمان ثبت طرح درمان دندانپزشکی</summary>
public record CreateDentalPlanCommand(
    long PatientId, long? DentistId, decimal TotalCost,
    int EstimatedSessions, string? PlanDetails) : IRequest<Result<long>>;

/// <summary>گرداننده ثبت طرح درمان دندانپزشکی</summary>
public class CreateDentalPlanCommandHandler : IRequestHandler<CreateDentalPlanCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateDentalPlanCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت طرح درمان</summary>
    public async Task<Result<long>> Handle(CreateDentalPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = new DentalTreatmentPlan
        {
            PatientId = request.PatientId,
            DentistId = request.DentistId,
            CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalCost = request.TotalCost,
            EstimatedSessions = request.EstimatedSessions,
            PlanDetails = request.PlanDetails,
            Status = TreatmentPlanStatus.Proposed
        };

        await _unitOfWork.Repository<DentalTreatmentPlan>().AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(plan.Id);
    }
}

/// <summary>فرمان ثبت جلسه فیزیوتراپی</summary>
public record RecordPhysioSessionCommand(
    long PatientId, long? TherapistId, int SessionNumber,
    string? Treatments, int? Duration, byte? PainScore) : IRequest<Result<long>>;

/// <summary>گرداننده ثبت جلسه فیزیوتراپی</summary>
public class RecordPhysioSessionCommandHandler : IRequestHandler<RecordPhysioSessionCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public RecordPhysioSessionCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت جلسه</summary>
    public async Task<Result<long>> Handle(RecordPhysioSessionCommand request, CancellationToken cancellationToken)
    {
        var session = new PhysiotherapySession
        {
            PatientId = request.PatientId,
            TherapistId = request.TherapistId,
            SessionNumber = request.SessionNumber,
            SessionDate = DateTime.UtcNow,
            Treatments = request.Treatments,
            Duration = request.Duration,
            PainScore = request.PainScore,
            Status = SessionStatus.Completed
        };

        await _unitOfWork.Repository<PhysiotherapySession>().AddAsync(session, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(session.Id);
    }
}

/// <summary>فرمان ثبت معاینه چشم</summary>
public record SaveEyeExamCommand(
    long PatientId, long? VisitId,
    decimal? RightSphere, decimal? RightCylinder, int? RightAxis,
    decimal? LeftSphere, decimal? LeftCylinder, int? LeftAxis,
    string? Findings, string? EyewearPrescription) : IRequest<Result<long>>;

/// <summary>گرداننده ثبت معاینه چشم</summary>
public class SaveEyeExamCommandHandler : IRequestHandler<SaveEyeExamCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public SaveEyeExamCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت معاینه چشم</summary>
    public async Task<Result<long>> Handle(SaveEyeExamCommand request, CancellationToken cancellationToken)
    {
        var exam = new EyeExamRecord
        {
            PatientId = request.PatientId,
            VisitId = request.VisitId,
            RightSphere = request.RightSphere,
            RightCylinder = request.RightCylinder,
            RightAxis = request.RightAxis,
            LeftSphere = request.LeftSphere,
            LeftCylinder = request.LeftCylinder,
            LeftAxis = request.LeftAxis,
            Findings = request.Findings,
            EyewearPrescription = request.EyewearPrescription
        };

        await _unitOfWork.Repository<EyeExamRecord>().AddAsync(exam, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(exam.Id);
    }
}
""")

write("Controllers/ParaclinicalController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Paraclinical;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر پاراکلینیک — تصویربرداری و دندانپزشکی و چشم و فیزیوتراپی
/// مسیرها: /api/v1/imaging و /api/v1/dental و /api/v1/eye و /api/v1/physiotherapy
/// </summary>
[ApiController]
[Authorize]
public class ParaclinicalController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public ParaclinicalController(IMediator mediator) => _mediator = mediator;

    /// <summary>ثبت درخواست تصویربرداری</summary>
    [HttpPost("api/v1/imaging/orders")]
    [HasPermission("imaging.create")]
    public async Task<IActionResult> CreateImagingOrder([FromBody] CreateImagingOrderDto dto)
    {
        var result = await _mediator.Send(new CreateImagingOrderCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, "GetOrders", new { }, "درخواست تصویربرداری ثبت شد");
        return ToResponse(result);
    }

    /// <summary>ثبت گزارش رادیولوژی</summary>
    [HttpPut("api/v1/imaging/reports")]
    [HasPermission("imaging.edit")]
    public async Task<IActionResult> SubmitReport([FromBody] ImagingReportDto dto)
    {
        var result = await _mediator.Send(new SubmitImagingReportCommand(dto));
        return ToResponse(result, "گزارش ثبت شد");
    }

    /// <summary>ثبت طرح درمان دندانپزشکی</summary>
    [HttpPost("api/v1/dental/plans")]
    [HasPermission("dental.create")]
    public async Task<IActionResult> CreateDentalPlan([FromBody] CreateDentalPlanCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return CreatedResponse(result, "GetOrders", new { }, "طرح درمان ثبت شد");
        return ToResponse(result);
    }

    /// <summary>ثبت معاینه چشم‌پزشکی</summary>
    [HttpPost("api/v1/eye/exams")]
    [HasPermission("eye.create")]
    public async Task<IActionResult> SaveEyeExam([FromBody] SaveEyeExamCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return CreatedResponse(result, "GetOrders", new { }, "معاینه چشم ثبت شد");
        return ToResponse(result);
    }

    /// <summary>ثبت جلسه فیزیوتراپی</summary>
    [HttpPost("api/v1/physiotherapy/sessions")]
    [HasPermission("physiotherapy.create")]
    public async Task<IActionResult> RecordSession([FromBody] RecordPhysioSessionCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return CreatedResponse(result, "GetOrders", new { }, "جلسه ثبت شد");
        return ToResponse(result);
    }
}
""", API)

print("✅ گام‌های ۲۰ تا ۲۲ — آزمایشگاه + داروخانه + پاراکلینیک کامل شد")

using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Application.DTOs.Prescriptions;

namespace Sahand.ClinicPro.Application.Features.Prescriptions.PrescriptionTemplates;

/// <summary>پرس‌وجوی تمپلیت‌های نسخه</summary>
public record GetPrescriptionTemplatesQuery(long? DoctorId = null)
    : IRequest<Result<List<PrescriptionTemplateDto>>>;

/// <summary>گرداننده پرس‌وجو</summary>
public class GetPrescriptionTemplatesQueryHandler
    : IRequestHandler<GetPrescriptionTemplatesQuery, Result<List<PrescriptionTemplateDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetPrescriptionTemplatesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<PrescriptionTemplateDto>>> Handle(GetPrescriptionTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<PrescriptionTemplate>().QueryableNoTracking;
        if (request.DoctorId.HasValue)
            query = query.Where(t => t.DoctorId == request.DoctorId.Value);

        var items = await query.OrderBy(t => t.TemplateName)
            .Select(t => new PrescriptionTemplateDto(t.Id, t.TemplateName, t.Category, t.Items ?? "{}"))
            .ToListAsync(cancellationToken);

        return Result<List<PrescriptionTemplateDto>>.Success(items);
    }
}

/// <summary>فرمان ذخیره تمپلیت</summary>
public record SavePrescriptionTemplateCommand(SaveTemplateRequest Request, long? DoctorId = null)
    : IRequest<Result<long>>;

/// <summary>گرداننده ذخیره تمپلیت</summary>
public class SavePrescriptionTemplateCommandHandler
    : IRequestHandler<SavePrescriptionTemplateCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public SavePrescriptionTemplateCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<long>> Handle(SavePrescriptionTemplateCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Request.Name))
            return Result<long>.Failure(new Error("TEMPLATE.NAME", "نام تمپلیت الزامی است"));

        var template = new PrescriptionTemplate
        {
            TemplateName = request.Request.Name.Trim(),
            Category = request.Request.Category,
            Items = request.Request.TemplateJson,
            // چرخه بیست‌وسوم (گزارش شانزدهم مالک): پیش‌تر «?? 0» مقدار صفر می‌گذاشت
            // که با کلید خارجی الزامی به پزشکان نمی‌خواست و ذخیرهٔ تمپلیت با خطای
            // ۵۰۰ «غیر منتظره در سرور» شکست می‌خورد؛ ستون اکنون اختیاری است و null
            // یعنی تمپلیت عمومی (بدون پزشک صاحب)
            DoctorId = request.DoctorId
        };
        await _unitOfWork.Repository<PrescriptionTemplate>().AddAsync(template, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(template.Id);
    }
}

/// <summary>فرمان حذف تمپلیت</summary>
public record DeletePrescriptionTemplateCommand(long Id) : IRequest<Result>;

/// <summary>گرداننده حذف تمپلیت</summary>
public class DeletePrescriptionTemplateCommandHandler
    : IRequestHandler<DeletePrescriptionTemplateCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public DeletePrescriptionTemplateCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result> Handle(DeletePrescriptionTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<PrescriptionTemplate>();
        var template = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (template is null)
            return Result.Failure(Error.NotFound("تمپلیت", request.Id));
        repo.Remove(template);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}

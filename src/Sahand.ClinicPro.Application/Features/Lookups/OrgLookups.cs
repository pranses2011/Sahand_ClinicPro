using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;
using MediatR;

namespace Sahand.ClinicPro.Application.Features.Lookups;

// ═══════════════════════════════════════════════════════════════════
// سمت‌ها و دپارتمان‌های پرسنل — چرخه بیست‌وچهارم (گزارش هفدهم مالک):
// «در تعریف پرسنل سمت و چند فیلد دیگر وجود ندارد» و «برای پرسنل، سمت و
// دپارتمان را نشان نمی‌دهد» — فهرست‌های مرجع سمت/دپارتمان پیش‌تر در هیچ
// endpointی عرضه نمی‌شدند و فرم پرسنل مقادیر تهی هاردکد می‌فرستاد؛ بنابراین
// لیست پرسنل همیشه «—» نشان می‌داد. این فایل:
//   • دو پرس‌وجوی خواندن (فعال‌ها برای ComboBox فرم + کامل برای مدیریت)
//   • دو فرمان ساخت سریع (Admin/Manager) را فراهم می‌کند تا بتوان از
//     داخل خود فرم پرسنل، سمت/دپارتمان تایپ‌شدهٔ جدید ساخت.
// ═══════════════════════════════════════════════════════════════════

/// <summary>آیتم مرجع سمت/دپارتمان</summary>
public class LookupItemDto
{
    /// <summary>شناسه</summary>
    public long Id { get; set; }

    /// <summary>نام نمایشی (عنوان سمت یا نام دپارتمان)</summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>پرس‌وجوی دپارتمان‌های فعال — ComboBox فرم پرسنل</summary>
public record GetDepartmentsQuery : IRequest<Result<List<LookupItemDto>>>;

/// <summary>گرداننده دپارتمان‌ها</summary>
public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, Result<List<LookupItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetDepartmentsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — فقط دپارتمان‌های فعال، مرتب بر اساس نام</summary>
    public async Task<Result<List<LookupItemDto>>> Handle(GetDepartmentsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<Department>().QueryableNoTracking
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.Name)
            .Select(d => new LookupItemDto { Id = d.Id, Name = d.Name })
            .ToListAsync(cancellationToken);

        return Result<List<LookupItemDto>>.Success(items);
    }
}

/// <summary>پرس‌وجوی سمت‌های فعال — ComboBox فرم پرسنل</summary>
public record GetPositionsQuery : IRequest<Result<List<LookupItemDto>>>;

/// <summary>گرداننده سمت‌ها</summary>
public class GetPositionsQueryHandler : IRequestHandler<GetPositionsQuery, Result<List<LookupItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetPositionsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — فقط سمت‌های فعال، مرتب بر اساس عنوان</summary>
    public async Task<Result<List<LookupItemDto>>> Handle(GetPositionsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<Position>().QueryableNoTracking
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Title)
            .Select(p => new LookupItemDto { Id = p.Id, Name = p.Title })
            .ToListAsync(cancellationToken);

        return Result<List<LookupItemDto>>.Success(items);
    }
}

/// <summary>
/// فرمان ساخت دپارتمان جدید — نام یکتا؛ اگر دپارتمانی با همین نام (فعال یا
/// حذف‌نرم‌شده) موجود باشد شناسهٔ همان بازگردانده می‌شود (رفع تکراری‌نویسی).
/// </summary>
public record CreateDepartmentIfMissingCommand(string Name) : IRequest<Result<long>>;

/// <summary>گرداننده ساخت دپارتمان</summary>
public class CreateDepartmentIfMissingCommandHandler
    : IRequestHandler<CreateDepartmentIfMissingCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public CreateDepartmentIfMissingCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — موجود؟ بازگشت شناسه؛ وگرنه ساخت رکورد نو</summary>
    public async Task<Result<long>> Handle(CreateDepartmentIfMissingCommand request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return Result<long>.Failure(new Domain.Common.Error("EMPTY", "نام دپارتمان خالی است"));

        var existing = await _unitOfWork.Repository<Department>().QueryableNoTracking
            .FirstOrDefaultAsync(d => d.Name == name, cancellationToken);
        if (existing is not null)
            return Result<long>.Success(existing.Id);

        var department = new Department { Name = name, CreatedAt = DateTime.UtcNow };
        await _unitOfWork.Repository<Department>().AddAsync(department, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(department.Id);
    }
}

/// <summary>
/// فرمان ساخت سمت جدید — عنوان یکتا؛ اگر سمتی با همین عنوان موجود باشد
/// شناسهٔ همان بازگردانده می‌شود.
/// </summary>
public record CreatePositionIfMissingCommand(string Title) : IRequest<Result<long>>;

/// <summary>گرداننده ساخت سمت</summary>
public class CreatePositionIfMissingCommandHandler
    : IRequestHandler<CreatePositionIfMissingCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public CreatePositionIfMissingCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — موجود؟ بازگشت شناسه؛ وگرنه ساخت رکورد نو</summary>
    public async Task<Result<long>> Handle(CreatePositionIfMissingCommand request,
        CancellationToken cancellationToken)
    {
        var title = request.Title.Trim();
        if (string.IsNullOrEmpty(title))
            return Result<long>.Failure(new Domain.Common.Error("EMPTY", "عنوان سمت خالی است"));

        var existing = await _unitOfWork.Repository<Position>().QueryableNoTracking
            .FirstOrDefaultAsync(p => p.Title == title, cancellationToken);
        if (existing is not null)
            return Result<long>.Success(existing.Id);

        var position = new Position { Title = title, CreatedAt = DateTime.UtcNow };
        await _unitOfWork.Repository<Position>().AddAsync(position, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(position.Id);
    }
}

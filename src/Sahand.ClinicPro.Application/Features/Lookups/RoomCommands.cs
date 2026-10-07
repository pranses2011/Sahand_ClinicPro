using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Lookups;

/// <summary>آیتم اتاق برای لیست مدیریت</summary>
public class RoomListItemDto
{
    /// <summary>شناسه</summary>
    public long Id { get; set; }

    /// <summary>شماره اتاق</summary>
    public string? RoomNumber { get; set; }

    /// <summary>نام اتاق</summary>
    public string? RoomName { get; set; }

    /// <summary>طبقه</summary>
    public int? Floor { get; set; }

    /// <summary>فعال/غیرفعال</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// پرس‌وجوی فهرست کامل اتاق‌ها (شامل غیرفعال) برای پنجرهٔ مدیریت — چرخه بیست‌ویکم
/// </summary>
public record GetRoomsForManageQuery : IRequest<Result<List<RoomListItemDto>>>;

/// <summary>گرداننده فهرست کامل اتاق‌ها</summary>
public class GetRoomsForManageQueryHandler : IRequestHandler<GetRoomsForManageQuery, Result<List<RoomListItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetRoomsForManageQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<RoomListItemDto>>> Handle(GetRoomsForManageQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<ExaminationRoom>().QueryableNoTracking
            .OrderBy(r => r.Id)
            .Select(r => new RoomListItemDto
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                RoomName = r.RoomName,
                Floor = r.Floor,
                IsActive = r.IsActive
            })
            .ToListAsync(cancellationToken);

        return Result<List<RoomListItemDto>>.Success(items);
    }
}

/// <summary>
/// فرمان ساخت اتاق معاینه — چرخه بیست‌ویکم (گزارش پانزدهم مالک):
/// «فیلد اتاق برای انتخاب هست ولی جایی برای تعریف اتاق و اسم اتاق نیست»
/// </summary>
public record CreateRoomCommand(string RoomNumber, string? RoomName, int? Floor)
    : IRequest<Result<long>>;

/// <summary>گرداننده ساخت اتاق</summary>
public class CreateRoomCommandHandler : IRequestHandler<CreateRoomCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public CreateRoomCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — شماره اتاق یکتاست</summary>
    public async Task<Result<long>> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        var number = request.RoomNumber?.Trim();
        if (string.IsNullOrWhiteSpace(number))
            return Result<long>.Failure(new Error("ROOM.NUMBER_REQUIRED", "شماره اتاق الزامی است"));

        var repo = _unitOfWork.Repository<ExaminationRoom>();
        var duplicate = await repo.QueryableNoTracking
            .AnyAsync(r => r.RoomNumber == number, cancellationToken);
        if (duplicate)
            return Result<long>.Failure(new Error("ROOM.DUPLICATE", "اتاقی با این شماره قبلاً ثبت شده است"));

        var room = new ExaminationRoom
        {
            RoomNumber = number,
            RoomName = string.IsNullOrWhiteSpace(request.RoomName) ? $"اتاق {number}" : request.RoomName.Trim(),
            Floor = request.Floor,
            Capacity = 1,
            IsActive = true
        };
        await repo.AddAsync(room, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(room.Id);
    }
}

/// <summary>فرمان ویرایش اتاق (نام/شماره/طبقه/وضعیت)</summary>
public record UpdateRoomCommand(long Id, string RoomNumber, string? RoomName, int? Floor, bool IsActive)
    : IRequest<Result>;

/// <summary>گرداننده ویرایش اتاق</summary>
public class UpdateRoomCommandHandler : IRequestHandler<UpdateRoomCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public UpdateRoomCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result> Handle(UpdateRoomCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ExaminationRoom>();
        var room = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (room is null)
            return Result.Failure(Error.NotFound("اتاق", request.Id));

        room.RoomNumber = request.RoomNumber.Trim();
        room.RoomName = string.IsNullOrWhiteSpace(request.RoomName)
            ? $"اتاق {request.RoomNumber.Trim()}"
            : request.RoomName.Trim();
        room.Floor = request.Floor;
        room.IsActive = request.IsActive;

        repo.Update(room);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>فرمان غیرفعال‌سازی (حذف نرم) اتاق — به‌جای حذف فیزیکی، ردیف‌های برنامه کاری سالم می‌مانند</summary>
public record DeactivateRoomCommand(long Id) : IRequest<Result>;

/// <summary>گرداننده غیرفعال‌سازی اتاق</summary>
public class DeactivateRoomCommandHandler : IRequestHandler<DeactivateRoomCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public DeactivateRoomCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result> Handle(DeactivateRoomCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<ExaminationRoom>();
        var room = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (room is null)
            return Result.Failure(Error.NotFound("اتاق", request.Id));

        room.IsActive = false;
        repo.Update(room);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}

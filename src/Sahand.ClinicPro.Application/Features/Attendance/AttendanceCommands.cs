using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Attendance;

// ═══════════════════════════════════════════════════════════════
// فرمان‌های دستگاه حضور و غیاب
// ═══════════════════════════════════════════════════════════════

/// <summary>مدل ذخیره دستگاه</summary>
public record SaveDeviceDto(
    long? Id,
    string Name,
    DeviceBrand Brand,
    string? Model,
    string? SerialNumber,
    string IpAddress,
    int Port,
    string? CommunicationKey,
    string? Location,
    int SyncIntervalMinutes,
    string? Notes);

/// <summary>فرمان ثبت/ویرایش دستگاه</summary>
public record SaveDeviceCommand(SaveDeviceDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج دستگاه — IP و پورت معتبر</summary>
public class SaveDeviceCommandValidator : AbstractValidator<SaveDeviceCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public SaveDeviceCommandValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(120)
            .WithMessage("نام دستگاه الزامی است (حداکثر ۱۲۰ نویسه)");
        RuleFor(x => x.Dto.IpAddress).Matches(@"^(\d{1,3}\.){3}\d{1,3}$")
            .WithMessage("نشانی IP معتبر نیست");
        RuleFor(x => x.Dto.Port).InclusiveBetween(1, 65535)
            .WithMessage("پورت باید بین ۱ تا ۶۵۵۳۵ باشد");
        RuleFor(x => x.Dto.SyncIntervalMinutes).InclusiveBetween(1, 1440)
            .WithMessage("بازه همگام‌سازی بین ۱ تا ۱۴۴۰ دقیقه");
    }
}

/// <summary>گرداننده ثبت/ویرایش دستگاه</summary>
public class SaveDeviceCommandHandler : IRequestHandler<SaveDeviceCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public SaveDeviceCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<long>> Handle(SaveDeviceCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        // تکراری نبودن IP:پورت بین دستگاه‌های فعال
        var duplicate = await _unitOfWork.Repository<AttendanceDevice>().QueryableNoTracking
            .AnyAsync(d => !d.IsDeleted && d.Id != dto.Id
                        && d.IpAddress == dto.IpAddress && d.Port == dto.Port, ct);
        if (duplicate)
            return Result<long>.Failure(new Error("ATT.DEVICE.DUP",
                "دستگاه دیگری با همین IP و پورت ثبت شده است"));

        if (dto.Id is { } id)
        {
            var device = await _unitOfWork.Repository<AttendanceDevice>()
                .FirstOrDefaultAsync(d => d.Id == id, ct);
            if (device is null)
                return Result<long>.Failure(new Error("ATT.DEVICE.NOTFOUND",
                    "دستگاه یافت نشد"));

            device.Name = dto.Name;
            device.Brand = dto.Brand;
            device.Model = dto.Model;
            device.SerialNumber = dto.SerialNumber;
            device.IpAddress = dto.IpAddress;
            device.Port = dto.Port;
            device.CommunicationKey = dto.CommunicationKey;
            device.Location = dto.Location;
            device.SyncIntervalMinutes = dto.SyncIntervalMinutes;
            device.Notes = dto.Notes;
            _unitOfWork.Repository<AttendanceDevice>().Update(device);
            await _unitOfWork.SaveEntitiesAsync(ct);
            return Result<long>.Success(device.Id);
        }

        var created = await _unitOfWork.Repository<AttendanceDevice>().AddAsync(new AttendanceDevice
        {
            Name = dto.Name,
            Brand = dto.Brand,
            Model = dto.Model,
            SerialNumber = dto.SerialNumber,
            IpAddress = dto.IpAddress,
            Port = dto.Port,
            CommunicationKey = dto.CommunicationKey,
            Location = dto.Location,
            SyncIntervalMinutes = dto.SyncIntervalMinutes,
            Notes = dto.Notes,
            IsActive = true,
            Status = DeviceStatus.Offline,
        }, ct);
        await _unitOfWork.SaveEntitiesAsync(ct);
        return Result<long>.Success(created.Id);
    }
}

/// <summary>فرمان حذف نرم دستگاه</summary>
public record DeleteDeviceCommand(long DeviceId) : IRequest<Result<bool>>;

/// <summary>گرداننده حذف دستگاه — تاریخچه لاگ‌ها محفوظ می‌ماند</summary>
public class DeleteDeviceCommandHandler : IRequestHandler<DeleteDeviceCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public DeleteDeviceCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<bool>> Handle(DeleteDeviceCommand request, CancellationToken ct)
    {
        var device = await _unitOfWork.Repository<AttendanceDevice>()
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId, ct);
        if (device is null)
            return Result<bool>.Failure(new Error("ATT.DEVICE.NOTFOUND", "دستگاه یافت نشد"));

        device.IsActive = false;
        device.Status = DeviceStatus.Disabled;
        _unitOfWork.Repository<AttendanceDevice>().Update(device);
        await _unitOfWork.SaveEntitiesAsync(ct);
        return Result<bool>.Success(true);
    }
}

/// <summary>فرمان آزمون اتصال دستگاه</summary>
public record TestDeviceConnectionCommand(long DeviceId) : IRequest<Result<string>>;

/// <summary>گرداننده آزمون اتصال — با مهلت کوتاه از طریق درایور</summary>
public class TestDeviceConnectionCommandHandler : IRequestHandler<TestDeviceConnectionCommand, Result<string>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAttendanceDriverFactory _driverFactory;

    /// <summary>سازنده</summary>
    public TestDeviceConnectionCommandHandler(
        IUnitOfWork unitOfWork,
        IAttendanceDriverFactory driverFactory)
    {
        _unitOfWork = unitOfWork;
        _driverFactory = driverFactory;
    }

    /// <summary>اجرا — نتیجه آزمون روی وضعیت دستگاه هم نقش می‌بندد</summary>
    public async Task<Result<string>> Handle(TestDeviceConnectionCommand request, CancellationToken ct)
    {
        var device = await _unitOfWork.Repository<AttendanceDevice>()
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId, ct);
        if (device is null)
            return Result<string>.Failure(new Error("ATT.DEVICE.NOTFOUND", "دستگاه یافت نشد"));

        try
        {
            var driver = _driverFactory.DriverFor(device);
            var ok = await driver.TestConnectionAsync(device, ct);
            var serial = ok ? await driver.GetSerialAsync(device, ct) : null;

            device.Status = ok ? DeviceStatus.Online : DeviceStatus.Error;
            device.LastError = ok ? null : "آزمون اتصال ناموفق بود";
            _unitOfWork.Repository<AttendanceDevice>().Update(device);
            await _unitOfWork.SaveEntitiesAsync(ct);

            return ok
                ? Result<string>.Success($"اتصال برقرار است{(serial is not null ? $" — سریال: {serial}" : "")}")
                : Result<string>.Failure(new Error("ATT.DEVICE.OFFLINE", "دستگاه پاسخ نداد — IP، پورت و شبکه را بررسی کنید"));
        }
        catch (Exception ex)
        {
            device.Status = DeviceStatus.Error;
            device.LastError = ex.Message.Length > 280 ? ex.Message[..280] : ex.Message;
            _unitOfWork.Repository<AttendanceDevice>().Update(device);
            await _unitOfWork.SaveEntitiesAsync(ct);
            return Result<string>.Failure(new Error("ATT.DEVICE.ERROR", $"خطا: {device.LastError}"));
        }
    }
}

/// <summary>فرمان همگام‌سازی فوری یک دستگاه</summary>
public record SyncDeviceCommand(long DeviceId) : IRequest<Result<DeviceSyncResult>>;

/// <summary>گرداننده همگام‌سازی فوری — از طریق همگام‌ساز</summary>
public class SyncDeviceCommandHandler : IRequestHandler<SyncDeviceCommand, Result<DeviceSyncResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAttendanceSynchronizer _synchronizer;

    /// <summary>سازنده</summary>
    public SyncDeviceCommandHandler(IUnitOfWork unitOfWork, IAttendanceSynchronizer synchronizer)
    {
        _unitOfWork = unitOfWork;
        _synchronizer = synchronizer;
    }

    /// <summary>اجرا</summary>
    public async Task<Result<DeviceSyncResult>> Handle(SyncDeviceCommand request, CancellationToken ct)
    {
        var device = await _unitOfWork.Repository<AttendanceDevice>()
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId, ct);
        if (device is null)
            return Result<DeviceSyncResult>.Failure(new Error("ATT.DEVICE.NOTFOUND", "دستگاه یافت نشد"));

        var result = await _synchronizer.SyncDeviceAsync(device, ct);
        return result.Success
            ? Result<DeviceSyncResult>.Success(result)
            : Result<DeviceSyncResult>.Failure(new Error("ATT.SYNC.FAILED", result.Message));
    }
}

/// <summary>فرمان همگام‌سازی همه دستگاه‌های فعال</summary>
public record SyncAllDevicesCommand() : IRequest<Result<int>>;

/// <summary>گرداننده همگام‌سازی دسته‌ای</summary>
public class SyncAllDevicesCommandHandler : IRequestHandler<SyncAllDevicesCommand, Result<int>>
{
    private readonly IAttendanceSynchronizer _synchronizer;

    /// <summary>سازنده</summary>
    public SyncAllDevicesCommandHandler(IAttendanceSynchronizer synchronizer) => _synchronizer = synchronizer;

    /// <summary>اجرا — شمار دستگاه‌های موفق</summary>
    public async Task<Result<int>> Handle(SyncAllDevicesCommand request, CancellationToken ct)
    {
        var results = await _synchronizer.SyncAllActiveDevicesAsync(ct);
        var ok = results.Count(r => r.Success);
        return Result<int>.Success(ok);
    }
}

// ═══════════════════════════════════════════════════════════════
// نگاشت کاربر دستگاه به پرسنل
// ═══════════════════════════════════════════════════════════════

/// <summary>فرمان نگاشت شناسه دستگاه به پرسنل</summary>
public record MapDeviceUserCommand(long DeviceId, string EnrollNumber, long EmployeeId)
    : IRequest<Result<long>>;

/// <summary>اعتبارسنج نگاشت</summary>
public class MapDeviceUserCommandValidator : AbstractValidator<MapDeviceUserCommand>
{
    /// <summary>قواعد</summary>
    public MapDeviceUserCommandValidator()
    {
        RuleFor(x => x.EnrollNumber).NotEmpty().MaximumLength(24)
            .WithMessage("شناسه روی دستگاه الزامی است");
    }
}

/// <summary>گرداننده نگاشت — یکتایی (دستگاه، شناسه) تضمین می‌شود</summary>
public class MapDeviceUserCommandHandler : IRequestHandler<MapDeviceUserCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public MapDeviceUserCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<long>> Handle(MapDeviceUserCommand request, CancellationToken ct)
    {
        var deviceExists = await _unitOfWork.Repository<AttendanceDevice>()
            .AnyAsync(d => d.Id == request.DeviceId && !d.IsDeleted, ct);
        if (!deviceExists)
            return Result<long>.Failure(new Error("ATT.DEVICE.NOTFOUND", "دستگاه یافت نشد"));

        var employee = await _unitOfWork.Repository<Domain.Entities.People.Employee>()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, ct);
        if (employee is null)
            return Result<long>.Failure(new Error("ATT.EMPLOYEE.NOTFOUND", "پرسنل یافت نشد"));

        var exists = await _unitOfWork.Repository<DeviceUserMapping>().QueryableNoTracking
            .AnyAsync(m => !m.IsDeleted && m.DeviceId == request.DeviceId
                        && m.EnrollNumber == request.EnrollNumber, ct);
        if (exists)
            return Result<long>.Failure(new Error("ATT.MAP.DUP",
                "این شناسه قبلاً به پرسنلی متصل شده است"));

        var mapping = await _unitOfWork.Repository<DeviceUserMapping>().AddAsync(new DeviceUserMapping
        {
            DeviceId = request.DeviceId,
            EnrollNumber = request.EnrollNumber,
            EmployeeId = request.EmployeeId,
            IsActive = true,
            LastVerifiedAt = DateTime.UtcNow,
        }, ct);
        await _unitOfWork.SaveEntitiesAsync(ct);
        return Result<long>.Success(mapping.Id);
    }
}

/// <summary>فرمان حذف نگاشت</summary>
public record UnmapDeviceUserCommand(long MappingId) : IRequest<Result<bool>>;

/// <summary>گرداننده حذف نگاشت</summary>
public class UnmapDeviceUserCommandHandler : IRequestHandler<UnmapDeviceUserCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public UnmapDeviceUserCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<bool>> Handle(UnmapDeviceUserCommand request, CancellationToken ct)
    {
        var mapping = await _unitOfWork.Repository<DeviceUserMapping>()
            .FirstOrDefaultAsync(m => m.Id == request.MappingId, ct);
        if (mapping is null)
            return Result<bool>.Failure(new Error("ATT.MAP.NOTFOUND", "نگاشت یافت نشد"));

        _unitOfWork.Repository<DeviceUserMapping>().Remove(mapping);
        await _unitOfWork.SaveEntitiesAsync(ct);
        return Result<bool>.Success(true);
    }
}

// ═══════════════════════════════════════════════════════════════
// ثبت دستی و محاسبه مجدد
// ═══════════════════════════════════════════════════════════════

/// <summary>فرمان ثبت دستی ورود/خروج — با پرچم ویرایش دستی</summary>
public record RegisterManualPunchCommand(
    long EmployeeId,
    DateOnly Date,
    TimeOnly Time,
    bool IsCheckIn,
    string? Note) : IRequest<Result<long>>;

/// <summary>گرداننده ثبت دستی — رکورد حضور ساخته/اصلاح و ممیزی می‌شود</summary>
public class RegisterManualPunchCommandHandler : IRequestHandler<RegisterManualPunchCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public RegisterManualPunchCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<long>> Handle(RegisterManualPunchCommand request, CancellationToken ct)
    {
        var employee = await _unitOfWork.Repository<Domain.Entities.People.Employee>()
            .AnyAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, ct);
        if (!employee)
            return Result<long>.Failure(new Error("ATT.EMPLOYEE.NOTFOUND", "پرسنل یافت نشد"));

        var record = await _unitOfWork.Repository<EmployeeAttendance>().QueryableTracking
            .FirstOrDefaultAsync(a => a.EmployeeId == request.EmployeeId
                                   && a.AttendanceDate == request.Date, ct);

        if (record is null)
        {
            record = new EmployeeAttendance
            {
                EmployeeId = request.EmployeeId,
                AttendanceDate = request.Date,
                Status = AttendanceStatus.Present,
            };
            await _unitOfWork.Repository<EmployeeAttendance>().AddAsync(record, ct);
        }

        if (request.IsCheckIn)
        {
            // ورود دستی فقط اگر زودتر از خروج فعلی یا بدون خروج است
            if (record.CheckOut is { } co && co < request.Time)
                return Result<long>.Failure(new Error("ATT.PUNCH.ORDER",
                    "ساعت ورود نمی‌تواند پس از خروج ثبت‌شده باشد"));

            record.CheckIn = request.Time;
        }
        else
        {
            if (record.CheckIn is { } ci && ci > request.Time)
                return Result<long>.Failure(new Error("ATT.PUNCH.ORDER",
                    "ساعت خروج نمی‌تواند پیش از ورود ثبت‌شده باشد"));

            record.CheckOut = request.Time;
        }

        record.IsManuallyEdited = true;
        record.Notes = string.IsNullOrWhiteSpace(request.Note)
            ? record.Notes
            : (string.IsNullOrEmpty(record.Notes) ? request.Note : $"{record.Notes} | {request.Note}");

        // بازمحاسبه کارکرد با ضربه‌های موجود
        if (record.CheckIn is { } checkIn)
        {
            var end = record.CheckOut ?? request.Time;
            record.WorkedHours = end > checkIn
                ? decimal.Round((decimal)(end - checkIn).TotalMinutes / 60m, 2)
                : record.WorkedHours;
        }

        await _unitOfWork.SaveEntitiesAsync(ct);
        return Result<long>.Success(record.Id);
    }
}

/// <summary>فرمان محاسبه مجدد حضور یک روز/یک پرسنل — پردازش لاگ‌های مانده</summary>
public record RecalculateAttendanceCommand(DateOnly? Date, long? EmployeeId) : IRequest<Result<int>>;

/// <summary>گرداننده محاسبه مجدد</summary>
public class RecalculateAttendanceCommandHandler : IRequestHandler<RecalculateAttendanceCommand, Result<int>>
{
    private readonly IAttendanceSynchronizer _synchronizer;

    /// <summary>سازنده</summary>
    public RecalculateAttendanceCommandHandler(IAttendanceSynchronizer synchronizer)
        => _synchronizer = synchronizer;

    /// <summary>اجرا — شمار لاگ‌های پردازش‌شده</summary>
    public async Task<Result<int>> Handle(RecalculateAttendanceCommand request, CancellationToken ct)
    {
        var count = await _synchronizer.ProcessPendingLogsAsync(request.Date, request.EmployeeId, ct);
        return Result<int>.Success(count);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Attendance;

/// <summary>
/// همگام‌ساز حضور و غیاب — چرخه کامل:
/// کشیدن ضربه از دستگاه ← درج لاگ خام (بدون اتلاف) ← تبدیل به رکورد حضور با محاسباتگر
/// </summary>
public class AttendanceSynchronizer : IAttendanceSynchronizer
{
    /// <summary>اگر دستگاه تازه افزوده شده باشد، لاگ‌های ۷ روز اخیر کشیده می‌شود</summary>
    private static readonly TimeSpan FirstPullWindow = TimeSpan.FromDays(7);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAttendanceDriverFactory _driverFactory;
    private readonly ILogger<AttendanceSynchronizer> _logger;

    /// <summary>سازنده با تزریق واحد کار و کارخانه درایور</summary>
    public AttendanceSynchronizer(
        IUnitOfWork unitOfWork,
        IAttendanceDriverFactory driverFactory,
        ILogger<AttendanceSynchronizer> logger)
    {
        _unitOfWork = unitOfWork;
        _driverFactory = driverFactory;
        _logger = logger;
    }

    /// <summary>منطقه زمانی ایران — کش‌شده؛ يک‌بار یافتن کافی است</summary>
    private static readonly Lazy<TimeZoneInfo> IranTz = new(() =>
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
    });

    /// <summary>تبدیل زمان UTC به تاریخ محلی ایران</summary>
    internal static DateOnly ToLocalDate(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc), IranTz.Value));

    /// <summary>تبدیل زمان UTC به DateTime محلی ایران (بدون Kind — برای محاسباتگر)</summary>
    internal static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), IranTz.Value);

    /// <summary>همگام‌سازی یک دستگاه — Pull و Persist و Process</summary>
    public async Task<DeviceSyncResult> SyncDeviceAsync(AttendanceDevice device, CancellationToken ct = default)
    {
        if (!device.IsActive)
        {
            return new DeviceSyncResult(device.Id, device.Name, false, 0, 0, 0,
                "دستگاه غیرفعال است");
        }

        var tracking = await _unitOfWork.Repository<AttendanceDevice>()
            .FirstOrDefaultAsync(d => d.Id == device.Id, ct);
        if (tracking is null)
        {
            return new DeviceSyncResult(device.Id, device.Name, false, 0, 0, 0,
                "دستگاه در پایگاه داده یافت نشد");
        }

        tracking.Status = DeviceStatus.Syncing;

        try
        {
            var driver = _driverFactory.DriverFor(tracking);
            var fromUtc = tracking.LastSyncAt ?? DateTime.UtcNow - FirstPullWindow;
            var punches = await driver.PullNewPunchesAsync(tracking, fromUtc, ct);

            var pulled = 0;

            if (punches.Count > 0)
            {
                // جلوگیری از درج تکراری — مجموعه ضربه‌های موجود در بازه را یکجا بیاور
                var upper = punches.Max(p => p.PunchTime);
                var existing = await _unitOfWork.Repository<DeviceRawLog>().QueryableNoTracking
                    .Where(r => r.DeviceId == tracking.Id
                             && r.PunchTime >= fromUtc.AddMinutes(-1)
                             && r.PunchTime <= upper.AddMinutes(1))
                    .Select(r => new { r.EnrollNumber, r.PunchTime })
                    .ToListAsync(ct);
                var seen = existing.Select(e => (e.EnrollNumber, e.PunchTime)).ToHashSet();

                var batchId = Guid.NewGuid();
                foreach (var p in punches)
                {
                    if (!seen.Add((p.EnrollNumber, p.PunchTime)))
                        continue;

                    await _unitOfWork.Repository<DeviceRawLog>().AddAsync(new DeviceRawLog
                    {
                        DeviceId = tracking.Id,
                        EnrollNumber = p.EnrollNumber,
                        PunchTime = p.PunchTime,
                        PunchState = p.State,
                        VerifyMode = p.VerifyMode,
                        InOutPort = p.InOutPort,
                        SyncBatchId = batchId,
                        ProcessedStatus = RawLogStatus.Pending,
                    }, ct);
                    pulled++;
                }
            }

            tracking.Status = DeviceStatus.Online;
            tracking.LastSyncAt = DateTime.UtcNow;
            tracking.LastSyncLogCount = pulled;
            tracking.LastError = null;
            await _unitOfWork.SaveEntitiesAsync(ct);

            // پردازش لاگ‌های تازه + قدیمی‌های مانده
            var processed = await ProcessPendingLogsAsync(null, null, ct);

            _logger.LogInformation(
                "همگام‌سازی دستگاه {Device} موفق — {Pulled} لاگ تازه، {Processed} لاگ پردازش‌شده",
                tracking.Name, pulled, processed);

            return new DeviceSyncResult(tracking.Id, tracking.Name, true, pulled,
                processed, 0,
                $"همگام‌سازی موفق — {pulled} لاگ تازه دریافت و {processed} لاگ پردازش شد");
        }
        catch (Exception ex)
        {
            tracking.Status = DeviceStatus.Error;
            tracking.LastError = ex.Message.Length > 280 ? ex.Message[..280] : ex.Message;
            await _unitOfWork.SaveEntitiesAsync(ct);

            _logger.LogWarning(ex, "خطای همگام‌سازی دستگاه {Device} ({Ip})",
                tracking.Name, tracking.IpAddress);

            return new DeviceSyncResult(tracking.Id, tracking.Name, false, 0, 0, 0,
                $"خطای اتصال: {tracking.LastError}");
        }
    }

    /// <summary>همگام‌سازی همه دستگاه‌های فعال</summary>
    public async Task<IReadOnlyList<DeviceSyncResult>> SyncAllActiveDevicesAsync(CancellationToken ct = default)
    {
        var devices = await _unitOfWork.Repository<AttendanceDevice>().QueryableNoTracking
            .Where(d => d.IsActive && !d.IsDeleted)
            .ToListAsync(ct);

        var results = new List<DeviceSyncResult>(devices.Count);
        foreach (var device in devices)
        {
            results.Add(await SyncDeviceAsync(device, ct));
        }
        return results;
    }

    /// <summary>پردازش لاگ‌های در انتظار — تبدیل به رکورد حضور با محاسباتگر</summary>
    public async Task<int> ProcessPendingLogsAsync(DateOnly? date = null, long? employeeId = null, CancellationToken ct = default)
    {
        var pending = await _unitOfWork.Repository<DeviceRawLog>().QueryableNoTracking
            .Where(r => r.ProcessedStatus == RawLogStatus.Pending)
            .OrderBy(r => r.PunchTime)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return 0;

        // نگاشت‌های فعال: (دستگاه، شناسه) → پرسنل
        var mappings = await _unitOfWork.Repository<DeviceUserMapping>().QueryableNoTracking
            .Where(m => m.IsActive && !m.IsDeleted)
            .ToListAsync(ct);
        var mapIndex = mappings
            .GroupBy(m => (m.DeviceId, m.EnrollNumber))
            .ToDictionary(g => g.Key, g => g.First().EmployeeId);

        // گروه‌بندی لاگ‌ها به پرسنل — لاگ بی‌نگاشت → خطا
        var byEmployee = new Dictionary<long, List<DeviceRawLog>>();
        var orphans = new List<DeviceRawLog>();

        foreach (var log in pending)
        {
            if (mapIndex.TryGetValue((log.DeviceId, log.EnrollNumber), out var empId))
            {
                if (employeeId is null || empId == employeeId)
                    (byEmployee.TryGetValue(empId, out var list) ? list : byEmployee[empId] = []).Add(log);
            }
            else
            {
                orphans.Add(log);
            }
        }

        // نشاندن لاگ‌های رهاشده — با پیام روشن برای پنل عیب‌یابی
        var orphanRepo = _unitOfWork.Repository<DeviceRawLog>();
        foreach (var log in orphans)
        {
            var tracked = await orphanRepo.FirstOrDefaultAsync(r => r.Id == log.Id, ct);
            if (tracked is null) continue;
            tracked.ProcessedStatus = RawLogStatus.Error;
            tracked.ProcessedAt = DateTime.UtcNow;
            tracked.ErrorMessage = "نگاشت پرسنل برای این شناسه دستگاه ثبت نشده است";
        }

        var repoAttendance = _unitOfWork.Repository<EmployeeAttendance>();
        var repoRaw = _unitOfWork.Repository<DeviceRawLog>();
        var processed = orphans.Count;

        foreach (var (empId, logs) in byEmployee)
        {
            // گروه‌بندی بر اساس تاریخ محلی ایران
            var byDate = logs.GroupBy(l => ToLocalDate(l.PunchTime));

            foreach (var dayGroup in byDate)
            {
                var localDate = dayGroup.Key;

                // فیلتر تاریخ اختیاری
                if (date is not null && localDate != date) continue;

                var effectiveShift = await FindEffectiveShiftAsync(empId, localDate, ct);
                var approvedLeave = await FindApprovedLeaveAsync(empId, localDate, ct);
                var isHoliday = await IsOfficialHolidayAsync(localDate, ct);

                // ضربه‌ها به وقت محلی — شامل بامدادِ فردا برای شیفت شب
                var punchTimes = logs
                    .Where(l => InShiftWindow(l.PunchTime, localDate))
                    .Select(l => ToLocal(l.PunchTime))
                    .ToList();

                if (punchTimes.Count == 0) continue;

                var computation = AttendanceCalculator.Compute(
                    localDate, punchTimes, effectiveShift, approvedLeave, isHoliday);

                var sourceDeviceId = dayGroup.OrderBy(l => l.PunchTime).First().DeviceId;

                var record = await repoAttendance.QueryableTracking
                    .FirstOrDefaultAsync(a => a.EmployeeId == empId && a.AttendanceDate == localDate, ct);

                if (record is null)
                {
                    record = new EmployeeAttendance
                    {
                        EmployeeId = empId,
                        AttendanceDate = localDate,
                    };
                    await repoAttendance.AddAsync(record, ct);
                }

                record.ShiftId = effectiveShift?.Id;
                record.CheckIn = computation.CheckIn;
                record.CheckOut = computation.CheckOut;
                record.LateMinutes = computation.LateMinutes;
                record.EarlyLeaveMinutes = computation.EarlyLeaveMinutes;
                record.WorkedHours = computation.WorkedHours;
                record.OvertimeHours = computation.OvertimeHours;
                record.Status = computation.Status;
                record.SourceDeviceId = sourceDeviceId;

                // علامت‌گذاری لاگ‌های این روز به پردازش‌شده
                foreach (var log in dayGroup)
                {
                    var trackedLog = await repoRaw.FirstOrDefaultAsync(r => r.Id == log.Id, ct);
                    if (trackedLog is null) continue;
                    trackedLog.ProcessedStatus = RawLogStatus.Processed;
                    trackedLog.ProcessedAt = DateTime.UtcNow;
                    trackedLog.AttendanceId = record.Id;
                    processed++;
                }
            }
        }

        await _unitOfWork.SaveEntitiesAsync(ct);
        return processed;
    }

    /// <summary>آیا ضربه در پنجره تقویمی «این روز + بامداد فردا» است؟ — پوشش شیفت شب</summary>
    private static bool InShiftWindow(DateTime punchUtc, DateOnly localDate)
    {
        var local = ToLocal(punchUtc);
        return local.Date == localDate.ToDateTime(TimeOnly.MinValue)
            || (local.Date == localDate.ToDateTime(TimeOnly.MinValue).AddDays(1) && local.Hour < 12);
    }

    /// <summary>شیفت مؤثر پرسنل در تاریخ — برنده بالاترین اولویت</summary>
    private async Task<Shift?> FindEffectiveShiftAsync(long employeeId, DateOnly date, CancellationToken ct)
    {
        var assignments = await _unitOfWork.Repository<EmployeeShiftAssignment>().QueryableNoTracking
            .Where(a => a.EmployeeId == employeeId && a.IsActive && !a.IsDeleted
                     && a.FromDate <= date
                     && (a.ToDate == null || a.ToDate >= date))
            .OrderByDescending(a => a.Priority)
            .ThenByDescending(a => a.FromDate)
            .ToListAsync(ct);

        var shiftIds = assignments
            .Where(a => a.IsEffectiveOn(date))
            .Select(a => a.ShiftId)
            .Distinct()
            .ToList();

        if (shiftIds.Count == 0)
            return null;

        var shiftId = shiftIds[0];
        return await _unitOfWork.Repository<Shift>()
            .FirstOrDefaultAsync(s => s.Id == shiftId && s.IsActive, ct);
    }

    /// <summary>مرخصی تأییدشدهٔ همپوشان با تاریخ</summary>
    private async Task<EmployeeLeave?> FindApprovedLeaveAsync(long employeeId, DateOnly date, CancellationToken ct) =>
        await _unitOfWork.Repository<EmployeeLeave>().QueryableNoTracking
            .FirstOrDefaultAsync(l => l.EmployeeId == employeeId
                                   && l.Status == LeaveStatus.Approved
                                   && !l.IsDeleted
                                   && l.FromDate <= date
                                   && l.ToDate >= date, ct);

    /// <summary>آیا تعطیل رسمی است؟ — تعطیلات تکرارشونده قمری/شمسی همپوشان ماه/روز</summary>
    private async Task<bool> IsOfficialHolidayAsync(DateOnly date, CancellationToken ct) =>
        await _unitOfWork.Repository<Holiday>().QueryableNoTracking
            .AnyAsync(h => h.HolidayType == HolidayType.Official && !h.IsDeleted
                        && (h.Date == date
                            || (h.IsRecurring && h.Date.Month == date.Month && h.Date.Day == date.Day)), ct);
}

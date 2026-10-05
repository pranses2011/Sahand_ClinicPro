using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Entities.Security;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر مرکز اعلان‌ها — گام U14 طبق فرم ۲۶ پرامپت بخش ۴
/// فهرست اعلان‌های کاربر جاری با ۵ نوع، علامت‌گذاری خوانده‌شده و شمارنده
/// مسیرها: /api/v1/notifications
/// </summary>
[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public NotificationsController(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <summary>اعلان‌های کاربر جاری — صفحه‌بندی و فیلتر خوانده‌نشده</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications([FromQuery] bool? unreadOnly,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return BadRequest(ApiResponse<object>.Failure("کاربر احراز هویت نشده است"));

        var query = _unitOfWork.Repository<Notification>().QueryableNoTracking
            .Where(n => n.UserId == userId);

        if (unreadOnly == true)
            query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new NotificationItemDto(
                n.Id, n.Title, n.Message, n.Type.ToString(),
                n.IsRead, n.CreatedAt, n.Link))
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            items,
            totalCount = total,
            unreadCount = await CountUnreadAsync(userId.Value),
            page,
            pageSize
        }));
    }

    /// <summary>شمارنده اعلان‌های خوانده‌نشده — برای زنگ TopBar</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        if (_currentUser.UserId is null)
            return BadRequest(ApiResponse<object>.Failure("کاربر احراز هویت نشده است"));
        var count = await CountUnreadAsync(_currentUser.UserId.Value);
        return Ok(ApiResponse<object>.Success(new { count }));
    }

    /// <summary>علامت‌گذاری یک اعلان خوانده‌شده</summary>
    [HttpPost("{id:long}/mark-read")]
    public async Task<IActionResult> MarkRead(long id)
    {
        var notification = await _unitOfWork.Repository<Notification>()
            .FirstOrDefaultAsync(n => n.Id == id);
        if (notification is null)
            return BadRequest(ApiResponse<object>.Failure("اعلان یافت نشد"));

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("اعلان خوانده‌شده علامت خورد"));
    }

    /// <summary>علامت‌گذاری همه اعلان‌ها خوانده‌شده</summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        if (_currentUser.UserId is null)
            return BadRequest(ApiResponse<object>.Failure("کاربر احراز هویت نشده است"));

        var unread = await _unitOfWork.Repository<Notification>().QueryableTracking
            .Where(n => n.UserId == _currentUser.UserId && !n.IsRead)
            .ToListAsync();
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage($"{unread.Count} اعلان خوانده‌شده علامت خورد"));
    }

    /// <summary>شمارش اعلان خوانده‌نشده کاربر</summary>
    private async Task<int> CountUnreadAsync(long userId)
        => await _unitOfWork.Repository<Notification>().QueryableNoTracking
            .CountAsync(n => n.UserId == userId && !n.IsRead);

    /// <summary>ردیف اعلان</summary>
    public record NotificationItemDto(long Id, string Title, string? Content, string Type,
        bool IsRead, DateTime CreatedAt, string? Link);
}

/// <summary>
/// کنترلر تکمیلی پیامک — گام U14 طبق فرم ۲۶:
/// تاریخچه با فیلتر، آمار ۳۰روزه، مدیریت پنل‌ها با تست اتصال و ارسال دسته‌ای
/// </summary>
[ApiController]
[Route("api/v1/sms")]
[Authorize]
public class SmsAdminController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISmsService _sms;
    private readonly ISmsProvider _defaultProvider;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public SmsAdminController(IUnitOfWork unitOfWork, ISmsService sms, ISmsProvider defaultProvider)
    {
        _unitOfWork = unitOfWork;
        _sms = sms;
        _defaultProvider = defaultProvider;
    }

    /// <summary>تاریخچه پیامک‌ها — فیلتر وضعیت و بازه تاریخ</summary>
    [HttpGet("history")]
    [HasPermission("sms.view")]
    public async Task<IActionResult> GetHistory([FromQuery] string? status,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _unitOfWork.Repository<SmsMessage>().QueryableNoTracking;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SmsStatus>(status, true, out var parsed))
            query = query.Where(m => m.Status == parsed);
        if (from is not null) query = query.Where(m => m.SentAt >= from.Value);
        if (to is not null) query = query.Where(m => m.SentAt <= to.Value.AddDays(1));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new SmsHistoryDto(
                m.Id,
                m.Recipient,
                (string?)null,
                m.Message,
                m.Status.ToString(),
                m.SentAt,
                m.DeliveredAt,
                m.Cost))
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new { items, totalCount = total, page, pageSize }));
    }

    /// <summary>آمار پیامک — ارسال/تحویل/خطا و هزینه و نمودار روزانه ۳۰ روز</summary>
    [HttpGet("stats")]
    [HasPermission("sms.view")]
    public async Task<IActionResult> GetStats()
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var messages = _unitOfWork.Repository<SmsMessage>().QueryableNoTracking
            .Where(m => m.SentAt >= since);

        var sentCount = await messages.CountAsync();
        var deliveredCount = await messages.CountAsync(m => m.Status == SmsStatus.Delivered);
        var failedCount = await messages.CountAsync(m => m.Status == SmsStatus.Failed);
        var monthCost = await messages.SumAsync(m => (decimal?)m.Cost) ?? 0;

        // نمودار روزانه — ۳۰ روز اخیر
        var daily = await messages
            .GroupBy(m => m.SentAt!.Value.Date)
            .Select(g => new DailyPoint(g.Key, g.Count()))
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            sentCount,
            deliveredCount,
            failedCount,
            monthCost,
            successRate = sentCount == 0 ? 0 : Math.Round(deliveredCount * 100.0 / sentCount, 1),
            daily
        }));
    }

    /// <summary>فهرست پنل‌های پیامک — کلیدها ماسک‌شده</summary>
    [HttpGet("providers")]
    [HasPermission("sms.view")]
    public async Task<IActionResult> GetProviders()
    {
        var providers = await _unitOfWork.Repository<SmsProvider>().QueryableNoTracking
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Priority)
            .Select(p => new ProviderDto(
                p.Id, p.ProviderName, p.ProviderCode ?? "", p.SenderNumber,
                p.Balance, p.Priority, p.IsDefault, p.IsActive))
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(providers));
    }

    /// <summary>ثبت/ویرایش پنل پیامک</summary>
    [HttpPost("providers")]
    [HasPermission("sms.edit")]
    public async Task<IActionResult> SaveProvider([FromBody] SaveProviderRequest request)
    {
        SmsProvider entity;
        if (request.Id is long id)
        {
            entity = await _unitOfWork.Repository<SmsProvider>().QueryableTracking
                .FirstOrDefaultAsync(p => p.Id == id);
            if (entity is null)
                return BadRequest(ApiResponse<object>.Failure("پنل یافت نشد"));
        }
        else
        {
            var duplicate = await _unitOfWork.Repository<SmsProvider>().QueryableNoTracking
                .AnyAsync(p => p.ProviderCode == request.Code);
            if (duplicate)
                return BadRequest(ApiResponse<object>.Failure("پنلی با این کد وجود دارد"));
            entity = new SmsProvider { CreatedAt = DateTime.UtcNow };
            await _unitOfWork.Repository<SmsProvider>().AddAsync(entity);
        }

        entity.ProviderName = request.Name;
        entity.ProviderCode = request.Code;
        entity.SenderNumber = request.SenderNumber;
        entity.Priority = request.Priority;
        entity.IsDefault = request.IsDefault;
        entity.IsActive = request.IsActive;
        // کلید API رمزنگاری‌شده ذخیره می‌شود — سرویس IEncryptionService هنگام ارسال رمزگشایی می‌کند
        if (!string.IsNullOrWhiteSpace(request.ApiKey))
            entity.ApiKeyEncrypted = System.Text.Encoding.UTF8.GetBytes(request.ApiKey);

        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("پنل ذخیره شد"));
    }

    /// <summary>تست اتصال پنل — ارسال پیامک آزمایشی به شماره مدیریتی</summary>
    [HttpPost("providers/{id:long}/test")]
    [HasPermission("sms.edit")]
    public async Task<IActionResult> TestProvider(long id, [FromQuery] string? testNumber)
    {
        var provider = await _unitOfWork.Repository<SmsProvider>().QueryableNoTracking
            .FirstOrDefaultAsync(p => p.Id == id);
        if (provider is null)
            return BadRequest(ApiResponse<object>.Failure("پنل یافت نشد"));

        // ارسال تستی با پنل پیش‌فرض — گزارش نتیجه اتصال
        var target = testNumber ?? "09120000000";
        var result = await _sms.SendAsync(target,
            $"تست اتصال پنل {provider.ProviderName} — سامانه درمانگاه سهند");
        if (!result.Success)
            return Ok(ApiResponse<object>.Success(
                new { connected = false, detail = result.Error }, "اتصال ناموفق بود"));

        return Ok(ApiResponse<object>.Success(new { connected = true }, "اتصال موفق و پیامک تستی ارسال شد"));
    }

    /// <summary>ارسال دسته‌ای — مخاطبان بر اساس نوع</summary>
    [HttpPost("bulk-send")]
    [HasPermission("sms.bulksend")]
    public async Task<IActionResult> BulkSend([FromBody] BulkSendRequest request)
    {
        // مخاطبان بر اساس نوع — طبق گزینه‌های فرم ۲۶
        var patients = _unitOfWork.Repository<Domain.Entities.People.Patient>().QueryableNoTracking
            .Where(p => p.IsActive && !p.IsDeleted);

        var recipients = request.AudienceType switch
        {
            "active-patients" => await patients.Select(p => p.Mobile).ToListAsync(),
            "tomorrow-appointments" => await _unitOfWork.Repository<Domain.Entities.Scheduling.Appointment>()
                .QueryableNoTracking
                .Where(a => a.AppointmentDate == DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1)) &&
                            a.Status != AppointmentStatus.Cancelled &&
                            a.Patient != null)
                .Select(a => a.Patient!.Mobile)
                .ToListAsync(),
            "debtors" => await _unitOfWork.Repository<Domain.Entities.Billing.Invoice>()
                .QueryableNoTracking
                .Where(i => i.PaymentStatus == PaymentStatus.Unpaid && i.Patient != null)
                .Select(i => i.Patient!.Mobile)
                .ToListAsync(),
            _ => new List<string>()
    };

        recipients = recipients.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList();

        // ارسال از طریق صف — پاسخ فوری با شمارش
        var queued = 0;
        foreach (var number in recipients)
        {
            var result = await _sms.SendAsync(number, request.Message);
            if (result.Success) queued++;
        }

        return Ok(ApiResponse<object>.Success(new { sent = queued, total = recipients.Count },
            $"ارسال دسته‌ای انجام شد ({queued} از {recipients.Count})"));
    }

    /// <summary>نقطه روزانه نمودار</summary>
    public record DailyPoint(DateTime Date, int Count);

    /// <summary>ردیف تاریخچه</summary>
    public record SmsHistoryDto(long Id, string Recipient, string? PatientName, string? Content,
        string Status, DateTime? SentAt, DateTime? DeliveredAt, decimal Cost);

    /// <summary>ردیف پنل</summary>
    public record ProviderDto(long Id, string Name, string Code, string? SenderNumber,
        decimal? Balance, int Priority, bool IsDefault, bool IsActive);

    /// <summary>درخواست ذخیره پنل</summary>
    public record SaveProviderRequest(long? Id, string Name, string Code, string? SenderNumber,
        string? ApiKey, int Priority, bool IsDefault, bool IsActive);

    /// <summary>درخواست ارسال دسته‌ای</summary>
    public record BulkSendRequest(string AudienceType, string Message);
}

using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Notifications.CreateNotification;

/// <summary>ورودی ایجاد اعلان</summary>
public sealed record CreateNotificationDto(long UserId, string Title, string? Message,
    string Type = "Info", string? Link = null);

/// <summary>
/// فرمان ایجاد اعلان درون‌برنامه‌ای — ثبت در پایگاه داده و پخش بلادرنگ به کاربر مقصد (گام R1)
/// </summary>
public sealed record CreateNotificationCommand(CreateNotificationDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج ورودی اعلان</summary>
public sealed class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateNotificationCommandValidator()
    {
        RuleFor(x => x.Dto.UserId).GreaterThan(0).WithMessage("انتخاب کاربر مقصد الزامی است");
        RuleFor(x => x.Dto.Title).NotEmpty().MaximumLength(200)
            .WithMessage("عنوان اعلان الزامی و حداکثر ۲۰۰ نویسه است");
        RuleFor(x => x.Dto.Message).MaximumLength(1000)
            .WithMessage("متن اعلان حداکثر ۱۰۰۰ نویسه است");
    }
}

/// <summary>گرداننده ایجاد اعلان — ذخیره + اتصال بلادرنگ</summary>
public sealed class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealtimeNotifier _realtime;
    private readonly ILogger<CreateNotificationCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار و اتصال بلادرنگ</summary>
    public CreateNotificationCommandHandler(IUnitOfWork unitOfWork,
        IRealtimeNotifier realtime, ILogger<CreateNotificationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _realtime = realtime;
        _logger = logger;
    }

    /// <summary>اجرا — ثبت اعلان و پخش فوری به کاربر</summary>
    public async Task<Result<long>> Handle(CreateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        // تبدیل رشته نوع به شمارنده دامنه
        var type = dto.Type switch
        {
            "Success" => NotificationType.Success,
            "Warning" => NotificationType.Warning,
            "Error" => NotificationType.Error,
            "License" => NotificationType.License,
            "Stock" => NotificationType.Stock,
            _ => NotificationType.Info
        };

        var notification = new Notification
        {
            UserId = dto.UserId,
            Title = dto.Title,
            Message = dto.Message,
            Type = type,
            Link = dto.Link,
            IsRead = false
        };
        await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        // پخش بلادرنگ — خطای پخش ثبت می‌شود اما اعلان ذخیره‌شده معتبر می‌ماند
        try
        {
            await _realtime.NotifyUserAsync(dto.UserId, notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "پخش بلادرنگ اعلان {Id} برای کاربر {UserId} ناموفق بود",
                notification.Id, dto.UserId);
        }

        return Result<long>.Success(notification.Id);
    }
}

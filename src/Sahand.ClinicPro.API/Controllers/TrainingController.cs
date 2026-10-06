using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر مرکز آموزش — پیشرفت یادگیری درس‌ها برای هر کاربر.
/// محتوای درس‌ها در کلاینت (HTML داخلی) است و سرور فقط پیشرفت را نگه می‌دارد.
/// مسیرها: /api/v1/training
/// چرخه سیزدهم (درخواست ۸-۶).
/// </summary>
[ApiController]
[Route("api/v1/training")]
[Authorize]
public class TrainingController : ApiBaseController
{
    private readonly Domain.Interfaces.IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public TrainingController(Domain.Interfaces.IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <summary>کلید تنظیم پیشرفت آموزش کاربر جاری</summary>
    private string ProgressKey => $"training-progress:{_currentUser.UserId ?? 0}";

    /// <summary>پیشرفت آموزش کاربر جاری — شناسه درس‌های تکمیل‌شده</summary>
    [HttpGet("progress")]
    public async Task<IActionResult> GetProgress(CancellationToken ct)
    {
        var setting = await _unitOfWork.Repository<Domain.Entities.Infrastructure.SystemSetting>()
            .QueryableNoTracking
            .FirstOrDefaultAsync(x => x.Key == ProgressKey && !x.IsDeleted, ct);

        var completed = TryParseList(setting?.Value);
        return Ok(ApiResponse<object>.Success(new
        {
            completedLessons = completed,
            completedCount = completed.Count,
            totalLessons = 10,
        }));
    }

    /// <summary>ثبت تکمیل یک درس — پیشرفت ایدمپوتنت به‌صورت فهرست شناسه‌ها</summary>
    [HttpPut("progress/{lessonId}")]
    public async Task<IActionResult> CompleteLesson(string lessonId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(lessonId) || lessonId.Length > 50)
            return BadRequest(ApiResponse<string>.Failure("شناسه درس نامعتبر است"));

        var setting = await _unitOfWork.Repository<Domain.Entities.Infrastructure.SystemSetting>()
            .QueryableTracking
            .FirstOrDefaultAsync(x => x.Key == ProgressKey && !x.IsDeleted, ct);

        var completed = TryParseList(setting?.Value);
        if (!completed.Contains(lessonId))
            completed.Add(lessonId);

        var value = System.Text.Json.JsonSerializer.Serialize(completed);
        if (setting is null)
        {
            setting = new Domain.Entities.Infrastructure.SystemSetting
            {
                Category = "Training",
                Key = ProgressKey,
                Value = value,
                DataType = Domain.Enums.SettingDataType.Json,
                Description = $"پیشرفت آموزش کاربر {_currentUser.Username}"
            };
            await _unitOfWork.Repository<Domain.Entities.Infrastructure.SystemSetting>().AddAsync(setting, ct);
        }
        else
        {
            setting.Value = value;
        }

        await _unitOfWork.SaveEntitiesAsync(ct);
        return Ok(ApiResponse<object>.SuccessMessage($"درس «{lessonId}» تکمیل شد — {completed.Count} از ۱۰"));
    }

    /// <summary>بازنشانی پیشرفت آموزش کاربر جاری</summary>
    [HttpDelete("progress")]
    public async Task<IActionResult> ResetProgress(CancellationToken ct)
    {
        var setting = await _unitOfWork.Repository<Domain.Entities.Infrastructure.SystemSetting>()
            .QueryableTracking
            .FirstOrDefaultAsync(x => x.Key == ProgressKey && !x.IsDeleted, ct);
        if (setting is not null)
        {
            setting.Value = "[]";
            await _unitOfWork.SaveEntitiesAsync(ct);
        }
        return Ok(ApiResponse<object>.SuccessMessage("پیشرفت آموزش بازنشانی شد"));
    }

    /// <summary>بازخوانی امن فهرست JSON — خروجی همیشه فهرست معتبر است</summary>
    private static List<string> TryParseList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
            return list ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}

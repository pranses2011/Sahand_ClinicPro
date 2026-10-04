# -*- coding: utf-8 -*-
"""گام ۵ — Application.Common: ApiResponse + Behaviors + Helpers + Interfaces + Mapping"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
APP = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Application"))


def write(rel, content):
    full = os.path.join(APP, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════ مدل‌های پاسخ استاندارد ═══════════
write("Common/Models/ApiResponse.cs", """
namespace Sahand.ClinicPro.Application.Common.Models;

/// <summary>
/// پاسخ استاندارد همه endpointهای API — فرمت یکسان برای موفقیت و خطا
/// </summary>
/// <typeparam name="T">نوع داده خروجی</typeparam>
public class ApiResponse<T>
{
    /// <summary>آیا عملیات موفق بود</summary>
    public bool IsSuccess { get; init; }

    /// <summary>کد وضعیت HTTP</summary>
    public int StatusCode { get; init; }

    /// <summary>پیام فارسی قابل نمایش به کاربر</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>داده خروجی در صورت موفقیت</summary>
    public T? Data { get; init; }

    /// <summary>فهرست خطاها در صورت شکست</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>شناسه ردیابی درخواست</summary>
    public string TraceId { get; init; } = string.Empty;

    /// <summary>زمان اجرای درخواست به میلی‌ثانیه</summary>
    public long ElapsedMs { get; init; }

    /// <summary>ساخت پاسخ موفق با داده</summary>
    public static ApiResponse<T> Success(T data, string message = "عملیات با موفقیت انجام شد",
        int statusCode = 200) => new()
    {
        IsSuccess = true, StatusCode = statusCode, Message = message, Data = data
    };

    /// <summary>ساخت پاسخ موفق بدون داده</summary>
    public static ApiResponse<T> SuccessMessage(string message = "عملیات با موفقیت انجام شد",
        int statusCode = 200) => new()
    {
        IsSuccess = true, StatusCode = statusCode, Message = message
    };

    /// <summary>ساخت پاسخ ناموفق</summary>
    public static ApiResponse<T> Failure(string message, int statusCode = 400,
        IReadOnlyList<string>? errors = null) => new()
    {
        IsSuccess = false, StatusCode = statusCode, Message = message,
        Errors = errors ?? Array.Empty<string>()
    };
}
""")

write("Common/Models/PagedResponse.cs", """
namespace Sahand.ClinicPro.Application.Common.Models;

/// <summary>
/// پاسخ صفحه‌بندی‌شده — فرم استاندارد فهرست‌ها با فراداده صفحه
/// </summary>
/// <typeparam name="T">نوع آیتم‌های فهرست</typeparam>
public class PagedResponse<T> : ApiResponse<List<T>>
{
    /// <summary>شماره صفحه جاری — از ۱ شروع می‌شود</summary>
    public int PageNumber { get; init; }

    /// <summary>اندازه صفحه</summary>
    public int PageSize { get; init; }

    /// <summary>کل رکوردهای منطبق</summary>
    public int TotalCount { get; init; }

    /// <summary>کل صفحات</summary>
    public int TotalPages { get; init; }

    /// <summary>آیا صفحه بعدی وجود دارد</summary>
    public bool HasNext { get; init; }

    /// <summary>آیا صفحه قبلی وجود دارد</summary>
    public bool HasPrevious { get; init; }

    /// <summary>ساخت پاسخ صفحه‌بندی‌شده</summary>
    public static PagedResponse<T> Create(List<T> items, int pageNumber, int pageSize, int totalCount)
    {
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
        return new PagedResponse<T>
        {
            IsSuccess = true,
            StatusCode = 200,
            Message = "فهرست با موفقیت دریافت شد",
            Data = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNext = pageNumber < totalPages,
            HasPrevious = pageNumber > 1
        };
    }
}
""")

write("Common/Models/PagedRequest.cs", """
namespace Sahand.ClinicPro.Application.Common.Models;

/// <summary>
/// درخواست صفحه‌بندی استاندارد همه فهرست‌ها — پارامترهای ?page= و pageSize= و sortBy= و sortDir=
/// </summary>
public class PagedRequest
{
    private const int MaxPageSize = 100;

    private int _pageNumber = 1;
    private int _pageSize = 20;

    /// <summary>شماره صفحه — از ۱</summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>اندازه صفحه — حداکثر ۱۰۰</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 20 : (value > MaxPageSize ? MaxPageSize : value);
    }

    /// <summary>مرتب‌سازی بر اساس ستون</summary>
    public string? SortBy { get; set; }

    /// <summary>جهت مرتب‌سازی — asc یا desc</summary>
    public string? SortDir { get; set; }

    /// <summary>عبارت جستجوی عمومی</summary>
    public string? Search { get; set; }
}
""")

# ═══════════ استثنای اعتبارسنجی سفارشی ═══════════
write("Common/Exceptions/AppValidationException.cs", """
namespace Sahand.ClinicPro.Application.Common.Exceptions;

/// <summary>
/// استثنای اعتبارسنجی — با دیکشنری فیلد به پیام فارسی
/// توسط ValidationBehavior پرتاب و توسط ExceptionMiddleware به 400 تبدیل می‌شود
/// </summary>
public class AppValidationException : Exception
{
    /// <summary>خطاهای اعتبارسنجی بر اساس نام فیلد</summary>
    public IDictionary<string, string[]> Errors { get; }

    /// <summary>ساخت استثنا از دیکشنری خطاها</summary>
    public AppValidationException(IDictionary<string, string[]> errors)
        : base("یک یا چند خطای اعتبارسنجی رخ داده است")
    {
        Errors = errors;
    }

    /// <summary>ساخت استثنا از یک فیلد و پیام</summary>
    public AppValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = new[] { message } })
    {
    }
}
""")

# ═══════════ Behaviors ═══════════
write("Common/Behaviors/ValidationBehavior.cs", """
using FluentValidation;
using MediatR;
using Sahand.ClinicPro.Application.Common.Exceptions;

namespace Sahand.ClinicPro.Application.Common.Behaviors;

/// <summary>
/// رفتار اعتبارسنجی خودکار — همه Validatorهای ثبت‌شده برای درخواست را اجرا می‌کند
/// اعتبارسنجی در Handler انجام نمی‌شود بلکه اینجا در خط لوله است
/// </summary>
/// <typeparam name="TRequest">نوع درخواست</typeparam>
/// <typeparam name="TResponse">نوع پاسخ</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>سازنده با تزریق همه اعتبارسنج‌های نوع درخواست</summary>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>اجرای همه اعتبارسنج‌ها قبل از Handler</summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new AppValidationException(
                failures.GroupBy(f => f.PropertyName, f => f.ErrorMessage)
                    .ToDictionary(g => g.Key, g => g.ToArray()));

        return await next();
    }
}
""")

write("Common/Behaviors/LoggingBehavior.cs", """
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Sahand.ClinicPro.Application.Common.Behaviors;

/// <summary>
/// رفتار لاگ‌گذاری — شروع و پایان هر درخواست با شناسه و مدت زمان را ثبت می‌کند
/// </summary>
/// <typeparam name="TRequest">نوع درخواست</typeparam>
/// <typeparam name="TResponse">نوع پاسخ</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>سازنده با تزریق لاگر</summary>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <summary>ثبت لاگ شروع و پایان درخواست</summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("▶ شروع {RequestName}", name);
        try
        {
            var response = await next();
            stopwatch.Stop();
            _logger.LogInformation("✔ پایان {RequestName} در {ElapsedMs} میلی‌ثانیه",
                name, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "✖ خطای {RequestName} پس از {ElapsedMs} میلی‌ثانیه",
                name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
""")

write("Common/Behaviors/PerformanceBehavior.cs", """
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Sahand.ClinicPro.Application.Common.Behaviors;

/// <summary>
/// رفتار عملکرد — درخواست‌های کندتر از آستانه را هشدار می‌دهد
/// </summary>
/// <typeparam name="TRequest">نوع درخواست</typeparam>
/// <typeparam name="TResponse">نوع پاسخ</typeparam>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>آستانه هشدار کندی به میلی‌ثانیه</summary>
    private const int WarningThresholdMs = 500;

    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    /// <summary>سازنده با تزریق لاگر</summary>
    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <summary>سنجش مدت درخواست و هشدار کندی</summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > WarningThresholdMs)
        {
            _logger.LogWarning(
                "⚠ عملکرد پایین: {RequestName} در {ElapsedMs} میلی‌ثانیه اجرا شد (آستانه {Threshold})",
                typeof(TRequest).Name, stopwatch.ElapsedMilliseconds, WarningThresholdMs);
        }

        return response;
    }
}
""")

write("Common/Behaviors/TransactionBehavior.cs", """
using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.Application.Common.Behaviors;

/// <summary>
/// رفتار تراکنش — Handler را در تراکنش دیتابیس می‌پیچد
/// در صورت استثنا Rollback و در صورت موفقیت Commit می‌شود
/// فقط برای Commandها فعال است (Queryها تراکنش نمی‌خواهند)
/// </summary>
/// <typeparam name="TRequest">نوع درخواست</typeparam>
/// <typeparam name="TResponse">نوع پاسخ</typeparam>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    /// <summary>سازنده با تزریق مدیر تراکنش</summary>
    public TransactionBehavior(ITransactionManager transactionManager,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _transactionManager = transactionManager;
        _logger = logger;
    }

    /// <summary>پیچیدن اجرای Handler در تراکنش</summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Queryها (پسوند Query) بدون تراکنش اجرا می‌شوند
        var isQuery = typeof(TRequest).Name.EndsWith("Query");
        if (isQuery)
            return await next();

        await _transactionManager.BeginAsync(cancellationToken);
        try
        {
            var response = await next();
            await _transactionManager.CommitAsync(cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            await _transactionManager.RollbackAsync(cancellationToken);
            _logger.LogWarning(ex, "تراکنش {RequestName} با خطا به عقب برگشت", typeof(TRequest).Name);
            throw;
        }
    }
}
""")

# ═══════════ اینترفیس‌ها ═══════════
write("Interfaces/ITransactionManager.cs", """
namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// مدیر تراکنش دیتابیس — انتزاع برای TransactionBehavior
/// </summary>
public interface ITransactionManager
{
    /// <summary>آغاز تراکنش جدید</summary>
    Task BeginAsync(CancellationToken cancellationToken = default);

    /// <summary>تثبیت تراکنش</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>به عقب برگرداندن تراکنش</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
""")

write("Interfaces/IEncryptionService.cs", """
namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// سرویس رمزنگاری AES-256 — برای فیلدهای حساس بیمار و اعتبارسنجی‌های سرویس‌های خارجی
/// </summary>
public interface IEncryptionService
{
    /// <summary>رمزنگاری متن ساده با AES-256-GCM و بازگشت Base64</summary>
    string Encrypt(string plainText);

    /// <summary>رمزگشایی متن Base64 رمزنگاری‌شده</summary>
    string Decrypt(string cipherText);

    /// <summary>رمزنگاری بایت‌های خام</summary>
    byte[] EncryptBytes(byte[] plainBytes);

    /// <summary>رمزگشایی بایت‌های خام</summary>
    byte[] DecryptBytes(byte[] cipherBytes);
}
""")

write("Interfaces/IDateTimeService.cs", """
namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// سرویس زمان — انتزاع زمان برای تست‌پذیری
/// </summary>
public interface IDateTimeService
{
    /// <summary>زمان فعلی به وقت جهانی</summary>
    DateTime UtcNow { get; }

    /// <summary>امروز به وقت محلی ایران</summary>
    DateOnly TodayIran { get; }

    /// <summary>زمان فعلی به وقت ایران</summary>
    DateTime NowIran { get; }
}
""")

# ═══════════ Helperها ═══════════
write("Common/Helpers/PersianDateHelper.cs", """
namespace Sahand.ClinicPro.Application.Common.Helpers;

/// <summary>
/// راهنمای تقویم شمسی جلالی — تبدیل دوطرفه میلادی و شمسی با الگوریتم استاندارد
/// هیچ وابستگی خارجی ندارد
/// </summary>
public static class PersianDateHelper
{
    /// <summary>تعداد روزهای ماه‌های شمسی — شش ماه اول ۳۱ روزه</summary>
    private static readonly int[] JalaliMonthDays = { 31, 31, 31, 31, 31, 31, 30, 30, 30, 30, 30, 29 };

    /// <summary>نام ماه‌های شمسی</summary>
    public static readonly string[] MonthNames =
    {
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    };

    /// <summary>نام روزهای هفته شمسی — شنبه اول</summary>
    public static readonly string[] DayNames =
    {
        "شنبه", "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه"
    };

    /// <summary>
    /// تبدیل تاریخ میلادی به رشته شمسی با فرمت 1403/07/12
    /// </summary>
    public static string ToShamsi(DateTime gregorianDate)
    {
        var (year, month, day) = ToJalali(gregorianDate);
        return $"{year:D4}/{month:D2}/{day:D2}";
    }

    /// <summary>
    /// تبدیل تاریخ میلادی به رشته شمسی با ساعت — فرمت 1403/07/12 14:35
    /// </summary>
    public static string ToShamsiWithTime(DateTime gregorianDate)
    {
        var iranTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            gregorianDate, "Iran Standard Time");
        return $"{ToShamsi(iranTime)} {iranTime.Hour:D2}:{iranTime.Minute:D2}";
    }

    /// <summary>
    /// تبدیل تاریخ میلادی به شمسی — خروجی سه‌تایی سال و ماه و روز
    /// </summary>
    public static (int Year, int Month, int Day) ToJalali(DateTime gregorianDate)
    {
        var gy = gregorianDate.Year;
        var gm = gregorianDate.Month;
        var gd = gregorianDate.Day;

        var gdm = new[] { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };

        var jy = gy <= 1600 ? 0 : 979;
        gy -= gy <= 1600 ? 621 : 1600;
        var gy2 = gm > 2 ? gy + 1 : gy;

        var days = 365 * gy
                   + (gy2 + 3) / 4 - (gy2 + 99) / 100 + (gy2 + 399) / 400 - 80
                   + gd + gdm[gm - 1];

        jy += 33 * (days / 12053);
        days %= 12053;
        jy += 4 * (days / 1461);
        days %= 1461;

        if (days > 365)
        {
            jy += (days - 1) / 365;
            days = (days - 1) % 365;
        }

        int jm, jd;
        if (days < 186)
        {
            jm = 1 + days / 31;
            jd = 1 + days % 31;
        }
        else
        {
            jm = 7 + (days - 186) / 30;
            jd = 1 + (days - 186) % 30;
        }

        return (jy, jm, jd);
    }

    /// <summary>
    /// تبدیل تاریخ شمسی به میلادی
    /// </summary>
    public static DateTime ToGregorian(int jalaliYear, int jalaliMonth, int jalaliDay)
    {
        if (jalaliMonth is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(jalaliMonth), "ماه شمسی باید بین ۱ تا ۱۲ باشد");
        if (jalaliDay is < 1 or > 31)
            throw new ArgumentOutOfRangeException(nameof(jalaliDay), "روز شمسی باید بین ۱ تا ۳۱ باشد");

        var gy = jalaliYear <= 979 ? 621 : 1600;
        var jy = jalaliYear - (jalaliYear <= 979 ? 0 : 979);

        var days = 365 * jy + (jy / 33) * 8 + ((jy % 33) + 3) / 4
                   + 78 + jalaliDay
                   + (jalaliMonth < 7 ? (jalaliMonth - 1) * 31 : (jalaliMonth - 7) * 30 + 186);

        gy += 400 * (days / 146097);
        days %= 146097;
        if (days > 36524)
        {
            gy += 100 * (--days / 36524);
            days %= 36524;
            if (days >= 365) days++;
        }
        gy += 4 * (days / 1461);
        days %= 1461;
        if (days > 365)
        {
            gy += (days - 1) / 365;
            days = (days - 1) % 365;
        }

        var gd = days + 1;
        var leap = (gy % 4 == 0 && gy % 100 != 0) || gy % 400 == 0;
        var salA = new[] { 0, 31, leap ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        var gm = 0;
        while (gm < 13 && gd > salA[gm]) gd -= salA[gm++];

        return new DateTime(gy, gm, gd);
    }

    /// <summary>
    /// تبدیل رشته شمسی به میلادی — فرمت قابل قبول: 1403/07/12 یا 1403-07-12
    /// </summary>
    public static DateTime? ToGregorian(string? shamsiDate)
    {
        if (string.IsNullOrWhiteSpace(shamsiDate)) return null;
        var parts = shamsiDate.Trim().Replace('-', '/').Split('/');
        if (parts.Length != 3) return null;
        if (!int.TryParse(parts[0], out var y) ||
            !int.TryParse(parts[1], out var m) ||
            !int.TryParse(parts[2], out var d)) return null;
        try
        {
            return ToGregorian(y, m, d);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// محاسبه سن از تاریخ تولد شمسی
    /// </summary>
    public static int CalculateAge(DateTime birthDate)
    {
        var (jy, jm, jd) = ToJalali(DateTime.UtcNow);
        var (by, bm, bd) = ToJalali(birthDate);
        var age = jy - by;
        if (jm < bm || (jm == bm && jd < bd)) age--;
        return age < 0 ? 0 : age;
    }

    /// <summary>
    /// تبدیل ارقام لاتین به فارسی برای نمایش
    /// </summary>
    public static string ToPersianDigits(string input)
    {
        var persian = "۰۱۲۳۴۵۶۷۸۹";
        var chars = input.Select(c => char.IsDigit(c) ? persian[c - '0'] : c);
        return new string(chars.ToArray());
    }
}
""")

write("Common/Helpers/NationalCodeValidator.cs", """
using Sahand.ClinicPro.Domain.ValueObjects;

namespace Sahand.ClinicPro.Application.Common.Helpers;

/// <summary>
/// اعتبارسنج کد ملی ۱۰ رقمی ایرانی — الگوریتم رقم کنترلی
/// </summary>
public static class NationalCodeValidator
{
    /// <summary>
    /// بررسی صحت کد ملی با الگوریتم رقم کنترلی
    /// </summary>
    public static bool IsValid(string? code) => NationalCode.IsValid(code);

    /// <summary>
    /// پیام خطای فارسی برای کد ملی نامعتبر
    /// </summary>
    public const string InvalidMessage = "کد ملی نامعتبر است";
}
""")

# ═══════════ Mapping پایه ═══════════
write("Common/Mappings/MappingProfile.cs", """
using AutoMapper;

namespace Sahand.ClinicPro.Application.Common.Mappings;

/// <summary>
/// پروفایل نقشه AutoMapper — نقشه‌های هر ماژول در گام‌های بعدی اضافه می‌شوند
/// </summary>
public class MappingProfile : Profile
{
    /// <summary>ساخت پروفایل و ثبت نقشه‌های پایه</summary>
    public MappingProfile()
    {
        // نقشه‌های ماژول‌ها در گام‌های ۶ تا ۲۸ اضافه می‌شوند
    }
}
""")

print("✅ گام ۵ — بخش ۱ (مدل‌ها و رفتارها) کامل شد")

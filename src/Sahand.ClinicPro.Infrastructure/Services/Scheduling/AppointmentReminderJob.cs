using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;

namespace Sahand.ClinicPro.Infrastructure.Services.Scheduling;

/// <summary>
/// کار یادآوری خودکار نوبت — چرخهٔ ۶۹ (فاز ۳ / F-03 نقشه راه ai-review)
/// «یادآوری خودکار نوبت: پیامک ۲۴/۲ ساعت + ساعات سکوت + ثبت Reminder»
///
/// ─────────────────────────────────────────────────────────────────────
/// طرز کار (هر ساعت با Hangfire اجرا می‌شود):
/// ۱) موج اول — ۲۴ ساعت قبل: نوبت‌های ساعتی که در ۲۴ ساعت آیندهٔ ایران شروع
///    می‌شوند و هنوز ReminderSent ندارند ← پیامک «یادآوری نوبت» در صف قرار
///    می‌گیرد و پرچم موج اول می‌خورد (تکرار ارسال ممکن نیست).
/// ۲) موج دوم — ۲ ساعت قبل: نوبت‌هایی که در ۲ ساعت آینده شروع می‌شوند و
///    Reminder2Sent ندارند ← پیامک کوتاه‌تر «امروز» با اولویت بالاتر (۲).
///    اگر نوبت کمتر از ۲ ساعت فاصله دارد، موج اول بی‌صدا رد می‌شود تا بیمارِ
///    نوبتِ نزدیک دو پیام هم‌زمان نگیرد.
/// ۳) ساعات سکوت (پیش‌فرض ۲۲:۰۰ تا ۰۷:۰۰ وقت ایران): پیامِ داخل بازهٔ سکوت
///    برای «اولین لحظهٔ فعال» (پایان سکوت) زمان‌بندی می‌شود نه حذف؛ برای
///    موج دوم اگر پایان سکوت از ساعت نوبت رد شده بود، همان‌جا ارسال فوری
///    می‌شود (یادآوریِ بعد از نوبت بی‌فایده است).
/// ۴) نوبت‌های لغو/عدم حضور/انجام‌شده و بیماران بدون موبایل معتبر (۰۹xxxxxxxxx)
///    از هر دو موج کنار گذاشته می‌شوند.
/// ۵) تنظیمات از جدول SystemSetting خوانده می‌شود و بدون توقف سرور قابل
///    تغییر است: sms.reminders.enabled / sms.reminders.quietstart /
///    sms.reminders.quietend
///
/// ─────────────────────────────────────────────────────────────────────
/// چرا صف (SmsQueue) و نه ارسال مستقیم؟
/// پردازشگر صف پیامک هر دقیقه با Hangfire اجرا می‌شود، retry و لاگ دیتابیسی
/// دارد و با کندی اپراتور، کار ساعتانهٔ ما بلاک نمی‌شود — جدایی وظایف.
/// </summary>
public class AppointmentReminderJob
{
    /// <summary>بازهٔ موج اول — ۲۴ ساعت قبل از نوبت (F-03)</summary>
    private const int Wave1Hours = 24;

    /// <summary>بازهٔ موج دوم — ۲ ساعت قبل از نوبت (F-03)</summary>
    private const int Wave2Hours = 2;

    /// <summary>کلید فعال/غیرفعال بودن یادآوری خودکار در SystemSetting</summary>
    public const string SettingEnabled = "sms.reminders.enabled";

    /// <summary>کلید ساعت شروع سکوت (وقت ایران، قالب HH:mm)</summary>
    public const string SettingQuietStart = "sms.reminders.quietstart";

    /// <summary>کلید ساعت پایان سکوت (وقت ایران، قالب HH:mm)</summary>
    public const string SettingQuietEnd = "sms.reminders.quietend";

    private readonly ClinicProDbContext _context;
    private readonly ISmsService _sms;
    private readonly ILogger<AppointmentReminderJob> _logger;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public AppointmentReminderJob(ClinicProDbContext context, ISmsService sms,
        ILogger<AppointmentReminderJob> logger)
    {
        _context = context;
        _sms = sms;
        _logger = logger;
    }

    /// <summary>
    /// نقطهٔ ورود Hangfire — هر ساعت؛ همهٔ استثناها بلعیده و فقط لاگ می‌شوند
    /// تا یک خطای گذرا هرگز زنجیرهٔ اجراهای بعدی را قطع نکند.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        try
        {
            await RunCoreAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // خاتمهٔ درخواست توقف — طبیعی است
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "کار یادآوری خودکار نوبت با خطا متوقف شد — اجرای ساعت بعد ادامه می‌یابد");
        }
    }

    /// <summary>منطق اصلی کار — موج ۲ ساعته و سپس موج ۲۴ ساعته</summary>
    private async Task RunCoreAsync(CancellationToken ct)
    {
        // ۱) تنظیمات — پیش‌فرض‌ها مطابق نقشه راه: فعال + سکوت ۲۲:۰۰ تا ۰۷:۰۰
        if (!await IsEnabledAsync(ct))
        {
            _logger.LogDebug("یادآوری خودکار نوبت در تنظیمات غیرفعال است — عبور");
            return;
        }

        var (quietStart, quietEnd) = await GetQuietHoursAsync(ct);

        // ۲) ساعت جاری ایران — نوبت‌ها با وقت محلی کلینیک ذخیره می‌شوند
        var now = NowInIran();

        // ۳) موج دوم (۲ ساعت) را «اول» پردازش می‌کنیم تا در نوبت‌های خیلی نزدیک،
        //    پرچم موج دوم بخورد و موج اولِ همان ردیف در همین اجرا ردش کند
        var wave2Count = await SendWaveAsync(
            fromHours: 0, toHours: Wave2Hours, isWave2: true,
            now: now, quietStart: quietStart, quietEnd: quietEnd, ct: ct);

        // ۴) موج اول (۲۴ ساعت) — نوبت‌های نزدیک‌تر از ۲ ساعت از این موج کنار
        //    می‌گذارند (پارامتر fromHours=Wave2Hours) تا دو پیام هم‌زمان نرود
        var wave1Count = await SendWaveAsync(
            fromHours: Wave2Hours, toHours: Wave1Hours, isWave2: false,
            now: now, quietStart: quietStart, quietEnd: quietEnd, ct: ct);

        if (wave1Count + wave2Count > 0)
            _logger.LogInformation(
                "یادآوری خودکار نوبت: {Wave1} پیامک ۲۴ساعته و {Wave2} پیامک ۲ساعته صف شد (سکوت {Start}-{End})",
                wave1Count, wave2Count, quietStart.ToString("HH:mm"), quietEnd.ToString("HH:mm"));
    }

    /// <summary>
    /// یک موج پیامک — نوبت‌های بازهٔ [fromHours, toHours) ساعتِ آینده که
    /// پرچم موج مربوطه را نخورده‌اند؛ پس از صف‌کردن، پرچم به‌صورت اتمی خورده می‌شود.
    /// </summary>
    private async Task<int> SendWaveAsync(int fromHours, int toHours, bool isWave2,
        DateTime now, TimeOnly quietStart, TimeOnly quietEnd, CancellationToken ct)
    {
        // بازهٔ شروع نوبت بر حسب وقت ایران
        var windowStart = now.AddHours(fromHours);
        var windowEnd = now.AddHours(toHours);
        var startDate = DateOnly.FromDateTime(windowStart);
        var endDate = DateOnly.FromDateTime(windowEnd);

        // فیلتر وضعیت‌ها همان فیلتر فرمان دستی موجود است — فقط نوبت‌های «زنده»
        var candidates = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.Status != AppointmentStatus.Cancelled
                     && a.Status != AppointmentStatus.NoShow
                     && a.Status != AppointmentStatus.Completed
                     && a.AppointmentDate >= startDate && a.AppointmentDate <= endDate
                     && (isWave2 ? !a.Reminder2Sent : !a.ReminderSent))
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.Employee)
            .ToListAsync(ct);

        var queued = 0;
        foreach (var a in candidates)
        {
            // ترکیب تاریخ و ساعت نوبت (وقت ایران) — نوبت باید داخل پنجرهٔ موج باشد؛
            // پالایش دوباره لازم است چون فیلتر تاریخ روزانه است و مرز شب‌های موج را قطعی می‌کند
            var startsAt = a.AppointmentDate.ToDateTime(TimeOnly.MinValue)
                           + a.AppointmentTime.ToTimeSpan();
            if (startsAt < windowStart || startsAt >= windowEnd)
                continue;

            // بیمار بدون موبایل معتبر (۰۹ + ۱۱ رقم) قابل یادآوری نیست
            var mobile = a.Patient?.Mobile?.Trim();
            if (string.IsNullOrWhiteSpace(mobile) || mobile.Length != 11 || !mobile.StartsWith("09"))
                continue;

            // زمان مؤثر ارسال — احترام به ساعات سکوت (F-03)
            var sendAtLocal = ResolveSendTime(now, startsAt, quietStart, quietEnd, isWave2);

            var patientName = a.Patient?.FirstName ?? string.Empty;
            var doctorName = a.Doctor?.Employee is null ? string.Empty :
                $"{a.Doctor.Employee.FirstName} {a.Doctor.Employee.LastName}";
            var dateShamsi = a.AppointmentDateShamsi ??
                PersianDateHelper.ToShamsi(a.AppointmentDate.ToDateTime(TimeOnly.MinValue));

            // متن موج‌ها متفاوت است: موج اول «اعلان برنامه» / موج دوم «یادآوری امروز»
            var message = isWave2
                ? $"{patientName} عزیز، یادآوری: نوبت شما امروز ساعت {a.AppointmentTime:HH:mm}" +
                  $" با {doctorName} در درمانگاه است — لطفاً ۱۰ دقیقه زودتر حاضر شوید."
                : $"{patientName} عزیز، نوبت شما در تاریخ {dateShamsi} ساعت {a.AppointmentTime:HH:mm}" +
                  $" با {doctorName} ثبت است. لغو یا تغییر: با درمانگاه تماس بگیرید.";

            // ScheduledAt باید UTC باشد — صف با DateTime.UtcNow مقایسه می‌شود
            var scheduledUtc = IranToUtc(sendAtLocal);

            // موج دوم اولویت بالاتر (عدد کوچک‌تر) — نزدیک‌ترین نوبت‌ها اول پردازش می‌شوند
            await _sms.EnqueueRawAsync(mobile, message, scheduledUtc,
                priority: isWave2 ? (byte)2 : (byte)3, ct: ct);

            // علامت‌گذاری پرچم موج به‌صورت اتمی — چون لیست با NoTracking خوانده شد،
            // ExecuteUpdate بدون بارگذاری مجدد و بدون شرایط مسابقه پرچم را می‌زند
            if (isWave2)
                await _context.Appointments
                    .Where(x => x.Id == a.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Reminder2Sent, true), ct);
            else
                await _context.Appointments
                    .Where(x => x.Id == a.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReminderSent, true), ct);

            queued++;
        }

        return queued;
    }

    /// <summary>
    /// زمان مؤثر ارسال با احترام به ساعات سکوت — سه حالت:
    /// ۱) الان داخل سکوت نیست ← همین حالا (فوری).
    /// ۲) داخل سکوت است و پایان سکوت «قبل از شروع نوبت» است ← پایان سکوت.
    /// ۳) داخل سکوت است ولی پایان سکوت از نوبت رد می‌شود ← همین حالا (فوری)؛
    ///    یادآوریِ بعد از موعد بی‌ارزش است، سکوت شکسته می‌شود.
    /// </summary>
    private static DateTime ResolveSendTime(DateTime nowLocal, DateTime startsAtLocal,
        TimeOnly quietStart, TimeOnly quietEnd, bool isWave2)
    {
        if (!IsInQuietHours(nowLocal, quietStart, quietEnd))
            return nowLocal; // ۱) فوری

        // پایان سکوت امروز یا فردا — اولین لحظهٔ فعال پس از الان
        var endToday = nowLocal.Date + quietEnd.ToTimeSpan();
        var nextActive = nowLocal < endToday ? endToday : endToday.AddDays(1);

        // برای موج دوم: اگر فعال‌شدن از نوبت رد شود، فوری می‌فرستیم
        if (isWave2 && nextActive >= startsAtLocal)
            return nowLocal; // ۳) سکوت شکسته می‌شود

        return nextActive; // ۲) پایان سکوت
    }

    /// <summary>
    /// آیا لحظهٔ داده‌شده داخل بازهٔ سکوت است؟ بازهٔ عبورکننده از نیمه‌شب
    /// (مثل ۲۲:۰۰→۰۷:۰۰) به‌درستی پشتیبانی می‌شود.
    /// </summary>
    private static bool IsInQuietHours(DateTime local, TimeOnly quietStart, TimeOnly quietEnd)
    {
        var t = TimeOnly.FromDateTime(local);
        return quietStart <= quietEnd
            ? t >= quietStart && t < quietEnd      // بازهٔ درون یک روز — مثل ۰۱:۰۰→۰۶:۰۰
            : t >= quietStart || t < quietEnd;     // بازهٔ عبورکننده از نیمه‌شب — مثل ۲۲:۰۰→۰۷:۰۰
    }

    /// <summary>فعال بودن یادآوری از SystemSetting — پیش‌فرض: فعال</summary>
    private async Task<bool> IsEnabledAsync(CancellationToken ct)
    {
        var value = await GetSettingAsync(SettingEnabled, ct);
        return value is null || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>خواندن ساعات سکوت از SystemSetting — پیش‌فرض ۲۲:۰۰ تا ۰۷:۰۰</summary>
    private async Task<(TimeOnly Start, TimeOnly End)> GetQuietHoursAsync(CancellationToken ct)
    {
        var startText = await GetSettingAsync(SettingQuietStart, ct);
        var endText = await GetSettingAsync(SettingQuietEnd, ct);

        var start = TimeOnly.TryParse(startText, out var s) ? s : new TimeOnly(22, 0);
        var end = TimeOnly.TryParse(endText, out var e) ? e : new TimeOnly(7, 0);
        return (start, end);
    }

    /// <summary>خواندن یک کلید تنظیم — ردیف حذف‌شدهٔ منطقی کنار می‌رود</summary>
    private async Task<string?> GetSettingAsync(string key, CancellationToken ct)
    {
        return await _context.SystemSettings
            .AsNoTracking()
            .Where(x => x.Key == key && !x.IsDeleted)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>لحظهٔ جاری به وقت ایران — نوبت‌ها با وقت ایران ذخیره می‌شوند</summary>
    private static DateTime NowInIran()
    {
        var tz = ResolveIranTimeZone();
        return TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Utc, tz);
    }

    /// <summary>تبدیل وقت ایران به UTC برای ستون ScheduledAt صف پیامک</summary>
    private static DateTime IranToUtc(DateTime iranLocal)
    {
        var tz = ResolveIranTimeZone();
        var offset = tz.GetUtcOffset(iranLocal);
        return new DateTime(iranLocal.Ticks - offset.Ticks, DateTimeKind.Utc);
    }

    /// <summary>منطقهٔ زمانی ایران — هر دو شناسهٔ ویندوزی و IANA پشتیبانی می‌شود</summary>
    private static TimeZoneInfo ResolveIranTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); }
    }
}

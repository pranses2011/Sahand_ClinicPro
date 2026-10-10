using System.Globalization;
using Serilog.Events;
using Serilog.Formatting;

namespace Sahand.ClinicPro.API.Logging;

/// <summary>
/// قالب‌بند کنسولِ سازگار با فارسی — چرخهٔ سی‌امین؛ بازطراحی حرفه‌ای چرخهٔ سی‌وهشتم
/// (گزارش مالک: «خروجی لاگ سرور API رنگ‌بندی شده و قالب‌بندی شده در بیار تا
/// زیبا و حرفه‌ای دیده بشه»).
///
/// چیدمان هر خط:
///   [HH:mm:ss] [بج سطحِ رنگیِ پس‌زمینه‌دار] پیام  [منبع]
///   ─────────  ──────────              ─────  ──────
///   زمان محلی   مثلاً « ERR » سفید روی قرمز  متن   SourceContext کم‌رنگ
///
/// درخواست‌های HTTP (رویدادهای UseSerilogRequestLogging) خط ویژه می‌گیرند:
///   [14:04:05] [INF] ← GET /api/v1/patients → 200 · 45ms
/// کد وضعیت رنگ می‌گیرد: ۲xx سبز، ۳xx فیروزه‌ای، ۴xx کهربایی، ۵xx قرمز.
///
/// رفتارهای سازگاری (حفظ از چرخهٔ سی‌ام):
///  • در conhost ویندوز (بدون موتور شکل‌دهی): متن پیام فارسی با RtlConsoleShaper
///    به «حروف متصل + چینش بصری راست‌به‌چپ» تبدیل می‌شود؛
///  • در ترمینال‌های مدرن و خروجی ریدایرکت‌شده: متن خام منطقی — فایل لاگ
///    (Logs/api-*.log) و Seq همیشه متن سالم دارند؛
///  • کدهای ANSI رنگ فقط وقتی خروجی مستقیم به کنسول واقعی است (ریدایرکت = تمیز).
/// پیشوند زمان/سطح/کد وضعیت همیشه لاتین و ستونی می‌ماند — خوانایی ستونی لاگ.
/// </summary>
public sealed class PersianConsoleTextFormatter : ITextFormatter
{
    /// <summary>
    /// تشخیص «کنسول بدون شکل‌دهی» هنگام ساخت (برای کل عمر پردازه پایدار
    /// است — کنسول در میانهٔ اجرا عوض نمی‌شود).
    /// </summary>
    private readonly bool _rtlShaping = DetectLegacyConhost();

    /// <summary>رنگ فقط برای کنسول واقعی — خروجی ریدایرکت‌شده باید تمیز باشد</summary>
    private readonly bool _ansiColors = !Console.IsOutputRedirected;

    /// <summary>سازندهٔ بدون پارامتر — لازم برای اتصال از appsettings.json</summary>
    public PersianConsoleTextFormatter()
    {
    }

    /// <summary>قالب‌بندی یک رویداد لاگ برای کنسول — بج رنگی + پیام شکل‌گرفته</summary>
    public void Format(LogEvent logEvent, TextWriter output)
    {
        // ── زمان محلی ──
        var ts = logEvent.Timestamp.ToLocalTime()
            .ToString("HH:mm:ss", CultureInfo.InvariantCulture);

        // ── درخواست HTTP؟ رویدادهای SerilogRequestLoading مشخصه‌های ساخت‌یافته دارند ──
        var http = TryRenderHttpRequest(logEvent);

        WriteTimestamp(output, ts);
        WriteLevelBadge(output, logEvent.Level);

        // ── متن پیام (یا خط ویژهٔ درخواست HTTP) ──
        if (http is not null)
            output.Write(http);
        else
            WriteRtlAware(output, logEvent.RenderMessage());

        // ── منبع رویداد (SourceContext) — کم‌رنگ، انتهای خط ──
        if (_ansiColors)
        {
            if (logEvent.Properties.TryGetValue("SourceContext", out var ctx)
                && ctx.ToString() is { Length: > 2 } ctxText)
            {
                // مقدار خصوصیت سریلوگ با کوتیشن می‌آید — برداشتنش برای نمایش
                var name = ctxText.Trim('"');
                output.Write("  ");
                output.Write("\u001b[90m[");
                output.Write(name);
                output.Write("]\u001b[0m");
            }
        }
        output.WriteLine();

        // ── استثنا — بلوک جدا با نوار قرمز؛ ردپای stack لاتین است و شکل‌دهی نمی‌خواهد ──
        if (logEvent.Exception is not null)
        {
            var exText = logEvent.Exception.ToString().TrimEnd('\r', '\n');
            var lines = exText.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (_ansiColors)
                {
                    output.Write(i == 0 ? "\u001b[1;91m✖ " : "\u001b[91m│ ");
                }
                output.Write(lines[i].TrimEnd('\r'));
                if (_ansiColors) output.Write("\u001b[0m");
                output.WriteLine();
            }
        }
    }

    /// <summary>
    /// خط ویژهٔ درخواست HTTP از مشخصه‌های SerilogRequestLogging —
    /// «← GET /api/v1/patients → 200 · 45ms» با رنگ کد وضعیت؛
    /// تهی یعنی رویداد درخواست نیست.
    /// </summary>
    private string? TryRenderHttpRequest(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("RequestMethod", out var methodProp)
            || !logEvent.Properties.TryGetValue("Path", out var pathProp))
            return null;

        var method = methodProp.ToString().Trim('"');
        var path = pathProp.ToString().Trim('"');

        var status = logEvent.Properties.TryGetValue("StatusCode", out var statusProp)
            && int.TryParse(statusProp.ToString(), out var code) ? code : (int?)null;

        var elapsed = logEvent.Properties.TryGetValue("Elapsed", out var elapsedProp)
            && double.TryParse(elapsedProp.ToString().Trim('"'), out var ms)
            ? $"{ms:0}ms" : null;

        // رنگ کد وضعیت — همان خانوادهٔ رنگی ابزارهای حرفه‌ای
        var statusColor = status switch
        {
            >= 500 => "\u001b[1;91m",                     // قرمز پررنگ
            >= 400 => "\u001b[93m",                       // کهربایی روشن
            >= 300 => "\u001b[96m",                       // فیروزه‌ای روشن
            >= 200 => "\u001b[92m",                       // سبز روشن
            _ => "",
        };

        var line = $"← {method} {path}";
        if (status is { } sc)
            line += $" → {sc}";
        if (elapsed is not null)
            line += $" · {elapsed}";

        if (!_ansiColors || status is null)
            return line;

        // رنگ فقط روی بخش «→ 200 · 45ms» — مسیر خنثی می‌ماند
        var neutralPart = $"← {method} {path} → ";
        return neutralPart + statusColor + $"{sc}" + (elapsed is null ? "" : $" · {elapsed}") + "\u001b[0m";
    }

    /// <summary>
    /// نوشتن متن با پردازش خط‌به‌خط: هر خط مستقل ارزیابی می‌شود؛ خط‌های
    /// بدون هیچ نویسهٔ راست‌به‌چپ (انگلیسی/عددی خالص — مثل ردپای stack)
    /// دست‌نخورده می‌مانند.
    /// </summary>
    private void WriteRtlAware(TextWriter output, string text)
    {
        if (!_rtlShaping)
        {
            output.Write(text);
            return;
        }

        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                output.Write('\n');
            output.Write(RtlConsoleShaper.ToVisualOrder(lines[i].TrimEnd('\r')));
        }
    }

    /// <summary>نوشتن زمان — همیشه چپ‌به‌راست و کم‌رنگ (ستون زمان)</summary>
    private void WriteTimestamp(TextWriter output, string ts)
    {
        if (!_ansiColors)
        {
            output.Write('[');
            output.Write(ts);
            output.Write("] ");
            return;
        }

        output.Write("\u001b[90m[");
        output.Write(ts);
        output.Write("]\u001b[0m ");
    }

    /// <summary>
    /// بج سطح — پس‌زمینهٔ رنگی با متن سفید/سیاه و هم‌عرض (۳ نویسه) تا ستون‌ها
    /// همیشه هم‌تراز بمانند؛ مثلاً « ERR » سفید روی قرمز. خوانایی در یک نگاه
    /// بدون خواندن متن — همان چیزی که لاگ حرفه‌ای می‌کند.
    /// </summary>
    private void WriteLevelBadge(TextWriter output, LogEventLevel level)
    {
        var lvl = LevelAbbreviation(level);
        if (!_ansiColors)
        {
            output.Write(lvl);
            output.Write("  ");
            return;
        }

        const string reset = "\u001b[0m";
        var (badge, width) = level switch
        {
            LogEventLevel.Fatal => ("\u001b[97;101m", 3),   // سفید روی قرمز پررنگ
            LogEventLevel.Error => ("\u001b[97;41m", 3),    // سفید روی قرمز
            LogEventLevel.Warning => ("\u001b[93;43m", 3),  // کهربایی روشن روی کهربایی
            LogEventLevel.Information => ("\u001b[97;44m", 3), // سفید روی آبی
            LogEventLevel.Debug => ("\u001b[90;100m", 3),   // خاکستری روی خاکستری روشن
            LogEventLevel.Verbose => ("\u001b[90;100m", 3),
            _ => ("\u001b[97;44m", 3),
        };

        output.Write(badge);
        output.Write(' ');
        output.Write(lvl);
        output.Write(' ');
        output.Write(reset);
        output.Write(' ');
    }

    /// <summary>مخفف سه‌حرفی سطح — هم‌ارز قالب {Level:u3} سریلوگ</summary>
    private static string LevelAbbreviation(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "VRB",
        LogEventLevel.Debug => "DBG",
        LogEventLevel.Information => "INF",
        LogEventLevel.Warning => "WRN",
        LogEventLevel.Error => "ERR",
        LogEventLevel.Fatal => "FTL",
        _ => "???",
    };

    /// <summary>
    /// تشخیص «کنسول بدون شکل‌دهی» (conhost): فقط ویندوز + خروجی مستقیم به
    /// کنسول + نبودِ نشانه‌های ترمینال‌های مدرن. Windows Terminal متغیر
    /// WT_SESSION، VS Code متغیر TERM_PROGRAM و پایانه‌های POSIX متغیر
    /// TERM را تنظیم می‌کنند؛ در conhost (اجرا با دابل‌کلیک روی exe،
    /// dotnet run یا کنسول قدیمی ویژوال استودیو) هیچ‌کدام وجود ندارد.
    /// هر خطا در تشخیص → متن خام (مسیر امن — هرگز دوبار معکوس نمی‌شود).
    /// </summary>
    private static bool DetectLegacyConhost()
    {
        try
        {
            if (!OperatingSystem.IsWindows() || Console.IsOutputRedirected)
                return false;

            return Environment.GetEnvironmentVariable("WT_SESSION") is null
                   && Environment.GetEnvironmentVariable("TERM_PROGRAM") is null
                   && Environment.GetEnvironmentVariable("TERM") is null;
        }
        catch
        {
            return false;
        }
    }
}

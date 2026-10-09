using System.Globalization;
using Serilog.Events;
using Serilog.Formatting;

namespace Sahand.ClinicPro.API.Logging;

/// <summary>
/// قالب‌بند کنسولِ سازگار با فارسی — چرخهٔ سی‌امین.
///
/// چیدمان خروجی مثل قالب استاندارد سریلوگ است — «[HH:mm:ss LVL] پیام» —
/// و رنگِ سطح با کدهای ANSI؛ تفاوتِ کلیدی در پیام‌های فارسی است:
///  • در conhost ویندوز (کنسولِ بدون موتور شکل‌دهی): متن پیام با
///    RtlConsoleShaper به «حروفِ متصل + چینش بصری راست‌به‌چپ» تبدیل
///    می‌شود تا «راه‌اندازی» دیگر به‌صورت «ی ز ا د ن ا ه ا ر» روی صفحه
///    ننشیند؛
///  • در ترمینال‌های مدرن (Windows Terminal/VS Code/پایانه‌های POSIX) و
///    خروجی ریدایرکت‌شده: متن خامِ منطقی بی‌کم‌وتشدید چاپ می‌شود — فایل
///    لاگ (Logs/api-*.log) و Seq همیشه متن منطقی سالم دارند.
/// پیشوند زمان/سطح همیشه چپ‌به‌راست و ستونی می‌ماند — زمان و سطح همیشه
/// لاتین‌اند و ستون‌بندیِ خوانایی لاگ را حفظ می‌کنند.
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

    /// <summary>قالب‌بندی یک رویداد لاگ برای کنسول — پیشوند LTL + پیام شکل‌گرفته</summary>
    public void Format(LogEvent logEvent, TextWriter output)
    {
        // ── پیشوند چپ‌به‌راست: [HH:mm:ss LVL] ──
        var ts = logEvent.Timestamp.ToLocalTime()
            .ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        var lvl = LevelAbbreviation(logEvent.Level);
        WritePrefix(output, ts, lvl, logEvent.Level);

        // ── متن پیام ──
        WriteRtlAware(output, logEvent.RenderMessage());
        output.WriteLine();

        // ── استثنا در خطوط بعدی — بدون پیشوند (مثل قالب استاندارد سریلوگ) ──
        if (logEvent.Exception is not null)
        {
            var exText = logEvent.Exception.ToString().TrimEnd('\r', '\n');
            WriteRtlAware(output, exText);
            output.WriteLine();
        }
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

    /// <summary>نوشتن پیشوند زمان/سطح — همیشه چپ‌به‌راست؛ رنگ سطح فقط با کنسول واقعی</summary>
    private void WritePrefix(TextWriter output, string ts, string lvl, LogEventLevel level)
    {
        if (!_ansiColors)
        {
            output.Write('[');
            output.Write(ts);
            output.Write(' ');
            output.Write(lvl);
            output.Write("] ");
            return;
        }

        // کدهای ANSI — همان خوانایی رنگیِ سینک کنسول استاندارد سریلوگ
        const string reset = "\u001b[0m";
        const string dim = "\u001b[90m";
        var levelColor = level switch
        {
            LogEventLevel.Fatal => "\u001b[1;91m",                    // قرمز پررنگ
            LogEventLevel.Error => "\u001b[91m",                      // قرمز روشن
            LogEventLevel.Warning => "\u001b[93m",                    // زرد روشن
            LogEventLevel.Debug or LogEventLevel.Verbose => dim,      // خاکستری
            _ => "\u001b[97m",                                        // اطلاعات — سفید روشن
        };

        output.Write(dim);
        output.Write('[');
        output.Write(ts);
        output.Write(' ');
        output.Write(levelColor);
        output.Write(lvl);
        output.Write(reset);
        output.Write(dim);
        output.Write("] ");
        output.Write(reset);
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

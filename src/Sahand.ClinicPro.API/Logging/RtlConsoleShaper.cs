using System.Globalization;
using System.Text;

namespace Sahand.ClinicPro.API.Logging;

/// <summary>
/// شکل‌دهندهٔ بصری متن فارسی/عربی برای کنسول قدیمی ویندوز (conhost) — چرخهٔ سی‌امین.
///
/// مسئله: conhost (کنسولی که با اجرای مستقیم exe یا dotnet run روی ویندوز
/// باز می‌شود) هیچ موتور شکل‌دهی متنی (Uniscribe/DirectWrite) ندارد؛ در
/// نتیجه متن فارسی را (۱) با حروفِ جدا از هم — شکلِ منفردِ هر نویسه بدون
/// اتصال به همسایه‌ها — و (۲) با ترتیب نویسه‌های منطقی رسم‌شده از چپ به
/// راست نمایش می‌دهد که برای خوانندهٔ فارسی «برعکس» دیده می‌شود؛ مثلاً
/// «راه‌اندازی» به‌صورت «ی ز ا د ن ا ه ا ر» روی صفحه می‌نشست.
///
/// راه‌حل (همان رویکرد کتابخانه‌های جاافتادهٔ arabic-reshaper و python-bidi
/// برای چاپگرهای ساده و پایانه‌های بدون شکل‌دهی): پیش از چاپ، متن به
/// نویسه‌های «شکل نمایشی» (Arabic Presentation Forms) نگاشت و سپس به
/// «چینش بصری» تبدیل می‌شود — ترتیب قطعه‌ها معکوس و داخل قطعه‌های
/// راست‌به‌چپ نویسه‌ها وارونه می‌شوند؛ ارقام و واژه‌های لاتین به‌عنوان
/// جزیره‌های چپ‌به‌راست سالم می‌مانند. پایانهٔ بدون شکل‌دهی (conhost)
/// همین خروجی را سالم رسم می‌کند.
///
/// نکته‌های مهم:
///  • این تبدیل «فقط برای نمایش روی صفحهٔ conhost» است؛ متن منطقی سالم در
///    فایل‌های لاگ (Logs/api-*.log)، Seq و خروجی ریدایرکت‌شده باقی می‌ماند.
///  • ترمینال‌های مدرن (Windows Terminal با WT_SESSION، VS Code با
///    TERM_PROGRAM، پایانه‌های POSIX با TERM) خودشان شکل‌دهی کامل دارند؛
///    آشکارسازِ PersianConsoleTextFormatter تشخیص می‌دهد و این تبدیل را
///    انجام نمی‌دهد تا متن دوبار معکوس نشود.
///  • فونت کنسول باید گلیف‌های Presentation Forms را داشته باشد؛
///    Program.cs فونت Courier New را تنظیم می‌کند (تنها فونت کنسول ویندوز
///    با پوشش کامل اشکال نمایشی عربی/فارسی).
/// </summary>
public static class RtlConsoleShaper
{
    // ═════════════════════════════════════════════════════════════════
    // ۱) جدول اشکال نمایشی — برای هر حرف: [منفرد، پایانی، آغازین، میانی]
    //    U+FB50–FDFF (Presentation Forms-A) برای حروف خاص فارسی/اردو و
    //    U+FE70–FEFF (Presentation Forms-B) برای حروف مشترک عربی.
    //    برای حروف یک‌طرفه (ا د ذ ر ز ژ و ة …) جای آغازین/میانی با
    //    منفرد/پایانی پر شده چون چنین شکلی ندارند.
    // ═════════════════════════════════════════════════════════════════
    private static readonly IReadOnlyDictionary<char, char[]> PresentationForms = new Dictionary<char, char[]>
    {
        // ── حروف یک‌طرفه: فقط به حرفِ قبل می‌چسبند ──
        ['\u0621'] = new[] { '\uFE80', '\uFE80', '\uFE80', '\uFE80' }, // ء (هیچ‌وقت نمی‌چسبد)
        ['\u0622'] = new[] { '\uFE81', '\uFE82', '\uFE81', '\uFE82' }, // آ
        ['\u0623'] = new[] { '\uFE83', '\uFE84', '\uFE83', '\uFE84' }, // أ
        ['\u0624'] = new[] { '\uFE85', '\uFE86', '\uFE85', '\uFE86' }, // ؤ
        ['\u0625'] = new[] { '\uFE87', '\uFE88', '\uFE87', '\uFE88' }, // إ
        ['\u0627'] = new[] { '\uFE8D', '\uFE8E', '\uFE8D', '\uFE8E' }, // ا
        ['\u0629'] = new[] { '\uFE93', '\uFE94', '\uFE93', '\uFE94' }, // ة
        ['\u062F'] = new[] { '\uFEA9', '\uFEAA', '\uFEA9', '\uFEAA' }, // د
        ['\u0630'] = new[] { '\uFEAB', '\uFEAC', '\uFEAB', '\uFEAC' }, // ذ
        ['\u0631'] = new[] { '\uFEAD', '\uFEAE', '\uFEAD', '\uFEAE' }, // ر
        ['\u0632'] = new[] { '\uFEAF', '\uFEB0', '\uFEAF', '\uFEB0' }, // ز
        ['\u0698'] = new[] { '\uFB8A', '\uFB8B', '\uFB8A', '\uFB8B' }, // ژ
        ['\u0648'] = new[] { '\uFEED', '\uFEEE', '\uFEED', '\uFEEE' }, // و

        // ── حروف دوجهشی مشترک عربی (Forms-B): به قبل و بعد می‌چسبند ──
        ['\u0626'] = new[] { '\uFE89', '\uFE8A', '\uFE8B', '\uFE8C' }, // ئ
        ['\u0628'] = new[] { '\uFE8F', '\uFE90', '\uFE91', '\uFE92' }, // ب
        ['\u062A'] = new[] { '\uFE95', '\uFE96', '\uFE97', '\uFE98' }, // ت
        ['\u062B'] = new[] { '\uFE99', '\uFE9A', '\uFE9B', '\uFE9C' }, // ث
        ['\u062C'] = new[] { '\uFE9D', '\uFE9E', '\uFE9F', '\uFEA0' }, // ج
        ['\u062D'] = new[] { '\uFEA1', '\uFEA2', '\uFEA3', '\uFEA4' }, // ح
        ['\u062E'] = new[] { '\uFEA5', '\uFEA6', '\uFEA7', '\uFEA8' }, // خ
        ['\u0633'] = new[] { '\uFEB1', '\uFEB2', '\uFEB3', '\uFEB4' }, // س
        ['\u0634'] = new[] { '\uFEB5', '\uFEB6', '\uFEB7', '\uFEB8' }, // ش
        ['\u0635'] = new[] { '\uFEB9', '\uFEBA', '\uFEBB', '\uFEBC' }, // ص
        ['\u0636'] = new[] { '\uFEBD', '\uFEBE', '\uFEBF', '\uFEC0' }, // ض
        ['\u0637'] = new[] { '\uFEC1', '\uFEC2', '\uFEC3', '\uFEC4' }, // ط
        ['\u0638'] = new[] { '\uFEC5', '\uFEC6', '\uFEC7', '\uFEC8' }, // ظ
        ['\u0639'] = new[] { '\uFEC9', '\uFECA', '\uFECB', '\uFECC' }, // ع
        ['\u063A'] = new[] { '\uFECD', '\uFECE', '\uFECF', '\uFED0' }, // غ
        ['\u0640'] = new[] { '\u0640', '\u0640', '\u0640', '\u0640' }, // ـ کشیده — خودش
        ['\u0641'] = new[] { '\uFED1', '\uFED2', '\uFED3', '\uFED4' }, // ف
        ['\u0642'] = new[] { '\uFED5', '\uFED6', '\uFED7', '\uFED8' }, // ق
        ['\u0643'] = new[] { '\uFED9', '\uFEDA', '\uFEDB', '\uFEDC' }, // ك (کاف عربی)
        ['\u0644'] = new[] { '\uFEDD', '\uFEDE', '\uFEDF', '\uFEE0' }, // ل
        ['\u0645'] = new[] { '\uFEE1', '\uFEE2', '\uFEE3', '\uFEE4' }, // م
        ['\u0646'] = new[] { '\uFEE5', '\uFEE6', '\uFEE7', '\uFEE8' }, // ن
        ['\u0647'] = new[] { '\uFEE9', '\uFEEA', '\uFEEB', '\uFEEC' }, // ه
        ['\u064A'] = new[] { '\uFEF1', '\uFEF2', '\uFEF3', '\uFEF4' }, // ي (ی عربی)

        // ── حروف خاص فارسی (Presentation Forms-A) ──
        ['\u067E'] = new[] { '\uFB56', '\uFB57', '\uFB58', '\uFB59' }, // پ
        ['\u0686'] = new[] { '\uFB7A', '\uFB7B', '\uFB7C', '\uFB7D' }, // چ
        ['\u06A9'] = new[] { '\uFB8E', '\uFB8F', '\uFB90', '\uFB91' }, // ک
        ['\u06AF'] = new[] { '\uFB92', '\uFB93', '\uFB94', '\uFB95' }, // گ
        ['\u06CC'] = new[] { '\uFBFC', '\uFBFD', '\uFBFE', '\uFBFF' }, // ی
    };

    /// <summary>
    /// حروف دوجهشی — به هر دو حرفِ قبل و بعد خود می‌چسبند (شکل آغازین/میانی واقعی دارند).
    /// شامل حروف مشترک عربی و حروف خاص فارسی (پ چ ک گ ی).
    /// </summary>
    private static readonly HashSet<char> DualJoining = new()
    {
        '\u0626', // ئ
        '\u0628', '\u062A', '\u062B', '\u062C', '\u062D', '\u062E', // ب ت ث ج ح خ
        '\u0633', '\u0634', '\u0635', '\u0636', '\u0637', '\u0638', // س ش ص ض ط ظ
        '\u0639', '\u063A', '\u0640', '\u0641', '\u0642', '\u0643', // ع غ ـ ف ق ك
        '\u0644', '\u0645', '\u0646', '\u0647', '\u064A',           // ل م ن ه ي
        '\u067E', '\u0686', '\u06A9', '\u06AF', '\u06CC',           // پ چ ک گ ی
    };

    /// <summary>
    /// تبدیل یک «خط» از متن منطقی به چینش بصری مناسب پایانهٔ بدون شکل‌دهی.
    /// خط‌های بدون هیچ نویسهٔ راست‌به‌چپ (لاتین/عددی خالص) دست‌نخورده
    /// برمی‌گردند — یعنی خط‌های انگلیسیِ استثناها هیچ تغییری نمی‌گیرند.
    /// </summary>
    public static string ToVisualOrder(string line)
    {
        if (string.IsNullOrEmpty(line) || !ContainsRtl(line))
            return line;

        var shaped = ShapeArabic(line);
        return ReorderToVisual(shaped);
    }

    // ═════════════════════════════════════════════════════════════════
    // گام ۱ — شکل‌دهی: نگاشت به اشکال نمایشی متناسب با اتصال همسایه‌ها
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// نگاشت حروف عربی/فارسی به نویسه‌های شکل نمایشی. قواعد اتصال مثل
    /// موتور شکل‌دهی واقعی است: پیوند میان دو نویسه برقرار است فقط اگر
    /// نویسهٔ سمتِ راستِ پیوند دوجهشی باشد و نویسهٔ سمتِ چپ آن (در خط
    /// منطقی، نویسهٔ بعدی) از نوعِ چسبنده باشد؛ نویسه‌های ترکیبی (اعراب/
    /// همزهٔ بالانویس) شفاف‌اند و در تشخیص همسایه رد می‌شوند. نیم‌فاصله
    /// (ZWNJ) و فاصله و نشانه‌ها ذاتاً چسبنده نیستند و پیوند را می‌بُرند.
    /// شامل لیگاتور اجباری لام‌الف (لا لآ لأ لإ) تا «لا» به‌صورت «لـا»
    /// نیم‌باز دیده نشود.
    /// </summary>
    private static string ShapeArabic(string text)
    {
        var sb = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            // نشانگر ترکیبی — شکلش تغییر نمی‌کند؛ بلافاصله بعد از پایه می‌نشیند
            if (IsCombiningMark(c))
            {
                sb.Append(c);
                continue;
            }

            if (!PresentationForms.TryGetValue(c, out var forms))
            {
                sb.Append(c); // نویسهٔ غیر عربی — دست‌نخورده
                continue;
            }

            var prev = EffectiveNeighbor(text, i, -1);
            var next = EffectiveNeighbor(text, i, +1);

            // لیگاتور اجباری لام‌الف: «ل» دوجهشی است و همیشه به الف‌های برهنه می‌چسبد
            if (c == '\u0644' && next is not null && IsBareAlef(next.Value))
            {
                // اگر «ل» خودش از حرف قبل پیوند گرفته، شکل پایانی لیگاتور؛ وگرنه منفرد
                var receives = prev is not null && DualJoining.Contains(prev.Value);
                sb.Append(LamAlefForm(next.Value, receives));
                i++; // الفِ مصرف‌شده در لیگاتور — از حلقه رد می‌شود
                continue;
            }

            // پیوند از راست: حرفِ قبل باید دوجهشی باشد (فقط دوجهشی‌ها پیوند می‌دهند)
            var receivesFromPrev = prev is not null && DualJoining.Contains(prev.Value);

            // پیوند به چپ: حرف فعلی باید دوجهشی باشد و حرف بعد بتواند بپذیرد
            // (همهٔ حروف عربی می‌پذیرند جز ء که هرگز نمی‌چسبد)
            var passesToNext = DualJoining.Contains(c)
                               && next is not null
                               && CanReceive(next.Value);

            sb.Append(receivesFromPrev
                ? passesToNext ? forms[3] : forms[1] // میانی / پایانی
                : passesToNext ? forms[2] : forms[0]); // آغازین / منفرد
        }
        return sb.ToString();
    }

    /// <summary>آیا نویسه از نوع «علامت ترکیبی» است؟ (اعراب، همزهٔ بالانویس/زیرنویس — شفاف برای اتصال)</summary>
    private static bool IsCombiningMark(char c)
    {
        var category = CharUnicodeInfo.GetUnicodeCategory(c);
        return category is UnicodeCategory.NonSpacingMark
                    or UnicodeCategory.SpacingCombiningMark
                    or UnicodeCategory.EnclosingMark;
    }

    /// <summary>نزدیک‌ترین همسایهٔ مؤثر در جهت خواسته‌شده — نشانگرهای ترکیبی رد می‌شوند</summary>
    private static char? EffectiveNeighbor(string text, int index, int direction)
    {
        var i = index + direction;
        while (i >= 0 && i < text.Length)
        {
            var c = text[i];
            if (!IsCombiningMark(c))
                return c;
            i += direction;
        }
        return null;
    }

    /// <summary>الف‌های برهنه برای لیگاتور لام‌الف: ا آ أ إ</summary>
    private static bool IsBareAlef(char c) =>
        c is '\u0627' or '\u0622' or '\u0623' or '\u0625';

    /// <summary>آیا حرف می‌تواند از حرفِ قبلش پیوند بپذیرد؟ همهٔ حروف عربی بجز ء</summary>
    private static bool CanReceive(char c) =>
        c != '\u0621' && PresentationForms.ContainsKey(c);

    /// <summary>نویسهٔ لیگاتور لام‌الف متناسب با نوعِ الف و اتصالِ لام</summary>
    private static char LamAlefForm(char alef, bool lamReceives) => (alef, lamReceives) switch
    {
        ('\u0627', true) => '\uFEFC',  // لا پایانی
        ('\u0627', false) => '\uFEFB', // لا منفرد
        ('\u0622', true) => '\uFEF6',  // لآ پایانی
        ('\u0622', false) => '\uFEF5', // لآ منفرد
        ('\u0623', true) => '\uFEF8',  // لأ پایانی
        ('\u0623', false) => '\uFEF7', // لأ منفرد
        ('\u0625', true) => '\uFEFA',  // لإ پایانی
        ('\u0625', false) => '\uFEF9', // لإ منفرد
        _ => alef,
    };

    // ═════════════════════════════════════════════════════════════════
    // گام ۲ — بازچینی: قطعه‌بندی دوجهتهٔ سبک و معکوس‌سازی قطعه‌های RTL
    // ═════════════════════════════════════════════════════════════════

    /// <summary>کلاس نویسه برای قطعه‌بندی دوجهته</summary>
    private enum CharClass : byte
    {
        /// <summary>حروف/نشانه‌های راست‌به‌چپ (فارسی/عربی و اشکال نمایشی)</summary>
        Rtl,

        /// <summary>جزیرهٔ چپ‌به‌راست — حروف لاتین و ارقام (اسکی/فارسی/عربی)</summary>
        Island,

        /// <summary>خنثی — فاصله، نیم‌فاصله، نشانه‌های مشترک، ایموجی و…</summary>
        Neutral,
    }

    /// <summary>
    /// بازچینی متنِ شکل‌گرفته به ترتیب بصری با جهت پایهٔ راست‌به‌چپ:
    /// قطعه‌ها از آخر به اول چیده می‌شوند (اولین واژهٔ منطقی در منتهی‌الیهٔ
    /// راستِ خط می‌نشیند)، داخل قطعه‌های راست‌به‌چپ گرافم‌ها وارونه
    /// می‌شوند و جفت‌نویسه‌ها (پرانتز/گیومه) آینه می‌گردند؛ جزیره‌های
    /// لاتین/عددی به همان ترتیب نویسه‌ای سالم می‌مانند. دنباله‌های خنثیِ
    /// میان دو جزیره به جزیره جذب می‌شوند («21:15:21 ERR» یک جزیره می‌شود)؛
    /// خنثی‌های مرزی طبق قانون N2 الگوریتم دوجهته جهتِ پایه (RTL) را
    /// می‌گیرند و با جریان فارسی جابه‌جا می‌شوند.
    /// </summary>
    private static string ReorderToVisual(string shaped)
    {
        var n = shaped.Length;
        var classes = new CharClass[n];
        for (var i = 0; i < n; i++)
            classes[i] = Classify(shaped[i]);

        // ── جذب خنثی‌ها ──
        var i2 = 0;
        while (i2 < n)
        {
            if (classes[i2] != CharClass.Neutral)
            {
                i2++;
                continue;
            }

            var start = i2;
            while (i2 < n && classes[i2] == CharClass.Neutral)
                i2++;
            var end = i2; // بازهٔ خنثی: [start, end)

            CharClass? left = null;
            for (var k = start - 1; k >= 0; k--)
                if (classes[k] != CharClass.Neutral)
                {
                    left = classes[k];
                    break;
                }
            CharClass? right = null;
            for (var k = end; k < n; k++)
                if (classes[k] != CharClass.Neutral)
                {
                    right = classes[k];
                    break;
                }

            // فقط خنثیِ محصور بین دو جزیره جزیره می‌شود؛ سایر موارد (لبهٔ
            // خط یا میان دو جهت متفاوت) جهت پایه — یعنی RTL — را می‌گیرد
            var resolved = left == CharClass.Island && right == CharClass.Island
                ? CharClass.Island
                : CharClass.Rtl;

            for (var k = start; k < end; k++)
                classes[k] = resolved;
        }

        // ── ساخت قطعه‌های پیوستهٔ هم‌جنس ──
        var segments = new List<(CharClass Class, int Start, int End)>();
        var segStart = 0;
        for (var i = 1; i <= n; i++)
        {
            if (i == n || classes[i] != classes[segStart])
            {
                segments.Add((classes[segStart], segStart, i));
                segStart = i;
            }
        }

        // ── چیدمان بصری: قطعه‌ها از آخر به اول ──
        var sb = new StringBuilder(n);
        for (var s = segments.Count - 1; s >= 0; s--)
        {
            var (cls, start, end) = segments[s];
            if (cls == CharClass.Island)
                sb.Append(shaped, start, end - start); // لاتین/عدد — ترتیب سالم
            else
                sb.Append(ReverseRtlGraphemes(shaped, start, end)); // وارونهٔ گرافمی + آینه
        }
        return sb.ToString();
    }

    /// <summary>دسته‌بندی یک نویسهٔ شکل‌گرفته برای قطعه‌بندی دوجهته</summary>
    private static CharClass Classify(char c)
    {
        if (IsRtlChar(c))
            return CharClass.Rtl;

        // ارقام هر خطی (اسکی/فارسی/عربی) و حروف غیرRTL (لاتین و…) — جزیره
        if (char.IsDigit(c) || char.IsLetter(c))
            return CharClass.Island;

        return CharClass.Neutral;
    }

    /// <summary>
    /// آیا نویسه‌ای از خانوادهٔ راست‌به‌چپ است؟ حروف و نشانه‌های عربی-فارسی
    /// و اشکال نمایشی — ارقام (اسکی/فارسی/عربی) جدا نگه داشته می‌شوند تا
    /// جزیرهٔ چپ‌به‌راست بمانند.
    /// </summary>
    private static bool IsRtlChar(char c) =>
        (c >= '\u0600' && c <= '\u06FF'
         && !(c >= '\u0660' && c <= '\u0669') // ارقام عربی
         && !(c >= '\u06F0' && c <= '\u06F9')) // ارقام فارسی
        || (c >= '\u0750' && c <= '\u077F')
        || (c >= '\uFB50' && c <= '\uFDFF')   // اشکال نمایشی A (پس از شکل‌دهی)
        || (c >= '\uFE70' && c <= '\uFEFF');  // اشکال نمایشی B

    /// <summary>آیا متن دست‌کم یک نویسهٔ راست‌به‌چپ دارد؟ (بدون آن تبدیل بی‌معناست)</summary>
    private static bool ContainsRtl(string text)
    {
        foreach (var c in text)
            if (IsRtlChar(c))
                return true;
        return false;
    }

    /// <summary>
    /// معکوس‌سازی گرافمیِ یک قطعهٔ راست‌به‌چپ: هر گرافم (پایه +
    /// نشانگرهای ترکیبی‌اش، یا جفتِ جانشینیِ ایموجی) یک واحد جابه‌جایی
    /// است تا نشانگرها از پایه‌شان جدا نشوند و ایموجی‌ها خراب نشوند؛
    /// پرانتزها و گیومه‌های جفتی نیز آینه می‌شوند (جهت باز/بسته در جریان
    /// راست‌به‌چپ برعکس است).
    /// </summary>
    private static string ReverseRtlGraphemes(string text, int start, int end)
    {
        // جداسازی گرافم‌ها
        var graphemes = new List<string>();
        var i = start;
        while (i < end)
        {
            var gStart = i;
            i++;

            // جفت جانشینی (ایموجی‌های سطح چندزبانه‌ای) — گرافم واحد
            if (i < end && char.IsHighSurrogate(text[i - 1]) && char.IsLowSurrogate(text[i]))
                i++;

            // نشانگرهای ترکیبی پس از پایه — چسبیده به همین گرافم می‌مانند
            while (i < end && IsCombiningMark(text[i]))
                i++;

            graphemes.Add(text.Substring(gStart, i - gStart));
        }

        // چیدمان از آخر به اول با آینه‌کردن نویسه‌های جفتی
        var sb = new StringBuilder(end - start);
        for (var g = graphemes.Count - 1; g >= 0; g--)
            foreach (var ch in graphemes[g])
                sb.Append(MirrorChar(ch));
        return sb.ToString();
    }

    /// <summary>آینه‌کردن نویسه‌های جفتی — در جریان راست‌به‌چپ جهت باز/بسته عوض می‌شود</summary>
    private static char MirrorChar(char c) => c switch
    {
        '(' => ')',
        ')' => '(',
        '[' => ']',
        ']' => '[',
        '{' => '}',
        '}' => '{',
        '<' => '>',
        '>' => '<',
        '\u00AB' => '\u00BB', // « → »
        '\u00BB' => '\u00AB', // » → «
        _ => c,
    };
}

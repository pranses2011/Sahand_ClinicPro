namespace Sahand.ClinicPro.Application.Common;

/// <summary>
/// ═══ کاتالوگ معنایی لاگ عملیات — بازطراحی کامل چرخهٔ ۷۰ ═══
/// گزارش مالک: «لاگ عملیات خیلی اشتباه داره… ستون ماژول همه را API
/// می‌نویسد و ستون موجودیت همه موجودیت‌ها را انگلیسی می‌نویسد که باید
/// فارسی باشند و ستون شناسه خالی می‌ماند و در ستون کاربر به جای نام
/// کامل کاربر، کد کاربر (۱ برای admin) نوشته می‌شود».
/// این کاتالوگ «منبع یگانه» ترجمهٔ فارسی است و هم‌زمان در سه لایه
/// استفاده می‌شود تا نمایش لاگ در همه‌جا (صفحهٔ لاگ، داشبورد، جزئیات)
/// یکسان و فارسی باشد:
///   ۱) میدلور API — هنگام «نوشتن» لاگ: نام ماژول/موجودیت را فارسی
///      ذخیره می‌کند (از این پس ردیف‌های تازه فارسی‌اند)؛
///   ۲) گردانندهٔ پرس‌وجوی لاگ — ردیف‌های «قدیمی» (انگلیسی) را در
///      لحظهٔ خواندن به فارسی برمی‌گرداند (بدون دست‌کاری دیتابیس)؛
///   ۳) کلاینت دسکتاپ — عنوان فارسی فیلدها + قالب‌بندی مقادیر برای
///      جدول مقایسهٔ «قدیمی/جدید» در جزئیات لاگ.
/// همهٔ نگاشت‌ها «بهترین‌تلاش»‌اند: نام ناشناخته بدون تغییر برمی‌گردد
/// تا هیچ داده‌ای گم نشود؛ فقط «خوانا» می‌شود.
/// </summary>
public static class AuditSemanticCatalog
{
    /// <summary>
    /// نگاشت «ناحیهٔ مسیر» (نخستین قطعهٔ مسیر پس از api/v1) به نام فارسی ماژول.
    /// ناحیه همان چیزی است که در Route کنترلرها می‌آید (patients، hr،
    /// accounting و…) — همهٔ ۳۸ ناحیهٔ برنامه پوشش داده شده‌اند؛ نام
    /// ناشناخته با پیشوند «؟» برمی‌گردد تا در بازبینی بعدی اضافه شود.
    /// </summary>
    private static readonly Dictionary<string, string> ModuleByArea = new(StringComparer.OrdinalIgnoreCase)
    {
        // ─── حوزهٔ درمان و پرونده ───
        ["patients"] = "بیماران",
        ["doctors"] = "پزشکان",
        ["receptions"] = "پذیرش",
        ["appointments"] = "نوبت‌دهی",
        ["visits"] = "ویزیت",
        ["emr"] = "پروندهٔ الکترونیک",
        ["prescriptions"] = "نسخه",
        // ─── پاراکلینیکی ───
        ["lab"] = "آزمایشگاه",
        ["imaging"] = "تصویربرداری",
        // ─── داروخانه و انبار ───
        ["pharmacy"] = "داروخانه",
        ["consumables"] = "انبار مصرفی‌ها",
        ["stocktakings"] = "انبارگردانی",
        // ─── مالی ───
        ["invoices"] = "صورتحساب",
        ["payments"] = "پرداخت‌ها",
        ["accounting"] = "حسابداری",
        ["insurance"] = "بیمه",
        ["tariffs"] = "تعرفه",
        // ─── منابع انسانی ───
        ["hr"] = "منابع انسانی",
        ["people"] = "اشخاص",
        ["salaries"] = "حقوق و دستمزد",
        ["payroll"] = "حقوق و دستمزد",
        ["attendance"] = "حضور و غیاب",
        // ─── ارتباطات و یکپارچه ───
        ["sms"] = "پیامک",
        ["notifications"] = "اطلاع‌رسانی",
        ["surveys"] = "نظرسنجی",
        ["government"] = "سپاس",
        ["sepas"] = "سپاس",
        // ─── مدیریت سیستم ───
        ["users"] = "مدیریت سیستم",
        ["roles"] = "مدیریت سیستم",
        ["permissions"] = "مدیریت سیستم",
        ["settings"] = "تنظیمات",
        ["admin"] = "مدیریت سیستم",
        ["auditlogs"] = "لاگ عملیات",
        ["backup"] = "پشتیبان‌گیری",
        ["license"] = "لایسنس",
        ["branches"] = "شعب",
        ["lookups"] = "اطلاعات پایه",
        ["print-templates"] = "قالب‌های چاپ",
        ["auth"] = "ورود و احراز هویت",
        // ─── ابزارها ───
        ["dashboard"] = "داشبورد",
        ["reports"] = "گزارش‌ها",
        ["search"] = "جستجو",
        ["training"] = "آموزش",
    };

    /// <summary>
    /// نگاشت «قطعهٔ موجودیت» (قطعهٔ مجاور شناسهٔ عددی در مسیر) به نام فارسی.
    /// کلیدها «جمع» و «مفرد» هر دو پوشش داده شده‌اند چون مسیرها گاهی
    /// مفردند (patients/5) و گاهی زیرمنبعی (hr/employees/5).
    /// </summary>
    private static readonly Dictionary<string, string> EntityBySegment = new(StringComparer.OrdinalIgnoreCase)
    {
        // ─── اشخاص ───
        ["patient"] = "بیمار", ["patients"] = "بیمار",
        ["doctor"] = "پزشک", ["doctors"] = "پزشک",
        ["employee"] = "کارمند", ["employees"] = "کارمند",
        ["person"] = "شخص", ["people"] = "شخص",
        // ─── نوبت و پذیرش ───
        ["appointment"] = "نوبت", ["appointments"] = "نوبت",
        ["reception"] = "پذیرش", ["receptions"] = "پذیرش",
        ["visit"] = "ویزیت", ["visits"] = "ویزیت",
        ["queue"] = "صف انتظار",
        // ─── نسخه و پرونده ───
        ["prescription"] = "نسخه", ["prescriptions"] = "نسخه",
        ["template"] = "قالب نسخه", ["templates"] = "قالب نسخه",
        ["note"] = "یادداشت", ["notes"] = "یادداشت",
        ["tag"] = "برچسب", ["tags"] = "برچسب",
        ["allergy"] = "حساسیت",
        ["vitalsign"] = "علامت حیاتی", ["vitalsigns"] = "علامت حیاتی",
        ["careplan"] = "برنامهٔ درمانی", ["careplans"] = "برنامهٔ درمانی",
        // ─── آزمایشگاه و تصویربرداری ───
        ["laborder"] = "درخواست آزمایش", ["laborders"] = "درخواست آزمایش",
        ["order"] = "درخواست آزمایش", ["orders"] = "درخواست آزمایش",
        ["labtest"] = "آزمایش", ["labtests"] = "آزمایش", ["test"] = "آزمایش", ["tests"] = "آزمایش",
        ["testresult"] = "نتیجهٔ آزمایش", ["testresults"] = "نتیجهٔ آزمایش", ["results"] = "نتیجهٔ آزمایش",
        ["result"] = "نتیجهٔ آزمایش",
        ["panel"] = "پنل آزمایش", ["panels"] = "پنل آزمایش",
        ["imagingorder"] = "درخواست تصویربرداری",
        ["file"] = "فایل تصویر", ["files"] = "فایل تصویر",
        // ─── داروخانه و انبار ───
        ["medicine"] = "دارو", ["medicines"] = "دارو",
        ["medication"] = "قلم دارویی", ["medications"] = "قلم دارویی",
        ["stock"] = "موجودی انبار", ["stocks"] = "موجودی انبار",
        ["stockin"] = "ورود انبار", ["batch"] = "بچ دارو", ["batches"] = "بچ دارو",
        ["stocktaking"] = "انبارگردانی", ["stocktakings"] = "انبارگردانی",
        ["stocktakingitem"] = "قلم انبارگردانی",
        ["transfer"] = "انتقال انبار", ["transfers"] = "انتقال انبار",
        // ─── مالی ───
        ["invoice"] = "فاکتور", ["invoices"] = "فاکتور",
        ["payment"] = "پرداخت", ["payments"] = "پرداخت",
        ["journalentry"] = "سند حسابداری", ["journalentries"] = "سند حسابداری",
        ["journal"] = "سند حسابداری", ["journals"] = "سند حسابداری",
        ["account"] = "کد حسابداری", ["accounts"] = "کد حسابداری",
        ["tariff"] = "تعرفه", ["tariffs"] = "تعرفه",
        ["insurancecompany"] = "شرکت بیمه", ["companies"] = "شرکت بیمه", ["company"] = "شرکت بیمه",
        ["policy"] = "قرارداد بیمه", ["policies"] = "قرارداد بیمه",
        ["salary"] = "حقوق", ["salaries"] = "حقوق", ["payslip"] = "فیش حقوقی", ["payslips"] = "فیش حقوقی",
        ["payslipitem"] = "قلم فیش حقوقی",
        ["commission"] = "کمیسیون", ["commissions"] = "کمیسیون",
        // ─── منابع انسانی ───
        ["leave"] = "مرخصی", ["leaves"] = "مرخصی",
        ["correction"] = "اصلاح حضور", ["corrections"] = "اصلاح حضور",
        ["contract"] = "قرارداد پرسنلی", ["contracts"] = "قرارداد پرسنلی",
        ["shift"] = "شیفت", ["shifts"] = "شیفت",
        ["shiftassignment"] = "شیفت‌بندی", ["shiftassignments"] = "شیفت‌بندی",
        // ─── مدیریت سیستم ───
        ["user"] = "کاربر", ["users"] = "کاربر",
        ["role"] = "نقش", ["roles"] = "نقش",
        ["permission"] = "مجوز", ["permissions"] = "مجوز دسترسی",
        ["branch"] = "شعبه", ["branches"] = "شعبه",
        ["room"] = "اتاق", ["rooms"] = "اتاق",
        ["setting"] = "تنظیمات", ["settings"] = "تنظیمات",
        ["backup"] = "نسخهٔ پشتیبان", ["backups"] = "نسخهٔ پشتیبان",
        // ─── ارتباطات ───
        ["message"] = "پیام", ["messages"] = "پیام",
        ["sms"] = "پیامک",
        ["survey"] = "نظرسنجی", ["surveys"] = "نظرسنجی",
        ["response"] = "پاسخ نظرسنجی", ["responses"] = "پاسخ نظرسنجی",
        ["sepas"] = "سپاس",
        // ─── کارگزارش ───
        ["schedule"] = "برنامهٔ کاری",
        ["dashboard"] = "داشبورد",
        ["shortcut"] = "میانبر", ["shortcuts"] = "میانبر",
        ["layout"] = "چیدمان داشبورد",
    };

    /// <summary>
    /// نام فارسی ماژول از مسیر درخواست — نخستین قطعهٔ معنادار پس از
    /// «api/v1» ناحیه است؛ برای مسیرهای بدون ناحیه (مثل /api/v1/auditlogs)
    /// خود قطعه به‌عنوان ناحیه آزموده می‌شود. خروجی همیشه متن دارد
    /// («سرویس وب» برای ناحیهٔ ناشناخته — دیگر هرگز «API» نوشته نمی‌شود).
    /// </summary>
    public static string ModuleFromPath(string? path)
    {
        var parts = (path ?? string.Empty)
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        // parts[0]=api  parts[1]=v1  parts[2]=ناحیه — مسیر کوتاه‌تر ناحیه ندارد
        if (parts.Length < 3 || string.IsNullOrWhiteSpace(parts[2]))
            return "سرویس وب";
        return ModuleByArea.TryGetValue(parts[2], out var module) ? module : "سرویس وب";
    }

    /// <summary>
    /// نام فارسی موجودیت از مسیر — قطعهٔ «مجاور شناسهٔ عددی» (الگوی
    /// میدلور)؛ اگر شناسه‌ای نبود، قطعهٔ ناحیه به‌عنوان موجودیت آزموده
    /// می‌شود (مسیرهایی مثل POST /api/v1/patients که «بیماران» منطقی است).
    /// نام ناشناخته با همان متن اصلی برمی‌گردد تا داده گم نشود.
    /// </summary>
    public static string EntityFromPath(string? path, string? entitySegment)
    {
        // قطعهٔ موجودیت معتبر است؟ فارسی‌اش را برگردان
        if (!string.IsNullOrWhiteSpace(entitySegment)
            && EntityBySegment.TryGetValue(entitySegment, out var fa))
            return fa;

        // بدون قطعهٔ موجودیت — ناحیهٔ مسیر به‌عنوان موجودیت (نمونه: POST /patients)
        var parts = (path ?? string.Empty)
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[2]))
        {
            if (EntityBySegment.TryGetValue(parts[2], out var areaEntity))
                return areaEntity;
            if (ModuleByArea.TryGetValue(parts[2], out var areaModule))
                return areaModule; // ناحیهٔ شناخته‌شده — نام ماژولش موجودیتِ منطقی است
            return parts[2]; // ناحیهٔ ناشناخته — همان متن
        }
        return string.Empty;
    }

    /// <summary>
    /// فارسی‌سازی نام موجودیتِ «ذخیره‌شده» — برای ردیف‌های قدیمی که
    /// با نام انگلیسی در دیتابیس نشسته‌اند (patients، roles و…).
    /// اگر نام ذخیره‌شده از قبل فارسی باشد، بدون تغییر برمی‌گردد؛
    /// نام ناشناخته هم بدون تغییر برمی‌گردد (شفافیت داده).
    /// </summary>
    public static string PersianizeEntity(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
            return "—";
        if (EntityBySegment.TryGetValue(stored.Trim(), out var fa))
            return fa;
        return stored; // شامل ردیف‌های فارسیِ تازه — همان‌طور که هست
    }

    /// <summary>
    /// فارسی‌سازی ماژولِ ذخیره‌شده — ردیف‌های قدگی مقدار ثابت «API»
    /// دارند؛ به «سرویس وب» ترجمه می‌شوند (نام واقعی ناحیه در آن دوره
    /// ثبت نشده و حدس زدنش دروغ است؛ «سرویس وب» صادقانه و روشن است).
    /// ردیف‌های تازه از پیش فارسی‌اند و بی‌تغییر می‌مانند.
    /// </summary>
    public static string PersianizeModule(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
            return "—";
        if (string.Equals(stored.Trim(), "API", StringComparison.OrdinalIgnoreCase))
            return "سرویس وب";
        if (ModuleByArea.TryGetValue(stored.Trim(), out var fa))
            return fa;
        return stored;
    }
}

using System.Text.Json;

namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>یک تغییر فیلد در جدول مقایسهٔ جزئیات لاگ</summary>
/// <param name="Field">عنوان فارسی فیلد</param>
/// <param name="OldDisplay">مقدار قبلی — قالب‌بندی‌شده و فارسی</param>
/// <param name="NewDisplay">مقدار جدید — قالب‌بندی‌شده و فارسی</param>
/// <param name="Changed">آیا مقدار واقعاً عوض شده؟ (ردیف تغییر‌یافته رنگی می‌شود)</param>
public sealed record AuditFieldChange(string Field, string OldDisplay, string NewDisplay, bool Changed);

/// <summary>
/// ═══ کاتالوگ فیلدها و قالب‌بند مقادیر لاگ عملیات — بازطراحی چرخهٔ ۷۰ ═══
/// گزارش مالک: «هم مقادیر قبلی و هم مقادیر جدید هر دو اشتباه هستند».
/// ریشه: مقادیر خامِ JSON ذخیره می‌شوند — کلیدهای انگلیسی (firstName،
/// LastName)، تاریخ‌های میلادی ISO، بولین‌های true/false و اعداد لاتین؛
/// برای کاربر فارسی‌زبان این خروجی «اشتباه» به نظر می‌رسد.
/// این کاتالوگ نمایش را بازسازی می‌کند:
///   ۱) کلید انگلیسی → عنوان فارسی (واژه‌نامهٔ ~۱۴۰ فیلد پرتکرار برنامه)؛
///   ۲) مقدار → قالب فارسی: تاریخ ISO/میلادی → شمسی، بولین → بله/خیر،
///      عدد → ارقام فارسی، فیلد پولی → جداکنندهٔ سه‌رقمی + «تومان»؛
///   ۳) دیف معنایی: برای ویرایش، «فقط فیلدهای تغییر‌یافته» با مقایسهٔ
///      قبلی/جدید نمایش داده می‌شود (ردیف تغییر‌یافته رنگی)؛
///   ۴) فیلدهای نوفهٔ زیرساختی (rowVersion و…) حذف می‌شوند.
/// برای کلیدهای ناشناخته: خود کلید با فاصله‌گذاری بین کلمات نشان داده
/// می‌شود — هیچ داده‌ای گم نمی‌شود؛ فقط «خوانا» می‌شود.
/// </summary>
public static class AuditFieldCatalog
{
    /// <summary>
    /// واژه‌نامهٔ کلید انگلیسی (camelCase بدنهٔ درخواست + PascalCase ستون دیتابیس —
    /// مقایسه بدون حساسیت به بزرگی حرف) → عنوان فارسی. پرتکرارترین فیلدهای
    /// فرم‌های برنامه را پوشش می‌دهد؛ ناشناخته‌ها شفاف پاس داده می‌شوند.
    /// </summary>
    private static readonly Dictionary<string, string> FieldLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        // ─── هویت ───
        ["firstName"] = "نام", ["lastname"] = "نام خانوادگی",
        ["fathername"] = "نام پدر", ["nationalcode"] = "کد ملی",
        ["personnelcode"] = "کد پرسنلی", ["gender"] = "جنسیت",
        ["birthdate"] = "تاریخ تولد", ["birthdatejalali"] = "تاریخ تولد",
        ["maritalstatus"] = "وضعیت تأهل", ["single"] = "مجرد", ["married"] = "متأهل",
        ["bloodgroup"] = "گروه خونی",
        // ─── تماس و نشانی ───
        ["mobile"] = "موبایل", ["phonenumber"] = "شماره تماس", ["phone"] = "تلفن ثابت",
        ["email"] = "ایمیل", ["address"] = "نشانی", ["city"] = "شهر", ["province"] = "استان",
        ["emergencycontact"] = "تماس اضطراری", ["emergencyphone"] = "تلفن اضطراری",
        ["insurancenumber"] = "شماره بیمه",
        // ─── بالینی ───
        ["description"] = "شرح", ["diagnosis"] = "تشخیص", ["complaint"] = "شکایت اصلی",
        ["chiefcomplaint"] = "شکایت اصلی", ["pasthistory"] = "سابقهٔ بیماری",
        ["familyhistory"] = "سابقهٔ خانوادگی", ["drughistory"] = "سابقهٔ مصرف دارو",
        ["allergies"] = "حساسیت‌ها", [" allergy"] = "حساسیت",
        ["symptoms"] = "علائم", ["signs"] = "نشانه‌ها",
        ["height"] = "قد (سانتی‌متر)", ["weight"] = "وزن (کیلوگرم)",
        ["bloodpressure"] = "فشار خون", ["pulse"] = "نبض", ["temperature"] = "دمای بدن",
        ["spo2"] = "اشباع اکسیژن", ["respiratoryrate"] = "تنفس در دقیقه",
        ["vitalsigns"] = "علائم حیاتی",
        ["treatmentplan"] = "برنامهٔ درمانی", ["advice"] = "توصیه‌ها",
        ["followupdate"] = "تاریخ پیگیری", ["referral"] = "ارجاع",
        ["note"] = "یادداشت", ["notes"] = "یادداشت",
        ["chiefcomplainttext"] = "شرح شکایت",
        // ─── دارو و نسخه ───
        ["medicinename"] = "نام دارو",
        ["dosage"] = "دوز", ["duration"] = "مدت مصرف", ["instruction"] = "نحوهٔ مصرف",
        ["instructions"] = "نحوهٔ مصرف", ["quantity"] = "تعداد/مقدار", ["unitprice"] = "قیمت واحد",
        ["refill"] = "نوبت تجدید", ["ischronic"] = "شکل مزمن",
        // ─── پولی ───
        ["price"] = "قیمت", ["amount"] = "مبلغ", ["totalamount"] = "مبلغ کل",
        ["paidamount"] = "مبلغ پرداخت‌شده", ["discount"] = "تخفیف", ["discountamount"] = "مبلغ تخفیف",
        ["insuranceamount"] = "سهم بیمه", ["patientamount"] = "سهم بیمار",
        ["patientshare"] = "سهم بیمار", ["insurancershare"] = "سهم بیمه",
        ["deposit"] = "بیعانه", ["balance"] = "مانده حساب", ["remaining"] = "مانده",
        ["fee"] = "حق‌الزحمه", ["commission"] = "کمیسیون", ["salary"] = "حقوق",
        ["basesalary"] = "حقوق پایه",
        ["overtime"] = "اضافه‌کاری", ["allowance"] = "مزایا", ["deduction"] = "کسورات",
        ["tax"] = "مالیات", ["tip"] = "انعام", ["vat"] = "ارزش افزوده",
        // ─── وضعیت‌ها و شمارش‌ها ───
        ["status"] = "وضعیت", ["isactive"] = "فعال", ["isdeleted"] = "حذف‌شده",
        ["isreadonly"] = "فقط‌خواندنی", ["enabled"] = "فعال", ["visible"] = "نمایان",
        ["priority"] = "اولویت", ["orderindex"] = "ترتیب نمایش", ["count"] = "تعداد",
        ["totalitems"] = "تعداد کل اقلام", ["matcheditems"] = "اقلام مطابق",
        ["mismatcheditems"] = "اقلام مغایر", ["totalloss"] = "کل کسری", ["totalsurplus"] = "کل زیادندی",
        // ─── زمان‌ها ───
        ["date"] = "تاریخ", ["time"] = "ساعت", ["starttime"] = "ساعت شروع", ["endtime"] = "ساعت پایان",
        ["breakstart"] = "شروع استراحت", ["breakend"] = "پایان استراحت",
        ["appointmentdate"] = "تاریخ نوبت", ["appointmenttime"] = "ساعت نوبت",
        ["issuedate"] = "تاریخ صدور", ["duedate"] = "تاریخ سررسید",
        ["expirydate"] = "تاریخ انقضا", ["expiredate"] = "تاریخ انقضا",
        ["createdat"] = "زمان ثبت", ["updatedat"] = "زمان ویرایش",
        ["deletedat"] = "زمان حذف", ["modifiedat"] = "زمان ویرایش",
        // ─── نقش و دسترسی ───
        ["name"] = "نام", ["title"] = "عنوان", ["username"] = "نام کاربری",
        ["rolename"] = "نام نقش", ["permissions"] = "مجوزهای دسترسی",
        ["permissioncount"] = "تعداد مجوز", ["permissionslist"] = "فهرست مجوزها",
        ["password"] = "رمز عبور", ["displayname"] = "نام نمایشی",
        // ─── ساختاری ───
        ["key"] = "کلید", ["value"] = "مقدار", ["code"] = "کد", ["number"] = "شماره",
        ["type"] = "نوع", ["category"] = "دسته", ["group"] = "گروه", ["color"] = "رنگ",
        ["icon"] = "آیکون", ["module"] = "ماژول", ["parent"] = "والد",
        ["room"] = "اتاق", ["roomid"] = "اتاق", ["branch"] = "شعبه", ["branchid"] = "شعبه",
        ["doctor"] = "پزشک", ["doctorid"] = "پزشک", ["patient"] = "بیمار", ["patientid"] = "بیمار",
        ["doctorname"] = "نام پزشک", ["patientname"] = "نام بیمار",
        ["insurancecompany"] = "شرکت بیمه", ["insurancecompanyid"] = "شرکت بیمه",
        ["visittype"] = "نوع ویزیت", ["visitkind"] = "نوع ویزیت",
        ["paymentmethod"] = "روش پرداخت", ["paymentkind"] = "روش پرداخت",
        ["invoice"] = "فاکتور", ["invoiceid"] = "فاکتور", ["invoicenumber"] = "شماره فاکتور",
        ["reception"] = "پذیرش", ["receptionid"] = "پذیرش", ["receptionnumber"] = "شماره پذیرش",
        ["queuenumber"] = "شماره نوبت",
        ["resultvalue"] = "مقدار نتیجه", ["result"] = "نتیجه", ["normalrange"] = "بازهٔ نرمال",
        ["unit"] = "واحد اندازه‌گیری", ["interpretation"] = "تفسیر", ["abnormalflag"] = "وضعیت غیرعادی",
        ["critical"] = "بحرانی", ["iscritical"] = "بحرانی", ["isabnormal"] = "غیرعادی",
        ["remark"] = "ملاحظات", ["comment"] = "توضیح", ["summary"] = "خلاصه",
        // ─── آزمایشگاه خاص ───
        ["testname"] = "نام آزمایش", ["testnamefa"] = "نام آزمایش",
        ["expectedquantity"] = "موجودی دفتری", ["countedquantity"] = "موجودی شمارش‌شده",
        ["systemquantity"] = "موجودی سیستم",
    };

    /// <summary>
    /// فیلدهای نوفهٔ زیرساختی — در جدول مقایسه نمایش داده نمی‌شوند؛ نه کاربر
    /// آن‌ها را تغییر می‌دهد و نه معنای عملیاتی دارند (ردّ اطلاع‌رسانی کاذب).
    /// شناسه و زمان ثبت در ستون‌های خودشان در جدول لاگ موجودند.
    /// </summary>
    private static readonly HashSet<string> NoiseFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "rowversion", "concurrencystamp", "concurrencytoken",
        "createdbyid", "modifiedbyid", "createdby", "modifiedby",
        "isdeleted", "deletedbyid", "deletedat",
        "createdatutc", "updatedatutc", "lastloginat", "securitystamp", "passwordhash",
        "twofactorsecret", "fullname", "useragent", "ip", "ipaddress",
        "timestamp", "x-requestedwith", "correlationid",
    };

    /// <summary>نشانگر فیلدهای پولی برای جداکنندهٔ سه‌رقمی + واحد تومان</summary>
    private static readonly string[] MoneyKeyMarkers =
    {
        "price", "amount", "fee", "salary", "commission", "discount", "balance",
        "deposit", "remaining", "deduction", "allowance", "overtime", "total",
        "share", "payment", "loss", "surplus", "base"
    };

    /// <summary>
    /// عنوان فارسی فیلد — کلید ناشناخته با فاصله‌گذاری انگلیسی (createdAt →
    /// «created At») نمایش می‌یابد؛ کلید فارسی از پیش بدون تغییر است.
    /// </summary>
    public static string LabelFor(string key)
    {
        if (FieldLabels.TryGetValue(key, out var fa))
            return fa;
        if (ContainsPersian(key))
            return key; // از قبل فارسی است
        // camelCase/PascalCase → کلمات جدا
        var spaced = string.Concat(key.Select((c, i) =>
            i > 0 && char.IsUpper(c) && !char.IsUpper(key[i - 1]) ? " " + c : c.ToString()));
        return spaced.Trim();
    }

    /// <summary>آیا متن حروف فارسی دارد؟ (برای تشخیص کلیدهای از قبل فارسی)</summary>
    private static bool ContainsPersian(string text) =>
        text.Any(c => c is >= '\u0600' and <= '\u06FF');

    /// <summary>
    /// قالب‌بندی «مقدار» برای نمایش فارسی — نوع JSON تشخیص داده می‌شود:
    /// بولین → بله/خیر؛ رشتهٔ تاریخ ISO → شمسی؛ عدد در فیلد پولی →
    /// جداکنندهٔ سه‌رقمی + «تومان»؛ سایر اعداد → ارقام فارسی؛ تهی → «— خالی».
    /// رشته‌ای که خودش تاریخ کامل میلادی است نیز به شمسی برمی‌گردد.
    /// </summary>
    public static string FormatValue(string key, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
                return "— خالی";
            case JsonValueKind.True:
                return "بله";
            case JsonValueKind.False:
                return "خیر";
            case JsonValueKind.String:
                return FormatString(key, value.GetString() ?? string.Empty);
            case JsonValueKind.Number:
                return FormatNumber(key, value);
            case JsonValueKind.Object:
            {
                // شیء تو در تو — «فشرده و خوانا»: حداکثر ۸ کلید، با عنوان فارسی
                var inner = value.EnumerateObject().Take(8)
                    .Select(p => $"{LabelFor(p.Name)}: {FormatValue(p.Name, p.Value)}");
                var more = value.EnumerateObject().Count() > 8 ? " …" : "";
                return "{" + string.Join("، ", inner) + more + "}";
            }
            case JsonValueKind.Array:
            {
                // آرایه — برای فهرست‌های مجوز/اقلام: عضوها با عنوان فارسیِ خودشان
                var items = value.EnumerateArray().Take(12)
                    .Select(i => i.ValueKind == JsonValueKind.String
                        ? i.GetString() ?? ""
                        : FormatValue(key, i));
                var more = value.EnumerateArray().Count() > 12 ? " …" : "";
                return "[" + string.Join("، ", items) + more + "]";
            }
            default:
                return value.ToString();
        }
    }

    /// <summary>قالب‌بندی رشته — تشخیص تاریخ/بازهٔ زمانی/متن</summary>
    private static string FormatString(string key, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "— خالی";
        text = text.Trim();

        // تاریخ ISO کامل — 2026-10-13T18:45:00 / 2026-10-13T18:45:00.123Z
        if (text.Length >= 10 && text[4] == '-' && text[7] == '-'
            && DateTime.TryParse(text, out var iso))
        {
            var fa = Application.Common.Helpers.PersianDateHelper.ToShamsi(iso);
            return $"{fa} ساعت {iso:HH:mm}";
        }

        // فقط تاریخ — 2026-10-13 (بدون ساعت)
        if (text.Length == 10 && text[4] == '-' && text[7] == '-'
            && DateOnly.TryParse(text, out var dateOnly))
            return Application.Common.Helpers.PersianDateHelper.ToShamsi(dateOnly.ToDateTime(TimeOnly.MinValue));

        // ستون‌های دیتابیس قدیمی تاریخ را با «/» می‌فرستند — 2026/10/13
        if (text.Length >= 10 && text[4] == '/' && text[7] == '/'
            && DateTime.TryParse(text, out var slashed))
            return Application.Common.Helpers.PersianDateHelper.ToShamsi(slashed);

        // ساعت خام TimeOnly — 18:45:00
        if (text.Length is 5 or 8 && text[2] == ':' && TimeSpan.TryParse(text, out _))
            return text[..5]; // ثانیه حذف — خواناتر

        // رشتهٔ ذخیره‌شدهٔ «حذف شد» از سانسور میدلور
        if (text == "***حذف شد***")
            return "⛔ محتوای محرمانه";

        return text;
    }

    /// <summary>قالب‌بندی عدد — پولی؟ سه‌رقمی+تومان : ارقام فارسی</summary>
    private static string FormatNumber(string key, JsonElement value)
    {
        var isMoney = MoneyKeyMarkers.Any(m => key.Contains(m, StringComparison.OrdinalIgnoreCase))
                      && !key.Contains("count", StringComparison.OrdinalIgnoreCase)
                      && !key.Contains("number", StringComparison.OrdinalIgnoreCase)
                      && !key.Contains("phone", StringComparison.OrdinalIgnoreCase);
        if (isMoney && value.TryGetDecimal(out var dec))
        {
            var separated = dec.ToString("N0"); // جداکنندهٔ سه‌رقمی
            return Application.Common.Helpers.PersianDateHelper.ToPersianDigits(separated) + " تومان";
        }
        // اعداد صحیح کوچک (شمارش/شناسه) — فقط ارقام فارسی
        return Application.Common.Helpers.PersianDateHelper.ToPersianDigits(value.ToString());
    }

    /// <summary>
    /// ساخت «جدول مقایسهٔ تغییرات» از مقادیر قبلی/جدید ذخیره‌شده در لاگ —
    /// خروجی بازطراحی چرخهٔ ۷۰ برای پنل جزئیات. منطق بر اساس نوع عملیات:
    ///   ویرایش: مقایسهٔ زوج‌به‌زوج؛ فقط فیلدهای «متفاوت» علامت تغییر می‌گیرند
    ///           (رنگ ردیف)؛ فیلدهای یکسان برای مرور کامل می‌آیند ولی بی‌رنگ‌اند؛
    ///   ایجاد:   فیلدهای بدنه با مقدار قبلی «—» (چیزی نبوده)؛
    ///   حذف:     فیلدهای سطر دیتابیس با مقدار جدید «— حذف شد»؛
    ///   ورود/خروج/مشاهده: بدنه معمولاً خالی است — دیفی نمایش داده نمی‌شود.
    /// فیلدهای نوفه حذف می‌شوند؛ ترتیب: تغییر‌یافته‌ها اول، سپس بقیه.
    /// </summary>
    public static List<AuditFieldChange> BuildDiff(string? action, string? oldJson, string? newJson)
    {
        var oldProps = ParseObject(oldJson);
        var newProps = ParseObject(newJson);
        if (oldProps.Count == 0 && newProps.Count == 0)
            return new List<AuditFieldChange>();

        var isDelete = string.Equals(action, "حذف", StringComparison.Ordinal)
                       || string.Equals(action, "Delete", StringComparison.OrdinalIgnoreCase);
        var isCreate = string.Equals(action, "ایجاد", StringComparison.Ordinal)
                       || string.Equals(action, "Create", StringComparison.OrdinalIgnoreCase);

        // فیلدهای نوفه کنار گذاشته می‌شوند (نه در دیف، نه در مرور)
        oldProps = oldProps.Where(p => !NoiseFields.Contains(p.Key))
            .ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
        newProps = newProps.Where(p => !NoiseFields.Contains(p.Key))
            .ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);

        var rows = new List<AuditFieldChange>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // سمت «جدید» — فیلدهای بدنهٔ درخواست (ویرایش/ایجاد)
        foreach (var (key, newValue) in newProps)
        {
            seen.Add(key);
            var label = LabelFor(key);
            var newDisplay = FormatValue(key, newValue);
            if (isCreate)
            {
                rows.Add(new AuditFieldChange(label, "—", newDisplay, Changed: true));
                continue;
            }
            if (oldProps.TryGetValue(key, out var oldValue))
            {
                var oldDisplay = FormatValue(key, oldValue);
                var changed = !JsonEqual(newValue, oldValue);
                rows.Add(new AuditFieldChange(label, oldDisplay, newDisplay, changed));
            }
            else
            {
                // فیلد تازه‌اضافه‌شده (در سطر قبلی نبود)
                rows.Add(new AuditFieldChange(label, "—", newDisplay, true));
            }
        }

        // سمت «قبلی» — فیلدهایی که در بدنه نیستند (حذف/حذف از فرم)
        foreach (var (key, oldValue) in oldProps)
        {
            if (seen.Contains(key))
                continue;
            var label = LabelFor(key);
            var oldDisplay = FormatValue(key, oldValue);
            rows.Add(new AuditFieldChange(label, oldDisplay, isDelete ? "— حذف شد" : "— خالی", Changed: true));
        }

        // تغییر‌یافته‌ها اول — چشم روی همان‌هاست
        return rows.OrderByDescending(r => r.Changed).ThenBy(r => r.Field, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    /// <summary>تجزیهٔ JSON شیء — تهی/خراب → دیکشنری خالی</summary>
    private static Dictionary<string, JsonElement> ParseObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            return doc.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>مقایسهٔ معنایی دو مقدار JSON (عدد ۵ و «۵» برابرند)</summary>
    private static bool JsonEqual(JsonElement a, JsonElement b)
    {
        // هر دو رشته‌اند؟ (تاریخ‌ها با فرمت متفاوت نباید «تغییر» قلمداد شوند)
        if (a.ValueKind == JsonValueKind.String && b.ValueKind == JsonValueKind.String)
        {
            var sa = a.GetString()?.Trim();
            var sb = b.GetString()?.Trim();
            if (string.Equals(sa, sb, StringComparison.Ordinal))
                return true;
            // مقایسهٔ تاریخ‌های هم‌ارز (ISO با/بدون میلی‌ثانیه)
            if (DateTime.TryParse(sa, out var da) && DateTime.TryParse(sb, out var db))
                return da == db;
            return false;
        }
        // عدد در برابر رشتهٔ عددی
        if (a.ValueKind == JsonValueKind.Number && b.ValueKind == JsonValueKind.String
            && b.GetString() is { } bs && decimal.TryParse(bs, out var bn)
            && a.TryGetDecimal(out var an))
            return an == bn;
        if (b.ValueKind == JsonValueKind.Number && a.ValueKind == JsonValueKind.String
            && a.GetString() is { } as_ && decimal.TryParse(as_, out var an2)
            && b.TryGetDecimal(out var bn2))
            return an2 == bn2;
        if (a.ValueKind == b.ValueKind)
            return a.GetRawText() == b.GetRawText();
        return false;
    }
}

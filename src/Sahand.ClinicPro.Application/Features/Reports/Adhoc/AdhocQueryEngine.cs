using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Reports.Adhoc;

#region مدل داده گزارش رایگانه

/// <summary>نوع فیلد در لیست سفید گزارش‌ساز</summary>
public enum AdhocFieldType
{
    /// <summary>متن</summary>
    Text,
    /// <summary>عدد صحیح</summary>
    Number,
    /// <summary>مبلغ ریالی</summary>
    Money,
    /// <summary>تاریخ (میلادی در بانک، شمسی در خروجی)</summary>
    Date,
    /// <summary>مقادیر شمارشی با برچسب فارسی</summary>
    Enum,
    /// <summary>بله/خیر</summary>
    Boolean,
    /// <summary>محاسبه‌شده از ناوبری (مثل نام بیمار) — قابل انتخاب و گروه‌بندی، غیرقابل فیلتر بانکی</summary>
    Computed,
}

/// <summary>یک فیلد مجاز در لیست سفید</summary>
public record AdhocField(
    string Name,
    string Title,
    AdhocFieldType Type,
    bool Filterable,
    List<string>? EnumOptions = null);

/// <summary>یک موجودیت مجاز در لیست سفید</summary>
public record AdhocEntitySchema(
    string Key,
    string Title,
    string Icon,
    string DateField,
    List<AdhocField> Fields);

/// <summary>عملگر فیلتر</summary>
public enum AdhocOp
{
    /// <summary>مساوی</summary>
    Eq,
    /// <summary>نامساوی</summary>
    Neq,
    /// <summary>شامل</summary>
    Contains,
    /// <summary>شامل نیست</summary>
    NotContains,
    /// <summary>شروع با</summary>
    StartsWith,
    /// <summary>پایان با</summary>
    EndsWith,
    /// <summary>بزرگ‌تر</summary>
    Gt,
    /// <summary>بزرگ‌تر یا مساوی</summary>
    Gte,
    /// <summary>کوچک‌تر</summary>
    Lt,
    /// <summary>کوچک‌تر یا مساوی</summary>
    Lte,
    /// <summary>مابین (دو مقدار)</summary>
    Between,
    /// <summary>خالی</summary>
    Empty,
    /// <summary>پر</summary>
    NotEmpty,
}

/// <summary>یک شرط فیلتر</summary>
public record AdhocFilter(string Field, AdhocOp Op, string? Value1, string? Value2);

/// <summary>عمل تجمیع</summary>
public enum AdhocAggregateFunc
{
    /// <summary>فهرست ردیف‌ها بدون تجمیع</summary>
    None,
    /// <summary>شمارش</summary>
    Count,
    /// <summary>جمع</summary>
    Sum,
    /// <summary>میانگین</summary>
    Avg,
    /// <summary>کمینه</summary>
    Min,
    /// <summary>بیشینه</summary>
    Max,
}

/// <summary>تعریف گزارش رایگانه — فقط از لیست سفید؛ هرگز SQL خام ندارد</summary>
public record AdhocDefinition(
    string Entity,
    List<string> Fields,
    List<AdhocFilter> Filters,
    string? GroupBy,
    AdhocAggregateFunc Aggregate = AdhocAggregateFunc.None,
    string? AggregateField = null,
    string? SortField = null,
    bool SortDesc = false,
    int Limit = 100,
    string Chart = "column");

/// <summary>نتیجه اجرا — هم‌شکل قرارداد گزارش‌های کاتالوگ (summary/columns/rows/chart)</summary>
public record AdhocResult(
    List<KeyValuePair<string, string>> Summary,
    List<string> Columns,
    List<List<string>> Rows,
    string? ChartKind,
    string? ChartTitle,
    string? ChartUnit);

#endregion

/// <summary>
/// موتور امن گزارش‌ساز رایگانه (کوئری‌ساز گرافیکی) — چرخه سیزدهم (درخواست ۴-۳).
/// کاربر بدون دانش SQL از میان لیست سفید موجودیت‌ها/فیلدها گزارش می‌سازد؛
/// هیچ عبارت SQL از کاربر پذیرفته نمی‌شود و اجرا کاملاً روی LINQ درون حافظه
/// پس از ماده‌سازی ستون‌های مجاز انجام می‌شود (ضد تزریق SQL به‌صورت ساختاری).
/// </summary>
public class AdhocQueryEngine
{
    /// <summary>حداکثر ردیف خروجی</summary>
    public const int MaxLimit = 1000;

    /// <summary>حداکثر تعداد ستون انتخابی</summary>
    public const int MaxFields = 10;

    /// <summary>حداکثر ردیف ماده‌سازی از بانک برای پردازش درون‌حافظه</summary>
    public const int MaxScan = 20000;

    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public AdhocQueryEngine(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    #region لیست سفید — طرح موجودیت‌ها

    /// <summary>فیلد متنی قابل فیلتر</summary>
    private static AdhocField T(string name, string title) => new(name, title, AdhocFieldType.Text, Filterable: true);

    /// <summary>فیلد عددی</summary>
    private static AdhocField N(string name, string title) => new(name, title, AdhocFieldType.Number, Filterable: true);

    /// <summary>فیلد مبلغی</summary>
    private static AdhocField M(string name, string title) => new(name, title, AdhocFieldType.Money, Filterable: true);

    /// <summary>فیلد تاریخی</summary>
    private static AdhocField D(string name, string title) => new(name, title, AdhocFieldType.Date, Filterable: true);

    /// <summary>فیلد شمارشی با گزینه‌های فارسی</summary>
    private static AdhocField E(string name, string title, List<string> options)
        => new(name, title, AdhocFieldType.Enum, Filterable: true, EnumOptions: options);

    /// <summary>فیلد بله/خیر</summary>
    private static AdhocField B(string name, string title) => new(name, title, AdhocFieldType.Boolean, Filterable: true);

    /// <summary>فیلد محاسبه‌شده (ناوبری) — فقط انتخاب/گروه‌بندی؛ فیلتر درون‌حافظه</summary>
    private static AdhocField C(string name, string title) => new(name, title, AdhocFieldType.Computed, Filterable: false);

    /// <summary>فهرست موجودیت‌های مجاز — قلب لیست سفید</summary>
    public static List<AdhocEntitySchema> Schema { get; } = new()
    {
        new("patient", "بیماران", "👤", "CreatedAt", new List<AdhocField>
        {
            N("Id", "شناسه"),
            T("MedicalRecordNo", "شماره پرونده"),
            T("NationalCode", "کد ملی"),
            T("FirstName", "نام"),
            T("LastName", "نام خانوادگی"),
            T("FatherName", "نام پدر"),
            E("Gender", "جنسیت", new() { "مرد", "زن" }),
            D("BirthDate", "تاریخ تولد"),
            E("MaritalStatus", "وضعیت تأهل", new() { "مجرد", "متأهل", "همسر فوت‌شده", "طلاق‌گرفته" }),
            T("Mobile", "موبایل"),
            T("City", "شهر"),
            T("Province", "استان"),
            T("InsuranceNumber", "شماره بیمه"),
            E("PatientType", "نوع بیمار", new() { "عادی", "ویژه", "پرسنل" }),
            T("Allergies", "حساسیت‌ها"),
            T("ChronicDiseases", "بیماری‌های مزمن"),
            D("CreatedAt", "تاریخ ثبت"),
        }),
        new("appointment", "نوبت‌ها", "📅", "CreatedAt", new List<AdhocField>
        {
            T("AppointmentCode", "کد نوبت"),
            C("PatientName", "نام بیمار"),
            C("DoctorName", "نام پزشک"),
            D("AppointmentDate", "تاریخ نوبت"),
            T("AppointmentTime", "ساعت نوبت"),
            E("Status", "وضعیت", new() { "رزرو شده", "تأیید شده", "حاضر", "در حال ویزیت", "تکمیل شده", "لغو شده", "عدم مراجعه" }),
            E("AppointmentType", "نوع نوبت", new() { "اولیه", "پیگیری", "اورژانس" }),
            E("Source", "منبع", new() { "حضوری", "تلفنی", "آنلاین" }),
            B("IsFirstVisit", "ویزیت اول"),
            E("Priority", "اولویت", new() { "اورژانس", "فوری", "عادی" }),
            M("EstimatedFee", "حق‌الویزیت تخمینی"),
            N("QueueNumber", "شماره نوبت صف"),
            D("CreatedAt", "تاریخ ثبت"),
        }),
        new("visit", "ویزیت‌ها", "🩺", "CreatedAt", new List<AdhocField>
        {
            T("VisitCode", "کد ویزیت"),
            C("PatientName", "نام بیمار"),
            C("DoctorName", "نام پزشک"),
            D("VisitDate", "تاریخ ویزیت"),
            E("VisitType", "نوع ویزیت", new() { "اولیه", "پیگیری", "اورژانس", "مشاوره" }),
            T("ChiefComplaint", "شکایت اصلی"),
            E("Status", "وضعیت", new() { "شروع‌شده", "در حال ویزیت", "تکمیل شده", "پیش‌نویس" }),
            D("CreatedAt", "تاریخ ثبت"),
        }),
        new("invoice", "فاکتورها", "🧾", "CreatedAt", new List<AdhocField>
        {
            T("InvoiceNumber", "شماره فاکتور"),
            C("PatientName", "نام بیمار"),
            D("InvoiceDate", "تاریخ فاکتور"),
            E("InvoiceType", "نوع فاکتور", new() { "ویزیت", "آزمایش", "تصویربرداری", "داروخانه", "دندانپزشکی", "فیزیوتراپی", "سایر" }),
            M("SubTotal", "جمع کل"),
            M("DiscountAmount", "تخفیف"),
            M("Tax", "مالیات"),
            M("TotalAmount", "مبلغ نهایی"),
            M("PaidAmount", "پرداختی"),
            M("RemainingAmount", "مانده"),
            E("Status", "وضعیت", new() { "پیش‌نویس", "صادره", "تسویه‌نشده", "تسویه‌شده", "لغو شده", "برگشتی" }),
            E("PaymentStatus", "وضعیت پرداخت", new() { "پرداخت‌نشده", "تسویه‌نشده", "تسویه‌شده", "پرداخت اضافه" }),
            D("CreatedAt", "تاریخ ثبت"),
        }),
        new("payment", "پرداخت‌ها", "💳", "CreatedAt", new List<AdhocField>
        {
            T("PaymentNumber", "شماره پرداخت"),
            C("PatientName", "نام بیمار"),
            M("Amount", "مبلغ"),
            E("Method", "روش پرداخت", new() { "نقدی", "کارت", "آنلاین", "چک", "حواله بانکی", "بیمه" }),
            D("PaymentDate", "تاریخ پرداخت"),
            E("Status", "وضعیت", new() { "در انتظار", "تکمیل شده", "ناموفق", "برگشتی" }),
            B("IsReturned", "برگشتی"),
            D("CreatedAt", "تاریخ ثبت"),
        }),
        new("prescription", "نسخه‌ها", "💊", "CreatedAt", new List<AdhocField>
        {
            T("PrescriptionCode", "کد نسخه"),
            C("PatientName", "نام بیمار"),
            C("DoctorName", "نام پزشک"),
            D("PrescriptionDate", "تاریخ نسخه"),
            E("Type", "نوع نسخه", new() { "دارویی", "پاراکلینیک", "ترکیبی" }),
            B("IsElectronic", "الکترونیک"),
            B("IsNarcotic", "مواد مخدر"),
            E("Status", "وضعیت", new() { "صادره", "ارسال به بیمه", "تأیید بیمه", "تحویل‌شده", "لغو شده", "منقضی" }),
            D("CreatedAt", "تاریخ ثبت"),
        }),
        new("laborder", "آزمایش‌ها", "🧪", "CreatedAt", new List<AdhocField>
        {
            T("OrderNumber", "شماره درخواست"),
            C("PatientName", "نام بیمار"),
            C("DoctorName", "نام پزشک"),
            D("OrderDate", "تاریخ درخواست"),
            E("Priority", "اولویت", new() { "اورژانس", "فوری", "عادی" }),
            E("Status", "وضعیت", new() { "درخواست‌شده", "نمونه‌گیری‌شده", "در حال انجام", "آماده", "تأییدشده", "تحویل‌شده", "لغو شده" }),
            D("CreatedAt", "تاریخ ثبت"),
        }),
    };

    /// <summary>یافتن طرح موجودیت با کلید</summary>
    public static AdhocEntitySchema? FindSchema(string key)
        => Schema.FirstOrDefault(s => s.Key == key);

    #endregion

    #region اجرای تعریف

    /// <summary>
    /// اجرای امن تعریف گزارش — اعتبارسنجی کامل در برابر لیست سفید،
    /// ماده‌سازی ستون‌های مجاز، فیلتر/گروه/تجمیع درون‌حافظه و قالب‌بندی شمسی.
    /// </summary>
    /// <exception cref="InvalidOperationException">تعریف نامعتبر یا خارج از لیست سفید</exception>
    public async Task<AdhocResult> RunAsync(AdhocDefinition definition, CancellationToken ct = default)
    {
        var schema = FindSchema(definition.Entity)
            ?? throw new InvalidOperationException($"موجودیت «{definition.Entity}» در لیست سفید گزارش‌ساز نیست");

        // ── اعتبارسنجی فیلدهای انتخابی ──
        var fields = definition.Fields.Distinct().ToList();
        if (fields.Count == 0) throw new InvalidOperationException("حداقل یک ستون انتخاب کنید");
        if (fields.Count > MaxFields) throw new InvalidOperationException($"حداکثر {MaxFields} ستون مجاز است");
        var fieldMap = schema.Fields.ToDictionary(f => f.Name, f => f);
        foreach (var f in fields)
            if (!fieldMap.ContainsKey(f))
                throw new InvalidOperationException($"فیلد «{f}» در موجودیت «{schema.Title}» مجاز نیست");

        // ── اعتبارسنجی فیلترها ──
        foreach (var flt in definition.Filters)
        {
            if (!fieldMap.TryGetValue(flt.Field, out var ff))
                throw new InvalidOperationException($"فیلتر روی فیلد غیرمجاز «{flt.Field}»");
            if (!ff.Filterable)
                throw new InvalidOperationException($"فیلد «{ff.Title}» قابل فیلتر نیست — فیلدهای محاسبه‌شده فقط قابل انتخاب‌اند");
        }

        // ── اعتبارسنجی گروه و تجمیع ──
        AdhocField? groupField = null;
        if (!string.IsNullOrWhiteSpace(definition.GroupBy))
        {
            if (!fieldMap.TryGetValue(definition.GroupBy, out groupField))
                throw new InvalidOperationException($"فیلد گروه‌بندی «{definition.GroupBy}» مجاز نیست");
        }
        AdhocField? aggField = null;
        if (definition.Aggregate != AdhocAggregateFunc.None && definition.Aggregate != AdhocAggregateFunc.Count)
        {
            if (string.IsNullOrWhiteSpace(definition.AggregateField) ||
                !fieldMap.TryGetValue(definition.AggregateField, out aggField))
                throw new InvalidOperationException("فیلد تجمیع نامعتبر است");
            if (aggField.Type is not (AdhocFieldType.Number or AdhocFieldType.Money))
                throw new InvalidOperationException("تجمیع فقط روی فیلدهای عددی/مبلغی مجاز است");
        }
        if (definition.Aggregate != AdhocAggregateFunc.None && groupField is null)
            throw new InvalidOperationException("برای تجمیع، یک فیلد گروه‌بندی لازم است");

        var limit = Math.Clamp(definition.Limit <= 0 ? 100 : definition.Limit, 1, MaxLimit);

        // ── ماده‌سازی ردیف‌های خام — فقط ستون‌های لیست سفید خوانده می‌شوند ──
        var raw = await MaterializeAsync(schema, ct);

        // ── اعمال فیلترها درون‌حافظه ──
        IEnumerable<Dictionary<string, object?>> rows = raw;
        foreach (var flt in definition.Filters)
        {
            var ff = fieldMap[flt.Field];
            rows = rows.Where(r => Matches(r[flt.Field], ff, flt));
        }

        // ── گروه‌بندی/تجمیع یا ردیف‌های ساده ──
        List<string> columns;
        List<List<string>> outRows;
        List<KeyValuePair<string, string>> summary;

        if (groupField is not null)
        {
            var groups = rows
                .GroupBy(r => Key(r[groupField.Name]) ?? "—")
                .Select(g => new { Key = g.Key, Rows = g.ToList() })
                .ToList();

            decimal Agg(IEnumerable<Dictionary<string, object?>> g) => definition.Aggregate switch
            {
                AdhocAggregateFunc.Count => g.Count(),
                AdhocAggregateFunc.Sum => g.Sum(r => ToDecimal(r[definition.AggregateField!])),
                AdhocAggregateFunc.Avg => g.Any() ? g.Average(r => ToDecimal(r[definition.AggregateField!])) : 0,
                AdhocAggregateFunc.Min => g.Any() ? g.Min(r => ToDecimal(r[definition.AggregateField!])) : 0,
                AdhocAggregateFunc.Max => g.Any() ? g.Max(r => ToDecimal(r[definition.AggregateField!])) : 0,
                _ => g.Count(),
            };

            var aggTitle = definition.Aggregate switch
            {
                AdhocAggregateFunc.Count => "تعداد",
                AdhocAggregateFunc.Sum => $"جمع {aggField!.Title}",
                AdhocAggregateFunc.Avg => $"میانگین {aggField!.Title}",
                AdhocAggregateFunc.Min => $"کمینه {aggField!.Title}",
                AdhocAggregateFunc.Max => $"بیشینه {aggField!.Title}",
                _ => "تعداد",
            };

            var grouped = groups
                .Select(g => new { g.Key, Value = Agg(g.Rows) })
                .OrderByDescending(x => x.Value)
                .Take(limit)
                .ToList();

            columns = new() { groupField.Title, aggTitle };
            outRows = grouped
                .Select(g => new List<string> { g.Key, FormatValue(aggField, g.Value) })
                .ToList();
            summary = new()
            {
                new("تعداد گروه", grouped.Count.ToString("N0")),
                new("جمع " + aggTitle, FormatValue(aggField, grouped.Sum(g => g.Value))),
                new("بیشترین گروه", grouped.FirstOrDefault()?.Key ?? "—"),
            };
        }
        else
        {
            // مرتب‌سازی
            var sortField = definition.SortField is not null && fieldMap.ContainsKey(definition.SortField)
                ? definition.SortField
                : fields[0];
            var sorted = definition.SortDesc
                ? rows.OrderByDescending(r => Key(r[sortField]), StringComparer.CurrentCulture)
                : rows.OrderBy(r => Key(r[sortField]), StringComparer.CurrentCulture);

            var limited = sorted.Take(limit).ToList();
            columns = fields.Select(f => fieldMap[f].Title).ToList();
            outRows = limited
                .Select(r => fields.Select(f => FormatValue(fieldMap[f], r[f])).ToList())
                .ToList();

            var matched = rows.Count();
            summary = new()
            {
                new("تعداد نتایج", limited.Count.ToString("N0")),
                new("کل ردیف‌های منطبق", matched.ToString("N0")),
                new("موجودیت", schema.Title),
            };
            if (matched > MaxScan)
                summary.Add(new("هشدار", $"فقط {MaxScan:N0} ردیف اخیر بررسی شد — برای دقت بیشتر فیلتر تاریخ بگذارید"));
        }

        var chartKind = definition.Chart is "none" or "" ? null : definition.Chart;
        var chartTitle = groupField is not null
            ? $"{(definition.Aggregate == AdhocAggregateFunc.Count ? "تعداد" : aggField!.Title)} به تفکیک {groupField.Title}"
            : null;

        return new AdhocResult(summary, columns, outRows, chartKind, chartTitle, aggField?.Type == AdhocFieldType.Money ? "ریال" : null);
    }

    /// <summary>تبدیل تعریف به JSON برای ذخیره در ReportDefinition.Parameters</summary>
    public static string Serialize(AdhocDefinition definition)
        => JsonSerializer.Serialize(definition, new JsonSerializerOptions { WriteIndented = false });

    /// <summary>بازخوانی تعریف از JSON ذخیره‌شده</summary>
    public static AdhocDefinition? Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<AdhocDefinition>(json); }
        catch { return null; }
    }

    #endregion

    #region ماده‌سازی ستون‌های مجاز

    /// <summary>ماده‌سازی ردیف‌های خام یک موجودیت — فقط ستون‌های لیست سفید خوانده می‌شوند</summary>
    private async Task<List<Dictionary<string, object?>>> MaterializeAsync(AdhocEntitySchema schema, CancellationToken ct)
    {
        var rows = new List<Dictionary<string, object?>>();
        switch (schema.Key)
        {
            case "patient":
            {
                var data = await _unitOfWork.Repository<Domain.Entities.People.Patient>().QueryableNoTracking
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.Id)
                    .Take(MaxScan)
                    .ToListAsync(ct);
                foreach (var p in data)
                    rows.Add(new Dictionary<string, object?>
                    {
                        ["Id"] = p.Id, ["MedicalRecordNo"] = p.MedicalRecordNo, ["NationalCode"] = p.NationalCode,
                        ["FirstName"] = p.FirstName, ["LastName"] = p.LastName, ["FatherName"] = p.FatherName,
                        ["Gender"] = PersianEnum.Gender(p.Gender), ["BirthDate"] = p.BirthDate,
                        ["MaritalStatus"] = PersianEnum.Marital(p.MaritalStatus), ["Mobile"] = p.Mobile,
                        ["City"] = p.City, ["Province"] = p.Province, ["InsuranceNumber"] = p.InsuranceNumber,
                        ["PatientType"] = PersianEnum.PatientType(p.PatientType),
                        ["Allergies"] = p.Allergies, ["ChronicDiseases"] = p.ChronicDiseases,
                        ["CreatedAt"] = p.CreatedAt,
                    });
                break;
            }
            case "appointment":
            {
                var data = await _unitOfWork.Repository<Domain.Entities.Scheduling.Appointment>().QueryableNoTracking
                    .Include(a => a.Patient).Include(a => a.Doctor!).ThenInclude(d => d.Employee)
                    .Where(a => !a.IsDeleted)
                    .OrderByDescending(a => a.Id)
                    .Take(MaxScan)
                    .ToListAsync(ct);
                foreach (var a in data)
                    rows.Add(new Dictionary<string, object?>
                    {
                        ["AppointmentCode"] = a.AppointmentCode,
                        ["PatientName"] = a.Patient is null ? null : $"{a.Patient.FirstName} {a.Patient.LastName}".Trim(),
                        ["DoctorName"] = a.Doctor?.Employee is null ? null : $"{a.Doctor.Employee.FirstName} {a.Doctor.Employee.LastName}".Trim(),
                        ["AppointmentDate"] = a.AppointmentDate,
                        ["AppointmentTime"] = a.AppointmentTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                        ["Status"] = PersianEnum.AppointmentStatus(a.Status),
                        ["AppointmentType"] = PersianEnum.AppointmentType(a.AppointmentType),
                        ["Source"] = PersianEnum.AppointmentSource(a.Source),
                        ["IsFirstVisit"] = a.IsFirstVisit,
                        ["Priority"] = PersianEnum.Priority(a.Priority),
                        ["EstimatedFee"] = a.EstimatedFee,
                        ["QueueNumber"] = a.QueueNumber,
                        ["CreatedAt"] = a.CreatedAt,
                    });
                break;
            }
            case "visit":
            {
                var data = await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>().QueryableNoTracking
                    .Include(v => v.Patient).Include(v => v.Doctor!).ThenInclude(d => d.Employee)
                    .Where(v => !v.IsDeleted)
                    .OrderByDescending(v => v.Id)
                    .Take(MaxScan)
                    .ToListAsync(ct);
                foreach (var v in data)
                    rows.Add(new Dictionary<string, object?>
                    {
                        ["VisitCode"] = v.VisitCode,
                        ["PatientName"] = v.Patient is null ? null : $"{v.Patient.FirstName} {v.Patient.LastName}".Trim(),
                        ["DoctorName"] = v.Doctor?.Employee is null ? null : $"{v.Doctor.Employee.FirstName} {v.Doctor.Employee.LastName}".Trim(),
                        ["VisitDate"] = v.VisitDate, ["VisitType"] = PersianEnum.VisitType(v.VisitType),
                        ["ChiefComplaint"] = v.ChiefComplaint, ["Status"] = PersianEnum.VisitStatus(v.Status),
                        ["CreatedAt"] = v.CreatedAt,
                    });
                break;
            }
            case "invoice":
            {
                var data = await _unitOfWork.Repository<Domain.Entities.Billing.Invoice>().QueryableNoTracking
                    .Include(i => i.Patient)
                    .Where(i => !i.IsDeleted)
                    .OrderByDescending(i => i.Id)
                    .Take(MaxScan)
                    .ToListAsync(ct);
                foreach (var i in data)
                    rows.Add(new Dictionary<string, object?>
                    {
                        ["InvoiceNumber"] = i.InvoiceNumber,
                        ["PatientName"] = i.Patient is null ? null : $"{i.Patient.FirstName} {i.Patient.LastName}".Trim(),
                        ["InvoiceDate"] = i.InvoiceDate, ["InvoiceType"] = PersianEnum.InvoiceType(i.InvoiceType),
                        ["SubTotal"] = i.SubTotal, ["DiscountAmount"] = i.DiscountAmount, ["Tax"] = i.Tax,
                        ["TotalAmount"] = i.TotalAmount, ["PaidAmount"] = i.PaidAmount, ["RemainingAmount"] = i.RemainingAmount,
                        ["Status"] = PersianEnum.InvoiceStatus(i.Status), ["PaymentStatus"] = PersianEnum.PaymentStatus(i.PaymentStatus),
                        ["CreatedAt"] = i.CreatedAt,
                    });
                break;
            }
            case "payment":
            {
                var data = await _unitOfWork.Repository<Domain.Entities.Billing.Payment>().QueryableNoTracking
                    .Include(p => p.Patient)
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.Id)
                    .Take(MaxScan)
                    .ToListAsync(ct);
                foreach (var p in data)
                    rows.Add(new Dictionary<string, object?>
                    {
                        ["PaymentNumber"] = p.PaymentNumber,
                        ["PatientName"] = p.Patient is null ? null : $"{p.Patient.FirstName} {p.Patient.LastName}".Trim(),
                        ["Amount"] = p.Amount, ["Method"] = PersianEnum.PaymentMethod(p.Method),
                        ["PaymentDate"] = p.PaymentDate, ["Status"] = PersianEnum.PaymentRecordStatus(p.Status),
                        ["IsReturned"] = p.IsReturned, ["CreatedAt"] = p.CreatedAt,
                    });
                break;
            }
            case "prescription":
            {
                var data = await _unitOfWork.Repository<Domain.Entities.Clinical.Prescription>().QueryableNoTracking
                    .Include(p => p.Patient).Include(p => p.Doctor!).ThenInclude(d => d.Employee)
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.Id)
                    .Take(MaxScan)
                    .ToListAsync(ct);
                foreach (var p in data)
                    rows.Add(new Dictionary<string, object?>
                    {
                        ["PrescriptionCode"] = p.PrescriptionCode,
                        ["PatientName"] = p.Patient is null ? null : $"{p.Patient.FirstName} {p.Patient.LastName}".Trim(),
                        ["DoctorName"] = p.Doctor?.Employee is null ? null : $"{p.Doctor.Employee.FirstName} {p.Doctor.Employee.LastName}".Trim(),
                        ["PrescriptionDate"] = p.PrescriptionDate, ["Type"] = PersianEnum.PrescriptionType(p.Type),
                        ["IsElectronic"] = p.IsElectronic, ["IsNarcotic"] = p.IsNarcotic,
                        ["Status"] = PersianEnum.PrescriptionStatus(p.Status), ["CreatedAt"] = p.CreatedAt,
                    });
                break;
            }
            case "laborder":
            {
                var data = await _unitOfWork.Repository<Domain.Entities.Paraclinical.LabOrder>().QueryableNoTracking
                    .Include(o => o.Patient).Include(o => o.Doctor!).ThenInclude(d => d.Employee)
                    .Where(o => !o.IsDeleted)
                    .OrderByDescending(o => o.Id)
                    .Take(MaxScan)
                    .ToListAsync(ct);
                foreach (var o in data)
                    rows.Add(new Dictionary<string, object?>
                    {
                        ["OrderNumber"] = o.OrderNumber,
                        ["PatientName"] = o.Patient is null ? null : $"{o.Patient.FirstName} {o.Patient.LastName}".Trim(),
                        ["DoctorName"] = o.Doctor?.Employee is null ? null : $"{o.Doctor.Employee.FirstName} {o.Doctor.Employee.LastName}".Trim(),
                        ["OrderDate"] = o.OrderDate, ["Priority"] = PersianEnum.Priority(o.Priority),
                        ["Status"] = PersianEnum.LabOrderStatus(o.Status), ["CreatedAt"] = o.CreatedAt,
                    });
                break;
            }
        }
        return rows;
    }

    #endregion

    #region فیلتر و قالب‌بندی

    /// <summary>تطبیق یک مقدار خام با شرط فیلتر</summary>
    private static bool Matches(object? value, AdhocField field, AdhocFilter f)
        => field.Type switch
        {
            AdhocFieldType.Text or AdhocFieldType.Computed => MatchesText(value?.ToString(), f),
            AdhocFieldType.Number => MatchesDecimal(ToNullableDecimal(value), f),
            AdhocFieldType.Money => MatchesDecimal(ToNullableDecimal(value), f),
            AdhocFieldType.Date => MatchesDate(value, f),
            AdhocFieldType.Enum => MatchesText(value?.ToString(), f),
            AdhocFieldType.Boolean => MatchesBool(value, f),
            _ => true,
        };

    /// <summary>فیلتر متنی</summary>
    private static bool MatchesText(string? value, AdhocFilter f) => f.Op switch
    {
        AdhocOp.Empty => string.IsNullOrWhiteSpace(value),
        AdhocOp.NotEmpty => !string.IsNullOrWhiteSpace(value),
        AdhocOp.Eq => string.Equals(value, f.Value1, StringComparison.OrdinalIgnoreCase),
        AdhocOp.Neq => !string.Equals(value, f.Value1, StringComparison.OrdinalIgnoreCase),
        AdhocOp.Contains => value?.Contains(f.Value1 ?? "", StringComparison.OrdinalIgnoreCase) == true,
        AdhocOp.NotContains => value?.Contains(f.Value1 ?? "", StringComparison.OrdinalIgnoreCase) != true,
        AdhocOp.StartsWith => value?.StartsWith(f.Value1 ?? "", StringComparison.OrdinalIgnoreCase) == true,
        AdhocOp.EndsWith => value?.EndsWith(f.Value1 ?? "", StringComparison.OrdinalIgnoreCase) == true,
        _ => true,
    };

    /// <summary>فیلتر عددی</summary>
    private static bool MatchesDecimal(decimal? value, AdhocFilter f)
    {
        var v1 = decimal.TryParse(f.Value1, NumberStyles.Any, CultureInfo.InvariantCulture, out var d1) ? d1 : (decimal?)null;
        var v2 = decimal.TryParse(f.Value2, NumberStyles.Any, CultureInfo.InvariantCulture, out var d2) ? d2 : (decimal?)null;
        return f.Op switch
        {
            AdhocOp.Eq => value is not null && v1 is not null && value == v1,
            AdhocOp.Neq => value is null || v1 is null || value != v1,
            AdhocOp.Gt => value is not null && v1 is not null && value > v1,
            AdhocOp.Gte => value is not null && v1 is not null && value >= v1,
            AdhocOp.Lt => value is not null && v1 is not null && value < v1,
            AdhocOp.Lte => value is not null && v1 is not null && value <= v1,
            AdhocOp.Between => value is not null && v1 is not null && v2 is not null && value >= v1 && value <= v2,
            AdhocOp.Empty => value is null,
            AdhocOp.NotEmpty => value is not null,
            _ => true,
        };
    }

    /// <summary>فیلتر تاریخی — مقدار خام DateOnly یا DateTime؛ مقدار شرط میلادی یا شمسی (۱۴۰۴/۰۱/۰۱)</summary>
    private static bool MatchesDate(object? value, AdhocFilter f)
    {
        DateTime? dt = value switch
        {
            DateTime d => d,
            DateOnly dob => dob.ToDateTime(TimeOnly.MinValue),
            _ => null,
        };
        if (f.Op is AdhocOp.Empty) return dt is null;
        if (f.Op is AdhocOp.NotEmpty) return dt is not null;
        if (dt is null) return false;

        DateTime? Parse(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var text = s.Trim();
            // ۱) میلادی استاندارد ISO با سال واقعی: 2025-04-10
            if (DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var g) && g.Year >= 1900)
                return g;
            // ۲) قالب شمسی ۱۴۰۴/۰۱/۰۱ — سال شمسی همیشه در بازه ۱۲۰۰..۱۶۰۰ است؛
            //    دقت: DateTime.TryParse خودش «1404/01/01» را سالِ میلادی ۱۴۰۴ می‌گیرد و باید قبل از آن هنگام شمسی را بگیریم
            var parts = text.Replace('-', '/').Split('/');
            if (parts.Length == 3 && int.TryParse(parts[0], out var y) && y is >= 1200 and <= 1600)
                return PersianDateHelper.ToGregorian(text);
            // ۳) سایر قالب‌های میلادی مثل 2025/4/10
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out g)) return g;
            return null;
        }

        var v1 = Parse(f.Value1);
        var v2 = Parse(f.Value2);
        return f.Op switch
        {
            AdhocOp.Eq => v1 is not null && dt.Value.Date == v1.Value.Date,
            AdhocOp.Neq => v1 is null || dt.Value.Date != v1.Value.Date,
            AdhocOp.Gt => v1 is not null && dt.Value.Date > v1.Value.Date,
            AdhocOp.Gte => v1 is not null && dt.Value.Date >= v1.Value.Date,
            AdhocOp.Lt => v1 is not null && dt.Value.Date < v1.Value.Date,
            AdhocOp.Lte => v1 is not null && dt.Value.Date <= v1.Value.Date,
            AdhocOp.Between => v1 is not null && v2 is not null && dt.Value.Date >= v1.Value.Date && dt.Value.Date <= v2.Value.Date,
            _ => true,
        };
    }

    /// <summary>فیلتر بولی — مقدار شرط: true/false یا بله/خیر</summary>
    private static bool MatchesBool(object? value, AdhocFilter f)
    {
        var b = value is bool bb && bb;
        var target = f.Value1 is not null && (f.Value1.Equals("true", StringComparison.OrdinalIgnoreCase)
            || f.Value1 == "بله" || f.Value1 == "1");
        return f.Op switch
        {
            AdhocOp.Eq => b == target,
            AdhocOp.Neq => b != target,
            _ => true,
        };
    }

    /// <summary>کلید گروه‌بندی و مرتب‌سازی — تاریخ‌ها شمسی و اعداد استاندارد</summary>
    private static string? Key(object? value) => value switch
    {
        null => null,
        DateOnly d => PersianDateHelper.ToShamsi(d.ToDateTime(TimeOnly.MinValue)),
        DateTime dt => PersianDateHelper.ToShamsi(dt),
        decimal m => m.ToString("0.##", CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    /// <summary>تبدیل مقدار خام به عدد برای تجمیع</summary>
    private static decimal ToDecimal(object? value) => ToNullableDecimal(value) ?? 0m;

    /// <summary>تبدیل امن به عدد اعشاری nullable</summary>
    private static decimal? ToNullableDecimal(object? value) => value switch
    {
        null => null,
        decimal d => d,
        int i => i,
        long l => l,
        double db => (decimal)db,
        _ => decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : null,
    };

    /// <summary>قالب‌بندی مقدار خروجی بر اساس نوع فیلد — تاریخ شمسی و اعداد جداکننده‌دار</summary>
    private static string FormatValue(AdhocField? field, object? value)
    {
        if (value is null) return "—";
        return field?.Type switch
        {
            AdhocFieldType.Date => value switch
            {
                DateTime dt => PersianDateHelper.ToShamsi(dt),
                DateOnly d => PersianDateHelper.ToShamsi(d.ToDateTime(TimeOnly.MinValue)),
                _ => value.ToString() ?? "—",
            },
            AdhocFieldType.Money => ToNullableDecimal(value)?.ToString("N0") ?? "—",
            AdhocFieldType.Number => ToNullableDecimal(value)?.ToString("N0") ?? "—",
            AdhocFieldType.Boolean => value is true ? "بله" : "خیر",
            _ => value.ToString() ?? "—",
        };
    }

    #endregion
}

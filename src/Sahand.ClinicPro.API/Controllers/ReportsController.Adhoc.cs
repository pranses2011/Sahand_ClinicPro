using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Reports.Adhoc;
using Sahand.ClinicPro.Domain.Entities.Reporting;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// بخش گزارش‌ساز رایگانه (کوئری‌ساز گرافیکی) — چرخه سیزدهم (درخواست ۴-۳).
/// کاربر بدون SQL از لیست سفید موجودیت/فیلد گزارش می‌سازد؛ اجرای امن
/// سمت سرور + ذخیره تعریف در ReportDefinition (دسته Adhoc) و اجرای مجدد.
/// مسیرها: /api/v1/reports/adhoc
/// </summary>
public partial class ReportsController
{
    /// <summary>آیا موتور گزارش رایگانه از قبل تزریق شده؟ (برای بک‌سازگاری سازنده)</summary>
    private AdhocQueryEngine? _adhocEngine;

    /// <summary>موتور گزارش رایگانه — ساخت تنبل برای پرهیز از تغییر سازنده‌های موجود</summary>
    private AdhocQueryEngine Adhoc => _adhocEngine ??= new AdhocQueryEngine(_unitOfWork);

    /// <summary>طرح لیست سفید — موجودیت‌ها/فیلدهای مجاز با نوع و گزینه‌های شمارشی</summary>
    [HttpGet("adhoc/schema")]
    [HasPermission("reports.view")]
    public IActionResult AdhocSchema()
    {
        return Ok(ApiResponse<object>.Success(new
        {
            maxLimit = AdhocQueryEngine.MaxLimit,
            maxFields = AdhocQueryEngine.MaxFields,
            operators = new[]
            {
                new { key = "eq", title = "مساوی" }, new { key = "neq", title = "نامساوی" },
                new { key = "contains", title = "شامل" }, new { key = "notContains", title = "شامل نیست" },
                new { key = "startsWith", title = "شروع با" }, new { key = "endsWith", title = "پایان با" },
                new { key = "gt", title = "بزرگ‌تر" }, new { key = "gte", title = "بزرگ‌تر مساوی" },
                new { key = "lt", title = "کوچک‌تر" }, new { key = "lte", title = "کوچک‌تر مساوی" },
                new { key = "between", title = "مابین" },
                new { key = "empty", title = "خالی" }, new { key = "notEmpty", title = "پر" },
            },
            aggregates = new[]
            {
                new { key = "none", title = "بدون تجمیع (فهرست)" },
                new { key = "count", title = "شمارش" },
                new { key = "sum", title = "جمع" },
                new { key = "avg", title = "میانگین" },
                new { key = "min", title = "کمینه" },
                new { key = "max", title = "بیشینه" },
            },
            entities = AdhocQueryEngine.Schema.Select(e => new
            {
                key = e.Key,
                title = e.Title,
                icon = e.Icon,
                fields = e.Fields.Select(f => new
                {
                    name = f.Name,
                    title = f.Title,
                    type = f.Type.ToString().ToLowerInvariant(),
                    filterable = f.Filterable,
                    options = f.EnumOptions,
                }),
            }),
        }));
    }

    /// <summary>اجرای امن تعریف گزارش رایگانه — بدون ذخیره</summary>
    /// <remarks>بدنه: { entity, fields[], filters[{field,op,value1,value2}], groupBy, aggregate, aggregateField, sortField, sortDesc, limit, chart }</remarks>
    [HttpPost("adhoc/run")]
    [HasPermission("reports.view")]
    public async Task<IActionResult> AdhocRun([FromBody] AdhocRunRequest request, CancellationToken ct)
    {
        if (request.Definition is null)
            return BadRequest(ApiResponse<string>.Failure("بدنه درخواست تعریف گزارش ندارد"));

        var definition = request.Definition.ToDefinition();
        try
        {
            var result = await Adhoc.RunAsync(definition, ct);
            return Ok(ApiResponse<object>.Success(new
            {
                available = true,
                summary = result.Summary.ToDictionary(s => s.Key, s => s.Value),
                columns = result.Columns,
                rows = result.Rows.Select(r => r.Select(c => (object)c).ToList()).ToList(),
                chart = result.ChartKind is null ? null : new
                {
                    type = result.ChartKind,
                    title = result.ChartTitle,
                    unit = result.ChartUnit,
                    points = BuildChartPoints(result),
                },
            }));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<object>.Success(new { available = false, message = ex.Message }));
        }
    }

    /// <summary>ذخیره تعریف گزارش رایگانه در ReportDefinition (دسته Adhoc) برای اجرای مجدد</summary>
    [HttpPost("adhoc/definitions")]
    [HasPermission("reports.view")]
    public async Task<IActionResult> AdhocSave([FromBody] AdhocSaveRequest request, CancellationToken ct)
    {
        if (request.Definition is null || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<string>.Failure("نام گزارش و تعریف آن الزامی است"));

        var definition = request.Definition.ToDefinition();
        // اعتبارسنجی پیش از ذخیره — تعریف معتبر باید اجراپذیر باشد
        _ = await Adhoc.RunAsync(definition with { Limit = 1 }, ct);

        var entity = new ReportDefinition
        {
            ReportName = request.Name.Trim(),
            Code = $"ADHOC-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            Category = "Adhoc",
            Description = "گزارش رایگانه ساخته‌شده با کوئری‌ساز گرافیکی",
            Parameters = AdhocQueryEngine.Serialize(definition),
            RequiredPermission = "reports.view",
        };
        await _unitOfWork.Repository<ReportDefinition>().AddAsync(entity, ct);
        await _unitOfWork.SaveEntitiesAsync(ct);
        return Ok(ApiResponse<object>.Success(new { id = entity.Id, code = entity.Code }));
    }

    /// <summary>فهرست تعریف‌های ذخیره‌شده گزارش رایگانه</summary>
    [HttpGet("adhoc/definitions")]
    [HasPermission("reports.view")]
    public async Task<IActionResult> AdhocList(CancellationToken ct)
    {
        var list = await _reporting.Set<ReportDefinition>().AsNoTracking()
            .Where(r => r.Category == "Adhoc" && !r.IsDeleted)
            .OrderByDescending(r => r.Id)
            .Select(r => new { r.Id, r.ReportName, r.Code, r.Parameters, r.CreatedAt })
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Success(list));
    }

    /// <summary>حذف نرم تعریف ذخیره‌شده</summary>
    [HttpDelete("adhoc/definitions/{id:long}")]
    [HasPermission("reports.view")]
    public async Task<IActionResult> AdhocDelete(long id, CancellationToken ct)
    {
        var entity = await _unitOfWork.Repository<ReportDefinition>().QueryableTracking
            .FirstOrDefaultAsync(r => r.Id == id && r.Category == "Adhoc" && !r.IsDeleted, ct);
        if (entity is null)
            return NotFound(ApiResponse<string>.Failure("تعریف گزارش یافت نشد"));

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.SaveEntitiesAsync(ct);
        return Ok(ApiResponse<object>.Success(new { deleted = true }));
    }

    /// <summary>ساخت نقاط نمودار از نتیجه گروه‌بندی — سطر اول=برچسب، سطر دوم=مقدار</summary>
    private static List<object> BuildChartPoints(AdhocResult result)
    {
        var pts = new List<object>();
        foreach (var row in result.Rows)
        {
            if (row.Count < 2) continue;
            var label = row[0];
            // زدن جداکننده‌های عددی برای بازخوانی مقدار عددی نمودار
            var raw = row[1].Replace(",", "").Replace("،", "");
            pts.Add(new { label, value = double.TryParse(raw, out var v) ? v : 0 });
        }
        return pts;
    }
}

#region مدل‌های درخواست

/// <summary>درخواست اجرای گزارش رایگانه</summary>
public class AdhocRunRequest
{
    /// <summary>تعریف گزارش</summary>
    public AdhocDefinitionDto? Definition { get; set; }
}

/// <summary>درخواست ذخیره گزارش رایگانه</summary>
public class AdhocSaveRequest
{
    /// <summary>نام نمایشی گزارش</summary>
    public string? Name { get; set; }

    /// <summary>تعریف گزارش</summary>
    public AdhocDefinitionDto? Definition { get; set; }
}

/// <summary>DTO تعریف گزارش رایگانه — از JSON درخواست به مدل دامنه‌ای موتور</summary>
public class AdhocDefinitionDto
{
    /// <summary>کلید موجودیت لیست سفید</summary>
    public string Entity { get; set; } = "patient";

    /// <summary>فیلدهای انتخابی</summary>
    public List<string> Fields { get; set; } = new();

    /// <summary>فیلترها</summary>
    public List<AdhocFilterDto> Filters { get; set; } = new();

    /// <summary>فیلد گروه‌بندی اختیاری</summary>
    public string? GroupBy { get; set; }

    /// <summary>عمل تجمیع — none/count/sum/avg/min/max</summary>
    public string? Aggregate { get; set; }

    /// <summary>فیلد تجمیع عددی/مبلغی</summary>
    public string? AggregateField { get; set; }

    /// <summary>فیلد مرتب‌سازی</summary>
    public string? SortField { get; set; }

    /// <summary>نزولی؟</summary>
    public bool SortDesc { get; set; }

    /// <summary>سقف ردیف خروجی</summary>
    public int Limit { get; set; } = 100;

    /// <summary>نوع نمودار — column/doughnut/hbar/none</summary>
    public string? Chart { get; set; }

    /// <summary>تبدیل به مدل موتور</summary>
    public AdhocDefinition ToDefinition() => new(
        Entity,
        Fields,
        Filters.Select(f => new AdhocFilter(
            f.Field ?? "",
            ParseOp(f.Op),
            f.Value1,
            f.Value2)).ToList(),
        GroupBy,
        ParseAggregate(Aggregate),
        AggregateField,
        SortField,
        SortDesc,
        Limit,
        Chart ?? "column");

    private static AdhocOp ParseOp(string? op) => op switch
    {
        "eq" => AdhocOp.Eq, "neq" => AdhocOp.Neq,
        "contains" => AdhocOp.Contains, "notContains" => AdhocOp.NotContains,
        "startsWith" => AdhocOp.StartsWith, "endsWith" => AdhocOp.EndsWith,
        "gt" => AdhocOp.Gt, "gte" => AdhocOp.Gte, "lt" => AdhocOp.Lt, "lte" => AdhocOp.Lte,
        "between" => AdhocOp.Between, "empty" => AdhocOp.Empty, "notEmpty" => AdhocOp.NotEmpty,
        _ => AdhocOp.Eq,
    };

    private static AdhocAggregateFunc ParseAggregate(string? agg) => agg switch
    {
        "count" => AdhocAggregateFunc.Count, "sum" => AdhocAggregateFunc.Sum,
        "avg" => AdhocAggregateFunc.Avg, "min" => AdhocAggregateFunc.Min,
        "max" => AdhocAggregateFunc.Max, _ => AdhocAggregateFunc.None,
    };
}

/// <summary>DTO یک شرط فیلتر</summary>
public class AdhocFilterDto
{
    /// <summary>نام فیلد لیست سفید</summary>
    public string? Field { get; set; }

    /// <summary>کلید عملگر</summary>
    public string? Op { get; set; }

    /// <summary>مقدار اول (یا تنها مقدار)</summary>
    public string? Value1 { get; set; }

    /// <summary>مقدار دوم عملگر مابین</summary>
    public string? Value2 { get; set; }
}

#endregion

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.Features.Reporting.PrintTemplates;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Entities.Reporting;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر قالب‌های چاپ سفارشی — طراح آنلاین با پیش‌نمایش زنده (چرخه ۱۳، درخواست ۱-۶)
/// مسیرها: /api/v1/print-templates
/// </summary>
[ApiController]
[Route("api/v1/print-templates")]
[Authorize]
public class PrintTemplatesController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با واحد کار</summary>
    public PrintTemplatesController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>فهرست قالب‌ها — با فیلتر نوع اختیاری</summary>
    /// <param name="type">فیلتر نوع — مثل Invoice یا Prescription</param>
    /// <param name="includeInactive">شامل قالب‌های غیرفعال</param>
    [HttpGet]
    [HasPermission("settings.view")]
    public async Task<IActionResult> GetAll([FromQuery] string? type = null,
        [FromQuery] bool includeInactive = true)
    {
        var query = _unitOfWork.Repository<PrintTemplate>().QueryableNoTracking
            .Where(t => !t.IsDeleted);
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<PrintTemplateType>(type, true, out var tFilter))
            query = query.Where(t => t.Type == tFilter);
        if (!includeInactive)
            query = query.Where(t => t.IsActive);

        // ═══ چرخهٔ سی‌ودوم ═══ پروجکشن دو مرحله‌ای: enum با ToString در
        // پروجکشن EF Core به SQL ترجمه نمی‌شود؛ ابتدا بار سبک (نوع عددی) خوانده
        // و نگاشت رشته‌ای/فارسی در حافظه انجام می‌شود — هم قابل‌ترجمه هم سریع
        var rows = await query
            .OrderBy(t => t.Type).ThenByDescending(t => t.IsDefault).ThenBy(t => t.TemplateName)
            .Select(t => new
            {
                t.Id, t.TemplateName, Type = (int)t.Type,
                t.PaperSize, t.Orientation, t.ShowClinicLogo, t.IsActive, t.IsDefault
            })
            .ToListAsync();

        var items = rows.Select(t =>
        {
            var type = (PrintTemplateType)t.Type;
            return new PrintTemplateListDto(
                t.Id, t.TemplateName, type.ToString(), TypeFa(type), t.PaperSize, t.Orientation,
                t.ShowClinicLogo, t.IsActive, t.IsDefault);
        }).ToList();

        return Ok(ApiResponse<List<PrintTemplateListDto>>.Success(items));
    }

    /// <summary>جزئیات کامل یک قالب</summary>
    /// <param name="id">شناسه قالب</param>
    [HttpGet("{id:long}")]
    [HasPermission("settings.view")]
    public async Task<IActionResult> GetById(long id)
    {
        var t = await _unitOfWork.Repository<PrintTemplate>().QueryableNoTracking
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (t is null)
            return NotFound(ApiResponse<object>.Failure("قالب یافت نشد"));

        return Ok(ApiResponse<PrintTemplateDetailDto>.Success(ToDetail(t)));
    }

    /// <summary>پیش‌نمایش زنده قالب با داده نمونه — HTML کامل آماده نمایش</summary>
    /// <param name="id">شناسه قالب</param>
    [HttpGet("{id:long}/preview")]
    [HasPermission("settings.view")]
    public async Task<IActionResult> Preview(long id)
    {
        var t = await _unitOfWork.Repository<PrintTemplate>().QueryableNoTracking
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (t is null)
            return NotFound(ApiResponse<object>.Failure("قالب یافت نشد"));

        if (t.Type == PrintTemplateType.ReceptionReceipt && t.HtmlBody == BuiltInPrintTemplates.ReceptionReceiptMarker)
            return Ok(ApiResponse<object>.Success(new { html = "", useOriginalReceptionReceipt = true }));

        if (PrintTemplateRenderer.HasUnsafeVariablePlacement(t.HtmlBody))
            return BadRequest(ApiResponse<object>.Failure("متغیرهای قالب فقط در متن مجازند، نه در صفت HTML، استایل یا اسکریپت."));
        var clinic = await ClinicInfoAsync();
        var sample = PrintTemplateRenderer.SampleData(t.Type.ToString());
        sample.TryAdd("clinic_name", clinic.ClinicName ?? "درمانگاه");
        sample.TryAdd("clinic_address", clinic.Address ?? "");
        sample.TryAdd("clinic_phone", clinic.Phone ?? "");
        sample.TryAdd("print_date", PersianDateHelper.ToShamsi(DateTime.UtcNow));
        var body = PrintTemplateRenderer.Render(t.HtmlBody, sample);
        var html = PrintTemplateRenderer.WrapDocument(
            body, clinic.ClinicName ?? "درمانگاه سهند", clinic.Address, clinic.Phone,
            logoUrl: null, t.AccentColor, t.FontSize, t.FontFamily,
            t.ShowClinicLogo, t.PaperSize, t.Orientation).Replace("__PRINT_DATE__", PersianDateHelper.ToShamsi(DateTime.UtcNow));

        return Ok(ApiResponse<object>.Success(new { html, paperSize = t.PaperSize, type = t.Type.ToString() }));
    }

    /// <summary>رندر با متغیرهای واقعی — برای استفاده ماژول‌های چاپ (فاکتور/نسخه/…)</summary>
    /// <param name="type">نوع سند — قالب پیش‌فرض همان نوع استفاده می‌شود</param>
    /// <param name="request">متغیرهای سند</param>
    /// <param name="authorization">بررسی دسترسی متناسب با نوع سند</param>
    [HttpPost("render/{type}")]
    public async Task<IActionResult> Render(string type, [FromBody] RenderRequest request,
        [FromServices] IAuthorizationService authorization)
    {
        if (!Enum.TryParse<PrintTemplateType>(type, true, out var tType) || !Enum.IsDefined(tType))
            return BadRequest(ApiResponse<object>.Failure("نوع سند نامعتبر است"));

        var canViewSettings = (await authorization.AuthorizeAsync(User, "PERMISSION:settings.view")).Succeeded;
        var canPrintReceipt = tType == PrintTemplateType.ReceptionReceipt &&
            (await authorization.AuthorizeAsync(User, "PERMISSION:receptions.print")).Succeeded;
        var canPrintAppointment = tType == PrintTemplateType.AppointmentTicket &&
            (await authorization.AuthorizeAsync(User, "PERMISSION:appointments.create")).Succeeded;
        var canPrintLabResult = tType == PrintTemplateType.LabResult &&
            (await authorization.AuthorizeAsync(User, "PERMISSION:lab.view")).Succeeded;
        var canPrintInvoice = tType == PrintTemplateType.Invoice &&
            (await authorization.AuthorizeAsync(User, "PERMISSION:invoices.view")).Succeeded;
        if (!canViewSettings && !canPrintReceipt && !canPrintAppointment && !canPrintLabResult && !canPrintInvoice) return Forbid();

        var template = await _unitOfWork.Repository<PrintTemplate>().QueryableNoTracking
            .Where(t => !t.IsDeleted && t.IsActive && t.Type == tType)
            .OrderByDescending(t => t.IsDefault)
            .FirstOrDefaultAsync();
        if (template is null)
            return NotFound(ApiResponse<object>.Failure("قالب فعالی برای این نوع سند ثبت نشده است"));

        // قبض اصلی دقیقاً با FlowDocument قبلی در کلاینت چاپ می‌شود، نه
        // بازسازی تقریبی HTML. انتخاب قالب‌های دیگر همچنان HTML را رندر می‌کند.
        if (tType == PrintTemplateType.ReceptionReceipt &&
            template.HtmlBody == BuiltInPrintTemplates.ReceptionReceiptMarker)
            return Ok(ApiResponse<object>.Success(new
            {
                html = "", templateId = template.Id, templateName = template.TemplateName,
                useOriginalReceptionReceipt = true
            }));

        if (PrintTemplateRenderer.HasUnsafeVariablePlacement(template.HtmlBody))
            return BadRequest(ApiResponse<object>.Failure("متغیرهای قالب فقط در متن مجازند، نه در صفت HTML، استایل یا اسکریپت."));
        var clinic = await ClinicInfoAsync();
        var variables = request.Variables ?? new Dictionary<string, object?>();
        variables.TryAdd("clinic_name", clinic.ClinicName ?? "درمانگاه");
        variables.TryAdd("clinic_address", clinic.Address ?? "");
        variables.TryAdd("clinic_phone", clinic.Phone ?? "");
        variables.TryAdd("print_date", PersianDateHelper.ToShamsi(DateTime.UtcNow));
        var body = PrintTemplateRenderer.Render(template.HtmlBody, variables);
        var html = PrintTemplateRenderer.WrapDocument(
            body, clinic.ClinicName ?? "درمانگاه سهند", clinic.Address, clinic.Phone,
            logoUrl: null, template.AccentColor, template.FontSize, template.FontFamily,
            template.ShowClinicLogo, template.PaperSize, template.Orientation).Replace("__PRINT_DATE__", PersianDateHelper.ToShamsi(DateTime.UtcNow));

        return Ok(ApiResponse<object>.Success(new { html, templateId = template.Id, templateName = template.TemplateName }));
    }

    /// <summary>پالت متغیرها — برای طراح دسکتاپ</summary>
    [HttpGet("variables")]
    [HasPermission("settings.view")]
    public IActionResult Variables()
        => Ok(ApiResponse<Dictionary<string, string[]>>.Success(PrintTemplateRenderer.VariablesOf()));

    /// <summary>ثبت قالب جدید</summary>
    /// <param name="dto">داده‌های قالب</param>
    [HttpPost]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> Create([FromBody] SavePrintTemplateDto dto)
    {
        // ═══ چرخهٔ سی‌ودوم (گزارش مالک) ═══ قرارداد نوع قالب رشته‌ای شد
        // ("Invoice"/"Prescription"/…) — پیش‌تر DTO از نوع enum بود و کلاینتِ
        // دسکتاپ که نام نوع را به‌صورت رشته می‌فرستاد با خطای اعتبارسنجی
        // «اطلاعات ارسالی نامعتبر است» رد می‌شد و ذخیرهٔ قالب هیچ‌وقت ممکن نبود.
        if (!TryParseTemplateType(dto.Type, out var parsedType))
            return BadRequest(ApiResponse<object>.Failure(
                "نوع قالب نامعتبر است — مثل Invoice یا Prescription"));

        var error = Validate(dto);
        if (error is not null) return BadRequest(ApiResponse<object>.Failure(error));

        var template = new PrintTemplate
        {
            TemplateName = dto.TemplateName.Trim(),
            Type = parsedType,
            Description = dto.Description?.Trim(),
            HtmlBody = dto.HtmlBody,
            PaperSize = dto.PaperSize,
            Orientation = dto.Orientation,
            FontSize = dto.FontSize,
            FontFamily = dto.FontFamily?.Trim(),
            ShowClinicLogo = dto.ShowClinicLogo,
            AccentColor = dto.AccentColor?.Trim(),
            IsActive = dto.IsActive,
            IsDefault = dto.IsDefault
        };

        if (template.IsDefault)
            await ClearDefaultAsync(template.Type, null);

        await _unitOfWork.Repository<PrintTemplate>().AddAsync(template);
        await _unitOfWork.SaveEntitiesAsync();
        return CreatedResponse(
            Result<PrintTemplateDetailDto>.Success(ToDetail(template)),
            nameof(GetById), new { id = template.Id }, $"قالب «{template.TemplateName}» ثبت شد");
    }

    /// <summary>ویرایش قالب</summary>
    /// <param name="id">شناسه قالب</param>
    /// <param name="dto">داده‌های جدید</param>
    [HttpPut("{id:long}")]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> Update(long id, [FromBody] SavePrintTemplateDto dto)
    {
        var template = await _unitOfWork.Repository<PrintTemplate>().QueryableTracking
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (template is null)
            return NotFound(ApiResponse<object>.Failure("قالب یافت نشد"));

        // چرخهٔ سی‌ودوم: نوع قالب به‌صورت رشته از کلاینت می‌آید (توضیح در Create)
        if (!TryParseTemplateType(dto.Type, out var parsedType))
            return BadRequest(ApiResponse<object>.Failure(
                "نوع قالب نامعتبر است — مثل Invoice یا Prescription"));

        var error = Validate(dto);
        if (error is not null) return BadRequest(ApiResponse<object>.Failure(error));

        if (template.HtmlBody == BuiltInPrintTemplates.ReceptionReceiptMarker &&
            (dto.HtmlBody != BuiltInPrintTemplates.ReceptionReceiptMarker || parsedType != PrintTemplateType.ReceptionReceipt))
            return BadRequest(ApiResponse<object>.Failure(
                "قبض اصلی با موتور داخلی چاپ می‌شود؛ برای طراحی HTML یک قالب جدید بسازید."));

        if (dto.IsDefault && !template.IsDefault)
            await ClearDefaultAsync(parsedType, id);

        template.TemplateName = dto.TemplateName.Trim();
        template.Type = parsedType;
        template.Description = dto.Description?.Trim();
        template.HtmlBody = dto.HtmlBody;
        template.PaperSize = dto.PaperSize;
        template.Orientation = dto.Orientation;
        template.FontSize = dto.FontSize;
        template.FontFamily = dto.FontFamily?.Trim();
        template.ShowClinicLogo = dto.ShowClinicLogo;
        template.AccentColor = dto.AccentColor?.Trim();
        template.IsActive = dto.IsActive;
        template.IsDefault = dto.IsDefault;

        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<PrintTemplateDetailDto>.Success(ToDetail(template), "قالب به‌روزرسانی شد"));
    }

    /// <summary>قالب پیش‌فرض یک نوع را جابه‌جا می‌کند</summary>
    /// <param name="id">شناسه قالب</param>
    [HttpPost("{id:long}/set-default")]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> SetDefault(long id)
    {
        var template = await _unitOfWork.Repository<PrintTemplate>().QueryableTracking
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (template is null)
            return NotFound(ApiResponse<object>.Failure("قالب یافت نشد"));

        await ClearDefaultAsync(template.Type, id);
        template.IsDefault = true;
        template.IsActive = true; // قالب پیش‌فرض باید فعال باشد
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.Success(new { template.Id }, "قالب پیش‌فرض این نوع سند شد"));
    }

    /// <summary>بازکردن (کپی) قالب — «ذخیره به‌عنوان نسخه جدید»</summary>
    /// <param name="id">شناسه قالب مبدأ</param>
    /// <param name="request">درخواست کپی — نام و توضیح نسخهٔ جدید (چرخه بیست‌وسوم رفع CS1572/CS1573)</param>
    [HttpPost("{id:long}/duplicate")]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> Duplicate(long id, [FromBody] DuplicateRequest request)
    {
        var src = await _unitOfWork.Repository<PrintTemplate>().QueryableNoTracking
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (src is null)
            return NotFound(ApiResponse<object>.Failure("قالب یافت نشد"));

        var copy = new PrintTemplate
        {
            TemplateName = string.IsNullOrWhiteSpace(request.NewName)
                ? $"{src.TemplateName} (کپی)" : request.NewName.Trim(),
            Type = src.Type,
            Description = src.Description,
            HtmlBody = src.HtmlBody,
            PaperSize = src.PaperSize,
            Orientation = src.Orientation,
            FontSize = src.FontSize,
            FontFamily = src.FontFamily,
            ShowClinicLogo = src.ShowClinicLogo,
            AccentColor = src.AccentColor,
            IsActive = src.IsActive,
            IsDefault = false // کپی هرگز خودکار پیش‌فرض نمی‌شود
        };
        await _unitOfWork.Repository<PrintTemplate>().AddAsync(copy);
        await _unitOfWork.SaveEntitiesAsync();
        return CreatedResponse(
            Result<PrintTemplateDetailDto>.Success(ToDetail(copy)),
            nameof(GetById), new { id = copy.Id }, $"نسخه جدید «{copy.TemplateName}» ساخته شد");
    }

    /// <summary>حذف نرم قالب</summary>
    /// <param name="id">شناسه قالب</param>
    [HttpDelete("{id:long}")]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> Delete(long id)
    {
        var template = await _unitOfWork.Repository<PrintTemplate>().QueryableTracking
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (template is null)
            return NotFound(ApiResponse<object>.Failure("قالب یافت نشد"));
        if (template.IsDefault)
            return BadRequest(ApiResponse<object>.Failure(
                "قالب پیش‌فرض قابل حذف نیست — ابتدا قالب دیگری را پیش‌فرض کنید"));

        template.IsDeleted = true;
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.Success(new { }, "قالب حذف شد"));
    }

    /* ═══════════ کمکی ═══════════ */

    /// <summary>اطلاعات درمانگاه برای سربرگ</summary>
    private async Task<(string? ClinicName, string? Address, string? Phone)> ClinicInfoAsync()
    {
        var info = await _unitOfWork.Repository<ClinicInfo>().QueryableNoTracking
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync();
        return (info?.ClinicName, info?.Address, info?.Phone);
    }

    /// <summary>پاک‌کردن پرچم پیش‌فرض بقیه قالب‌های همان نوع</summary>
    private async Task ClearDefaultAsync(PrintTemplateType type, long? exceptId)
    {
        var others = await _unitOfWork.Repository<PrintTemplate>().QueryableTracking
            .Where(t => !t.IsDeleted && t.Type == type && t.IsDefault && t.Id != exceptId)
            .ToListAsync();
        foreach (var t in others)
            t.IsDefault = false;
    }

    /// <summary>اعتبارسنجی مشترک</summary>
    private static string? Validate(SavePrintTemplateDto dto)
    {
        if (dto.IsDefault && !dto.IsActive)
            return "قالب پیش‌فرض باید فعال باشد";
        if (string.IsNullOrWhiteSpace(dto.TemplateName) || dto.TemplateName.Trim().Length < 3)
            return "نام قالب حداقل ۳ نویسه باشد";
        if (string.IsNullOrWhiteSpace(dto.HtmlBody) || dto.HtmlBody.Trim().Length < 10)
            return "بدنه HTML قالب خالی است";
        if (dto.HtmlBody != BuiltInPrintTemplates.ReceptionReceiptMarker && PrintTemplateRenderer.HasUnsafeVariablePlacement(dto.HtmlBody))
            return "متغیرهای قالب فقط در متن مجازند، نه در صفت HTML، استایل یا اسکریپت.";
        // ═══ چرخه سی‌ویکم (گزارش مالک) — قبول پهنای کامل اندازه‌های کاغذ:
        // استانداردهای A3/A4/A5/A6/B5/Letter/Legal + فیش حرارتی ۸۰/۵۸ میلی‌متری
        // + قالب سفارشی «Custom:عرضxارتفاع» (میلی‌متر). پیش‌تر فقط A4/A5/A6
        // پذیرفته می‌شد و ذخیرهٔ ویرایش قالب‌های حرارتیِ بذرفشان با خطای
        // «اندازه کاغذ باید A4 یا A5 یا A6 باشد» شکست می‌خورد ═══
        if (!IsValidPaperSize(dto.PaperSize))
            return "اندازه کاغذ معتبر نیست — استاندارد (A3…Legal)، حرارتی (Thermal80/58) یا Custom:عرضxارتفاع";
        if (!string.Equals(dto.Orientation, "Portrait", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(dto.Orientation, "Landscape", StringComparison.OrdinalIgnoreCase))
            return "جهت کاغذ معتبر نیست.";
        if (string.Equals(dto.Orientation, "Landscape", StringComparison.OrdinalIgnoreCase) && PrintTemplateRenderer.LandscapePaper(dto.PaperSize) is null)
            return "برای کاغذ پیوسته یا اندازه نامعتبر، جهت افقی پشتیبانی نمی‌شود.";
        if (dto.FontSize is < 8 or > 24)
            return "اندازه فونت بین ۸ تا ۲۴ باشد";
        return null;
    }

    /// <summary>
    /// تحلیل امن نوع قالب — چرخهٔ سی‌ودوم: قرارداد نوع رشته‌ای شد تا کلاینتِ
    /// ارسال‌کنندهٔ نام نوع (مثل "Invoice") بدون خطای بستهٔ بندی JSON/System.Text.Json
    /// (که پیش‌فرض رشته را به enum نمی‌پذیرد) پذیرفته شود؛ نام فارسی هم پذیرفته نمی‌شود
    /// چون قرارداد رسمی، نام لاتین enum است.
    /// </summary>
    private static bool TryParseTemplateType(string? type, out PrintTemplateType parsed)
        => Enum.TryParse(type, true, out parsed) && Enum.IsDefined(parsed);

    /// <summary>
    /// اعتبارسنجی اندازهٔ کاغذ — پذیرش اندازه‌های استاندارد، فیش حرارتی و قالب سفارشی
    /// الگوی سفارشی: Custom:100x180 یا Custom:100×180 (میلی‌متر، اعداد صحیح ۲۰ تا ۱۰۰۰)
    /// </summary>
    private static bool IsValidPaperSize(string? paper)
    {
        if (string.IsNullOrWhiteSpace(paper)) return false;
        if (new[] { "A3", "A4", "A5", "A6", "B5", "Letter", "Legal", "Thermal80", "Thermal58" }
            .Contains(paper, StringComparer.OrdinalIgnoreCase)) return true;
        var match = System.Text.RegularExpressions.Regex.Match(paper, @"^Custom:\s*(\d{1,4})\s*[x×]\s*(\d{1,4})\s*(mm)?$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        return match.Success && int.TryParse(match.Groups[1].Value, out var width) && int.TryParse(match.Groups[2].Value, out var height)
            && width is >= 20 and <= 1000 && height is >= 20 and <= 1000;
    }

    /// <summary>تبدیل به DTO جزئیات</summary>
    private static PrintTemplateDetailDto ToDetail(PrintTemplate t) => new(
        t.Id, t.TemplateName, t.Type.ToString(), TypeFa(t.Type), t.Description, t.HtmlBody,
        t.PaperSize, t.Orientation, t.FontSize, t.FontFamily, t.ShowClinicLogo,
        t.AccentColor, t.IsActive, t.IsDefault);

    /// <summary>نام فارسی نوع</summary>
    private static string TypeFa(PrintTemplateType type) => type switch
    {
        PrintTemplateType.Invoice => "فاکتور و قبض پرداخت",
        PrintTemplateType.Prescription => "نسخه پزشکی",
        PrintTemplateType.LabOrder => "درخواست آزمایش",
        PrintTemplateType.LabResult => "نتیجه آزمایش",
        PrintTemplateType.ImagingOrder => "درخواست تصویربرداری",
        PrintTemplateType.AppointmentTicket => "نوبت‌نامه",
        PrintTemplateType.ReceptionReceipt => "رسید پذیرش",
        PrintTemplateType.MedicalCertificate => "گواهی پزشکی",
        PrintTemplateType.PayrollSlip => "فیش حقوقی",
        _ => type.ToString()
    };
}

/// <summary>ردیف فهرست قالب‌ها — چرخهٔ سی‌ودوم: Type رشته‌ای شد ("Invoice"…)
/// چون سریالایز پیش‌فرض enum را عدد می‌فرستاد و کلاینت دسکتاپ (DTO رشته‌ای)
/// با JsonException کل لیست را از دست می‌داد — «قالب‌ها نمایش داده نمی‌شدند»</summary>
public record PrintTemplateListDto(
    long Id,
    string TemplateName,
    string Type,
    string TypeFa,
    string PaperSize,
    string Orientation,
    bool ShowClinicLogo,
    bool IsActive,
    bool IsDefault);

/// <summary>جزئیات کامل قالب — Type رشته‌ای (هم‌قرارداد فهرست، چرخهٔ سی‌ودوم)</summary>
public record PrintTemplateDetailDto(
    long Id,
    string TemplateName,
    string Type,
    string TypeFa,
    string? Description,
    string HtmlBody,
    string PaperSize,
    string Orientation,
    int FontSize,
    string? FontFamily,
    bool ShowClinicLogo,
    string? AccentColor,
    bool IsActive,
    bool IsDefault);

/// <summary>ثبت/ویرایش قالب — چرخهٔ سی‌ودوم: Type رشته‌ای (نام لاتین مثل
/// "Invoice") تا بستهٔ JSON پیش‌فرض رشته را به enum نبندد و کلاینت رد نشود</summary>
public record SavePrintTemplateDto(
    string TemplateName,
    string Type,
    string? Description,
    string HtmlBody,
    string PaperSize,
    string Orientation,
    int FontSize,
    string? FontFamily,
    bool ShowClinicLogo,
    string? AccentColor,
    bool IsActive,
    bool IsDefault);

/// <summary>درخواست رندر با متغیرهای واقعی</summary>
public record RenderRequest(Dictionary<string, object?>? Variables);

/// <summary>درخواست کپی قالب</summary>
public record DuplicateRequest(string? NewName);

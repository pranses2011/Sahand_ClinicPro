using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Infrastructure.Services.Reporting;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// بخش خروجی PDF گزارش‌ها با سربرگ درمانگاه — چرخه سیزدهم (درخواست ۴-۶).
/// کلاینت نتیجه اجرای هر گزارش (کاتالوگی یا رایگانه) را می‌فرستد و سرور
/// PDF صفحه‌آرایی‌شده با سربرگ رسمی درمانگاه (از ClinicInfo) برمی‌گرداند.
/// مسیر: POST /api/v1/reports/pdf
/// </summary>
public partial class ReportsController
{
    /// <summary>سرویس PDF تزریق‌شده در سازنده جزئی — نام یکتا برای پرهیز از تداخل</summary>
    private ReportPdfService? _pdfService;

    /// <summary>دسترسی تنبل به سرویس PDF</summary>
    private ReportPdfService Pdf => _pdfService ??= HttpContext.RequestServices
        .GetService<ReportPdfService>() ?? new ReportPdfService();

    /// <summary>خروجی PDF نتیجه هر گزارش جدولی — با سربرگ درمانگاه و امضای پایانی</summary>
    /// <remarks>
    /// بدنه: { title, summary{}, columns[], rows[][], footerNote }
    /// سقف: ۱۰۰ ستون و ۲۰۰۰ ردیف. سربرگ (نام/نشانی/تلفن/لوگو) از ClinicInfo خوانده می‌شود.
    /// </remarks>
    [HttpPost("pdf")]
    [HasPermission("reports.view")]
    public async Task<IActionResult> ExportPdf([FromBody] ReportPdfRequestDto request, CancellationToken ct)
    {
        if (request.Columns is null || request.Columns.Count == 0)
            return BadRequest(ApiResponse<string>.Failure("گزارش بدون ستون قابل چاپ نیست"));
        if (request.Columns.Count > 100)
            return BadRequest(ApiResponse<string>.Failure("حداکثر ۱۰۰ ستون برای PDF مجاز است"));
        var rows = request.Rows ?? new List<List<string?>>();
        if (rows.Count > 2000)
            return BadRequest(ApiResponse<string>.Failure("حداکثر ۲۰۰۰ ردیف برای PDF مجاز است — ابتدا با فیلتر محدوده کنید"));

        // سربرگ رسمی درمانگاه از تنظیمات — نبود آن fatal نیست؛ مقدار پیش‌فرض می‌نشیند
        var clinic = await _reporting.Set<ClinicInfo>().AsNoTracking()
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync(ct);
        byte[]? logo = null;
        if (!string.IsNullOrWhiteSpace(clinic?.LogoPath))
        {
            try
            {
                if (System.IO.File.Exists(clinic.LogoPath))
                    logo = await System.IO.File.ReadAllBytesAsync(clinic.LogoPath, ct);
            }
            catch
            {
                // لوگو اختیاری است — نبود آن مانع تولید PDF نمی‌شود
            }
        }

        var model = new ReportPdfModel
        {
            Title = string.IsNullOrWhiteSpace(request.Title) ? "گزارش" : request.Title!.Trim(),
            ClinicName = clinic?.ClinicName ?? "درمانگاه",
            ClinicAddress = clinic?.Address,
            ClinicPhone = clinic?.Phone,
            LogoBytes = logo,
            Summary = (request.Summary ?? new Dictionary<string, string?>())
                .Where(s => s.Key is not null)
                .Select(s => new KeyValuePair<string, string>(s.Key!, s.Value ?? "—"))
                .Take(12)
                .ToList(),
            Columns = request.Columns!,
            Rows = rows
                .Select(r => request.Columns!
                    .Select((_, i) => r is not null && r.Count > i && r[i] is not null ? r[i]! : "—")
                    .ToList())
                .ToList(),
            FooterNote = request.FooterNote,
        };

        try
        {
            var pdf = Pdf.Render(model);
            var fileName = $"report_{Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.IranNow():yyyyMMdd_HHmm}.pdf";
            return File(pdf, "application/pdf", fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<string>.Failure(ex.Message));
        }
    }
}

/// <summary>درخواست خروجی PDF — نتیجه اجرا‌شده گزارش در کلاینت</summary>
public class ReportPdfRequestDto
{
    /// <summary>عنوان گزارش</summary>
    public string? Title { get; set; }

    /// <summary>کارت‌های خلاصه — دیکشنری عنوان/مقدار</summary>
    public Dictionary<string, string?>? Summary { get; set; }

    /// <summary>عنوان ستون‌ها</summary>
    public List<string>? Columns { get; set; }

    /// <summary>ردیف‌ها — متن سلول‌ها</summary>
    public List<List<string?>>? Rows { get; set; }

    /// <summary>یادداشت پایانی اختیاری</summary>
    public string? FooterNote { get; set; }
}

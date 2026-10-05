using ClosedXML.Excel;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Reports;

/// <summary>
/// پرس‌وجوی خروجی اکسل گزارش تفصیلی «سود و حقوق درصدی هر پرسنل» — چرخه دهم
/// </summary>
/// <remarks>
/// خروجی: فایل xlsx با دو برگ «خلاصه پرسنل» و «جزئیات اقلام» — راست‌به‌چپ.
/// داده‌ها از همان سازنده مشترک گزارش JSON تولید می‌شوند تا نمایش و
/// خروجی اکسل همیشه یک عدد واحد را نشان دهند.
/// </remarks>
public sealed record ExportCommissionDetailExcelQuery(
    int Year, int Month, long? DepartmentId = null, long? EmployeeId = null)
    : IRequest<Result<byte[]>>;

/// <summary>اعتبارسنج دوره خروجی اکسل</summary>
public sealed class ExportCommissionDetailExcelQueryValidator
    : AbstractValidator<ExportCommissionDetailExcelQuery>
{
    /// <summary>قواعد دوره شمسی</summary>
    public ExportCommissionDetailExcelQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(1400, 1500)
            .WithMessage("سال دوره بین ۱۴۰۰ تا ۱۵۰۰ معتبر است");
        RuleFor(x => x.Month).InclusiveBetween(1, 12)
            .WithMessage("ماه دوره بین ۱ تا ۱۲ است");
    }
}

/// <summary>گرداننده خروجی اکسل گزارش</summary>
public sealed class ExportCommissionDetailExcelQueryHandler
    : IRequestHandler<ExportCommissionDetailExcelQuery, Result<byte[]>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public ExportCommissionDetailExcelQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — ساخت گزارش و تبدیل به xlsx</summary>
    public async Task<Result<byte[]>> Handle(
        ExportCommissionDetailExcelQuery request, CancellationToken cancellationToken)
    {
        var report = await CommissionDetailReportBuilder.BuildAsync(
            _unitOfWork, request.Year, request.Month,
            request.DepartmentId, request.EmployeeId, cancellationToken);
        if (report.IsFailure)
            return Result<byte[]>.Failure(report.Error);

        return Result<byte[]>.Success(
            CommissionDetailExcelWriter.Write(report.Value));
    }
}

/// <summary>
/// نویسنده اکسل گزارش سود و حقوق درصدی — دو برگ راست‌به‌چپ با جمع کل
/// </summary>
internal static class CommissionDetailExcelWriter
{
    /// <summary>فونت پایه — طهوما در همه ویندوزها فارسی را کامل پشتیبانی می‌کند</summary>
    private const string BaseFont = "Tahoma";

    /// <summary>قالب عدد ریالی با جداکننده هزارگان</summary>
    private const string RialFormat = "#,##0";

    /// <summary>رنگ سربرگ برند درمانگاه</summary>
    private const string HeaderBg = "FF16355E";

    /// <summary>رنگ سطر جمع کل</summary>
    private const string TotalBg = "FFE8F0FE";

    /// <summary>ساخت فایل xlsx کامل گزارش</summary>
    public static byte[] Write(CommissionDetailReportDto report)
    {
        using var workbook = new XLWorkbook();
        WriteSummarySheet(workbook, report);
        WriteDetailSheet(workbook, report);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>برگ خلاصه — یک ردیف برای هر پرسنل + سطر جمع کل</summary>
    private static void WriteSummarySheet(XLWorkbook workbook, CommissionDetailReportDto report)
    {
        var ws = workbook.Worksheets.Add("خلاصه پرسنل");
        ws.RightToLeft = true;
        ws.Style.Font.FontName = BaseFont;

        // ── سربرگ گزارش ──
        ws.Cell(1, 1).Value = report.ClinicName;
        ws.Range(1, 1, 1, 13).Merge().Style
            .Font.SetBold(true).Font.SetFontSize(15)
            .Fill.SetBackgroundColor(XLColor.FromHtml(HeaderBg))
            .Font.SetFontColor(XLColor.White)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        ws.Row(1).Height = 30;

        ws.Cell(2, 1).Value =
            $"گزارش تفصیلی سود و حقوق درصدی پرسنل — دوره {report.PeriodText} — " +
            $"تولید: {report.GeneratedAtShamsi}";
        ws.Range(2, 1, 2, 13).Merge().Style
            .Font.SetFontSize(11)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // ── سطر عنوان ستون‌ها ──
        string[] headers =
        {
            "کد پرسنلی", "نام و نام خانوادگی", "بخش", "سمت", "پزشک متصل",
            "تعداد اقلام", "جمع فروش (ریال)", "جمع هزینه (ریال)", "سود خدمات (ریال)",
            "درصد از سود (٪)", "مبنای حقوق درصدی (ریال)", "حقوق درصدی (ریال)", "وضعیت فیش"
        };
        var headerRow = 4;
        for (var c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(headerRow, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.SetBold(true).Font.SetFontSize(10.5)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("FF2E5C9E"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }
        ws.SheetView.FreezeRows(headerRow);

        // ── ردیف‌های پرسنل ──
        var row = headerRow + 1;
        foreach (var s in report.Summary)
        {
            ws.Cell(row, 1).Value = s.PersonnelCode;
            ws.Cell(row, 2).Value = s.FullName;
            ws.Cell(row, 3).Value = s.DepartmentName;
            ws.Cell(row, 4).Value = s.PositionName;
            ws.Cell(row, 5).Value = s.DoctorNames;
            ws.Cell(row, 6).Value = s.ServiceCount;
            SetRial(ws.Cell(row, 7), s.TotalRevenue);
            SetRial(ws.Cell(row, 8), s.TotalCost);
            SetRial(ws.Cell(row, 9), s.TotalProfit);
            ws.Cell(row, 10).Value = s.CommissionPercent;
            ws.Cell(row, 10).Style.NumberFormat.Format = "0.##";
            SetRial(ws.Cell(row, 11), s.CommissionBase);
            SetRial(ws.Cell(row, 12), s.ServiceCommission);
            ws.Cell(row, 12).Style.Font.SetBold(true);
            ws.Cell(row, 13).Value = s.SalaryStatusText;
            row++;
        }

        // ── سطر جمع کل ──
        ws.Cell(row, 1).Value = "جمع کل";
        ws.Range(row, 1, row, 5).Merge().Style.Font.SetBold(true)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        ws.Cell(row, 6).Value = report.ServiceCountTotal;
        SetRial(ws.Cell(row, 7), report.TotalRevenue);
        SetRial(ws.Cell(row, 8), report.TotalCost);
        SetRial(ws.Cell(row, 9), report.TotalProfit);
        ws.Cell(row, 10).Value = "—";
        ws.Cell(row, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        SetRial(ws.Cell(row, 11), Math.Max(0m, report.TotalProfit));
        SetRial(ws.Cell(row, 12), report.TotalCommission);
        ws.Cell(row, 13).Value = $"{report.EmployeeCount} پرسنل";
        ws.Range(row, 1, row, 13).Style
            .Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml(TotalBg));

        // ── یادداشت قاعده مالک ──
        var noteRow = row + 2;
        ws.Cell(noteRow, 1).Value =
            "قاعده: حقوق درصدی فقط از «سود خدمات انجام‌شده توسط خودِ پرسنل» محاسبه می‌شود " +
            "(مبلغ قلم منهای هزینه تمام‌شده آن) — نه از کل فروش و نه از کل دریافتی درمانگاه؛ " +
            "خدمات ویزیت/درمانی پزشکانِ دارای رکورد پرسنل مرتبط نیز در فیش همان پرسنل لحاظ می‌شود.";
        ws.Range(noteRow, 1, noteRow, 13).Merge().Style
            .Font.SetFontSize(9).Font.SetFontColor(XLColor.FromHtml("FF5B7290"))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
            .Alignment.SetWrapText(true);
        ws.Row(noteRow).Height = 26;

        // عرض ستون‌ها
        ws.Column(1).Width = 11;
        ws.Column(2).Width = 22;
        ws.Column(3).Width = 14;
        ws.Column(4).Width = 14;
        ws.Column(5).Width = 20;
        ws.Column(6).Width = 10;
        for (var c = 7; c <= 12; c++) ws.Column(c).Width = 17;
        ws.Column(13).Width = 14;
    }

    /// <summary>برگ جزئیات — یک ردیف برای هر قلم منتسب‌شده + جمع</summary>
    private static void WriteDetailSheet(XLWorkbook workbook, CommissionDetailReportDto report)
    {
        var ws = workbook.Worksheets.Add("جزئیات اقلام");
        ws.RightToLeft = true;
        ws.Style.Font.FontName = BaseFont;

        // ── سطر عنوان ستون‌ها ──
        string[] headers =
        {
            "پرسنل", "شماره فاکتور", "تاریخ", "بیمار", "خدمت",
            "تعداد", "قیمت واحد (ریال)", "هزینه واحد (ریال)",
            "مبلغ قلم (ریال)", "سود قلم (ریال)", "نحوه انتساب"
        };
        for (var c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.SetBold(true).Font.SetFontSize(10.5)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("FF2E5C9E"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }
        ws.SheetView.FreezeRows(1);

        var row = 2;
        foreach (var d in report.Details)
        {
            ws.Cell(row, 1).Value = d.EmployeeName;
            ws.Cell(row, 2).Value = d.InvoiceNumber;
            ws.Cell(row, 3).Value = d.InvoiceDateShamsi;
            ws.Cell(row, 4).Value = d.PatientName;
            ws.Cell(row, 5).Value = d.ServiceName;
            ws.Cell(row, 6).Value = d.Quantity;
            ws.Cell(row, 6).Style.NumberFormat.Format = "0.##";
            SetRial(ws.Cell(row, 7), d.UnitPrice);
            SetRial(ws.Cell(row, 8), d.UnitCost);
            SetRial(ws.Cell(row, 9), d.TotalPrice);
            SetRial(ws.Cell(row, 10), d.Profit);
            ws.Cell(row, 11).Value = d.Attribution;
            row++;
        }

        // ── سطر جمع کل ──
        ws.Cell(row, 1).Value = "جمع کل";
        ws.Range(row, 1, row, 5).Merge().Style.Font.SetBold(true)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        ws.Cell(row, 6).Value = report.ServiceCountTotal;
        ws.Cell(row, 7).Value = "—";
        ws.Cell(row, 8).Value = "—";
        SetRial(ws.Cell(row, 9), report.TotalRevenue);
        SetRial(ws.Cell(row, 10), report.TotalProfit);
        ws.Cell(row, 11).Value = "";
        ws.Range(row, 1, row, 11).Style
            .Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml(TotalBg));

        ws.Column(1).Width = 22;
        ws.Column(2).Width = 13;
        ws.Column(3).Width = 12;
        ws.Column(4).Width = 20;
        ws.Column(5).Width = 24;
        ws.Column(6).Width = 9;
        for (var c = 7; c <= 10; c++) ws.Column(c).Width = 16;
        ws.Column(11).Width = 17;
    }

    /// <summary>مقدار ریالی با قالب جداکننده هزارگان</summary>
    private static void SetRial(IXLCell cell, decimal amount)
    {
        cell.Value = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
        cell.Style.NumberFormat.Format = RialFormat;
    }
}

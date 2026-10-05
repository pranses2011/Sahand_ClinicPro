using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Queries.GetAbnormalSummary;

/// <summary>پرس‌وجوی خلاصه نتایج غیرعادی پاراکلینیک بیمار — چرخه ۱۳ (درخواست ۱-۴)</summary>
public record GetAbnormalSummaryQuery(long PatientId) : IRequest<Result<AbnormalSummaryDto>>;

/// <summary>گرداننده — آخرین نتایج آزمایش خارج از محدوده + یافته‌های تصویری هشداردهنده</summary>
public class GetAbnormalSummaryQueryHandler
    : IRequestHandler<GetAbnormalSummaryQuery, Result<AbnormalSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetAbnormalSummaryQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — نوار رنگی بالای پرونده بیمار</summary>
    public async Task<Result<AbnormalSummaryDto>> Handle(GetAbnormalSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var pid = request.PatientId;

        // ═══ ۱) آخرین نتایج غیرطبیعی آزمایش (به‌ازای هر تست، جدیدترین نتیجه) ═══
        var abnormalLabs = await _unitOfWork.Repository<LabOrderItem>().QueryableNoTracking
            .Where(li => li.LabOrder!.PatientId == pid
                         && li.IsAbnormal
                         && li.ResultNumeric != null
                         && !li.IsDeleted)
            .OrderByDescending(li => li.CreatedAt)
            .Select(li => new
            {
                li.TestId,
                TestName = li.Test!.TestName ?? "",
                li.ResultNumeric,
                li.Result,
                li.NormalRange,
                li.Unit,
                li.AbnormalFlag,
                ReportedAt = li.LabOrder!.ResultReadyAt ?? li.LabOrder.OrderDate,
                OrderId = li.LabOrderId
            })
            .Take(120)
            .ToListAsync(cancellationToken);

        var labs = abnormalLabs
            .GroupBy(x => x.TestId) // جدیدترین نتیجه هر تست
            .Select(g => new AbnormalItemDto(
                g.First().TestId,
                g.First().TestName,
                g.First().ResultNumeric,
                g.First().Result,
                g.First().NormalRange,
                g.First().Unit,
                FlagText(g.First().AbnormalFlag),
                FlagColor(g.First().AbnormalFlag),
                g.First().ReportedAt is { } d ? PersianDateHelper.ToShamsi(d) : "—",
                g.First().OrderId,
                Kind: "lab"))
            .OrderBy(x => x.Title)
            .ToList();

        // ═══ ۲) یافته‌های تصویری هشداردهنده (کلیدواژه بحرانی در Impression) ═══
        var imaging = await _unitOfWork.Repository<ImagingOrder>().QueryableNoTracking
            .Where(io => io.PatientId == pid && !io.IsDeleted
                         && (io.Status == ImagingOrderStatus.Reported || io.Status == ImagingOrderStatus.Performed)
                         && io.Impression != null && io.Impression != "")
            .OrderByDescending(io => io.ReportedAt ?? io.PerformedAt ?? DateTime.MinValue)
            .Take(30)
            .Select(io => new
            {
                io.Id, io.BodyPart, io.Modality, io.Impression,
                At = io.ReportedAt ?? io.PerformedAt
            })
            .ToListAsync(cancellationToken);

        var criticalKeywords = new[]
        {
            "بدخیم", "ملیگنان", "توده", "ضایعه", "شکستگی", "هماتوم",
            "انسداد", "تنگی", "دیسک", "عفونت", "آبسه", "سنگ", "هیدروفوز", "اتساع"
        };

        var imagings = imaging
            .Where(io => io.Impression != null && criticalKeywords.Any(k =>
                io.Impression!.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .Select(io => new AbnormalItemDto(
                io.Id,
                $"{BodyPartFa(io.Modality, io.BodyPart)}",
                null,
                io.Impression,
                null,
                null,
                "یافته هشدار",
                "#FFEBEE",
                io.At is { } d ? PersianDateHelper.ToShamsi(d) : "—",
                io.Id,
                Kind: "imaging"))
            .ToList();

        var dto = new AbnormalSummaryDto(
            labs.Count + imagings.Count,
            labs.Count,
            imagings.Count,
            labs.Concat(imagings).OrderByDescending(x => x.DateShamsi).ToList(),
            labs.Count + imagings.Count > 0);
        return Result<AbnormalSummaryDto>.Success(dto);
    }

    /// <summary>متن پرچم</summary>
    private static string FlagText(string? flag) => flag?.Trim().ToUpperInvariant() switch
    {
        "H" or "HH" or "HIGH" => "بالاتر از حد",
        "L" or "LL" or "LOW" => "پایین‌تر از حد",
        _ => "خارج از محدوده"
    };

    /// <summary>رنگ پرچم — نوار قرمز برای بالا/پایین، نارنجی برای عمومی</summary>
    private static string FlagColor(string? flag) => flag?.Trim().ToUpperInvariant() switch
    {
        "HH" or "LL" => "#B71C1C", // بحرانی — قرمز تیره
        "H" or "L" => "#FFEBEE",  // غیرطبیعی — قرمز روشن
        _ => "#FFF3E0"            // نامشخص — نارنجی
    };

    /// <summary>نام فارسی نوع تصویر + ناحیه</summary>
    private static string BodyPartFa(ImagingModality modality, string? bodyPart)
    {
        var mod = modality switch
        {
            ImagingModality.XRay => "رادیوگرافی",
            ImagingModality.CT => "سی‌تی‌اسکن",
            ImagingModality.MRI => "ام‌آر‌آی",
            ImagingModality.Ultrasound => "سونوگرافی",
            ImagingModality.Mammography => "ماموگرافی",
            _ => "تصویربرداری"
        };
        return string.IsNullOrWhiteSpace(bodyPart) ? mod : $"{mod} {bodyPart}";
    }
}

/// <summary>ردیف نوار نتایج غیرعادی</summary>
public record AbnormalItemDto(
    long RefId,
    string Title,
    decimal? Value,
    string? ResultText,
    string? NormalRange,
    string? Unit,
    string FlagText,
    string FlagColor,
    string DateShamsi,
    long OrderId,
    string Kind);

/// <summary>خلاصه نتایج غیرعادی بیمار — نوار رنگی بالای پرونده</summary>
public record AbnormalSummaryDto(
    int TotalCount,
    int LabCount,
    int ImagingCount,
    List<AbnormalItemDto> Items,
    bool HasAbnormal);

using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Queries.GetPatientTimeline;

/// <summary>پرس‌وجوی خط زمان رویدادهای بیمار — چرخه ۱۳ (درخواست ۱-۵)</summary>
public record GetPatientTimelineQuery(
    long PatientId,
    string? Kinds = null,
    int? YearShamsi = null) : IRequest<Result<PatientTimelineDto>>;

/// <summary>
/// گرداننده — ادغام یکپارچه رویدادهای بیمار روی خط زمان شمسی:
/// ویزیت، آزمایش، تصویربرداری، نسخه، پذیرش، فاکتور، پرداخت
/// </summary>
public class GetPatientTimelineQueryHandler
    : IRequestHandler<GetPatientTimelineQuery, Result<PatientTimelineDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetPatientTimelineQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — رویدادها به‌ترتیب نزولی تاریخ با برچسب شمسی</summary>
    public async Task<Result<PatientTimelineDto>> Handle(GetPatientTimelineQuery request,
        CancellationToken cancellationToken)
    {
        var pid = request.PatientId;
        var kinds = (request.Kinds ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var events = new List<TimelineEventDto>();

        // ═══ ۱) ویزیت‌ها ═══
        if (kinds.Count == 0 || kinds.Contains("visit"))
        {
            var visits = await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>().QueryableNoTracking
                .Where(v => v.PatientId == pid && !v.IsDeleted)
                .OrderByDescending(v => v.VisitDate)
                .Take(200)
                .Select(v => new
                {
                    v.Id, v.VisitDate, v.ChiefComplaint,
                    Doctor = v.Doctor!.Employee != null
                        ? v.Doctor.Employee.FirstName + " " + v.Doctor.Employee.LastName
                        : "پزشک #" + v.DoctorId,
                    v.Status
                })
                .ToListAsync(cancellationToken);
            events.AddRange(visits.Select(v => new TimelineEventDto(
                "visit", "📋 ویزیت", v.ChiefComplaint ?? "ویزیت پزشکی",
                $"پزشک: {v.Doctor}", v.VisitDate, v.Id)));
        }

        // ═══ ۲) آزمایش‌ها ═══
        if (kinds.Count == 0 || kinds.Contains("lab"))
        {
            var labs = await _unitOfWork.Repository<Domain.Entities.Paraclinical.LabOrder>().QueryableNoTracking
                .Where(l => l.PatientId == pid && !l.IsDeleted)
                .OrderByDescending(l => l.OrderDate)
                .Take(200)
                .Select(l => new { l.Id, l.OrderDate, l.Status, l.OrderNumber })
                .ToListAsync(cancellationToken);
            events.AddRange(labs.Select(l => new TimelineEventDto(
                "lab", "🧪 آزمایش",
                $"درخواست آزمایش {(string.IsNullOrWhiteSpace(l.OrderNumber) ? "" : l.OrderNumber)}",
                $"وضعیت: {LabStatusFa(l.Status)}",
                l.OrderDate ?? DateTime.MinValue, l.Id)));
        }

        // ═══ ۳) تصویربرداری ═══
        if (kinds.Count == 0 || kinds.Contains("imaging"))
        {
            var imgs = await _unitOfWork.Repository<Domain.Entities.Paraclinical.ImagingOrder>().QueryableNoTracking
                .Where(i => i.PatientId == pid && !i.IsDeleted)
                .OrderByDescending(i => i.ReportedAt ?? i.PerformedAt ?? i.ScheduledAt)
                .Take(200)
                .Select(i => new
                {
                    i.Id,
                    At = (DateTime?)(i.ReportedAt ?? i.PerformedAt ?? i.ScheduledAt),
                    i.BodyPart, i.Modality
                })
                .ToListAsync(cancellationToken);
            events.AddRange(imgs.Select(i => new TimelineEventDto(
                "imaging", "🩻 تصویربرداری",
                $"{ModalityFa(i.Modality)} {(string.IsNullOrWhiteSpace(i.BodyPart) ? "" : i.BodyPart)}",
                i.At is null ? "در انتظار انجام" : "انجام شده",
                i.At ?? DateTime.MinValue, i.Id)));
        }

        // ═══ ۴) نسخه‌ها ═══
        if (kinds.Count == 0 || kinds.Contains("prescription"))
        {
            var rx = await _unitOfWork.Repository<Domain.Entities.Clinical.Prescription>().QueryableNoTracking
                .Where(p => p.PatientId == pid && !p.IsDeleted)
                .OrderByDescending(p => p.PrescriptionDate)
                .Take(200)
                .Select(p => new
                {
                    p.Id, p.PrescriptionDate,
                    Doctor = p.Doctor!.Employee != null
                        ? p.Doctor.Employee.FirstName + " " + p.Doctor.Employee.LastName
                        : "پزشک #" + p.DoctorId,
                    p.IsElectronic
                })
                .ToListAsync(cancellationToken);
            events.AddRange(rx.Select(p => new TimelineEventDto(
                "prescription", "💊 نسخه",
                p.IsElectronic ? "نسخه الکترونیک" : "نسخه دارویی",
                $"پزشک: {p.Doctor}",
                p.PrescriptionDate ?? DateTime.MinValue, p.Id)));
        }

        // ═══ ۵) پذیرش‌ها ═══
        if (kinds.Count == 0 || kinds.Contains("reception"))
        {
            var recs = await _unitOfWork.Repository<Domain.Entities.Scheduling.Reception>().QueryableNoTracking
                .Where(r => r.PatientId == pid && !r.IsDeleted)
                .OrderByDescending(r => r.ReceptionDate)
                .Take(200)
                .Select(r => new { r.Id, r.ReceptionDate, Doctor = r.Doctor!.Employee!.FirstName + " " + r.Doctor.Employee.LastName })
                .ToListAsync(cancellationToken);
            events.AddRange(recs.Select(r => new TimelineEventDto(
                "reception", "🛎 پذیرش",
                "پذیرش حضوری",
                $"پزشک: {r.Doctor}",
                r.ReceptionDate, r.Id)));
        }

        // ═══ ۶) فاکتورها ═══
        if (kinds.Count == 0 || kinds.Contains("invoice"))
        {
            var invs = await _unitOfWork.Repository<Domain.Entities.Billing.Invoice>().QueryableNoTracking
                .Where(i => i.PatientId == pid && !i.IsDeleted)
                .OrderByDescending(i => i.InvoiceDate)
                .Take(200)
                .Select(i => new { i.Id, i.InvoiceDate, i.TotalAmount, i.Status })
                .ToListAsync(cancellationToken);
            events.AddRange(invs.Select(i => new TimelineEventDto(
                "invoice", "🧾 صورتحساب",
                $"{i.TotalAmount:N0} ریال",
                $"وضعیت: {InvoiceStatusFa(i.Status)}",
                i.InvoiceDate, i.Id)));
        }

        // ═══ ۷) پرداخت‌ها ═══
        if (kinds.Count == 0 || kinds.Contains("payment"))
        {
            var pays = await _unitOfWork.Repository<Domain.Entities.Billing.Payment>().QueryableNoTracking
                .Where(p => p.Invoice != null && p.Invoice.PatientId == pid && !p.IsDeleted)
                .OrderByDescending(p => p.PaymentDate)
                .Take(200)
                .Select(p => new { p.Id, p.PaymentDate, p.Amount, p.Method })
                .ToListAsync(cancellationToken);
            events.AddRange(pays.Select(p => new TimelineEventDto(
                "payment", "💳 پرداخت",
                $"{p.Amount:N0} ریال",
                $"روش: {MethodFa(p.Method)}",
                p.PaymentDate, p.Id)));
        }

        // ═══ ۸) طرح‌های درمان دندانپزشکی — چرخهٔ ۴۸ (گزارش مالک: همهٔ موارد
        //     شامل دندانپزشکی/چشم‌پزشکی/فیزیوتراپی هم در پروندهٔ بیمار ثبت شوند) ═══
        if (kinds.Count == 0 || kinds.Contains("dental"))
        {
            var dental = await _unitOfWork.Repository<Domain.Entities.Paraclinical.DentalTreatmentPlan>()
                .QueryableNoTracking
                .Where(d => d.PatientId == pid && !d.IsDeleted)
                .OrderByDescending(d => d.CreatedAt)
                .Take(200)
                .Select(d => new
                {
                    d.Id,
                    At = (DateTime?)(d.CreatedDate.HasValue
                        ? d.CreatedDate.Value.ToDateTime(TimeOnly.MinValue)
                        : d.CreatedAt),
                    d.TotalCost,
                    d.EstimatedSessions,
                    d.Status
                })
                .ToListAsync(cancellationToken);
            events.AddRange(dental.Select(d => new TimelineEventDto(
                "dental", "🦷 دندانپزشکی",
                $"طرح درمان دندان — {(d.EstimatedSessions.HasValue ? $"{d.EstimatedSessions} جلسه" : "بدون برآورد جلسه")}",
                $"{d.TotalCost:N0} ریال — {DentalStatusFa(d.Status)}",
                d.At ?? DateTime.MinValue, d.Id)));
        }

        // ═══ ۹) معاینه‌های چشم‌پزشکی ═══
        if (kinds.Count == 0 || kinds.Contains("eye"))
        {
            var eyes = await _unitOfWork.Repository<Domain.Entities.Paraclinical.EyeExamRecord>()
                .QueryableNoTracking
                .Where(e => e.PatientId == pid && !e.IsDeleted)
                .OrderByDescending(e => e.CreatedAt)
                .Take(200)
                .Select(e => new
                {
                    e.Id,
                    e.CreatedAt,
                    e.RightIop,
                    e.LeftIop,
                    e.RightVisualAcuity,
                    e.LeftVisualAcuity,
                    e.Findings
                })
                .ToListAsync(cancellationToken);
            events.AddRange(eyes.Select(e => new TimelineEventDto(
                "eye", "👁️ چشم‌پزشکی",
                "معاینه چشم و عینک‌سنجی",
                string.IsNullOrWhiteSpace(e.Findings)
                    ? $"IOP: {e.RightIop?.ToString() ?? "—"}/{e.LeftIop?.ToString() ?? "—"} • VA: {e.RightVisualAcuity ?? "—"}/{e.LeftVisualAcuity ?? "—"}"
                    : e.Findings.Length > 80 ? e.Findings[..80] + "…" : e.Findings,
                e.CreatedAt, e.Id)));
        }

        // ═══ ۱۰) جلسات فیزیوتراپی ═══
        if (kinds.Count == 0 || kinds.Contains("physio"))
        {
            var physio = await _unitOfWork.Repository<Domain.Entities.Paraclinical.PhysiotherapySession>()
                .QueryableNoTracking
                .Where(s => s.PatientId == pid && !s.IsDeleted)
                .OrderByDescending(s => s.SessionDate)
                .Take(200)
                .Select(s => new { s.Id, s.SessionDate, s.SessionNumber, s.Treatments })
                .ToListAsync(cancellationToken);
            events.AddRange(physio.Select(s => new TimelineEventDto(
                "physio", "🏃 فیزیوتراپی",
                $"جلسهٔ {s.SessionNumber}",
                s.Treatments is { Length: > 80 } ? s.Treatments[..80] + "…" : s.Treatments,
                s.SessionDate ?? DateTime.MinValue, s.Id)));
        }

        // ═══ مرتب‌سازی نزولی + فیلتر سال شمسی + برچسب تاریخ ═══
        var ordered = events
            .Where(e => e.At > DateTime.MinValue)
            .OrderByDescending(e => e.At)
            .AsEnumerable();

        if (request.YearShamsi is { } year)
            ordered = ordered.Where(e => PersianDateHelper.ToJalali(e.At).Year == year);

        var result = ordered
            .Select(e => e with
            {
                DateShamsi = PersianDateHelper.ToShamsi(e.At),
                TimeShamsi = e.At.ToString("HH:mm")
            })
            .ToList();

        var years = result.Select(e => PersianDateHelper.ToJalali(e.At).Year)
            .Distinct().OrderByDescending(y => y).ToList();

        return Result<PatientTimelineDto>.Success(new PatientTimelineDto(
            result.Count, years, result));
    }

    /// <summary>وضعیت طرح درمان دندان به فارسی — چرخهٔ ۴۸</summary>
    private static string DentalStatusFa(TreatmentPlanStatus status) => status switch
    {
        TreatmentPlanStatus.Proposed => "پیشنهادی",
        TreatmentPlanStatus.Approved => "تأییدشده",
        TreatmentPlanStatus.InProgress => "در جریان",
        TreatmentPlanStatus.Completed => "تکمیل‌شده",
        TreatmentPlanStatus.Cancelled => "لغوشده",
        _ => status.ToString()
    };

    /// <summary>وضعیت آزمایش به فارسی</summary>
    private static string LabStatusFa(LabOrderStatus status) => status switch
    {
        LabOrderStatus.Ordered => "درخواست‌شده",
        LabOrderStatus.SampleCollected => "نمونه‌گیری‌شده",
        LabOrderStatus.InProgress => "در حال انجام",
        LabOrderStatus.ResultReady => "نتیجه آماده",
        LabOrderStatus.Verified => "تأییدشده",
        LabOrderStatus.Delivered => "تحویل‌شده",
        LabOrderStatus.Cancelled => "لغوشده",
        _ => status.ToString()
    };

    /// <summary>وضعیت فاکتور به فارسی</summary>
    private static string InvoiceStatusFa(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "پیش‌نویس",
        InvoiceStatus.Issued => "صدورشده",
        InvoiceStatus.PartiallyPaid => "پرداخت جزئی",
        InvoiceStatus.Paid => "پرداخت‌شده",
        InvoiceStatus.Cancelled => "ابطال‌شده",
        InvoiceStatus.Refunded => "برگشت هزینه",
        _ => status.ToString()
    };

    /// <summary>روش پرداخت به فارسی</summary>
    private static string MethodFa(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "نقدی",
        PaymentMethod.Card => "کارتخوان",
        PaymentMethod.Online => "آنلاین",
        PaymentMethod.Cheque => "چک",
        PaymentMethod.BankTransfer => "انتقال بانکی",
        PaymentMethod.Insurance => "بیمه",
        _ => method.ToString()
    };

    /// <summary>نوع تصویربرداری به فارسی</summary>
    private static string ModalityFa(ImagingModality modality) => modality switch
    {
        ImagingModality.XRay => "رادیوگرافی",
        ImagingModality.CT => "سی‌تی‌اسکن",
        ImagingModality.MRI => "ام‌آر‌آی",
        ImagingModality.Ultrasound => "سونوگرافی",
        ImagingModality.Mammography => "ماموگرافی",
        _ => "تصویربرداری"
    };
}

/// <summary>یک رویداد خط زمان</summary>
public record TimelineEventDto(
    string Kind,
    string KindLabel,
    string Title,
    string? Subtitle,
    DateTime At,
    long RefId,
    string? DateShamsi = null,
    string? TimeShamsi = null);

/// <summary>خط زمان بیمار</summary>
public record PatientTimelineDto(
    int TotalCount,
    List<int> YearsShamsi,
    List<TimelineEventDto> Events);

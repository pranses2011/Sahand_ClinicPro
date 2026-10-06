using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.Archive;

/// <summary>نتیجه پیش‌نمایش یک جدول</summary>
public record ArchivePreviewItem(string Table, string Title, int Candidates, int Blocked);

/// <summary>گزارش اجرای آرشیو یک جدول</summary>
public record ArchiveRunItem(string Table, string Title, int Candidates, int Archived, int Blocked);

/// <summary>گزارش کل اجرا — مجموع تجمعی در طول اجرا تغییر می‌کند</summary>
public class ArchiveRunReport
{
    /// <summary>سال شمسی آرشیو</summary>
    public int YearJalali { get; }

    /// <summary>تاریخ برش میلادی</summary>
    public DateTime Cutoff { get; }

    /// <summary>گزارش جدول‌به‌جدول</summary>
    public List<ArchiveRunItem> Items { get; }

    /// <summary>مجموع ردیف‌های منتقل‌شده</summary>
    public int TotalArchived { get; set; }

    /// <summary>سازنده گزارش</summary>
    public ArchiveRunReport(int yearJalali, DateTime cutoff, List<ArchiveRunItem> items, int totalArchived)
    {
        YearJalali = yearJalali;
        Cutoff = cutoff;
        Items = items;
        TotalArchived = totalArchived;
    }
}

/// <summary>ردیف مرورگر آرشیو</summary>
public record ArchiveRow(
    long Id, string SourceTable, long SourceId, long? PatientId, string? PatientName,
    DateTime RecordDate, int YearJalali, string? SummaryLine);

/// <summary>
/// سرویس آرشیو سالانه — چرخه سیزدهم (درخواست ۷-۷).
/// جابه‌جایی داده‌های قدیمی‌تر از سال شمسی انتخابی از جداول عملیاتی
/// (نوبت/پذیرش/ویزیت/فاکتور/پرداخت/نسخه/آزمایش) به جدول ArchiveRecords
/// با تصویر JSON کامل + حفظ ارجاع بیمار.
/// امنیت ارجاعی: ردیف‌هایی که هنوز توسط ردیف‌های جدیدتر (بازه نجات‌یافته)
/// ارجاع شده‌اند هرگز آرشیو نمی‌شوند (Blocked) — ترتیب پردازش فرزند-به-والد:
/// پرداخت ← فاکتور/نسخه/آزمایش ← ویزیت ← پذیرش ← نوبت.
/// </summary>
public class ArchiveService
{
    /// <summary>گزینه‌های سریالایز JSON</summary>
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private readonly IUnitOfWork _unitOfWork;
    private readonly Persistence.ClinicProDbContext _context;
    private readonly ILogger<ArchiveService> _logger;

    /// <summary>پاک‌سازی فیزیکی پس از آرشیو؟ — پیش‌فرض حذف نرم امن؛ پاک‌سازی با اسکریپت نگهداشت یا این گزینه</summary>
    private bool _hardPurge;

    /// <summary>سازنده با تزریق وابستگی‌ها — کانتکست مستقیم برای پاک‌سازی فیزیکی اختیاری</summary>
    public ArchiveService(IUnitOfWork unitOfWork, Persistence.ClinicProDbContext context,
        ILogger<ArchiveService> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
    }

    /// <summary>عناوین فارسی جداول آرشیوشونده</summary>
    public static readonly Dictionary<string, string> TableTitles = new()
    {
        ["Payments"] = "پرداخت‌ها", ["Invoices"] = "فاکتورها",
        ["Prescriptions"] = "نسخه‌ها", ["LabOrders"] = "آزمایش‌ها",
        ["Visits"] = "ویزیت‌ها", ["Receptions"] = "پذیرش‌ها", ["Appointments"] = "نوبت‌ها",
    };

    /// <summary>محاسبه تاریخ برش — آغاز سال شمسی انتخابی</summary>
    public static DateTime CutoffOf(int yearJalali)
        => PersianDateHelper.ToGregorian(yearJalali, 1, 1);

    /// <summary>
    /// پیش‌نمایش شمارشی — چند ردیف از هر جدول آرشیو می‌شود و چند ردیف
    /// به‌دلیل ارجاع ردیف‌های جدیدتر مسدود است. هیچ تغییری اعمال نمی‌شود.
    /// </summary>
    public async Task<List<ArchivePreviewItem>> PreviewAsync(int yearJalali, CancellationToken ct = default)
    {
        var cutoff = CutoffOf(yearJalali);
        var result = new List<ArchivePreviewItem>();

        // پرداخت‌ها
        var payCandidates = await IdsAsync(_unitOfWork.Repository<Payment>().QueryableNoTracking
            .Where(p => p.PaymentDate < cutoff), ct);
        result.Add(new("Payments", TableTitles["Payments"], payCandidates.Count, 0));

        // فاکتورها — مسدود اگر پرداختِ نجات‌یافته ارجاع کند
        var invCandidates = await IdsAsync(_unitOfWork.Repository<Invoice>().QueryableNoTracking
            .Where(i => i.InvoiceDate < cutoff), ct);
        var invBlocked = await _unitOfWork.Repository<Payment>().QueryableNoTracking
            .Where(p => p.PaymentDate >= cutoff && p.InvoiceId != null && invCandidates.Contains(p.InvoiceId!.Value))
            .CountAsync(ct);
        result.Add(new("Invoices", TableTitles["Invoices"], invCandidates.Count, invBlocked));

        // نسخه‌ها
        var rxCandidates = await IdsAsync(_unitOfWork.Repository<Prescription>().QueryableNoTracking
            .Where(p => p.PrescriptionDate < cutoff), ct);
        result.Add(new("Prescriptions", TableTitles["Prescriptions"], rxCandidates.Count, 0));

        // آزمایش‌ها
        var labCandidates = await IdsAsync(_unitOfWork.Repository<LabOrder>().QueryableNoTracking
            .Where(o => o.OrderDate < cutoff), ct);
        result.Add(new("LabOrders", TableTitles["LabOrders"], labCandidates.Count, 0));

        // ویزیت‌ها — مسدود اگر فاکتور/نسخه/آزمایشِ نجات‌یافته ارجاع کند
        var visitCandidates = await IdsAsync(_unitOfWork.Repository<Visit>().QueryableNoTracking
            .Where(v => v.VisitDate < cutoff), ct);
        var visitBlocked =
            await _unitOfWork.Repository<Invoice>().QueryableNoTracking
                .Where(i => i.InvoiceDate >= cutoff && i.VisitId != null && visitCandidates.Contains(i.VisitId!.Value))
                .CountAsync(ct)
            + await _unitOfWork.Repository<Prescription>().QueryableNoTracking
                .Where(p => p.PrescriptionDate >= cutoff && p.VisitId != null && visitCandidates.Contains(p.VisitId!.Value))
                .CountAsync(ct)
            + await _unitOfWork.Repository<LabOrder>().QueryableNoTracking
                .Where(o => o.OrderDate >= cutoff && o.VisitId != null && visitCandidates.Contains(o.VisitId!.Value))
                .CountAsync(ct);
        result.Add(new("Visits", TableTitles["Visits"], visitCandidates.Count, visitBlocked));

        // پذیرش‌ها — مسدود اگر ویزیت/فاکتورِ نجات‌یافته ارجاع کند
        var recCandidates = await IdsAsync(_unitOfWork.Repository<Reception>().QueryableNoTracking
            .Where(r => r.ReceptionDate < cutoff), ct);
        var recBlocked =
            await _unitOfWork.Repository<Visit>().QueryableNoTracking
                .Where(v => v.VisitDate >= cutoff && v.ReceptionId != null && recCandidates.Contains(v.ReceptionId!.Value))
                .CountAsync(ct)
            + await _unitOfWork.Repository<Invoice>().QueryableNoTracking
                .Where(i => i.InvoiceDate >= cutoff && i.ReceptionId != null && recCandidates.Contains(i.ReceptionId!.Value))
                .CountAsync(ct);
        result.Add(new("Receptions", TableTitles["Receptions"], recCandidates.Count, recBlocked));

        // نوبت‌ها — مسدود اگر پذیرشِ نجات‌یافته ارجاع کند
        var aptCandidates = await IdsAsync(_unitOfWork.Repository<Appointment>().QueryableNoTracking
            .Where(a => a.AppointmentDate < DateOnly.FromDateTime(cutoff)), ct);
        var aptBlocked = await _unitOfWork.Repository<Reception>().QueryableNoTracking
            .Where(r => r.ReceptionDate >= cutoff && r.AppointmentId != null && aptCandidates.Contains(r.AppointmentId!.Value))
            .CountAsync(ct);
        result.Add(new("Appointments", TableTitles["Appointments"], aptCandidates.Count, aptBlocked));

        return result;
    }

    /// <summary>
    /// اجرای آرشیو — جابه‌جایی ردیف‌های قدیمی با محافظت ارجاعی.
    /// ذخیره به‌ازای هر جدول اعمال می‌شود تا شکست میانی جداول تکمیل‌شده را خراب نکند.
    /// پاک‌سازی: پیش‌فرض حذف نرم (ردیف با IsDeleted=1 از دید برنامه می‌رود ولی فیزیکی می‌ماند)؛
    /// با hardPurge=true پس از ثبت رکورد آرشیو حذف فیزیکی با SQL خام اجرا می‌شود.
    /// </summary>
    public async Task<ArchiveRunReport> RunAsync(int yearJalali, long? archivedBy = null,
        bool hardPurge = false, CancellationToken ct = default)
    {
        var cutoff = CutoffOf(yearJalali);
        _hardPurge = hardPurge;
        var report = new ArchiveRunReport(yearJalali, cutoff, new(), 0);

        // ۱) پرداخت‌ها — برگ
        var payments = await _context.Payments
            .Where(p => p.PaymentDate < cutoff).ToListAsync(ct);
        await ArchivePaymentsAsync(payments, yearJalali, archivedBy, report, ct);

        // ۲) فاکتورها — مسدود به‌واسطه پرداخت‌های نجات‌یافته
        var invoices = await _context.Invoices
            .Where(i => i.InvoiceDate < cutoff).ToListAsync(ct);
        var survivingInvoiceRefs = new HashSet<long>(await _unitOfWork.Repository<Payment>().QueryableNoTracking
            .Where(p => p.PaymentDate >= cutoff && p.InvoiceId != null)
            .Select(p => p.InvoiceId!.Value).ToListAsync(ct));
        var invArchivable = invoices.Where(i => !survivingInvoiceRefs.Contains(i.Id)).ToList();
        await ArchiveInvoicesAsync(invArchivable, yearJalali, archivedBy, report, ct);
        report.Items.Add(new("Invoices", TableTitles["Invoices"], invoices.Count, invArchivable.Count,
            invoices.Count - invArchivable.Count));

        // ۳) نسخه‌ها — برگ
        var prescriptions = await _context.Prescriptions
            .Where(p => p.PrescriptionDate < cutoff).ToListAsync(ct);
        await ArchivePrescriptionsAsync(prescriptions, yearJalali, archivedBy, report, ct);

        // ۴) آزمایش‌ها — برگ
        var labOrders = await _context.LabOrders
            .Where(o => o.OrderDate < cutoff).ToListAsync(ct);
        await ArchiveLabOrdersAsync(labOrders, yearJalali, archivedBy, report, ct);

        // ۵) ویزیت‌ها — مسدود اگر فاکتور/نسخه/آزمایشِ نجات‌یافته ارجاع کند
        var visits = await _context.Visits
            .Where(v => v.VisitDate < cutoff).ToListAsync(ct);
        var survivingVisitRefs = new HashSet<long>();
        survivingVisitRefs.UnionWith(await _unitOfWork.Repository<Invoice>().QueryableNoTracking
            .Where(i => i.InvoiceDate >= cutoff && i.VisitId != null)
            .Select(i => i.VisitId!.Value).ToListAsync(ct));
        survivingVisitRefs.UnionWith(await _unitOfWork.Repository<Prescription>().QueryableNoTracking
            .Where(p => p.PrescriptionDate >= cutoff && p.VisitId != null)
            .Select(p => p.VisitId!.Value).ToListAsync(ct));
        survivingVisitRefs.UnionWith(await _unitOfWork.Repository<LabOrder>().QueryableNoTracking
            .Where(o => o.OrderDate >= cutoff && o.VisitId != null)
            .Select(o => o.VisitId!.Value).ToListAsync(ct));
        var visitArchivable = visits.Where(v => !survivingVisitRefs.Contains(v.Id)).ToList();
        await ArchiveVisitsAsync(visitArchivable, yearJalali, archivedBy, report, ct);
        report.Items.Add(new("Visits", TableTitles["Visits"], visits.Count, visitArchivable.Count,
            visits.Count - visitArchivable.Count));

        // ۶) پذیرش‌ها — مسدود اگر ویزیت/فاکتورِ نجات‌یافته ارجاع کند
        var receptions = await _context.Receptions
            .Where(r => r.ReceptionDate < cutoff).ToListAsync(ct);
        var survivingReceptionRefs = new HashSet<long>();
        survivingReceptionRefs.UnionWith(await _unitOfWork.Repository<Visit>().QueryableNoTracking
            .Where(v => v.VisitDate >= cutoff && v.ReceptionId != null)
            .Select(v => v.ReceptionId!.Value).ToListAsync(ct));
        survivingReceptionRefs.UnionWith(await _unitOfWork.Repository<Invoice>().QueryableNoTracking
            .Where(i => i.InvoiceDate >= cutoff && i.ReceptionId != null)
            .Select(i => i.ReceptionId!.Value).ToListAsync(ct));
        var recArchivable = receptions.Where(r => !survivingReceptionRefs.Contains(r.Id)).ToList();
        await ArchiveReceptionsAsync(recArchivable, yearJalali, archivedBy, report, ct);
        report.Items.Add(new("Receptions", TableTitles["Receptions"], receptions.Count, recArchivable.Count,
            receptions.Count - recArchivable.Count));

        // ۷) نوبت‌ها — مسدود اگر پذیرشِ نجات‌یافته ارجاع کند
        var appointments = await _context.Appointments
            .Where(a => a.AppointmentDate < DateOnly.FromDateTime(cutoff)).ToListAsync(ct);
        var survivingAppointmentRefs = new HashSet<long>(await _unitOfWork.Repository<Reception>().QueryableNoTracking
            .Where(r => r.ReceptionDate >= cutoff && r.AppointmentId != null)
            .Select(r => r.AppointmentId!.Value).ToListAsync(ct));
        var aptArchivable = appointments.Where(a => !survivingAppointmentRefs.Contains(a.Id)).ToList();
        await ArchiveAppointmentsAsync(aptArchivable, yearJalali, archivedBy, report, ct);
        report.Items.Add(new("Appointments", TableTitles["Appointments"], appointments.Count, aptArchivable.Count,
            appointments.Count - aptArchivable.Count));

        _logger.LogInformation(
            "آرشیو سال {Year} تکمیل شد — {Count} ردیف منتقل، برش {Cutoff:yyyy-MM-dd}",
            yearJalali, report.TotalArchived, cutoff);
        return report;
    }

    /// <summary>جستجوی آرشیو — پرونده قدیمی بیمار با شناسه یا نام یا شرح</summary>
    public async Task<List<ArchiveRow>> SearchAsync(long? patientId, string? term,
        string? sourceTable = null, int take = 100, CancellationToken ct = default)
    {
        var query = _unitOfWork.Repository<ArchiveRecord>().QueryableNoTracking.AsQueryable();
        if (patientId is > 0) query = query.Where(r => r.PatientId == patientId);
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(r =>
                (r.PatientName != null && r.PatientName.Contains(term)) ||
                (r.SummaryLine != null && r.SummaryLine.Contains(term)));
        if (!string.IsNullOrWhiteSpace(sourceTable))
            query = query.Where(r => r.SourceTable == sourceTable);

        return await query
            .OrderByDescending(r => r.RecordDate)
            .Take(Math.Clamp(take, 1, 500))
            .Select(r => new ArchiveRow(r.Id, r.SourceTable, r.SourceId, r.PatientId, r.PatientName,
                r.RecordDate, r.YearJalali, r.SummaryLine))
            .ToListAsync(ct);
    }

    /// <summary>خواندن تصویر JSON یک رکورد آرشیو</summary>
    public async Task<string?> GetPayloadAsync(long archiveRecordId, CancellationToken ct = default)
    {
        var record = await _unitOfWork.Repository<ArchiveRecord>().QueryableNoTracking
            .FirstOrDefaultAsync(r => r.Id == archiveRecordId, ct);
        return record?.Payload;
    }

    /* ─── آرشیوگرهای جدول‌به‌جدول — تصویر JSON + حذف + ثبت گزارش ─── */

    /// <summary>آرشیو پرداخت‌ها</summary>
    private async Task ArchivePaymentsAsync(List<Payment> rows, int year, long? by,
        ArchiveRunReport report, CancellationToken ct)
    {
        var patients = await LoadPatientNamesAsync(rows.Select(r => r.PatientId).Distinct(), ct);
        var repo = _unitOfWork.Repository<Payment>();
        
        foreach (var p in rows)
        {
            _context.ArchiveRecords.Add(new ArchiveRecord
            {
                SourceTable = "Payments", SourceId = p.Id, PatientId = p.PatientId,
                PatientName = patients.GetValueOrDefault(p.PatientId),
                RecordDate = p.PaymentDate, YearJalali = year,
                SummaryLine = $"پرداخت {p.Amount:N0} ریال — {p.Method}",
                Payload = JsonSerializer.Serialize(new
                {
                    p.Id, p.PaymentNumber, p.InvoiceId, p.PatientId, p.Amount,
                    Method = p.Method.ToString(), p.PaymentDate, p.ReferenceNumber,
                    p.BankName, Status = p.Status.ToString(), p.IsReturned, p.CreatedAt,
                }, Json),
                ArchivedBy = by, ArchivedAt = DateTime.UtcNow,
            });
            repo.Remove(p);
        }
        await _unitOfWork.SaveEntitiesAsync(ct);
        await HardPurgeAsync("Payments", rows.Select(r => r.Id).ToList(), ct);
        report.Items.Add(new("Payments", TableTitles["Payments"], rows.Count, rows.Count, 0));
        report.TotalArchived += rows.Count;
    }

    /// <summary>آرشیو فاکتورها</summary>
    private async Task ArchiveInvoicesAsync(List<Invoice> rows, int year, long? by,
        ArchiveRunReport report, CancellationToken ct)
    {
        var patients = await LoadPatientNamesAsync(rows.Select(r => r.PatientId).Distinct(), ct);
        var repo = _unitOfWork.Repository<Invoice>();
        
        foreach (var i in rows)
        {
            _context.ArchiveRecords.Add(new ArchiveRecord
            {
                SourceTable = "Invoices", SourceId = i.Id, PatientId = i.PatientId,
                PatientName = patients.GetValueOrDefault(i.PatientId),
                RecordDate = i.InvoiceDate, YearJalali = year,
                SummaryLine = $"فاکتور {i.InvoiceNumber} — {i.TotalAmount:N0} ریال ({i.Status})",
                Payload = JsonSerializer.Serialize(new
                {
                    i.Id, i.InvoiceNumber, i.PatientId, i.VisitId, i.ReceptionId,
                    i.InvoiceDate, Type = i.InvoiceType.ToString(),
                    i.SubTotal, i.DiscountAmount, i.Tax, i.TotalAmount, i.PaidAmount, i.RemainingAmount,
                    Status = i.Status.ToString(), PaymentStatus = i.PaymentStatus.ToString(), i.CreatedAt,
                }, Json),
                ArchivedBy = by, ArchivedAt = DateTime.UtcNow,
            });
            repo.Remove(i);
        }
        await _unitOfWork.SaveEntitiesAsync(ct);
        await HardPurgeAsync("Invoices", rows.Select(r => r.Id).ToList(), ct);
        report.TotalArchived += rows.Count;
    }

    /// <summary>آرشیو نسخه‌ها</summary>
    private async Task ArchivePrescriptionsAsync(List<Prescription> rows, int year, long? by,
        ArchiveRunReport report, CancellationToken ct)
    {
        var patients = await LoadPatientNamesAsync(rows.Select(r => r.PatientId).Distinct(), ct);
        var repo = _unitOfWork.Repository<Prescription>();
        
        foreach (var p in rows)
        {
            _context.ArchiveRecords.Add(new ArchiveRecord
            {
                SourceTable = "Prescriptions", SourceId = p.Id, PatientId = p.PatientId,
                PatientName = patients.GetValueOrDefault(p.PatientId),
                RecordDate = p.PrescriptionDate ?? p.CreatedAt, YearJalali = year,
                SummaryLine = $"نسخه {p.PrescriptionCode} — {p.Type} ({p.Status})",
                Payload = JsonSerializer.Serialize(new
                {
                    p.Id, p.PrescriptionCode, p.PatientId, p.DoctorId, p.VisitId,
                    p.PrescriptionDate, Type = p.Type.ToString(), p.IsElectronic,
                    p.IsNarcotic, Status = p.Status.ToString(), p.Notes, p.CreatedAt,
                }, Json),
                ArchivedBy = by, ArchivedAt = DateTime.UtcNow,
            });
            repo.Remove(p);
        }
        await _unitOfWork.SaveEntitiesAsync(ct);
        await HardPurgeAsync("Prescriptions", rows.Select(r => r.Id).ToList(), ct);
        report.Items.Add(new("Prescriptions", TableTitles["Prescriptions"], rows.Count, rows.Count, 0));
        report.TotalArchived += rows.Count;
    }

    /// <summary>آرشیو آزمایش‌ها</summary>
    private async Task ArchiveLabOrdersAsync(List<LabOrder> rows, int year, long? by,
        ArchiveRunReport report, CancellationToken ct)
    {
        var patients = await LoadPatientNamesAsync(rows.Select(r => r.PatientId).Distinct(), ct);
        var repo = _unitOfWork.Repository<LabOrder>();
        
        foreach (var o in rows)
        {
            _context.ArchiveRecords.Add(new ArchiveRecord
            {
                SourceTable = "LabOrders", SourceId = o.Id, PatientId = o.PatientId,
                PatientName = patients.GetValueOrDefault(o.PatientId),
                RecordDate = o.OrderDate ?? o.CreatedAt, YearJalali = year,
                SummaryLine = $"آزمایش {o.OrderNumber} — {o.Status}",
                Payload = JsonSerializer.Serialize(new
                {
                    o.Id, o.OrderNumber, o.PatientId, o.DoctorId, o.VisitId,
                    o.OrderDate, Priority = o.Priority.ToString(), Status = o.Status.ToString(),
                    o.ClinicalInfo, o.CreatedAt,
                }, Json),
                ArchivedBy = by, ArchivedAt = DateTime.UtcNow,
            });
            repo.Remove(o);
        }
        await _unitOfWork.SaveEntitiesAsync(ct);
        await HardPurgeAsync("LabOrders", rows.Select(r => r.Id).ToList(), ct);
        report.Items.Add(new("LabOrders", TableTitles["LabOrders"], rows.Count, rows.Count, 0));
        report.TotalArchived += rows.Count;
    }

    /// <summary>آرشیو ویزیت‌ها</summary>
    private async Task ArchiveVisitsAsync(List<Visit> rows, int year, long? by,
        ArchiveRunReport report, CancellationToken ct)
    {
        var patients = await LoadPatientNamesAsync(rows.Select(r => r.PatientId).Distinct(), ct);
        var repo = _unitOfWork.Repository<Visit>();
        
        foreach (var v in rows)
        {
            _context.ArchiveRecords.Add(new ArchiveRecord
            {
                SourceTable = "Visits", SourceId = v.Id, PatientId = v.PatientId,
                PatientName = patients.GetValueOrDefault(v.PatientId),
                RecordDate = v.VisitDate, YearJalali = year,
                SummaryLine = $"ویزیت — {v.ChiefComplaint}",
                Payload = JsonSerializer.Serialize(new
                {
                    v.Id, v.VisitCode, v.PatientId, v.DoctorId, v.ReceptionId,
                    v.VisitDate, Type = v.VisitType.ToString(), v.ChiefComplaint,
                    v.SoapAssessment, Status = v.Status.ToString(), v.CreatedAt,
                }, Json),
                ArchivedBy = by, ArchivedAt = DateTime.UtcNow,
            });
            repo.Remove(v);
        }
        await _unitOfWork.SaveEntitiesAsync(ct);
        await HardPurgeAsync("Visits", rows.Select(r => r.Id).ToList(), ct);
        report.TotalArchived += rows.Count;
    }

    /// <summary>آرشیو پذیرش‌ها</summary>
    private async Task ArchiveReceptionsAsync(List<Reception> rows, int year, long? by,
        ArchiveRunReport report, CancellationToken ct)
    {
        var patients = await LoadPatientNamesAsync(rows.Select(r => r.PatientId).Distinct(), ct);
        var repo = _unitOfWork.Repository<Reception>();
        
        foreach (var r in rows)
        {
            _context.ArchiveRecords.Add(new ArchiveRecord
            {
                SourceTable = "Receptions", SourceId = r.Id, PatientId = r.PatientId,
                PatientName = patients.GetValueOrDefault(r.PatientId),
                RecordDate = r.ReceptionDate, YearJalali = year,
                SummaryLine = $"پذیرش {r.ReceptionNumber} — {r.Status}",
                Payload = JsonSerializer.Serialize(new
                {
                    r.Id, r.ReceptionNumber, r.PatientId, r.DoctorId, r.AppointmentId,
                    r.ReceptionDate, Status = r.Status.ToString(), r.EnterTime, r.ExitTime,
                    r.WaitingTime, r.Notes, r.CreatedAt,
                }, Json),
                ArchivedBy = by, ArchivedAt = DateTime.UtcNow,
            });
            repo.Remove(r);
        }
        await _unitOfWork.SaveEntitiesAsync(ct);
        await HardPurgeAsync("Receptions", rows.Select(r => r.Id).ToList(), ct);
        report.TotalArchived += rows.Count;
    }

    /// <summary>آرشیو نوبت‌ها</summary>
    private async Task ArchiveAppointmentsAsync(List<Appointment> rows, int year, long? by,
        ArchiveRunReport report, CancellationToken ct)
    {
        var patients = await LoadPatientNamesAsync(rows.Select(r => r.PatientId).Distinct(), ct);
        var repo = _unitOfWork.Repository<Appointment>();
        
        foreach (var a in rows)
        {
            _context.ArchiveRecords.Add(new ArchiveRecord
            {
                SourceTable = "Appointments", SourceId = a.Id, PatientId = a.PatientId,
                PatientName = patients.GetValueOrDefault(a.PatientId),
                RecordDate = a.AppointmentDate.ToDateTime(TimeOnly.MinValue), YearJalali = year,
                SummaryLine = $"نوبت {a.AppointmentCode} — {a.Status}",
                Payload = JsonSerializer.Serialize(new
                {
                    a.Id, a.AppointmentCode, a.PatientId, a.DoctorId,
                    a.AppointmentDate, a.AppointmentTime, Status = a.Status.ToString(),
                    Type = a.AppointmentType.ToString(), Source = a.Source.ToString(),
                    a.IsFirstVisit, a.EstimatedFee, a.Notes, a.CreatedAt,
                }, Json),
                ArchivedBy = by, ArchivedAt = DateTime.UtcNow,
            });
            repo.Remove(a);
        }
        await _unitOfWork.SaveEntitiesAsync(ct);
        await HardPurgeAsync("Appointments", rows.Select(r => r.Id).ToList(), ct);
        report.TotalArchived += rows.Count;
    }

    /// <summary>نام بیماران — برای ذخیره در رکورد آرشیو بدون join آینده</summary>
    private async Task<Dictionary<long, string>> LoadPatientNamesAsync(IEnumerable<long> ids, CancellationToken ct)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return new Dictionary<long, string>();
        var names = await _unitOfWork.Repository<Domain.Entities.People.Patient>().QueryableNoTracking
            .Where(p => idList.Contains(p.Id))
            .Select(p => new { p.Id, p.FirstName, p.LastName })
            .ToListAsync(ct);
        return names.ToDictionary(n => n.Id, n => $"{n.FirstName} {n.LastName}".Trim());
    }

    /// <summary>کوئری کمکی — شناسه‌ها</summary>
    private static async Task<List<long>> IdsAsync<T>(IQueryable<T> query, CancellationToken ct)
        where T : Domain.Common.BaseEntity
        => await query.Select(x => x.Id).ToListAsync(ct);

    /// <summary>
    /// پاک‌سازی فیزیکی ردیف‌های آرشیوشده — فقط با hardPurge فعال.
    /// نام جدول از مجموع ثابت داخلی است (بدون ورودی کاربر)؛ پس از حذف،
    /// ایندکس‌های جدول با اسکریپت نگهداشت (docs/guides/archive-maintenance.sql) بازسازی می‌شود.
    /// </summary>
    private async Task HardPurgeAsync(string tableName, IReadOnlyCollection<long> ids, CancellationToken ct)
    {
        if (!_hardPurge || ids.Count == 0) return;

        var idList = string.Join(",", ids);
        var affected = await _context.Database.ExecuteSqlRawAsync(
            $"DELETE FROM {tableName} WHERE Id IN ({idList})", ct);
        _logger.LogInformation("پاک‌سازی فیزیکی {Table}: {Count} ردیف حذف شد", tableName, affected);
    }
}

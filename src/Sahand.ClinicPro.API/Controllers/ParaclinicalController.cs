using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Paraclinical;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر پاراکلینیک — تصویربرداری و دندانپزشکی و چشم و فیزیوتراپی
/// مسیرها: /api/v1/imaging و /api/v1/dental و /api/v1/eye و /api/v1/physiotherapy
/// گام R4: مدیریت فایل‌های تصویر شامل آپلود DICOM و دانلود
/// چرخه بیستم (گزارش چهاردهم مالک): قالب‌های مسیر با پیشوند ~/ مطلق شدند تا
/// به مسیر پایهٔ api/v1/[controller] الحاق نشوند (وگرنه 404 برمی‌گشتند).
/// </summary>
[ApiController]
[Authorize]
public class ParaclinicalController : ApiBaseController
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _host;
    private readonly IConfiguration _configuration;

    /// <summary>سازنده با تزریق میانجی و واحد کار و میزبان</summary>
    public ParaclinicalController(IMediator mediator, IUnitOfWork unitOfWork,
        IWebHostEnvironment host, IConfiguration configuration)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _host = host;
        _configuration = configuration;
    }

    /// <summary>ثبت درخواست تصویربرداری</summary>
    [HttpPost("~/api/v1/imaging/orders")]
    [HasPermission("imaging.create")]
    public async Task<IActionResult> CreateImagingOrder([FromBody] CreateImagingOrderDto dto)
    {
        var result = await _mediator.Send(new CreateImagingOrderCommand(dto));
        if (result.IsSuccess)
            // چرخهٔ سی‌امین — «GetOrders» به nameof(GetTodayOrders) اصلاح شد (اکشنی با آن نام وجود نداشت)
            return CreatedResponse(result, nameof(GetTodayOrders), new { }, "درخواست تصویربرداری ثبت شد");
        return ToResponse(result);
    }

    /// <summary>فهرست درخواست‌های تصویربرداری امروز — برای انتخاب در گشودن فایل DICOM (گام R4)</summary>
    [HttpGet("~/api/v1/imaging/orders")]
    [HasPermission("imaging.create")]
    public async Task<IActionResult> GetTodayOrders()
    {
        var (todayStart, tomorrowStart) = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.IranDayUtcBounds(DateTime.UtcNow);
        var items = await _unitOfWork.Repository<Domain.Entities.Paraclinical.ImagingOrder>()
            .QueryableNoTracking
            .Where(o => o.CreatedAt >= todayStart && o.CreatedAt < tomorrowStart && !o.IsDeleted)
            .OrderByDescending(o => o.Id)
            .Take(100)
            .Select(o => new
            {
                o.Id,
                o.PatientId,
                PatientName = o.Patient != null ? o.Patient.FirstName + " " + o.Patient.LastName : "",
                PatientGender = o.Patient != null ? o.Patient.Gender.ToString() : "",
                PatientBirthShamsi = o.Patient != null ? o.Patient.BirthDateShamsi : null,
                Modality = o.Modality.ToString(),
                o.BodyPart,
                Status = (int)o.Status,
                Priority = (int)o.Priority,
                o.ClinicalInfo,
                o.Technique,
                o.Findings,
                o.Impression,
                o.Recommendation,
                o.ReportDetails,
                o.CreatedAt,
                o.PerformedAt,
                o.ReportedAt
            })
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(items));
    }

    /// <summary>تغییر وضعیت گردش کار درخواست (شروع انجام/انجام‌شده) — ماژول حرفه‌ای تصویربرداری</summary>
    [HttpPost("~/api/v1/imaging/orders/status")]
    [HasPermission("imaging.edit")]
    public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateImagingOrderStatusCommand command)
    {
        var result = await _mediator.Send(command);
        return ToResponse(result, "وضعیت درخواست به‌روز شد");
    }

    /// <summary>ثبت گزارش رادیولوژی</summary>
    [HttpPut("~/api/v1/imaging/reports")]
    [HasPermission("imaging.edit")]
    public async Task<IActionResult> SubmitReport([FromBody] ImagingReportDto dto)
    {
        var result = await _mediator.Send(new SubmitImagingReportCommand(dto));
        return ToResponse(result, "گزارش ثبت شد");
    }

    /// <summary>ثبت طرح درمان دندانپزشکی</summary>
    [HttpPost("~/api/v1/dental/plans")]
    [HasPermission("dental.create")]
    public async Task<IActionResult> CreateDentalPlan([FromBody] CreateDentalPlanCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            // چرخهٔ سی‌امین — نام اکشن هدف با nameof وصل شد؛ رشتهٔ «GetOrders»
            // وجود نداشت و پس از ثبت موفقِ طرح، اجرای نتیجه با
            // «No route matches the supplied values» فرو می‌ریخت و کلاینت ۵۰۰ می‌گرفت
            return CreatedResponse(result, nameof(GetTodayOrders), new { }, "طرح درمان ثبت شد");
        return ToResponse(result);
    }

    /// <summary>
    /// بازیابی آخرین چارت دندان و طرح درمان یک بیمار — چرخهٔ بیست‌وهشتم
    /// (گزارش هجدهم مالک): «در دندانپزشکی مشخص نیست اطلاعات برای چه کسی
    /// ذخیره و از کجا بازیابی می‌شود». این نقطهٔ پایانی، آخرین طرح درمانِ
    /// ثبت‌شدهٔ بیمار را برمی‌گرداند: وضعیت تک‌تک دندان‌ها (نقشهٔ شماره →
    /// وضعیت) + ردیف‌های طرح درمان + تاریخ ثبت. PlanDetails طرح به‌صورت
    /// JSON ذخیره می‌شود و همین‌جا تجزیه می‌شود؛ طرح‌های متنی قدیمی بدون
    /// چارت، فهرست ردیف خالی اما مشخصات طرح را برمی‌گردانند.
    /// </summary>
    /// <param name="patientId">شناسه بیمار</param>
    [HttpGet("~/api/v1/dental/chart")]
    [HasPermission("dental.create")]
    public async Task<IActionResult> GetDentalChart([FromQuery] long patientId)
    {
        if (patientId <= 0)
            return BadRequest(ApiResponse<object>.Failure("شناسه بیمار نامعتبر است"));

        // آخرین طرح درمان بیمار (حذف‌نرم‌شده‌ها بی‌اثر)
        var plan = await _unitOfWork.Repository<Domain.Entities.Paraclinical.DentalTreatmentPlan>()
            .QueryableNoTracking
            .Where(p => p.PatientId == patientId && !p.IsDeleted)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        if (plan is null)
            return Ok(ApiResponse<object>.Success(new
            {
                planId = (long?)null,
                createdDateShamsi = (string?)null,
                teeth = new Dictionary<string, int>(),
                rows = Array.Empty<object>(),
                note = "چارتی برای این بیمار ثبت نشده است"
            }));

        if (!DentalChartCompatibility.CanEdit(plan.PlanDetails))
            return Ok(ApiResponse<object>.Success(new
            {
                planId = (long?)plan.Id,
                canEdit = false,
                note = plan.PlanDetails,
                teeth = new Dictionary<string, int>(),
                rows = Array.Empty<object>()
            }));

        // تجزیهٔ JSON ذخیره‌شده در PlanDetails — ساختار:
        // { "teeth": {"11": 2, ...}, "rows": [{"tooth":11,"procedure":"ترمیم","price":1500000,"status":"پیشنهاد"}] }
        var teeth = new Dictionary<string, int>();
        var rows = new List<object>();
        object? extras = null;
        if (!string.IsNullOrWhiteSpace(plan.PlanDetails))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(plan.PlanDetails);
                if (doc.RootElement.TryGetProperty("teeth", out var teethEl))
                    foreach (var prop in teethEl.EnumerateObject())
                        if (prop.Value.TryGetInt32(out var cond))
                            teeth[prop.Name] = cond;
                if (doc.RootElement.TryGetProperty("rows", out var rowsEl))
                    foreach (var row in rowsEl.EnumerateArray())
                        rows.Add(new
                        {
                            tooth = row.TryGetProperty("tooth", out var t) && t.TryGetInt32(out var tv) ? tv : 0,
                            procedure = row.TryGetProperty("procedure", out var pr) ? pr.GetString() : null,
                            price = row.TryGetProperty("price", out var p) && p.TryGetDecimal(out var pv) ? pv : 0m,
                            status = row.TryGetProperty("status", out var s) ? s.GetString() : null,
                            surface = row.TryGetProperty("surface", out var sf) ? sf.GetString() : null,
                            schedule = row.TryGetProperty("schedule", out var sc) ? sc.GetString() : null
                        });
                // بخش‌های تازهٔ ماژول دندانپزشکی (پریودنتال/پزشکی/نسخه/رادیوگرافی/رضایت)
                if (doc.RootElement.TryGetProperty("extras", out var ex))
                {
                    extras = new
                    {
                        perio = ex.TryGetProperty("perio", out var pe) ? pe.GetString() : null,
                        medical = ex.TryGetProperty("medical", out var me) ? me.GetString() : null,
                        prescription = ex.TryGetProperty("prescription", out var rx) ? rx.GetString() : null,
                        radio = ex.TryGetProperty("radio", out var ra) ? ra.GetString() : null,
                        clinical = ex.TryGetProperty("clinical", out var cl) ? cl.GetString() : null,
                        isChildChart = ex.TryGetProperty("isChildChart", out var child) && child.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False ? (bool?)child.GetBoolean() : null,
                        consent = ex.TryGetProperty("consent", out var co) && co.ValueKind == System.Text.Json.JsonValueKind.True
                    };
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // طرح متنی قدیمی — بدون چارت؛ خود متن به‌عنوان یادداشت برمی‌گردد
            }
        }

        return Ok(ApiResponse<object>.Success(new
        {
            planId = plan.Id,
            createdDateShamsi = plan.CreatedDate.HasValue
                ? Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(plan.CreatedDate.Value.ToDateTime(TimeOnly.MinValue))
                : null,
            teeth,
            rows,
            extras,
            canEdit = true,
            note = (string?)null
        }));
    }

    /// <summary>ثبت معاینه چشم‌پزشکی</summary>
    [HttpPost("~/api/v1/eye/exams")]
    [HasPermission("eye.create")]
    public async Task<IActionResult> SaveEyeExam([FromBody] SaveEyeExamCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            // چرخهٔ سی‌امین — «GetOrders» به nameof(GetTodayOrders) اصلاح شد (اکشنی با آن نام وجود نداشت)
            return CreatedResponse(result, nameof(GetTodayOrders), new { }, "معاینه چشم ثبت شد");
        return ToResponse(result);
    }

    /// <summary>
    /// سابقهٔ معاینه‌های چشم یک بیمار — چرخهٔ بیست‌وهشتم (گزارش هجدهم مالک):
    /// «در چشم‌پزشکی مشخص نیست اطلاعات برای چه کسی ذخیره و از کجا بازیابی
    /// می‌شود». ۲۰ معاینهٔ آخر بیمار (نزولی) با همهٔ اندازه‌ها برگردانده
    /// می‌شود؛ کلاینت «آخرین» معاینه را در فرم می‌نشاند و باقی به‌عنوان
    /// سابقهٔ قابل مرور نمایش داده می‌شوند.
    /// </summary>
    /// <param name="patientId">شناسه بیمار</param>
    [HttpGet("~/api/v1/eye/exams")]
    [HasPermission("eye.create")]
    public async Task<IActionResult> GetEyeExams([FromQuery] long patientId)
    {
        if (patientId <= 0)
            return BadRequest(ApiResponse<object>.Failure("شناسه بیمار نامعتبر است"));

        var exams = await _unitOfWork.Repository<Domain.Entities.Paraclinical.EyeExamRecord>()
            .QueryableNoTracking
            .Where(e => e.PatientId == patientId && !e.IsDeleted)
            .OrderByDescending(e => e.Id)
            .Take(20)
            .Select(e => new
            {
                e.Id,
                dateShamsi = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(e.CreatedAt),
                e.VisitId,
                od = new { sphere = e.RightSphere, cylinder = e.RightCylinder, axis = e.RightAxis,
                           acuity = e.RightVisualAcuity, iop = e.RightIop },
                os = new { sphere = e.LeftSphere, cylinder = e.LeftCylinder, axis = e.LeftAxis,
                           acuity = e.LeftVisualAcuity, iop = e.LeftIop },
                pd = e.PupilDistance,
                colorVision = e.ColorVision,
                e.Findings,
                e.VisualField,
                e.EyewearPrescription
            })
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(exams));
    }

    /// <summary>ثبت جلسه فیزیوتراپی</summary>
    [HttpPost("~/api/v1/physiotherapy/sessions")]
    [HasPermission("physiotherapy.create")]
    public async Task<IActionResult> RecordSession([FromBody] RecordPhysioSessionCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            // چرخهٔ سی‌امین — «GetOrders» به nameof(GetTodayOrders) اصلاح شد (اکشنی با آن نام وجود نداشت)
            return CreatedResponse(result, nameof(GetTodayOrders), new { }, "جلسه ثبت شد");
        return ToResponse(result);
    }

    /// <summary>
    /// سابقهٔ جلسه‌های فیزیوتراپی یک بیمار — ماژول حرفه‌ای فیزیوتراپی
    /// (طرح تأییدشدهٔ مالک): ۲۰ جلسهٔ آخر بیمار (صعودی بر اساس شمارهٔ جلسه)
    /// برای نوار پیشرفت دوره، نمودار روند درد/دامنهٔ حرکت و پیش‌پرکردن
    /// ارزیابی ساخت‌یافته برگردانده می‌شود.
    /// </summary>
    /// <param name="patientId">شناسه بیمار</param>
    [HttpGet("~/api/v1/physiotherapy/sessions")]
    [HasPermission("physiotherapy.create")]
    public async Task<IActionResult> GetPhysioSessions([FromQuery] long patientId)
    {
        if (patientId <= 0)
            return BadRequest(ApiResponse<object>.Failure("شناسه بیمار نامعتبر است"));

        var sessions = await _unitOfWork.Repository<Domain.Entities.Paraclinical.PhysiotherapySession>()
            .QueryableNoTracking
            .Where(e => e.PatientId == patientId && !e.IsDeleted)
            .OrderByDescending(e => e.Id)
            .Take(20)
            .Select(e => new
            {
                e.Id,
                dateShamsi = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(e.CreatedAt),
                e.SessionNumber,
                e.PainScore,
                e.Duration,
                e.Treatments,
                e.ProgressNote
            })
            .ToListAsync();
        sessions.Reverse();
        return Ok(ApiResponse<object>.Success(sessions));
    }

    // ═══════════════ فایل‌های تصویربرداری — گام R4 ═══════════════

    /// <summary>مسیر ریشه ذخیره فایل‌های تصویر</summary>
    private string ImagingRoot => Path.Combine(
        _host.ContentRootPath, _configuration["Storage:ImagingDirectory"] ?? "uploads/imaging");

    /// <summary>بارگذاری فایل تصویر (DICOM یا معمولی) — تشخیص خودکار از جادوی DICM</summary>
    /// <param name="orderId">شناسه درخواست تصویربرداری</param>
    /// <param name="file">فایل چندبخشی</param>
    [HttpPost("~/api/v1/imaging/files")]
    [HasPermission("imaging.edit")]
    [RequestSizeLimit(52_428_800)] // سقف ۵۰ مگابایت
    public async Task<IActionResult> UploadFile([FromForm] long orderId, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Failure("فایلی برای بارگذاری انتخاب نشده است"));
        if (file.Length > 52_428_800)
            return BadRequest(ApiResponse<object>.Failure("حجم فایل بیش از ۵۰ مگابایت است"));

        var order = await _unitOfWork.Repository<Domain.Entities.Paraclinical.ImagingOrder>()
            .GetByIdAsync(orderId);
        if (order is null)
            return BadRequest(ApiResponse<object>.Failure("درخواست تصویربرداری یافت نشد"));

        // تشخیص DICOM از جادوی DICM در ۱۲۸ بایت ابتدایی
        bool isDicom;
        var probe = new byte[132];
        await using (var stream = file.OpenReadStream())
        {
            var read = await stream.ReadAsync(probe.AsMemory(0, Math.Min(132, (int)file.Length)));
            isDicom = read >= 132 &&
                      probe[128] == (byte)'D' && probe[129] == (byte)'I' &&
                      probe[130] == (byte)'C' && probe[131] == (byte)'M';
        }

        // ذخیره فیزیکی: uploads/imaging/yyyyMM/تعمیم‌شده-نام‌اصلی
        var folder = Path.Combine(ImagingRoot, DateTime.UtcNow.ToString("yyyyMM"));
        Directory.CreateDirectory(folder);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
            extension = isDicom ? ".dcm" : ".bin";
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, storedName);
        await using (var target = System.IO.File.Create(fullPath))
        {
            await using var source = file.OpenReadStream();
            await source.CopyToAsync(target);
        }

        var entity = new Domain.Entities.Paraclinical.ImagingFile
        {
            ImagingOrderId = orderId,
            FileName = Path.GetFileName(file.FileName),
            FilePath = Path.GetRelativePath(_host.ContentRootPath, fullPath).Replace('\\', '/'),
            FileType = extension.TrimStart('.'),
            FileSize = file.Length,
            IsDicom = isDicom,
            UploadedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<Domain.Entities.Paraclinical.ImagingFile>().AddAsync(entity);
        await _unitOfWork.SaveEntitiesAsync();

        return Ok(ApiResponse<object>.Success(new { fileId = entity.Id, isDicom },
            isDicom ? "فایل DICOM بارگذاری شد" : "فایل تصویر بارگذاری شد"));
    }

    /// <summary>فهرست فایل‌های یک درخواست تصویربرداری</summary>
    [HttpGet("~/api/v1/imaging/files")]
    [HasPermission("imaging.create")]
    public async Task<IActionResult> GetFiles([FromQuery] long orderId)
    {
        var items = await _unitOfWork.Repository<Domain.Entities.Paraclinical.ImagingFile>()
            .QueryableNoTracking
            .Where(f => f.ImagingOrderId == orderId && !f.IsDeleted)
            .OrderByDescending(f => f.Id)
            .Select(f => new ImagingFileRowDto(f.Id, f.ImagingOrderId, f.FileName,
                f.FileType, f.FileSize, f.IsDicom, f.UploadedAt, f.FilePath))
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(items));
    }

    /// <summary>دانلود محتوای فایل — برای گشودن در نمایشگر DICOM کلاینت</summary>
    [HttpGet("~/api/v1/imaging/files/{id:long}/content")]
    [HasPermission("imaging.create")]
    public async Task<IActionResult> DownloadFile(long id)
    {
        var entity = await _unitOfWork.Repository<Domain.Entities.Paraclinical.ImagingFile>()
            .QueryableNoTracking
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        if (entity is null)
            return BadRequest(ApiResponse<object>.Failure("فایل یافت نشد"));

        var fullPath = Path.Combine(_host.ContentRootPath, entity.FilePath ?? string.Empty);
        if (!System.IO.File.Exists(fullPath))
            return BadRequest(ApiResponse<object>.Failure("فایل روی دیسک یافت نشد"));

        var stream = System.IO.File.OpenRead(fullPath);
        return File(stream, "application/octet-stream",
            entity.FileName ?? $"imaging-{id}{(entity.IsDicom ? ".dcm" : "")}");
    }

    /// <summary>حذف نرم فایل تصویر</summary>
    [HttpDelete("~/api/v1/imaging/files/{id:long}")]
    [HasPermission("imaging.edit")]
    public async Task<IActionResult> DeleteFile(long id)
    {
        var entity = await _unitOfWork.Repository<Domain.Entities.Paraclinical.ImagingFile>()
            .QueryableTracking
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        if (entity is null)
            return BadRequest(ApiResponse<object>.Failure("فایل یافت نشد"));

        _unitOfWork.Repository<Domain.Entities.Paraclinical.ImagingFile>().Remove(entity);
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("فایل حذف شد"));
    }

    /// <summary>ردیف فایل تصویر</summary>
    public record ImagingFileRowDto(long Id, long ImagingOrderId, string? FileName,
        string? FileType, long? FileSize, bool IsDicom, DateTime? UploadedAt, string? FilePath);
}

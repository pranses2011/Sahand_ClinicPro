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
            return CreatedResponse(result, "GetOrders", new { }, "درخواست تصویربرداری ثبت شد");
        return ToResponse(result);
    }

    /// <summary>فهرست درخواست‌های تصویربرداری امروز — برای انتخاب در گشودن فایل DICOM (گام R4)</summary>
    [HttpGet("~/api/v1/imaging/orders")]
    [HasPermission("imaging.create")]
    public async Task<IActionResult> GetTodayOrders()
    {
        var today = DateTime.UtcNow.Date;
        var items = await _unitOfWork.Repository<Domain.Entities.Paraclinical.ImagingOrder>()
            .QueryableNoTracking
            .Where(o => o.CreatedAt >= today && !o.IsDeleted)
            .OrderByDescending(o => o.Id)
            .Take(100)
            .Select(o => new
            {
                o.Id,
                PatientName = o.Patient != null ? o.Patient.FirstName + " " + o.Patient.LastName : "",
                Modality = o.Modality.ToString(),
                o.BodyPart,
                o.Status
            })
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(items));
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
            return CreatedResponse(result, "GetOrders", new { }, "طرح درمان ثبت شد");
        return ToResponse(result);
    }

    /// <summary>ثبت معاینه چشم‌پزشکی</summary>
    [HttpPost("~/api/v1/eye/exams")]
    [HasPermission("eye.create")]
    public async Task<IActionResult> SaveEyeExam([FromBody] SaveEyeExamCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return CreatedResponse(result, "GetOrders", new { }, "معاینه چشم ثبت شد");
        return ToResponse(result);
    }

    /// <summary>ثبت جلسه فیزیوتراپی</summary>
    [HttpPost("~/api/v1/physiotherapy/sessions")]
    [HasPermission("physiotherapy.create")]
    public async Task<IActionResult> RecordSession([FromBody] RecordPhysioSessionCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return CreatedResponse(result, "GetOrders", new { }, "جلسه ثبت شد");
        return ToResponse(result);
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

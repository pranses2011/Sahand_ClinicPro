using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر عکس پروفایل پرسنل و پزشکان — چرخه هجدهم (گزارش دوازدهم مالک):
/// «برای تمامی پرسنل و دکترها عکس هم انتخاب بشه و در جاهای مناسب نشون داده
/// بشه و با کلیک روی عکس اون رو بزرگ نشون بده؛ همیشه آدرس فایل عکس‌ها رو در
/// دیتابیس ذخیره کن و فایل اصلی رو در پوشهٔ برنامه و داخل زیرپوشه‌های مرتبط
/// قرار بده».
///
/// معماری:
/// - فایل اصلی در «پوشهٔ برنامه» زیر Photos/Employees ذخیره می‌شود
///   (مثلاً Photos/Employees/emp-12.jpg)؛
/// - «آدرس نسبی فایل» در ستون ProfilePicture دیتابیس ذخیره می‌شود؛
/// - پزشک عکس پرسنلِ مرتبط با خودش را نشان می‌دهد (Doctor.EmployeeId)؛
/// - GET عکس‌ها برای همهٔ کاربران واردشده مجاز است (نمایش در فرم‌ها/فهرست‌ها)؛
///   بارگذاری/حذف نیازمند مجوز ایجاد پرسنل است — هماهنگ با سایر مسیرهای پرسنل.
/// مسیرها: /api/v1/people
/// </summary>
[ApiController]
[Route("api/v1/people")]
[Authorize]
public class PeoplePhotosController : ApiBaseController
{
    /// <summary>پسوندهای مجاز عکس پروفایل</summary>
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };

    /// <summary>حداکثر اندازهٔ عکس — ۵ مگابایت</summary>
    private const int MaxPhotoBytes = 5 * 1024 * 1024;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _env;

    /// <summary>سازنده با واحد کار و محیط میزبان (برای ریشهٔ پوشهٔ برنامه)</summary>
    public PeoplePhotosController(IUnitOfWork unitOfWork, IWebHostEnvironment env)
    {
        _unitOfWork = unitOfWork;
        _env = env;
    }

    /// <summary>
    /// بارگذاری/تعویض عکس پروفایل پرسنل — چندبخشی با فیلد «file»؛
    /// فایل قبلی (با هر پسوند) حذف و مسیر نسبی جدید در دیتابیس ذخیره می‌شود.
    /// </summary>
    /// <param name="id">شناسه پرسنل</param>
    [HttpPost("employees/{id:long}/photo")]
    [HasPermission("patients.create")]
    public async Task<IActionResult> UploadEmployeePhoto(long id)
    {
        var file = Request.Form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Failure("فایل عکس ارسال نشده است"));

        if (file.Length > MaxPhotoBytes)
            return BadRequest(ApiResponse<object>.Failure("حجم عکس نباید بیش از ۵ مگابایت باشد"));

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest(ApiResponse<object>.Failure("قالب عکس باید JPG یا PNG باشد"));

        var employee = await _unitOfWork.Repository<Employee>().QueryableTracking
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (employee is null)
            return NotFound(ApiResponse<object>.Failure("پرسنل یافت نشد"));

        // پوشهٔ هدف: Photos/Employees داخل پوشهٔ برنامه
        var photosRoot = Path.Combine(_env.ContentRootPath, "Photos", "Employees");
        Directory.CreateDirectory(photosRoot);

        // حذف فایل‌های قبلی همین پرسنل (تعویض قالب هم پاکسازی شود)
        foreach (var ext in AllowedExtensions)
        {
            var stale = Path.Combine(photosRoot, $"emp-{id}{ext}");
            if (System.IO.File.Exists(stale))
                System.IO.File.Delete(stale);
        }

        var fileName = $"emp-{id}{extension}";
        var fullPath = Path.Combine(photosRoot, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        // آدرس نسبی در دیتابیس — چرخه هجدهم (قاعدهٔ مالک)
        employee.ProfilePicture = $"Photos/Employees/{fileName}";
        await _unitOfWork.SaveEntitiesAsync();

        return Ok(ApiResponse<object>.Success(
            new { profilePicture = employee.ProfilePicture }, "عکس پروفایل ذخیره شد"));
    }

    /// <summary>
    /// دریافت بایت‌های عکس پروفایل پرسنل — برای نمایش در فرم‌ها و بزرگ‌نمایی.
    /// </summary>
    /// <param name="id">شناسه پرسنل</param>
    [HttpGet("employees/{id:long}/photo")]
    public async Task<IActionResult> GetEmployeePhoto(long id)
    {
        var employee = await _unitOfWork.Repository<Employee>().QueryableNoTracking
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        return await ServePhotoAsync(employee?.ProfilePicture);
    }

    /// <summary>
    /// دریافت عکس پزشک — از رکورد پرسنل مرتبط (Doctor.EmployeeId)؛
    /// اگر پزشک پرسنل مرتبط نداشته باشد عکسی هم ندارد.
    /// </summary>
    /// <param name="doctorId">شناسه پزشک</param>
    [HttpGet("doctors/{doctorId:long}/photo")]
    public async Task<IActionResult> GetDoctorPhoto(long doctorId)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().QueryableNoTracking
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.Id == doctorId && !d.IsDeleted);
        return await ServePhotoAsync(doctor?.Employee?.ProfilePicture);
    }

    /// <summary>
    /// حذف عکس پروفایل پرسنل — فایل ازディسک پاک و آدرس در دیتابیس تهی می‌شود.
    /// </summary>
    /// <param name="id">شناسه پرسنل</param>
    [HttpDelete("employees/{id:long}/photo")]
    [HasPermission("patients.create")]
    public async Task<IActionResult> DeleteEmployeePhoto(long id)
    {
        var employee = await _unitOfWork.Repository<Employee>().QueryableTracking
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (employee is null)
            return NotFound(ApiResponse<object>.Failure("پرسنل یافت نشد"));

        if (!string.IsNullOrEmpty(employee.ProfilePicture))
        {
            var fullPath = Path.Combine(_env.ContentRootPath,
                employee.ProfilePicture.Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        employee.ProfilePicture = null;
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("عکس پروفایل حذف شد"));
    }

    /// <summary>
    /// سرو فایل عکس از مسیر نسبی ذخیره‌شده — با محافظت از پیمایش مسیر
    /// (نورم‌سازی و چسباندن به ریشهٔ Photos).
    /// </summary>
    private async Task<IActionResult> ServePhotoAsync(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return NotFound(ApiResponse<object>.Failure("عکسی ثبت نشده است"));

        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        if (!normalized.StartsWith("Photos/", StringComparison.OrdinalIgnoreCase))
            return NotFound(ApiResponse<object>.Failure("مسیر عکس نامعتبر است"));

        var fullPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath,
            normalized.Replace('/', Path.DirectorySeparatorChar)));

        // گارد پیمایش مسیر — مسیر نهایی باید داخل ریشهٔ برنامه بماند
        if (!fullPath.StartsWith(Path.GetFullPath(_env.ContentRootPath),
                StringComparison.OrdinalIgnoreCase))
            return NotFound(ApiResponse<object>.Failure("مسیر عکس نامعتبر است"));

        if (!System.IO.File.Exists(fullPath))
            return NotFound(ApiResponse<object>.Failure("فایل عکس یافت نشد"));

        var contentType = Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            _ => "image/jpeg",
        };
        return PhysicalFile(fullPath, contentType);
    }
}

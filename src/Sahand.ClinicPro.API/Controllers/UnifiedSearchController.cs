using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر جستجوی یکپارچه — چرخهٔ ۶۹ (فاز ۳ / F-06 نقشه راه ai-review)
/// «جستجوی یکپارچه Ctrl+K (بیمار/نوبت/نسخه)»
///
/// یک درخواست واحد، سه حوزهٔ پرکاربرد را با هم جستجو می‌کند تا پنجرهٔ
/// جستجوی سریع دسکتاپ (Ctrl+K) با یک رفت‌وبرگشت شبکه پر شود:
/// ۱) بیماران — نام/نام خانوادگی/کد ملی/موبایل (حداکثر ۸ نتیجه)
/// ۲) نوبت‌های امروز — بر اساس نام بیمار (حداکثر ۵ نتیجه)
/// ۳) نسخه‌های اخیر — شمارهٔ نسخه یا نام بیمار (حداکثر ۵ نتیجه)
///
/// سازگاری آینده: کلید «type» هر نتیجه، مقصد ناوبری کلاینت را تعیین می‌کند؛
/// افزودن حوزهٔ چهارم فقط با افزودن یک فهرست دیگر به پاسخ ممکن است.
/// مسیرها: /api/v1/search
/// </summary>
[ApiController]
[Route("api/v1/search")]
[Authorize]
public class UnifiedSearchController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public UnifiedSearchController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>
    /// جستجوی یکپارچه — حداقل ۲ نویسه؛ همهٔ حوزه‌ها با هم در یک پاسخ
    /// </summary>
    /// <param name="q">عبارت جستجو — نام بیمار، کد ملی، موبایل یا شمارهٔ نسخه</param>
    /// <param name="ct">توکن لغو</param>
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
    {
        // عبارت کوتاه‌تر از ۲ نویسه نتیجهٔ معناداری ندارد و فقط سرور را می‌آزارد
        var term = q?.Trim();
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return Ok(Application.Common.Models.ApiResponse<object>.Success(new
            {
                patients = Array.Empty<object>(),
                appointments = Array.Empty<object>(),
                prescriptions = Array.Empty<object>()
            }, "عبارت جستجو کوتاه است"));

        // ═══ ۱) بیماران — نام/نام خانوادگی/کد ملی/موبایل ═══
        var patients = await _unitOfWork.Repository<Domain.Entities.People.Patient>()
            .QueryableNoTracking
            .Where(p => !p.IsDeleted &&
                        (p.FirstName.Contains(term) ||
                         p.LastName.Contains(term) ||
                         (p.NationalCode != null && p.NationalCode.Contains(term)) ||
                         (p.Mobile != null && p.Mobile.Contains(term))))
            .OrderBy(p => p.Id)
            .Take(8)
            .Select(p => new
            {
                type = "patient",
                id = p.Id,
                title = p.FirstName + " " + p.LastName,
                subtitle = (p.NationalCode ?? "—") + " · " + (p.Mobile ?? "—")
            })
            .ToListAsync(ct);

        // ═══ ۲) نوبت‌های امروز — نام بیمار یا شمارهٔ پیگیری ═══
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var appointments = await _unitOfWork.Repository<Domain.Entities.Scheduling.Appointment>()
            .QueryableNoTracking
            .Where(a => !a.IsDeleted
                        && a.AppointmentDate == today
                        && a.Status != AppointmentStatus.Cancelled
                        && ((a.Patient != null && (a.Patient.FirstName + " " + a.Patient.LastName).Contains(term))
                            || (a.AppointmentCode != null && a.AppointmentCode.Contains(term))))
            .OrderBy(a => a.AppointmentTime)
            .Take(5)
            .Select(a => new
            {
                type = "appointment",
                id = a.Id,
                title = a.Patient != null ? a.Patient.FirstName + " " + a.Patient.LastName : "—",
                subtitle = "نوبت " + a.AppointmentTime.ToString("HH:mm") + " · "
                            + (a.Doctor != null && a.Doctor.Employee != null
                                ? a.Doctor.Employee.FirstName + " " + a.Doctor.Employee.LastName
                                : "پزشک")
            })
            .ToListAsync(ct);

        // ═══ ۳) نسخه‌های اخیر — شمارهٔ نسخه یا نام بیمار ═══
        var prescriptions = await _unitOfWork.Repository<Domain.Entities.Clinical.Prescription>()
            .QueryableNoTracking
            .Where(r => !r.IsDeleted
                        && ((r.PrescriptionCode != null && r.PrescriptionCode.Contains(term))
                            || (r.Patient != null && (r.Patient.FirstName + " " + r.Patient.LastName).Contains(term))))
            .OrderByDescending(r => r.Id)
            .Take(5)
            .Select(r => new
            {
                type = "prescription",
                id = r.Id,
                title = r.PrescriptionCode != null ? "نسخهٔ " + r.PrescriptionCode : "نسخه",
                subtitle = (r.Patient != null ? r.Patient.FirstName + " " + r.Patient.LastName : "—")
                           + (r.PrescriptionDate.HasValue ? " · " + PersianDateHelper.ToShamsi(r.PrescriptionDate.Value) : "")
            })
            .ToListAsync(ct);

        return Ok(Application.Common.Models.ApiResponse<object>.Success(new
        {
            patients,
            appointments,
            prescriptions
        }, "نتایج جستجو دریافت شد"));
    }
}

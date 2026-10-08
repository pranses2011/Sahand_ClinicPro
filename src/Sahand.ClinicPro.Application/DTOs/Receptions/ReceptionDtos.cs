using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Receptions;

/// <summary>ردیف فهرست پذیرش روز</summary>
public record ReceptionListDto(
    long Id,
    string? ReceptionNumber,
    string PatientName,
    /// <summary>چرخه ۲۴: در پذیرش خدمتی به‌جای پزشک، نام خدمت (یا «خدمت درمانی») می‌آید</summary>
    string DoctorName,
    string TimeShamsi,
    ReceptionStatus Status,
    string? InsuranceName,
    int? WaitingTime);

/// <summary>درخواست ثبت پذیرش</summary>
public record CreateReceptionDto(
    long PatientId,
    /// <summary>چرخه ۲۴: پزشک در پذیرش «خدمت درمانی» اختیاری است — یا پزشک یا خدمت</summary>
    long? DoctorId,
    long? AppointmentId,
    long? InsuranceCompanyId,
    string? ReferralCode,
    string? Notes,
    /// <summary>شناسهٔ خدمت تعرفه‌دار — تزریقات/پانسمان/آزمایش/تصویربرداری (چرخه ۲۴)</summary>
    long? ServiceId = null,
    /// <summary>نام خدمت برای نمایش تاریخی (چرخه ۲۴)</summary>
    string? ServiceName = null,
    /// <summary>پرسنل انجام‌دهندهٔ خدمت (چرخه ۲۴)</summary>
    long? PerformedByEmployeeId = null);

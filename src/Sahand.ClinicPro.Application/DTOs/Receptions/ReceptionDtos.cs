using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Receptions;

/// <summary>ردیف فهرست پذیرش روز</summary>
public record ReceptionListDto(
    long Id,
    string? ReceptionNumber,
    string PatientName,
    // چرخه ۲۴: در پذیرش خدمتی به‌جای پزشک، نام خدمت (یا «خدمت درمانی») می‌آید
    string DoctorName,
    string TimeShamsi,
    ReceptionStatus Status,
    string? InsuranceName,
    int? WaitingTime);

/// <summary>درخواست ثبت پذیرش</summary>
public record CreateReceptionDto(
    long PatientId,
    // چرخه ۲۴: پزشک در پذیرش «خدمت درمانی» اختیاری است — یا پزشک یا خدمت
    long? DoctorId,
    long? AppointmentId,
    long? InsuranceCompanyId,
    string? ReferralCode,
    string? Notes,
    // شناسهٔ خدمت تعرفه‌دار — تزریقات/پانسمان/آزمایش/تصویربرداری (چرخه ۲۴)
    long? ServiceId = null,
    // نام خدمت برای نمایش تاریخی (چرخه ۲۴)
    string? ServiceName = null,
    // پرسنل انجام‌دهندهٔ خدمت (چرخه ۲۴)
    long? PerformedByEmployeeId = null);

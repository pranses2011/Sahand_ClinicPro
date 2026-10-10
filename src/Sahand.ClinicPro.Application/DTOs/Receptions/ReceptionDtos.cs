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
    int? WaitingTime,
    // ═══ چرخه سی‌ونه‌م (گزارش مالک): اتاق محل انجام خدمت در فهرست روز ═══
    // «اتاق معاینه برای پزشک در برنامهٔ کاری پزشک مشخص می‌شود ولی برای خدمات
    // درمانی نمی‌شود معین کرد» — نام اتاق انتخابی پذیرش خدمتی در ستون خودش
    // می‌آید تا منشی جهت‌دهی بیمار به اتاق درست را از همین فهرست انجام دهد
    string? RoomName,
    // ═══ چرخه سی‌ویکم (گزارش مالک): «هیچ مبلغی در هیچ‌کجای برنامه نمایش داده
    // نمی‌شود» — جمع فاکتورهای متصل به این پذیرش و مبلغ پرداخت‌شدهٔ آنها در
    // فهرست پذیرش روز نمایش داده می‌شود تا گردش مالی همان جا دیده شود ═══
    decimal InvoiceTotal,
    decimal PaidAmount);

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
    long? PerformedByEmployeeId = null,
    // ═══ چرخهٔ سی‌ونه‌م (گزارش مالک) — اتاق انجام خدمت پذیرش خدمتی ═══
    // «اتاق معاینه برای پزشک در برنامهٔ کاری پزشک مشخص می‌شود ولی برای خدمات
    // درمانی نمی‌شود معین کرد» — شناسهٔ اتاق از فهرست اتاق‌های فعال درمانگاه
    long? RoomId = null);

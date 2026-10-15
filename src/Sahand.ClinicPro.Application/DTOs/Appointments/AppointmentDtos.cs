using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Appointments;

/// <summary>ردیف فهرست نوبت‌ها</summary>
public record AppointmentListDto(
    long Id,
    string? AppointmentCode,
    string PatientName,
    string DoctorName,
    string DateShamsi,
    string Time,
    AppointmentStatus Status,
    AppointmentType Type,
    int QueueNumber,
    // اولویت نوبت — ۱ اورژانس / ۲ بالا / ۳ عادی. چرخهٔ ۷۸: پیش‌تر این فیلد
    // برنمی‌گشت و کلاینت همیشه پیش‌فرض «عادی» را نمایش می‌داد (گزارش مالک:
    // «اولویت رو اشتباه میاره — همشون رو معمولی نوشته»).
    Priority Priority = Priority.Normal);

/// <summary>جزئیات نوبت</summary>
public record AppointmentDetailDto(
    long Id,
    string? AppointmentCode,
    long PatientId,
    string PatientName,
    long DoctorId,
    string DoctorName,
    string? SpecialtyName,
    string DateShamsi,
    string Time,
    int Duration,
    AppointmentStatus Status,
    AppointmentType Type,
    int? QueueNumber,
    decimal EstimatedFee,
    bool IsFirstVisit,
    string? Notes);

/// <summary>درخواست ثبت نوبت</summary>
public record CreateAppointmentDto(
    long PatientId,
    long DoctorId,
    long? SpecialtyId,
    string DateShamsi,
    string Time,
    AppointmentType Type = AppointmentType.Initial,
    AppointmentSource Source = AppointmentSource.InPerson,
    bool IsFirstVisit = false,
    // اولویت نوبت — ۱ اورژانس / ۲ بالا / ۳ عادی (چرخهٔ ۷۸). پیش‌تر این فیلد
    // در درخواست نبود و سرور همیشه «عادی» ذخیره می‌کرد — یعنی انتخاب کاربر
    // در فرم نوبت‌دهی عملاً نادیده گرفته می‌شد.
    Priority Priority = Priority.Normal,
    string? Notes = null);

namespace Sahand.ClinicPro.Application.DTOs.Appointments;

/// <summary>
/// یک اسلات زمانی ویزیت — گام U4 طبق فرم ۸
/// </summary>
public record SlotDto(
    string Time,
    bool IsAvailable,
    string? PatientName,
    bool IsBreak,
    string? RoomName,
    decimal Fee);

/// <summary>
/// خلاصه ماه تقویم نوبت‌دهی — برای نشانگرهای PersianCalendar
/// </summary>
public record CalendarSummaryDto(
    List<CalendarDayDto> Days);

/// <summary>
/// وضعیت یک روز در تقویم
/// </summary>
public record CalendarDayDto(
    string DateShamsi,
    bool IsDisabled,
    int FreeSlots,
    bool HasSchedule);

/// <summary>
/// نتیجه عملیات دسته‌ای
/// </summary>
public record BatchOperationResultDto(int Total, int Succeeded, int Failed);

/// <summary>
/// درخواست لغو دسته‌ای
/// </summary>
public record BatchCancelRequest(List<long> Ids, string? Reason);

/// <summary>
/// درخواست جستجوی پیشرفته نوبت‌ها — فرم ۹
/// </summary>
public record AppointmentSearchFilter(
    string? FromDateShamsi,
    string? ToDateShamsi,
    long? DoctorId,
    int? Status,
    int? Type,
    int? Source,
    string? Search,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// ردیف نتیجه جستجوی نوبت‌ها — با موبایل و تخصص برای ۱۲ ستون فرم ۹
/// </summary>
public record AppointmentSearchItemDto(
    long Id,
    string? AppointmentCode,
    string PatientName,
    string? PatientMobile,
    string DoctorName,
    string? SpecialtyName,
    string DateShamsi,
    string Time,
    int Status,
    int Type,
    int Source,
    decimal EstimatedFee);

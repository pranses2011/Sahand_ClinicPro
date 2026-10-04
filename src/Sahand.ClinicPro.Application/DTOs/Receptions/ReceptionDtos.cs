using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Receptions;

/// <summary>ردیف فهرست پذیرش روز</summary>
public record ReceptionListDto(
    long Id,
    string? ReceptionNumber,
    string PatientName,
    string DoctorName,
    string TimeShamsi,
    ReceptionStatus Status,
    string? InsuranceName,
    int? WaitingTime);

/// <summary>درخواست ثبت پذیرش</summary>
public record CreateReceptionDto(
    long PatientId,
    long DoctorId,
    long? AppointmentId,
    long? InsuranceCompanyId,
    string? ReferralCode,
    string? Notes);

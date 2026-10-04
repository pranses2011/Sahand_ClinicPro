using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Insurance;

/// <summary>ردیف فهرست مطالبات</summary>
public record ClaimListDto(
    long Id,
    string? ClaimNumber,
    string InsuranceName,
    string? PeriodStartShamsi,
    string? PeriodEndShamsi,
    decimal TotalAmount,
    decimal ApprovedAmount,
    decimal ReceivedAmount,
    ClaimStatus Status);

/// <summary>درخواست ثبت مطالبه دوره‌ای</summary>
public record SubmitClaimDto(
    long InsuranceCompanyId,
    string? PeriodStartShamsi,
    string? PeriodEndShamsi);

/// <summary>درخواست استعلام بیمه</summary>
public record InsuranceInquiryDto(
    long PatientId,
    long InsuranceCompanyId,
    string? InsuranceNumber);

using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Accounting;

/// <summary>سرفصل حساب در نمایش درختی</summary>
public record AccountNodeDto(
    long Id,
    string AccountCode,
    string AccountName,
    AccountType Type,
    AccountLevel Level,
    bool IsDetail,
    decimal CurrentBalance,
    List<AccountNodeDto> Children);

/// <summary>قلم سند حسابداری برای ثبت</summary>
public record JournalLineDto(long AccountId, decimal Debit, decimal Credit, string? Description);

/// <summary>درخواست ثبت سند دستی</summary>
public record CreateJournalEntryDto(
    string? Description,
    List<JournalLineDto> Lines,
    JournalReferenceType ReferenceType = JournalReferenceType.Manual,
    long? ReferenceId = null);

/// <summary>ردیف فهرست اسناد</summary>
public record JournalEntryListDto(
    long Id,
    string? EntryNumber,
    string DateShamsi,
    string? Description,
    decimal TotalDebit,
    decimal TotalCredit,
    JournalEntryStatus Status);

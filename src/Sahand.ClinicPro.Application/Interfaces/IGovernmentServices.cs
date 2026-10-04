namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>نتیجه فراخوانی سامانه دولتی</summary>
public record GovCallResult(bool Success, string? ReferenceId, string? Error);

/// <summary>
/// سرویس سپاس — ثبت ویزیت در سامانه پوشش سلامت همگا
/// </summary>
public interface ISepasService
{
    /// <summary>ثبت ویزیت (Encounter) در سپاس</summary>
    Task<GovCallResult> RegisterEncounterAsync(long visitId, CancellationToken ct = default);
}

/// <summary>
/// سرویس نسخه الکترونیک — ارسال به سامانه سلامت یا تأمین اجتماعی
/// </summary>
public interface IElectronicPrescriptionService
{
    /// <summary>ارسال نسخه به سامانه بیمه — خروجی کد رهگیری</summary>
    Task<GovCallResult> SendAsync(long prescriptionId, CancellationToken ct = default);
}

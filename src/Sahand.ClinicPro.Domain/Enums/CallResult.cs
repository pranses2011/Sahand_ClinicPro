namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نتیجه تماس
/// </summary>
public enum CallResult : byte
{
    /// <summary>پاسخ داده شد</summary>
    Answered = 1,
    /// <summary>پاسخ نداد</summary>
    NoAnswer = 2,
    /// <summary>مشغول</summary>
    Busy = 3,
    /// <summary>بازگشت تماس</summary>
    Callback = 4,
}

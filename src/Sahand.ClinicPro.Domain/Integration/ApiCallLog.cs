namespace Sahand.ClinicPro.Domain.Entities.Integration;

/// <summary>
/// لاگ فراخوانی API - تمام تماس‌های خارجی با پایش زمان
/// </summary>
public class ApiCallLog : AuditableEntity
{
    /// <summary>نام سرویس مثل سپاس</summary>
    public string? ServiceName { get; set; }

    /// <summary>نشانی فراخوانی‌شده</summary>
    public string? Endpoint { get; set; }

    /// <summary>فعل HTTP</summary>
    public string? Method { get; set; }

    /// <summary>بدنه درخواست</summary>
    public string? RequestBody { get; set; }

    /// <summary>بدنه پاسخ</summary>
    public string? ResponseBody { get; set; }

    /// <summary>کد وضعیت HTTP</summary>
    public int? StatusCode { get; set; }

    /// <summary>مدت به میلی‌ثانیه</summary>
    public int? Duration { get; set; }

    /// <summary>زمان فراخوانی</summary>
    public DateTime? CalledAt { get; set; }

    /// <summary>موفق</summary>
    public bool IsSuccess { get; set; } = false;

    /// <summary>شناسه کاربر آغازگر</summary>
    public long? UserId { get; set; }
}

namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// سرویس کاربر فعلی — شناسه و نام کاربر احراز هویت‌شده جاری را فراهم می‌کند
/// برای پر کردن خودکار فیلدهای حسابرسی استفاده می‌شود
/// </summary>
public interface ICurrentUserService
{
    /// <summary>شناسه کاربر فعلی — تهی یعنی کاربر احراز هویت نشده</summary>
    long? UserId { get; }

    /// <summary>نام کاربری فعلی</summary>
    string? Username { get; }

    /// <summary>آیا کاربر احراز هویت شده است</summary>
    bool IsAuthenticated { get; }

    /// <summary>نشانی IP درخواست جاری</summary>
    string? IpAddress { get; }

    /// <summary>شناسه شعبه جاری از هدر X-Branch-Id — گام U17 چندشعبه‌ای</summary>
    long? BranchId { get; }
}

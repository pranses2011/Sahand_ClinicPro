using System;

namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// اینترفیس حذف نرم — هیچ رکوردی واقعاً حذف نمی‌شود
/// Global Query Filter به‌صورت خودکار رکوردهای حذف‌شده را پنهان می‌کند
/// </summary>
public interface ISoftDeletable
{
    /// <summary>آیا رکورد حذف (نرم) شده است</summary>
    bool IsDeleted { get; set; }

    /// <summary>تاریخ حذف نرم</summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>شناسه کاربر حذف‌کننده</summary>
    public long? DeletedBy { get; set; }
}

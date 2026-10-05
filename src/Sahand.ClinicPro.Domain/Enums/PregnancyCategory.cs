namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// رده بارداری دارو (استاندارد FDA) — خطر مصرف در دوران بارداری
/// </summary>
public enum PregnancyCategory : byte
{
    /// <summary>تایید شده بی‌خطر — مطالعات انسانی کافی</summary>
    A = 1,

    /// <summary>احتمالاً بی‌خطر — مطالعه حیوانی بی‌خطر، مطالعه انسانی ناکافی</summary>
    B = 2,

    /// <summary>خطر قابل توجه — مطالعه حیوانی عارضه دیده، داده انسانی ناکافی</summary>
    C = 3,

    /// <summary>شواهد خطر انسانی — فقط در موارد ضروری با نظارت</summary>
    D = 4,

    /// <summary>منع مصرف در بارداری — خطر قطعی جنینی</summary>
    X = 5,

    /// <summary>رده‌بندی نشده</summary>
    NotRated = 0
}

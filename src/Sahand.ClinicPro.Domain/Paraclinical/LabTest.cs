namespace Sahand.ClinicPro.Domain.Entities.Paraclinical;

/// <summary>
/// آزمایش - کاتالوگ تست‌های آزمایشگاهی
/// </summary>
public class LabTest : AuditableEntity, ISoftDeletable
{
    /// <summary>کد تست - یکتا</summary>
    public string? TestCode { get; set; }

    /// <summary>نام آزمایش</summary>
    public string TestName { get; set; } = null!;

    /// <summary>نام انگلیسی</summary>
    public string? TestNameEn { get; set; }

    /// <summary>دسته آزمایش مثل بیوشیمی</summary>
    public string? Category { get; set; }

    /// <summary>نوع نمونه مثل خون یا ادرار</summary>
    public string? SampleType { get; set; }

    /// <summary>نوع ظرف نمونه</summary>
    public string? ContainerType { get; set; }

    /// <summary>حد طبیعی آقایان</summary>
    public string? NormalRangeMale { get; set; }

    /// <summary>حد طبیعی خانم‌ها</summary>
    public string? NormalRangeFemale { get; set; }

    // ═══ چرخهٔ ۶۹ (فاز ۳ / F-28+F-29 نقشه راه ai-review) — بازه‌های مرجع ساخت‌یافته ═══
    // «محدوده مرجع سنی/جنسیتی + مقادیر بحرانی»: ستون‌های رشته‌ای قدیمی
    // (NormalRangeMale/Female) برای نمایش عمومی می‌مانند؛ این ستون‌های عددی
    // موتور تفسیر خودکار (LabResultInterpreter) را تغذیه می‌کنند — فلگ H/L/HH/LL
    // و رنگ ردیف بر اساس «مشخصات بیمار» (سن/جنسیت) محاسبه می‌شود.
    // همه تهی‌پذیرند: تست بدون بازهٔ ثبت‌شده تفسیر نمی‌شود (نه طبیعی نه غیرطبیعی).

    /// <summary>کف بازهٔ مرجع بزرگسال مرد (مثل ۱۳ برای هموگلوبین مردان)</summary>
    public decimal? RefMinAdultMale { get; set; }

    /// <summary>سقف بازهٔ مرجع بزرگسال مرد (مثل ۱۷)</summary>
    public decimal? RefMaxAdultMale { get; set; }

    /// <summary>کف بازهٔ مرجع بزرگسال زن</summary>
    public decimal? RefMinAdultFemale { get; set; }

    /// <summary>سقف بازهٔ مرجع بزرگسال زن</summary>
    public decimal? RefMaxAdultFemale { get; set; }

    /// <summary>کف بازهٔ مرجع کودک — پیش از این سن اعمال می‌شود</summary>
    public decimal? RefMinChild { get; set; }

    /// <summary>سقف بازهٔ مرجع کودک</summary>
    public decimal? RefMaxChild { get; set; }

    /// <summary>
    /// سقف سن کودک به سال (پیش‌فرض ۱۲) — بیمار جوان‌تر از این، بازهٔ کودک
    /// می‌گیرد؛ تهی یعنی بازهٔ کودک برای این تست کاربرد ندارد.
    /// </summary>
    public int? ChildAgeMaxYears { get; set; }

    /// <summary>
    /// کف مقدار بحرانی (Critical Low) — نتیجهٔ برابر یا کمتر، هشدار فوری
    /// پزشک با دیالوگ مسدودکننده می‌سازد (F-29).
    /// </summary>
    public decimal? CriticalLow { get; set; }

    /// <summary>
    /// سقف مقدار بحرانی (Critical High) — نتیجهٔ برابر یا بیشتر، هشدار فوری
    /// پزشک با دیالوگ مسدودکننده می‌سازد (F-29).
    /// </summary>
    public decimal? CriticalHigh { get; set; }

    /// <summary>واحد سنجش</summary>
    public string? Unit { get; set; }

    /// <summary>قیمت</summary>
    public decimal Price { get; set; } = 0m;

    /// <summary>زمان جواب به ساعت</summary>
    public int? TurnAroundTime { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>اقلام درخواست با این تست</summary>
    public ICollection<LabOrderItem> OrderItems { get; set; } = new List<LabOrderItem>();
}

using Microsoft.EntityFrameworkCore;

namespace Sahand.ClinicPro.Infrastructure.Persistence;

/// <summary>
/// کانتکست فقط-خواندنی گزارش‌ها — چرخهٔ ۶۴ نقشهٔ راه (گام اول A-01: تقسیم DbContext).
/// </summary>
/// <remarks>
/// چرا جدا از ClinicProDbContext؟ گزارش‌ها سنگین‌ترین پرس‌وجوهای برنامه‌اند و
/// تا این چرخه از همان کانتکستِ نوشتنی عبور می‌کردند؛ تقسیم، سه سود دارد:
///   ۱) مدلِ گزارش از صف ChangeTracker نوشتن‌ها جدا می‌شود — هیچ ردیابی،
///      هیچ رویداد دامنه، هیچ تداخل با تراکنش‌های ثبت/پرداخت؛
///   ۲) رفت‌وبرگشت‌های گزارش با «استخر کانتکست» (AddDbContextPool) سرو
///      می‌شوند — بدون هزینهٔ ساخت مدل/اتصال در هر درخواست؛
///   ۳) گارد فقط-خواندنی: هر SaveChanges روی این کانتکست استثنا می‌دهد —
///      اگر روزی گزارشی بخواهد «چیزی بنویسد»، در بازبینی همان لحظه روشن
///      می‌شود نه اینکه بی‌صدا رفتار گزارش را تغییر دهد.
/// مدل: دقیقاً همان مدل مشترک (SharedModel.Build) — روابط چندناوبری، سکانس‌ها،
/// فیلتر سراسری حذف نرم/شعبه و ایندکس‌های فیلترشده همه یکسان اعمال می‌شوند؛
/// هیچ مایگریشن جدیدی لازم نیست (مایگریشن‌ها به ClinicProDbContext تعلق دارند).
/// NoTracking پیش‌فرض در ثبت DI تنظیم می‌شود (QueryTrackingBehavior.NoTracking)
/// تا پرس‌وجوهایی که AsNoTracking فراموش کنند هم امن بمانند.
/// کلاس «بسته» نیست: تست‌ها می‌توانند برای موتورهای دیگر (SQLite) از آن
/// مشتق شوند و همان اصلاحات موتور را اعمال کنند — الگوی SqliteTestDbContext اصلی.
/// </remarks>
public class ReportingDbContext : DbContext
{
    /// <summary>
    /// آیا موتور درون‌حافظه‌ای است (تست)؟ — همان تشخیص کانتکست اصلی؛
    /// در حالت درون‌حافظه‌ای سکانس‌ها و ایندکس‌های فیلترشدهٔ SQL خاموش می‌شوند.
    /// </summary>
    private readonly bool _isInMemory;

    /// <summary>
    /// شعبهٔ جاری برای فیلتر سراسری شعبه‌ای — همان قرارداد کانتکست اصلی.
    /// مقدار تهی یعنی «همهٔ شعبه‌ها»؛ گزارش‌های چندشعبه‌ای فعلاً همین رفتار
    /// را می‌خواهند و در آینده (فعال‌سازی چندشعبه) از همان مسیرِ تنظیم
    /// کانتکست اصلی هم‌زمان مقدار می‌گیرد.
    /// </summary>
    public long? CurrentBranchId { get; set; }

    /// <summary>سازنده با گزینه‌های اختصاصی این کانتکست</summary>
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options)
    {
        _isInMemory = options.Extensions.Any(e =>
            string.Equals(e.GetType().Name, "InMemoryOptionsExtension", StringComparison.Ordinal));
    }

    /// <summary>
    /// همان مدل مشترک — بدون هیچ اختلافی با کانتکست اصلی؛
    /// فیلتر شعبه از نمونهٔ همین کانتکست خوانده می‌شود (CurrentBranchId).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // همان پیکربندی‌های موجودیت از اسمبلی — دقیقاً به ترتیب کانتکست اصلی:
        // پیکربندی‌ها → مدل مشترک (روابط/سکانس/فیلترها/ایندکس‌ها)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicProDbContext).Assembly);
        SharedModel.Build(modelBuilder, _isInMemory, this);
    }

    /// <summary>
    /// گارد فقط-خواندنی — ذخیره‌سازی روی کانتکست گزارش ممنوع است.
    /// پیام فارسی تا در لاگ سرور و خطای API بلافاصله قابل فهم باشد.
    /// </summary>
    private static InvalidOperationException ReadOnlyViolation()
        => new("کانتکست گزارش‌ها فقط-خواندنی است — نوشتن باید از کانتکست اصلی (ClinicProDbContext) انجام شود");

    /// <inheritdoc />
    public override int SaveChanges()
        => throw ReadOnlyViolation();

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
        => throw ReadOnlyViolation();

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => throw ReadOnlyViolation();

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
        => throw ReadOnlyViolation();
}

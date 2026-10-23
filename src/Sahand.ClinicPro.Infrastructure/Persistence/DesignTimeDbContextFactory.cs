using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sahand.ClinicPro.Infrastructure.Persistence;

/// <summary>
/// کارخانهٔ زمان‌طراحی برای ابزارهای EF Core (dotnet-ef) — چرخه ۲۵
/// </summary>
/// <remarks>
/// «dotnet ef migrations add / has-pending-model-changes» برای ساخت DbContext
/// به یک میزبان قابل‌اجرا نیاز دارد؛ این کارخانهٔ سبک آن نیاز را حذف می‌کند
/// تا دستورات EF مستقیماً روی پروژهٔ زیرساخت اجرا شوند، بی‌آنکه کل API
/// بالا بیاید یا اتصال دیتابیسی برقرار شود (رشتهٔ اتصال فقط برای ساخت
/// مدل است و هرگز باز نمی‌شود). چرخه ۲۵ برای همگام‌سازی ModelSnapshot با
/// مدل جاری پس از افشودن DbContextAttribute به مهاجرت‌های دست‌نویس لازم شد.
/// </remarks>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ClinicProDbContext>
{
    /// <summary>
    /// ساخت کانتکست با provider سرور SQL و رشتهٔ اتصال صوری —
    /// بدون دیتابیس واقعی؛ فقط برای ساخت مدل در زمان طراحی.
    /// </summary>
    public ClinicProDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ClinicProDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.;Database=ClinicProDesignTime;Trusted_Connection=True;TrustServerCertificate=True");

        // همان تولیدگر idempotent توضیحاتِ جدول/ستون که در زمان اجرا ثبت می‌شود؛
        // «dotnet ef migrations script» هم باید اسکریپتی بسازد که اجرای بعدی‌اش
        // با خطای ۱۵۲۳۳ (توضیح تکراری) نشکند.
        optionsBuilder.ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator,
            Migrations.ClinicProMigrationsSqlGenerator>();

        return new ClinicProDbContext(optionsBuilder.Options);
    }
}

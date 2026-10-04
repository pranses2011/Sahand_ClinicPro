using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Licensing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول LicenseCheckLog — لاگ اعتبارسنجی لایسنس - هر بررسی روزانه ثبت می‌شود</summary>
public class LicenseCheckLogConfiguration : IEntityTypeConfiguration<LicenseCheckLog>
{
    public void Configure(EntityTypeBuilder<LicenseCheckLog> builder)
    {
        builder.ToTable("LicenseCheckLog", t => t.HasComment("لاگ اعتبارسنجی لایسنس - هر بررسی روزانه ثبت می‌شود"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Result).HasConversion<byte>().HasComment("نتیجه");

    }
}

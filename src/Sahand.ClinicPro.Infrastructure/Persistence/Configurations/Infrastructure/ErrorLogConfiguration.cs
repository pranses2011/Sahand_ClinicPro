using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ErrorLogs — لاگ خطای سیستم - برای پایش سلامت نرم‌افزار</summary>
public class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<ErrorLog> builder)
    {
        builder.ToTable("ErrorLogs", t => t.HasComment("لاگ خطای سیستم - برای پایش سلامت نرم‌افزار"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Level).HasConversion<byte>().HasComment("سطح خطا");
        builder.Property(x => x.Source).HasMaxLength(200).HasComment("محل وقوع خطا");
        builder.Property(x => x.RequestUrl).HasMaxLength(200).HasComment("نشانی درخواست");

    }
}

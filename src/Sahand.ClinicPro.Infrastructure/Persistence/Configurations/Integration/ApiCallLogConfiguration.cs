using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Integration;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ApiCallLogs — لاگ فراخوانی API - تمام تماس‌های خارجی با پایش زمان</summary>
public class ApiCallLogConfiguration : IEntityTypeConfiguration<ApiCallLog>
{
    public void Configure(EntityTypeBuilder<ApiCallLog> builder)
    {
        builder.ToTable("ApiCallLogs", t => t.HasComment("لاگ فراخوانی API - تمام تماس‌های خارجی با پایش زمان"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceName).HasMaxLength(50).HasComment("نام سرویس مثل سپاس");
        builder.Property(x => x.Endpoint).HasMaxLength(200).HasComment("نشانی فراخوانی‌شده");
        builder.Property(x => x.Method).HasMaxLength(10).HasComment("فعل HTTP");

    }
}

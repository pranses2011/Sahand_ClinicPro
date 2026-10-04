using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول SystemSettings — تنظیمات سیستم - کلید و مقدار با پشتیبانی رمزنگاری</summary>
public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings", t => t.HasComment("تنظیمات سیستم - کلید و مقدار با پشتیبانی رمزنگاری"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Category).HasMaxLength(200).HasComment("دسته تنظیم مثل عمومی یا مالی");
        builder.Property(x => x.Key).HasMaxLength(200).IsRequired().HasComment("کلید تنظیم - یکتا");
        builder.Property(x => x.DataType).HasConversion<byte>().HasComment("نوع داده");

        builder.HasIndex(x => new { x.Key }).IsUnique().HasDatabaseName("IX_SystemSetting_Key");
    }
}

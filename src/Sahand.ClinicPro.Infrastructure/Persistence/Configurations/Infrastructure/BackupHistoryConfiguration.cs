using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول BackupHistory — تاریخچه پشتیبان‌گیری - سه‌سطحی: کامل و افزایشی و لاگ</summary>
public class BackupHistoryConfiguration : IEntityTypeConfiguration<BackupHistory>
{
    public void Configure(EntityTypeBuilder<BackupHistory> builder)
    {
        builder.ToTable("BackupHistory", t => t.HasComment("تاریخچه پشتیبان‌گیری - سه‌سطحی: کامل و افزایشی و لاگ"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BackupType).HasConversion<byte>().HasComment("نوع پشتیبان");
        builder.Property(x => x.FilePath).HasMaxLength(500).HasComment("مسیر فایل پشتیبان");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت");
        builder.Property(x => x.StorageType).HasConversion<byte>().HasComment("محل ذخیره");

    }
}

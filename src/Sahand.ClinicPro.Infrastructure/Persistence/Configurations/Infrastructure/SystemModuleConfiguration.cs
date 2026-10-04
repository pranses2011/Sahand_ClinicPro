using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Modules — ماژول سیستم - فهرست ماژول‌ها با وضعیت فعال‌سازی لایسنس</summary>
public class SystemModuleConfiguration : IEntityTypeConfiguration<SystemModule>
{
    public void Configure(EntityTypeBuilder<SystemModule> builder)
    {
        builder.ToTable("Modules", t => t.HasComment("ماژول سیستم - فهرست ماژول‌ها با وضعیت فعال‌سازی لایسنس"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired().HasComment("نام ماژول");
        builder.Property(x => x.Code).HasMaxLength(20).HasComment("کد یکتای ماژول");
        builder.Property(x => x.Category).HasMaxLength(200).HasComment("دسته ماژول");
        builder.Property(x => x.IconPath).HasMaxLength(200).HasComment("آیکون");
        builder.Property(x => x.Version).HasMaxLength(20).HasComment("نسخه ماژول");

        builder.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("IX_SystemModule_Code").HasFilter("[IsDeleted] = 0");
    }
}

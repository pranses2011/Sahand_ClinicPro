using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول AuditLogs — لاگ عملیات - ثبت تمام تغییرات داده‌ها با مقادیر قبل و بعد</summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", t => t.HasComment("لاگ عملیات - ثبت تمام تغییرات داده‌ها با مقادیر قبل و بعد"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Username).HasMaxLength(50).HasComment("نام کاربری در لحظه عملیات");
        builder.Property(x => x.ActionType).HasConversion<byte>().HasComment("نوع عملیات");
        builder.Property(x => x.EntityName).HasMaxLength(200).HasComment("نام موجودیت تغییر‌یافته");
        builder.Property(x => x.IpAddress).HasMaxLength(50).HasComment("نشانی IP");
        builder.Property(x => x.Module).HasMaxLength(200).HasComment("ماژول عملیات");
        builder.Property(x => x.Severity).HasConversion<byte>().HasComment("شدت رخداد");

        builder.HasIndex(x => new { x.UserId, x.Timestamp }).HasDatabaseName("IX_AuditLog_UserId_Timestamp");
        builder.HasIndex(x => new { x.EntityName, x.EntityId }).HasDatabaseName("IX_AuditLog_EntityName_EntityId");
    }
}

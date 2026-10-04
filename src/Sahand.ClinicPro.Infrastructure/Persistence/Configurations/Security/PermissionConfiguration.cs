using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Permissions — دسترسی - مجوز اتمی بر اساس ماژول و عملیات</summary>
public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", t => t.HasComment("دسترسی - مجوز اتمی بر اساس ماژول و عملیات"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired().HasComment("نام یکتای دسترسی مثل patients.create");
        builder.Property(x => x.Category).HasMaxLength(200).HasComment("دسته‌بندی");
        builder.Property(x => x.Module).HasMaxLength(200).HasComment("نام ماژول");
        builder.Property(x => x.Action).HasMaxLength(200).HasComment("نوع عملیات مثل view یا create");

        builder.HasIndex(x => new { x.Name }).IsUnique().HasDatabaseName("IX_Permission_Name").HasFilter("[IsDeleted] = 0");
    }
}

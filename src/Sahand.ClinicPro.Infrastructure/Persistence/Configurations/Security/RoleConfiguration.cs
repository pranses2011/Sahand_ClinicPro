using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Roles — نقش - دسته‌بندی دسترسی کاربران</summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", t => t.HasComment("نقش - دسته‌بندی دسترسی کاربران"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired().HasComment("نام نقش - یکتا");
        builder.Property(x => x.NormalizedName).HasMaxLength(200).HasComment("نام نرمال‌شده برای جستجو");

        builder.HasIndex(x => new { x.Name }).IsUnique().HasDatabaseName("IX_Role_Name").HasFilter("[IsDeleted] = 0");
    }
}

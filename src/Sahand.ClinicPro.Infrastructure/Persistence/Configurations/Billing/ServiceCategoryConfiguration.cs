using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Billing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ServiceCategories — دسته‌بندی خدمات - ساختار درختی</summary>
public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.ToTable("ServiceCategories", t => t.HasComment("دسته‌بندی خدمات - ساختار درختی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired().HasComment("نام دسته");
        builder.Property(x => x.Code).HasMaxLength(20).HasComment("کد یکتا");

        builder.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("IX_ServiceCategory_Code").HasFilter("[IsDeleted] = 0");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Departments — بخش سازمانی درمانگاه - ساختار درختی</summary>
public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments", t => t.HasComment("بخش سازمانی درمانگاه - ساختار درختی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired().HasComment("نام بخش");
        builder.Property(x => x.Code).HasMaxLength(20).HasComment("کد یکتای بخش");
        builder.Property(x => x.Phone).HasMaxLength(15).HasComment("تلفن بخش");

        builder.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("IX_Department_Code").HasFilter("[IsDeleted] = 0");
    }
}

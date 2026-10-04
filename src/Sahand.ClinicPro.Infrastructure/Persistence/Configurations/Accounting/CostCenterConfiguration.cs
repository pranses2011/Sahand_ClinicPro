using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Accounting;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول CostCenters — مرکز هزینه - تخصیص هزینه‌ها به بخش‌ها</summary>
public class CostCenterConfiguration : IEntityTypeConfiguration<CostCenter>
{
    public void Configure(EntityTypeBuilder<CostCenter> builder)
    {
        builder.ToTable("CostCenters", t => t.HasComment("مرکز هزینه - تخصیص هزینه‌ها به بخش‌ها"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired().HasComment("نام مرکز هزینه");
        builder.Property(x => x.Code).HasMaxLength(20).HasComment("کد یکتا");

    }
}

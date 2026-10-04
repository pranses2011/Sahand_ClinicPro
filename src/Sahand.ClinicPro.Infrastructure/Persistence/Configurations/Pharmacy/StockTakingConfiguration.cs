using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Pharmacy;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول StockTakings — انبارگردانی - شمارش ادواری موجودی</summary>
public class StockTakingConfiguration : IEntityTypeConfiguration<StockTaking>
{
    public void Configure(EntityTypeBuilder<StockTaking> builder)
    {
        builder.ToTable("StockTakings", t => t.HasComment("انبارگردانی - شمارش ادواری موجودی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت");

    }
}

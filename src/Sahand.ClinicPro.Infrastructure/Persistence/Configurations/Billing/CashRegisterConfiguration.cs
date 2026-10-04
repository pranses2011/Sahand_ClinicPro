using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Billing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول CashRegisters — صندوق - کارکرد روزانه صندوقدار با موجودی</summary>
public class CashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
    public void Configure(EntityTypeBuilder<CashRegister> builder)
    {
        builder.ToTable("CashRegisters", t => t.HasComment("صندوق - کارکرد روزانه صندوقدار با موجودی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RegisterName).HasMaxLength(50).IsRequired().HasComment("نام صندوق");
        builder.Property(x => x.OpeningBalance).HasPrecision(18, 0).HasComment("موجودی اولیه");
        builder.Property(x => x.ClosingBalance).HasPrecision(18, 0).HasComment("موجودی پایانی");
        builder.Property(x => x.TotalCash).HasPrecision(18, 0).HasComment("جمع نقدی");
        builder.Property(x => x.TotalCard).HasPrecision(18, 0).HasComment("جمع کارتی");
        builder.Property(x => x.TotalOnline).HasPrecision(18, 0).HasComment("جمع آنلاین");
        builder.Property(x => x.Difference).HasPrecision(18, 0).HasComment("اختلاف شمارش");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت صندوق");

        builder.HasIndex(x => new { x.RegisterName }).HasDatabaseName("IX_CashRegister_RegisterName");
    }
}

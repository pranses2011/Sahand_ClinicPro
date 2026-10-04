using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Pharmacy;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول PharmacyTransactions — تراکنش داروخانه - ورود و خروج و تحویل دارو</summary>
public class PharmacyTransactionConfiguration : IEntityTypeConfiguration<PharmacyTransaction>
{
    public void Configure(EntityTypeBuilder<PharmacyTransaction> builder)
    {
        builder.ToTable("PharmacyTransactions", t => t.HasComment("تراکنش داروخانه - ورود و خروج و تحویل دارو"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionNumber).HasMaxLength(20).HasDefaultValueSql("CONCAT('PHT-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqPharmacyTransactionNumber AS varchar(10)), 6))").HasComment("شماره تراکنش - یکتا");
        builder.Property(x => x.TransactionType).HasConversion<byte>().HasComment("نوع تراکنش");
        builder.Property(x => x.Quantity).HasPrecision(10, 2).HasComment("تعداد");
        builder.Property(x => x.UnitPrice).HasPrecision(18, 0).HasComment("قیمت واحد");
        builder.Property(x => x.TotalPrice).HasPrecision(18, 0).HasComment("قیمت کل");
        builder.Property(x => x.ReferenceType).HasConversion<byte>().HasComment("منشأ تراکنش");

        builder.HasIndex(x => new { x.TransactionNumber }).IsUnique().HasDatabaseName("IX_PharmacyTransaction_TransactionNumber").HasFilter("[IsDeleted] = 0");
    }
}

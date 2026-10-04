using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Integration;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول SepasTransactions — تراکنش سپاس - ثبت ویزیت در سامانه پوشش سلامت همگا</summary>
public class SepasTransactionConfiguration : IEntityTypeConfiguration<SepasTransaction>
{
    public void Configure(EntityTypeBuilder<SepasTransaction> builder)
    {
        builder.ToTable("SepasTransactions", t => t.HasComment("تراکنش سپاس - ثبت ویزیت در سامانه پوشش سلامت همگا"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionType).HasConversion<byte>().HasComment("نوع تراکنش");
        builder.Property(x => x.SepasReferenceId).HasMaxLength(50).HasComment("شناسه ارجاع سپاس");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت همگام‌سازی");

        builder.HasIndex(x => new { x.SepasReferenceId }).HasDatabaseName("IX_SepasTransaction_SepasReferenceId");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Accounting;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول JournalEntryLines — سطر سند حسابداری - یک حساب با بدهکار یا بستانکار</summary>
public class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
{
    public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
    {
        builder.ToTable("JournalEntryLines", t => t.HasComment("سطر سند حسابداری - یک حساب با بدهکار یا بستانکار"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Debit).HasPrecision(18, 0).HasComment("مبلغ بدهکار");
        builder.Property(x => x.Credit).HasPrecision(18, 0).HasComment("مبلغ بستانکار");

    }
}

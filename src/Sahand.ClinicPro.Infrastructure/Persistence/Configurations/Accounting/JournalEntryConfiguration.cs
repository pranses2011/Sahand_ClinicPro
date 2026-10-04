using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Accounting;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول JournalEntries — سند حسابداری دوبل - خلاصه تراکنش مالی</summary>
public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries", t => t.HasComment("سند حسابداری دوبل - خلاصه تراکنش مالی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntryNumber).HasMaxLength(20).HasDefaultValueSql("CONCAT('JE-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqJournalEntryNumber AS varchar(10)), 6))").HasComment("شماره سند - یکتا");
        builder.Property(x => x.EntryDateShamsi).HasMaxLength(200).HasComment("تاریخ شمسی سند");
        builder.Property(x => x.ReferenceType).HasConversion<byte>().HasComment("منشأ سند - فاکتور یا پرداخت یا دستی");
        builder.Property(x => x.TotalDebit).HasPrecision(18, 0).HasComment("جمع بدهکار");
        builder.Property(x => x.TotalCredit).HasPrecision(18, 0).HasComment("جمع بستانکار");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت سند");

        builder.HasIndex(x => new { x.EntryNumber }).IsUnique().HasDatabaseName("IX_JournalEntry_EntryNumber").HasFilter("[IsDeleted] = 0");
    }
}

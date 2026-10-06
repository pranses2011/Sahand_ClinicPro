using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations.Infrastructure;

/// <summary>پیکربندی EF Core برای جدول ArchiveRecord — آرشیو سالانه چرخه سیزدهم (درخواست ۷-۷)</summary>
public class ArchiveRecordConfiguration : IEntityTypeConfiguration<ArchiveRecord>
{
    /// <summary>پیکربندی جدول و ایندکس‌های جستجو</summary>
    public void Configure(EntityTypeBuilder<ArchiveRecord> builder)
    {
        builder.ToTable("ArchiveRecords", t => t.HasComment("رکورد آرشیو سالانه — تصویر JSON ردیف‌های قدیمی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceTable).HasMaxLength(50).HasComment("نام جدول مبدأ");
        builder.Property(x => x.PatientName).HasMaxLength(200).HasComment("نام بیمار هنگام آرشیو");
        builder.Property(x => x.SummaryLine).HasMaxLength(300).HasComment("خلاصه‌سطر نمایشی");
        builder.Property(x => x.Payload).HasComment("تصویر کامل ردیف JSON");

        // جستجوی پرونده قدیمی بیمار + پیمایش بازه‌ای
        builder.HasIndex(x => new { x.PatientId, x.SourceTable });
        builder.HasIndex(x => new { x.SourceTable, x.SourceId }).IsUnique();
        builder.HasIndex(x => x.RecordDate);
        builder.HasIndex(x => x.YearJalali);
    }
}

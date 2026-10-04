using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول DentalRecords — رکورد دندانپزشکی - وضعیت هر دندان با شماره‌گذاری FDI</summary>
public class DentalRecordConfiguration : IEntityTypeConfiguration<DentalRecord>
{
    public void Configure(EntityTypeBuilder<DentalRecord> builder)
    {
        builder.ToTable("DentalRecords", t => t.HasComment("رکورد دندانپزشکی - وضعیت هر دندان با شماره‌گذاری FDI"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Surface).HasMaxLength(20).HasComment("سطح دندان مثل M یا O");
        builder.Property(x => x.Condition).HasConversion<byte>().HasComment("وضعیت دندان");
        builder.Property(x => x.MaterialUsed).HasMaxLength(100).HasComment("مواد مصرفی");
        builder.Property(x => x.Price).HasPrecision(18, 0).HasComment("هزینه");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت");

        builder.HasIndex(x => new { x.PatientId, x.ToothNumber }).HasDatabaseName("IX_DentalRecord_PatientId_ToothNumber");
    }
}

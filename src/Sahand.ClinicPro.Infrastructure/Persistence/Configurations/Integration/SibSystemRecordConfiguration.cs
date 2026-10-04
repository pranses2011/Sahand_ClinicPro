using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Integration;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول SibSystemRecords — رکورد سیب - همگام‌سازی با سامانه سطح یک بهداشت</summary>
public class SibSystemRecordConfiguration : IEntityTypeConfiguration<SibSystemRecord>
{
    public void Configure(EntityTypeBuilder<SibSystemRecord> builder)
    {
        builder.ToTable("SibSystemRecords", t => t.HasComment("رکورد سیب - همگام‌سازی با سامانه سطح یک بهداشت"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SibRecordId).HasMaxLength(50).HasComment("شناسه رکورد در سیب");
        builder.Property(x => x.ServiceType).HasConversion<byte>().HasComment("نوع خدمت");
        builder.Property(x => x.ReferralType).HasConversion<byte>().HasComment("نوع ارجاع");
        builder.Property(x => x.SyncStatus).HasConversion<byte>().HasComment("وضعیت همگام‌سازی");

    }
}

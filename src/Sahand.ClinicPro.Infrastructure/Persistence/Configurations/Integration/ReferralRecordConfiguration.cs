using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Integration;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ReferralSystem — ارجاع - داخلی یا بین‌مرکزی با کد ارجاع</summary>
public class ReferralRecordConfiguration : IEntityTypeConfiguration<ReferralRecord>
{
    public void Configure(EntityTypeBuilder<ReferralRecord> builder)
    {
        builder.ToTable("ReferralSystem", t => t.HasComment("ارجاع - داخلی یا بین‌مرکزی با کد ارجاع"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReferralCode).HasMaxLength(50).HasComment("کد ارجاع - یکتا");
        builder.Property(x => x.ReferralType).HasConversion<byte>().HasComment("نوع ارجاع");
        builder.Property(x => x.DestinationType).HasMaxLength(200).HasComment("نوع مقصد ارجاع");
        builder.Property(x => x.Reason).HasMaxLength(200).HasComment("دلیل ارجاع");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت ارجاع");

        builder.HasIndex(x => new { x.ReferralCode }).IsUnique().HasDatabaseName("IX_ReferralRecord_ReferralCode").HasFilter("[IsDeleted] = 0");
    }
}

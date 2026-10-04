using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Billing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول InsuranceClaims — مطالبه بیمه - جمع ثبت‌شده برای ارسال به بیمهگر</summary>
public class InsuranceClaimConfiguration : IEntityTypeConfiguration<InsuranceClaim>
{
    public void Configure(EntityTypeBuilder<InsuranceClaim> builder)
    {
        builder.ToTable("InsuranceClaims", t => t.HasComment("مطالبه بیمه - جمع ثبت‌شده برای ارسال به بیمهگر"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClaimNumber).HasMaxLength(20).HasDefaultValueSql("CONCAT('CLM-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqClaimNumber AS varchar(10)), 6))").HasComment("شماره مطالبه - یکتا");
        builder.Property(x => x.TotalAmount).HasPrecision(18, 0).HasComment("مبلغ کل مطالبه");
        builder.Property(x => x.ApprovedAmount).HasPrecision(18, 0).HasComment("مبلغ تأییدشده");
        builder.Property(x => x.RejectedAmount).HasPrecision(18, 0).HasComment("مبلغ ردشده");
        builder.Property(x => x.ReceivedAmount).HasPrecision(18, 0).HasComment("مبلغ دریافتی");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت مطالبه");

        builder.HasIndex(x => new { x.ClaimNumber }).IsUnique().HasDatabaseName("IX_InsuranceClaim_ClaimNumber").HasFilter("[IsDeleted] = 0");
    }
}

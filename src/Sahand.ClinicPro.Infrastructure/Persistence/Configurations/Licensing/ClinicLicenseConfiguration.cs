using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Licensing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ClinicLicense — لایسنس محلی درمانگاه - رکورد رمزنگاری‌شده با امضای RSA-2048</summary>
public class ClinicLicenseConfiguration : IEntityTypeConfiguration<ClinicLicense>
{
    public void Configure(EntityTypeBuilder<ClinicLicense> builder)
    {
        builder.ToTable("ClinicLicense", t => t.HasComment("لایسنس محلی درمانگاه - رکورد رمزنگاری‌شده با امضای RSA-2048"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LicenseKey).HasMaxLength(200).IsRequired().HasComment("کلید لایسنس");
        builder.Property(x => x.ActivationCode).HasMaxLength(20).HasComment("کد فعال‌سازی");
        builder.Property(x => x.LicenseType).HasConversion<byte>().HasComment("نوع لایسنس - اشتراکی یا دائمی");
        builder.Property(x => x.HardwareFingerprint).HasMaxLength(200).HasComment("اثر انگشت سخت‌افزاری - SHA-256 از شش مؤلفه");

    }
}

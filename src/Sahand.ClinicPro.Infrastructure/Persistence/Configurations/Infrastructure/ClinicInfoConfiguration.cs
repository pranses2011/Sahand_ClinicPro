using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ClinicInfo — اطلاعات درمانگاه - مشخصات هویتی برای سربرگ چاپ</summary>
public class ClinicInfoConfiguration : IEntityTypeConfiguration<ClinicInfo>
{
    public void Configure(EntityTypeBuilder<ClinicInfo> builder)
    {
        builder.ToTable("ClinicInfo", t => t.HasComment("اطلاعات درمانگاه - مشخصات هویتی برای سربرگ چاپ"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClinicName).HasMaxLength(200).IsRequired().HasComment("نام درمانگاه");
        builder.Property(x => x.ClinicNameEn).HasMaxLength(200).HasComment("نام انگلیسی");
        builder.Property(x => x.LogoPath).HasMaxLength(500).HasComment("مسیر لوگو");
        builder.Property(x => x.LicenseNumber).HasMaxLength(20).HasComment("شماره مجوز");
        builder.Property(x => x.EconomicCode).HasMaxLength(20).HasComment("کد اقتصادی");
        builder.Property(x => x.NationalId).HasMaxLength(200).HasComment("شناسه ملی");
        builder.Property(x => x.Address).HasMaxLength(500).HasComment("نشانی");
        builder.Property(x => x.Phone).HasMaxLength(15).HasComment("تلفن");
        builder.Property(x => x.Fax).HasMaxLength(15).HasComment("فکس");
        builder.Property(x => x.Email).HasMaxLength(100).HasComment("ایمیل");
        builder.Property(x => x.Website).HasMaxLength(100).HasComment("وب‌سایت");
        builder.Property(x => x.ManagerName).HasMaxLength(100).HasComment("نام مدیر");

    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Billing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول InsuranceCompanies — شرکت بیمه - اطلاعات قرارداد و اتصال API</summary>
public class InsuranceCompanyConfiguration : IEntityTypeConfiguration<InsuranceCompany>
{
    public void Configure(EntityTypeBuilder<InsuranceCompany> builder)
    {
        builder.ToTable("InsuranceCompanies", t => t.HasComment("شرکت بیمه - اطلاعات قرارداد و اتصال API"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired().HasComment("نام شرکت بیمه");
        builder.Property(x => x.Code).HasMaxLength(20).HasComment("کد یکتای بیمه");
        builder.Property(x => x.Type).HasConversion<byte>().HasComment("نوع بیمه");
        builder.Property(x => x.LogoPath).HasMaxLength(500).HasComment("مسیر لوگو");
        builder.Property(x => x.ContractNumber).HasMaxLength(20).HasComment("شماره قرارداد");
        builder.Property(x => x.DefaultPatientShare).HasPrecision(5, 2).HasComment("سهم پیش‌فرض بیمار");
        builder.Property(x => x.DefaultInsuranceShare).HasPrecision(5, 2).HasComment("سهم پیش‌فرض بیمه");
        // ═══ چرخه سی‌ویکم (گزارش مالک) — رفع هشدار EF «No store type was specified for
        // the decimal property 'DefaultCoveragePercent'»: بدون دقت صریح، مقادیر
        // خارج از decimal(18,2) پیش‌فرض به‌صورت خاموش کوتاه می‌شدند. درصد پوشش
        // ۰ تا ۱۰۰ با دو رقم اعشار → decimal(5,2) هم‌خانوادهٔ ستون‌های درصدی دیگر
        builder.Property(x => x.DefaultCoveragePercent).HasPrecision(5, 2).HasComment("درصد پوشش پیش‌فرض بیمه");
        builder.Property(x => x.ApiEndpoint).HasMaxLength(200).HasComment("نشانی API استعلام");
        builder.Property(x => x.ApiUsername).HasMaxLength(200).HasComment("نام کاربری API");
        builder.Property(x => x.ApiToken).HasMaxLength(200).HasComment("توکن پایدار API");
        builder.Property(x => x.ContactPhone).HasMaxLength(15).HasComment("تلفن رابط");
        builder.Property(x => x.Address).HasMaxLength(500).HasComment("نشانی");

        builder.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("IX_InsuranceCompany_Code").HasFilter("[IsDeleted] = 0");
    }
}

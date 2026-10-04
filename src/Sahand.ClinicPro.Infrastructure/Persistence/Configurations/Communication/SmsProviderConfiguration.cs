using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Communication;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول SmsProviders — پنل پیامک - تنظیمات ده پنل ایرانی با احراز رمزنگاری‌شده</summary>
public class SmsProviderConfiguration : IEntityTypeConfiguration<SmsProvider>
{
    public void Configure(EntityTypeBuilder<SmsProvider> builder)
    {
        builder.ToTable("SmsProviders", t => t.HasComment("پنل پیامک - تنظیمات ده پنل ایرانی با احراز رمزنگاری‌شده"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProviderName).HasMaxLength(50).IsRequired().HasComment("نام پنل مثل کاوه‌نگار");
        builder.Property(x => x.ProviderCode).HasMaxLength(20).HasComment("کد یکتای پنل");
        builder.Property(x => x.ApiUrl).HasMaxLength(200).HasComment("نشانی API");
        builder.Property(x => x.Username).HasMaxLength(50).HasComment("نام کاربری پنل");
        builder.Property(x => x.SenderNumber).HasMaxLength(20).HasComment("شماره فرستنده");
        builder.Property(x => x.Balance).HasPrecision(18, 0).HasComment("موجودی اعتبار");

        builder.HasIndex(x => new { x.ProviderCode }).IsUnique().HasDatabaseName("IX_SmsProvider_ProviderCode").HasFilter("[IsDeleted] = 0");
    }
}

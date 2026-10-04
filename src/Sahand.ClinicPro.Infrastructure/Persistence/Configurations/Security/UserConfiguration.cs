using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Users — کاربر سیستم - حساب ورود پرسنل به نرم‌افزار</summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", t => t.HasComment("کاربر سیستم - حساب ورود پرسنل به نرم‌افزار"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Username).HasMaxLength(50).IsRequired().HasComment("نام کاربری - یکتا");
        builder.Property(x => x.Email).HasMaxLength(100).IsRequired().HasComment("ایمیل - یکتا");
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired().HasComment("هش رمز عبور با BCrypt - ضریب ۱۲");
        builder.Property(x => x.SecurityStamp).HasMaxLength(100).HasComment("مهر امنیتی برای ابطال توکن‌ها");
        builder.Property(x => x.TwoFactorSecret).HasMaxLength(200).HasComment("کلید دومرحله‌ای");
        builder.Property(x => x.PhoneNumber).HasMaxLength(20).HasComment("موبایل کاربر");
        builder.Property(x => x.LastLoginIp).HasMaxLength(50).HasComment("IP آخرین ورود");

        builder.HasIndex(x => new { x.Username }).IsUnique().HasDatabaseName("IX_User_Username").HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.Email }).IsUnique().HasDatabaseName("IX_User_Email").HasFilter("[IsDeleted] = 0");
    }
}

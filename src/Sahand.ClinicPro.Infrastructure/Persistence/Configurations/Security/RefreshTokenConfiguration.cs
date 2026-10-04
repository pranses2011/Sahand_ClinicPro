using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول RefreshTokens — توکن تازه‌سازی - چرخه عمر توکن بدون ورود مجدد</summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", t => t.HasComment("توکن تازه‌سازی - چرخه عمر توکن بدون ورود مجدد"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token).HasMaxLength(500).IsRequired().HasComment("مقدار توکن - یکتا و هش‌شده");
        builder.Property(x => x.JwtId).HasMaxLength(100).HasComment("شناسه توکن JWT مرتبط");
        builder.Property(x => x.DeviceInfo).HasMaxLength(200).HasComment("اطلاعات دستگاه");
        builder.Property(x => x.IpAddress).HasMaxLength(50).HasComment("نشانی IP");

        builder.HasIndex(x => new { x.Token }).IsUnique().HasDatabaseName("IX_RefreshToken_Token");
    }
}

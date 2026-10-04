using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Integration;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ApiCredentials — اعتبارسنجی سرویس خارجی - رمزنگاری‌شده با AES-256</summary>
public class ApiCredentialConfiguration : IEntityTypeConfiguration<ApiCredential>
{
    public void Configure(EntityTypeBuilder<ApiCredential> builder)
    {
        builder.ToTable("ApiCredentials", t => t.HasComment("اعتبارسنجی سرویس خارجی - رمزنگاری‌شده با AES-256"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceName).HasMaxLength(50).IsRequired().HasComment("نام سرویس - یکتا");
        builder.Property(x => x.Username).HasMaxLength(50).HasComment("نام کاربری");
        builder.Property(x => x.Token).HasMaxLength(500).HasComment("توکن دسترسی");
        builder.Property(x => x.Endpoint).HasMaxLength(200).HasComment("نشانی پایه سرویس");

        builder.HasIndex(x => new { x.ServiceName }).IsUnique().HasDatabaseName("IX_ApiCredential_ServiceName");
    }
}

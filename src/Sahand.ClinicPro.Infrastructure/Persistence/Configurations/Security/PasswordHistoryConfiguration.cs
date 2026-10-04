using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول PasswordHistory — تاریخچه رمز عبور - جلوگیری از تکرار رمزهای قبلی</summary>
public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        builder.ToTable("PasswordHistory", t => t.HasComment("تاریخچه رمز عبور - جلوگیری از تکرار رمزهای قبلی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired().HasComment("هش رمز پیشین");

    }
}

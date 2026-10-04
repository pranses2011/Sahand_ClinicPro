using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول UserSessions — نشست کاربر - ردیابی دستگاه‌های متصل</summary>
public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions", t => t.HasComment("نشست کاربر - ردیابی دستگاه‌های متصل"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SessionToken).HasMaxLength(200).HasComment("توکن نشست");
        builder.Property(x => x.IpAddress).HasMaxLength(50).HasComment("نشانی IP");
        builder.Property(x => x.DeviceType).HasConversion<byte>().HasComment("نوع دستگاه");

    }
}

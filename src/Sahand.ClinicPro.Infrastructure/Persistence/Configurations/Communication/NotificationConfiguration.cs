using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Communication;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Notifications — اعلان درون‌برنامه‌ای - اعلان‌های سیستم و کسب‌وکار</summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", t => t.HasComment("اعلان درون‌برنامه‌ای - اعلان‌های سیستم و کسب‌وکار"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired().HasComment("عنوان اعلان");
        builder.Property(x => x.Type).HasConversion<byte>().HasComment("نوع اعلان");
        builder.Property(x => x.Priority).HasConversion<byte>().HasComment("اولویت");
        builder.Property(x => x.Link).HasMaxLength(200).HasComment("پیوند مقصد در صورت کلیک");

    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Communication;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول SmsQueue — صف ارسال پیامک - پردازش پس‌زمینه با Hangfire</summary>
public class SmsQueueItemConfiguration : IEntityTypeConfiguration<SmsQueueItem>
{
    public void Configure(EntityTypeBuilder<SmsQueueItem> builder)
    {
        builder.ToTable("SmsQueue", t => t.HasComment("صف ارسال پیامک - پردازش پس‌زمینه با Hangfire"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Recipient).HasMaxLength(11).IsRequired().HasComment("شماره گیرنده");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت در صف");

    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Communication;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول SmsMessages — پیامک ارسالی - رکورد کامل همراه وضعیت تحویل</summary>
public class SmsMessageConfiguration : IEntityTypeConfiguration<SmsMessage>
{
    public void Configure(EntityTypeBuilder<SmsMessage> builder)
    {
        builder.ToTable("SmsMessages", t => t.HasComment("پیامک ارسالی - رکورد کامل همراه وضعیت تحویل"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Recipient).HasMaxLength(11).IsRequired().HasComment("شماره گیرنده - ۱۱ رقم");
        builder.Property(x => x.RecipientType).HasConversion<byte>().HasComment("نوع گیرنده");
        builder.Property(x => x.Provider).HasMaxLength(200).HasComment("نام پنل ارسال‌کننده");
        builder.Property(x => x.ProviderMessageId).HasMaxLength(100).HasComment("شناسه پیام در پنل");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت ارسال");
        builder.Property(x => x.Cost).HasPrecision(10, 2).HasComment("هزینه ارسال");
        builder.Property(x => x.MessageType).HasConversion<byte>().HasComment("نوع پیام");
        builder.Property(x => x.ReferenceType).HasConversion<byte>().HasComment("منشأ پیام");

    }
}

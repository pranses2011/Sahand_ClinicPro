using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Communication;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول PhoneCalls — تماس تلفنی - ثبت تماس‌های مرتبط با بیماران</summary>
public class PhoneCallConfiguration : IEntityTypeConfiguration<PhoneCall>
{
    public void Configure(EntityTypeBuilder<PhoneCall> builder)
    {
        builder.ToTable("PhoneCalls", t => t.HasComment("تماس تلفنی - ثبت تماس‌های مرتبط با بیماران"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CallerNumber).HasMaxLength(15).HasComment("شماره تماس‌گیرنده");
        builder.Property(x => x.Direction).HasConversion<byte>().HasComment("جهت تماس");
        builder.Property(x => x.Result).HasConversion<byte>().HasComment("نتیجه تماس");
        builder.Property(x => x.RecordingPath).HasMaxLength(500).HasComment("مسیر فایل صوت");

    }
}

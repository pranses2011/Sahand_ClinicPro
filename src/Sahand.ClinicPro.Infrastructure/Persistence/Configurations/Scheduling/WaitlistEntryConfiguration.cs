using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Scheduling;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول WaitlistEntries — لیست انتظار نوبت (چرخه ۱۳، درخواست ۳-۴)</summary>
public class WaitlistEntryConfiguration : IEntityTypeConfiguration<WaitlistEntry>
{
    /// <summary>پیکربندی جدول و ایندکس‌ها</summary>
    public void Configure(EntityTypeBuilder<WaitlistEntry> builder)
    {
        builder.ToTable("WaitlistEntries",
            t => t.HasComment("لیست انتظار نوبت — بیماران منتظر جای خالی با پیشنهاد خودکار"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PreferredTimeOfDay).HasMaxLength(20).HasComment("بازه روز دلخواه — صبح/عصر/شب/هر");
        builder.Property(x => x.Priority).HasConversion<byte>().HasComment("اولویت — بالاتر اول پیشنهاد می‌شود");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت — در انتظار/اطلاع‌رسانی/تأیید/منقضی/لغو");
        builder.Property(x => x.ContactMobile).HasMaxLength(15).HasComment("شماره تماس عکس‌فوری هنگام ثبت");
        builder.Property(x => x.Notes).HasMaxLength(500).HasComment("یادداشت");

        builder.HasIndex(x => new { x.Status, x.DoctorId, x.SpecialtyId })
            .HasDatabaseName("IX_Waitlist_Status_Doctor")
            .HasFilter("[IsDeleted] = 0");
    }
}

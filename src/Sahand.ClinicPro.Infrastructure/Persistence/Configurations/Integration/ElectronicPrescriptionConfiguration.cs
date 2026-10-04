using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Integration;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ElectronicPrescriptions — نسخه الکترونیک - رکورد ارسال نسخه به سامانه سلامت یا تأمین</summary>
public class ElectronicPrescriptionConfiguration : IEntityTypeConfiguration<ElectronicPrescription>
{
    public void Configure(EntityTypeBuilder<ElectronicPrescription> builder)
    {
        builder.ToTable("ElectronicPrescriptions", t => t.HasComment("نسخه الکترونیک - رکورد ارسال نسخه به سامانه سلامت یا تأمین"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SystemType).HasConversion<byte>().HasComment("سامانه مقصد - سلامت یا تأمین");
        builder.Property(x => x.TrackingCode).HasMaxLength(50).HasComment("کد رهگیری - یکتا");
        builder.Property(x => x.RegistrationCode).HasMaxLength(50).HasComment("کد ثبت در سامانه");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت");
        builder.Property(x => x.ErrorCode).HasMaxLength(20).HasComment("کد خطای سامانه");

        builder.HasIndex(x => new { x.TrackingCode }).IsUnique().HasDatabaseName("IX_ElectronicPrescription_TrackingCode").HasFilter("[IsDeleted] = 0");
    }
}

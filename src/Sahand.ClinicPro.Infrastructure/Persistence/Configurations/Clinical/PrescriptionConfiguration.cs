using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Clinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Prescriptions — نسخه پزشکی - دارویی یا پاراکلینیک یا ترکیبی</summary>
public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions", t => t.HasComment("نسخه پزشکی - دارویی یا پاراکلینیک یا ترکیبی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PrescriptionCode).HasMaxLength(20).HasDefaultValueSql("CONCAT('RX-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqPrescriptionCode AS varchar(10)), 6))").HasComment("کد نسخه - فرمت RX-XXXXXXXX");
        builder.Property(x => x.Type).HasConversion<byte>().HasComment("نوع نسخه");
        builder.Property(x => x.ElectronicTrackingCode).HasMaxLength(20).HasComment("کد رهگیری نسخه الکترونیک");
        builder.Property(x => x.InsuranceSystemType).HasConversion<byte>().HasComment("سامانه مقصد نسخه الکترونیک");
        builder.Property(x => x.InsuranceApprovalCode).HasMaxLength(50).HasComment("کد تأیید بیمه");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت نسخه");

        builder.HasIndex(x => new { x.PrescriptionCode }).IsUnique().HasDatabaseName("IX_Prescription_PrescriptionCode").HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.PatientId }).HasDatabaseName("IX_Prescription_PatientId");
    }
}

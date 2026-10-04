using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Doctors — پزشک - اطلاعات حرفه‌ای و قراردادی پزشکان</summary>
public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors", t => t.HasComment("پزشک - اطلاعات حرفه‌ای و قراردادی پزشکان"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MedicalLicenseNo).HasMaxLength(200).HasComment("شماره پروانه پزشکی - یکتا");
        builder.Property(x => x.NezamPezeshkiCode).HasMaxLength(20).HasComment("کد نظام پزشکی - یکتا");
        builder.Property(x => x.AcademicRank).HasMaxLength(200).HasComment("مرتبه علمی - استاد یا دانشیار و غیره");
        builder.Property(x => x.ConsultationFee).HasPrecision(18, 0).HasComment("حق ویزیت به ریال");
        builder.Property(x => x.DoctorPercentage).HasPrecision(5, 2).HasComment("درصد سهم پزشک از ویزیت");
        builder.Property(x => x.Rating).HasPrecision(3, 2).HasComment("امتیاز پزشک از ۵");

        builder.HasIndex(x => new { x.NezamPezeshkiCode }).IsUnique().HasDatabaseName("IX_Doctor_NezamPezeshkiCode").HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.MedicalLicenseNo }).IsUnique().HasDatabaseName("IX_Doctor_MedicalLicenseNo").HasFilter("[IsDeleted] = 0");
    }
}

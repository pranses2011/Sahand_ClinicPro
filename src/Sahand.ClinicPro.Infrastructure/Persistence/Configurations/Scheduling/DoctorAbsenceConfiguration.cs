using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Scheduling;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول DoctorAbsences — غیبت برنامه‌ریزی‌شده پزشک - مسدودکننده اسلات‌های نوبت</summary>
public class DoctorAbsenceConfiguration : IEntityTypeConfiguration<DoctorAbsence>
{
    public void Configure(EntityTypeBuilder<DoctorAbsence> builder)
    {
        builder.ToTable("DoctorAbsences", t => t.HasComment("غیبت برنامه‌ریزی‌شده پزشک - مسدودکننده اسلات‌های نوبت"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AbsenceType).HasConversion<byte>().HasComment("نوع غیبت");
        builder.Property(x => x.Reason).HasMaxLength(200).HasComment("دلیل غیبت");

    }
}

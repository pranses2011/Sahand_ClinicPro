using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Scheduling;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول DoctorSchedules — برنامه کاری هفتگی پزشک - مبنای تولید اسلات‌های نوبت</summary>
public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("DoctorSchedules", t => t.HasComment("برنامه کاری هفتگی پزشک - مبنای تولید اسلات‌های نوبت"));
        builder.HasKey(x => x.Id);


    }
}

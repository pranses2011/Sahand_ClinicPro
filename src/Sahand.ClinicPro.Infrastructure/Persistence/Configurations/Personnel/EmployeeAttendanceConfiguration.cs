using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Personnel;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول EmployeeAttendances — حضور و غیاب پرسنل - ورود و خروج روزانه</summary>
public class EmployeeAttendanceConfiguration : IEntityTypeConfiguration<EmployeeAttendance>
{
    public void Configure(EntityTypeBuilder<EmployeeAttendance> builder)
    {
        builder.ToTable("EmployeeAttendances", t => t.HasComment("حضور و غیاب پرسنل - ورود و خروج روزانه"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت حضور");
        builder.Property(x => x.WorkedHours).HasPrecision(5, 2).HasComment("ساعات کارکرد");
        builder.Property(x => x.OvertimeHours).HasPrecision(5, 2).HasComment("ساعات اضافه‌کاری");

    }
}

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
        builder.Property(x => x.LateMinutes).HasComment("دقایق تأخیر ورود");
        builder.Property(x => x.EarlyLeaveMinutes).HasComment("دقایق تعجیل خروج");
        builder.Property(x => x.IsManuallyEdited).HasComment("پرچم ویرایش دستی");
        builder.Property(x => x.Notes).HasMaxLength(300).HasComment("یادداشت");

        // رکورد حضور یکتا به ازای هر پرسنل در هر تاریخ
        builder.HasIndex(x => new { x.EmployeeId, x.AttendanceDate })
            .IsUnique()
            .HasDatabaseName("IX_EmployeeAttendances_Employee_Date_Unique");

        builder.HasIndex(x => x.AttendanceDate).HasDatabaseName("IX_EmployeeAttendances_Date");

        builder.HasOne(x => x.SourceDevice)
            .WithMany()
            .HasForeignKey(x => x.SourceDeviceId);

    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Personnel;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول AttendanceDevices — دستگاه‌های حضور و غیاب</summary>
public class AttendanceDeviceConfiguration : IEntityTypeConfiguration<AttendanceDevice>
{
    public void Configure(EntityTypeBuilder<AttendanceDevice> builder)
    {
        builder.ToTable("AttendanceDevices", t => t.HasComment("دستگاه‌های حضور و غیاب - اتصال و همگام‌سازی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(120).IsRequired().HasComment("نام نمایشی دستگاه");
        builder.Property(x => x.Brand).HasConversion<byte>().HasComment("برند/پروتکل دستگاه");
        builder.Property(x => x.Model).HasMaxLength(60).HasComment("مدل دستگاه");
        builder.Property(x => x.SerialNumber).HasMaxLength(60).HasComment("شماره سریال");
        builder.Property(x => x.IpAddress).HasMaxLength(45).IsRequired().HasComment("نشانی IP");
        builder.Property(x => x.CommunicationKey).HasMaxLength(100).HasComment("کلید ارتباطی");
        builder.Property(x => x.Location).HasMaxLength(150).HasComment("محل نصب");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت اتصال");
        builder.Property(x => x.LastError).HasMaxLength(300).HasComment("آخرین خطا");
        builder.Property(x => x.Notes).HasMaxLength(400).HasComment("توضیحات");

        builder.HasIndex(x => x.IpAddress).HasDatabaseName("IX_AttendanceDevices_Ip");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("IX_AttendanceDevices_IsActive");

        builder.HasMany(x => x.RawLogs)
            .WithOne(x => x.Device!)
            .HasForeignKey(x => x.DeviceId);

        builder.HasMany(x => x.UserMappings)
            .WithOne(x => x.Device!)
            .HasForeignKey(x => x.DeviceId);
    }
}

/// <summary>پیکربندی EF Core برای جدول DeviceUserMappings — نگاشت کاربر دستگاه به پرسنل</summary>
public class DeviceUserMappingConfiguration : IEntityTypeConfiguration<DeviceUserMapping>
{
    public void Configure(EntityTypeBuilder<DeviceUserMapping> builder)
    {
        builder.ToTable("DeviceUserMappings", t => t.HasComment("نگاشت شناسه کاربر دستگاه به پرسنل درمانگاه"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EnrollNumber).HasMaxLength(24).IsRequired().HasComment("شناسه روی دستگاه");

        // یک کاربر دستگاه فقط یک‌بار به یک پرسنل نگاشت می‌شود
        builder.HasIndex(x => new { x.DeviceId, x.EnrollNumber })
            .IsUnique()
            .HasDatabaseName("IX_DeviceUserMappings_Device_Enroll");

        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_DeviceUserMappings_Employee");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId);
    }
}

/// <summary>پیکربندی EF Core برای جدول DeviceRawLogs — لاگ خام ضربه‌های دستگاه</summary>
public class DeviceRawLogConfiguration : IEntityTypeConfiguration<DeviceRawLog>
{
    public void Configure(EntityTypeBuilder<DeviceRawLog> builder)
    {
        builder.ToTable("DeviceRawLogs", t => t.HasComment("لاگ خام ضربه‌ها - منبع حقیقت پیش از پردازش"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EnrollNumber).HasMaxLength(24).IsRequired().HasComment("شناسه روی دستگاه");
        builder.Property(x => x.PunchState).HasConversion<byte>().HasComment("نوع ضربه");
        builder.Property(x => x.ProcessedStatus).HasConversion<byte>().HasComment("وضعیت پردازش");
        builder.Property(x => x.ErrorMessage).HasMaxLength(300).HasComment("خطای پردازش");

        // پرس‌وجوی اصلی سرویس پردازش: لاگ‌های در انتظار به ترتیب زمان
        builder.HasIndex(x => new { x.ProcessedStatus, x.PunchTime })
            .HasDatabaseName("IX_DeviceRawLogs_Status_Time");

        // جلوگیری از درج تکراری هنگام Pull مجدد دستگاه
        builder.HasIndex(x => new { x.DeviceId, x.EnrollNumber, x.PunchTime })
            .IsUnique()
            .HasDatabaseName("IX_DeviceRawLogs_Unique_Punch");
    }
}

/// <summary>پیکربندی EF Core برای جدول EmployeeShiftAssignments — تخصیص شیفت به پرسنل</summary>
public class EmployeeShiftAssignmentConfiguration : IEntityTypeConfiguration<EmployeeShiftAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeShiftAssignment> builder)
    {
        builder.ToTable("EmployeeShiftAssignments", t => t.HasComment("تخصیص شیفت به پرسنل با الگوی تکرار"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Recurrence).HasConversion<byte>().HasComment("الگوی تکرار");
        builder.Property(x => x.Notes).HasMaxLength(300).HasComment("یادداشت");

        // یافتن شیفت مؤثر هر پرسنل در یک تاریخ
        builder.HasIndex(x => new { x.EmployeeId, x.FromDate })
            .HasDatabaseName("IX_ShiftAssignments_Employee_From");

        builder.HasIndex(x => x.ShiftId).HasDatabaseName("IX_ShiftAssignments_Shift");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId);

        builder.HasOne(x => x.Shift)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.ShiftId);
    }
}

/// <summary>پیکربندی EF Core برای جدول Holidays — تقویم تعطیلات</summary>
public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays", t => t.HasComment("تقویم تعطیلات - رسمی و سازمانی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(120).IsRequired().HasComment("عنوان تعطیلی");
        builder.Property(x => x.HolidayType).HasConversion<byte>().HasComment("نوع تعطیلی");

        builder.HasIndex(x => x.Date).HasDatabaseName("IX_Holidays_Date");

        // ستون JSON پرسنل پوشش‌دهنده (On-Call)
        builder.Property(x => x.OnCallEmployeeIds)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(long.Parse).ToList(),
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<ICollection<long>>(
                    (a, b) => a!.SequenceEqual(b!),
                    c => c.Aggregate(0, (h, x) => HashCode.Combine(h, x.GetHashCode())),
                    c => c.ToList()))
            .HasMaxLength(400)
            .HasComment("شناسه‌های پرسنل پوشش‌دهنده جدا با کاما");
    }
}

/// <summary>پیکربندی EF Core برای جدول AttendanceCorrections — درخواست اصلاح کارت حضور</summary>
public class AttendanceCorrectionConfiguration : IEntityTypeConfiguration<AttendanceCorrection>
{
    public void Configure(EntityTypeBuilder<AttendanceCorrection> builder)
    {
        builder.ToTable("AttendanceCorrections", t => t.HasComment("درخواست اصلاح کارت حضور - جریان تأیید سرپرست"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FieldName).HasMaxLength(20).IsRequired().HasComment("فیلد مورد اصلاح");
        builder.Property(x => x.OldValue).HasMaxLength(10).HasComment("مقدار فعلی");
        builder.Property(x => x.NewValue).HasMaxLength(10).IsRequired().HasComment("مقدار درخواستی");
        builder.Property(x => x.Reason).HasMaxLength(300).IsRequired().HasComment("دلیل اصلاح");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت درخواست");
        builder.Property(x => x.ApproverNote).HasMaxLength(300).HasComment("ملاحظات تأییدکننده");

        builder.HasIndex(x => new { x.EmployeeId, x.AttendanceDate })
            .HasDatabaseName("IX_AttendanceCorrections_Employee_Date");

        builder.HasIndex(x => x.Status).HasDatabaseName("IX_AttendanceCorrections_Status");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId);
    }
}

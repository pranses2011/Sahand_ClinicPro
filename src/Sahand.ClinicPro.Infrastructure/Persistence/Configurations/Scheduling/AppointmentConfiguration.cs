using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Scheduling;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Appointments — نوبت - قلب ماژول نوبت‌دهی</summary>
public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments", t => t.HasComment("نوبت - قلب ماژول نوبت‌دهی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AppointmentCode).HasMaxLength(20).HasDefaultValueSql("CONCAT('APT-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqAppointmentCode AS varchar(10)), 6))").HasComment("کد پیگیری - فرمت APT-XXXXXXXX با SQL Sequence");
        builder.Property(x => x.AppointmentDateShamsi).HasMaxLength(200).HasComment("تاریخ شمسی نوبت");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت نوبت");
        builder.Property(x => x.AppointmentType).HasConversion<byte>().HasComment("نوع نوبت");
        builder.Property(x => x.Source).HasConversion<byte>().HasComment("منبع ثبت نوبت");
        builder.Property(x => x.Priority).HasConversion<byte>().HasComment("اولویت نوبت");
        builder.Property(x => x.EstimatedFee).HasPrecision(18, 0).HasComment("تخمین هزینه به ریال");
        builder.Property(x => x.CancellationReason).HasMaxLength(200).HasComment("دلیل لغو نوبت");

        builder.HasIndex(x => new { x.AppointmentCode }).IsUnique().HasDatabaseName("IX_Appointment_AppointmentCode").HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.AppointmentDate, x.DoctorId }).HasDatabaseName("IX_Appointment_AppointmentDate_DoctorId");
        builder.HasIndex(x => new { x.PatientId }).HasDatabaseName("IX_Appointment_PatientId");
        builder.HasIndex(x => new { x.Status }).HasDatabaseName("IX_Appointment_Status");
    }
}

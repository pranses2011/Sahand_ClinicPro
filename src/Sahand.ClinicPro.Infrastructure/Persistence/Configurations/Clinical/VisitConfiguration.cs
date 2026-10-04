using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Clinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Visits — ویزیت - پرونده یک جلسه معاینه با الگوی SOAP</summary>
public class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.ToTable("Visits", t => t.HasComment("ویزیت - پرونده یک جلسه معاینه با الگوی SOAP"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VisitCode).HasMaxLength(20).HasDefaultValueSql("CONCAT('VST-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqVisitCode AS varchar(10)), 6))").HasComment("کد ویزیت - فرمت VST-XXXXXXXX");
        builder.Property(x => x.VisitDateShamsi).HasMaxLength(200).HasComment("تاریخ شمسی ویزیت");
        builder.Property(x => x.VisitType).HasConversion<byte>().HasComment("نوع ویزیت");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت ویزیت");
        builder.Property(x => x.VisitFee).HasPrecision(18, 0).HasComment("هزینه ویزیت به ریال");
        builder.Property(x => x.SepasReferenceCode).HasMaxLength(20).HasComment("کد ارجاع سامانه سپاس");

        builder.HasIndex(x => new { x.VisitCode }).IsUnique().HasDatabaseName("IX_Visit_VisitCode").HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.PatientId, x.VisitDate }).HasDatabaseName("IX_Visit_PatientId_VisitDate");
        builder.HasIndex(x => new { x.DoctorId, x.VisitDate }).HasDatabaseName("IX_Visit_DoctorId_VisitDate");
    }
}

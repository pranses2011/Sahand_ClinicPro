using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول EyeExamRecords — معاینه چشم - تجویز عینک و اندازه‌های چشم</summary>
public class EyeExamRecordConfiguration : IEntityTypeConfiguration<EyeExamRecord>
{
    public void Configure(EntityTypeBuilder<EyeExamRecord> builder)
    {
        builder.ToTable("EyeExamRecords", t => t.HasComment("معاینه چشم - تجویز عینک و اندازه‌های چشم"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RightSphere).HasPrecision(4, 2).HasComment("عدسی چشم راست");
        builder.Property(x => x.RightCylinder).HasPrecision(4, 2).HasComment("استوانه راست");
        builder.Property(x => x.LeftSphere).HasPrecision(4, 2).HasComment("عدسی چپ");
        builder.Property(x => x.LeftCylinder).HasPrecision(4, 2).HasComment("استوانه چپ");
        builder.Property(x => x.RightVisualAcuity).HasMaxLength(10).HasComment("حدت بینایی راست");
        builder.Property(x => x.LeftVisualAcuity).HasMaxLength(10).HasComment("حدت بینایی چپ");
        builder.Property(x => x.RightCorrectedVisualAcuity).HasMaxLength(10).HasComment("حدت بینایی راست با اصلاح");
        builder.Property(x => x.LeftCorrectedVisualAcuity).HasMaxLength(10).HasComment("حدت بینایی چپ با اصلاح");
        builder.Property(x => x.PupilDistance).HasPrecision(4, 1).HasComment("فاصقه مردمک");
        builder.Property(x => x.ColorVision).HasMaxLength(100).HasComment("تست تشخیص رنگ");

        builder.HasIndex(x => new { x.PatientId }).HasDatabaseName("IX_EyeExamRecord_PatientId");
    }
}

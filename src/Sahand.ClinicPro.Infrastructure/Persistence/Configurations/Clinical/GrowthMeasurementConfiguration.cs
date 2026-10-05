using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Clinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول GrowthMeasurements — اندازه‌گیری رشد کودک (گام R2)</summary>
public class GrowthMeasurementConfiguration : IEntityTypeConfiguration<GrowthMeasurement>
{
    /// <summary>تعریف جدول، ایندکس‌ها و محدودیت‌ها</summary>
    public void Configure(EntityTypeBuilder<GrowthMeasurement> builder)
    {
        builder.ToTable("GrowthMeasurements", t => t.HasComment("اندازه‌گیری رشد کودک — وزن/قد/محیط سر"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.WeightKg).HasPrecision(6, 2).HasComment("وزن کیلوگرم");
        builder.Property(x => x.HeightCm).HasPrecision(6, 1).HasComment("قد سانتی‌متر");
        builder.Property(x => x.HeadCircumferenceCm).HasPrecision(6, 1).HasComment("محیط سر سانتی‌متر");
        builder.Property(x => x.Notes).HasMaxLength(500).HasComment("یادداشت شرایط اندازه‌گیری");

        // یک اندازه‌گیری در هر تاریخ برای هر بیمار — جلوگیری از ثبت تکراری
        builder.HasIndex(x => new { x.PatientId, x.MeasuredAt }).IsUnique()
            .HasDatabaseName("IX_GrowthMeasurements_Patient_Date");

        builder.HasIndex(x => x.PatientId).HasDatabaseName("IX_GrowthMeasurements_Patient");
    }
}

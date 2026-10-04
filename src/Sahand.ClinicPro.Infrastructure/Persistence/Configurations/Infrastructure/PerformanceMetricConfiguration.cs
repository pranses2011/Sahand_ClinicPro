using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول PerformanceMetrics — سنجه عملکرد - نمونه‌برداری دوره‌ای سلامت سیستم</summary>
public class PerformanceMetricConfiguration : IEntityTypeConfiguration<PerformanceMetric>
{
    public void Configure(EntityTypeBuilder<PerformanceMetric> builder)
    {
        builder.ToTable("PerformanceMetrics", t => t.HasComment("سنجه عملکرد - نمونه‌برداری دوره‌ای سلامت سیستم"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MetricType).HasMaxLength(200).HasComment("نوع سنجه مثل CPU");
        builder.Property(x => x.MetricName).HasMaxLength(200).HasComment("نام سنجه");
        builder.Property(x => x.Value).HasPrecision(18, 0).HasComment("مقدار اندازه‌گیری");
        builder.Property(x => x.Unit).HasMaxLength(20).HasComment("واحد");

    }
}

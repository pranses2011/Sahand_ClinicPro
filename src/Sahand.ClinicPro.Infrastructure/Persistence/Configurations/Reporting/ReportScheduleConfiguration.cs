using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Reporting;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ReportSchedules — زمان‌بندی گزارش - تولید خودکار دوره‌ای</summary>
public class ReportScheduleConfiguration : IEntityTypeConfiguration<ReportSchedule>
{
    public void Configure(EntityTypeBuilder<ReportSchedule> builder)
    {
        builder.ToTable("ReportSchedules", t => t.HasComment("زمان‌بندی گزارش - تولید خودکار دوره‌ای"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Frequency).HasConversion<byte>().HasComment("دوره تکرار");

    }
}

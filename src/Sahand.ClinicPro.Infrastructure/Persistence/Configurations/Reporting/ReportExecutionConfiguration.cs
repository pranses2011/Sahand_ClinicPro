using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Reporting;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ReportExecutions — اجرا گزارش - رکورد هر بار تولید گزارش</summary>
public class ReportExecutionConfiguration : IEntityTypeConfiguration<ReportExecution>
{
    public void Configure(EntityTypeBuilder<ReportExecution> builder)
    {
        builder.ToTable("ReportExecutions", t => t.HasComment("اجرا گزارش - رکورد هر بار تولید گزارش"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExportFormat).HasMaxLength(20).HasComment("قالب خروجی مثل PDF");

    }
}

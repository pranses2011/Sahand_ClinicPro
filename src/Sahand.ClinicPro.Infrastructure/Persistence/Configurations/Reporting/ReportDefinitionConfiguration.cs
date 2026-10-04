using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Reporting;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ReportDefinitions — تعریف گزارش - قالب فایل گزارش یا کوئری SQL</summary>
public class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        builder.ToTable("ReportDefinitions", t => t.HasComment("تعریف گزارش - قالب فایل گزارش یا کوئری SQL"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReportName).HasMaxLength(200).IsRequired().HasComment("نام گزارش");
        builder.Property(x => x.Code).HasMaxLength(20).HasComment("کد یکتای گزارش");
        builder.Property(x => x.Category).HasMaxLength(200).HasComment("دسته گزارش");
        builder.Property(x => x.RequiredPermission).HasMaxLength(200).HasComment("دسترسی موردنیاز اجرا");

        builder.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("IX_ReportDefinition_Code").HasFilter("[IsDeleted] = 0");
    }
}

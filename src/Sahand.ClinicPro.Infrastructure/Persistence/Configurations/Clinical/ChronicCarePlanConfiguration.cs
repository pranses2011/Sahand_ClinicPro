using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Clinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ChronicCarePlans — طرح مراقبت بیمار مزمن (گام R2)</summary>
public class ChronicCarePlanConfiguration : IEntityTypeConfiguration<ChronicCarePlan>
{
    /// <summary>تعریف جدول، ایندکس‌ها و محدودیت‌ها</summary>
    public void Configure(EntityTypeBuilder<ChronicCarePlan> builder)
    {
        builder.ToTable("ChronicCarePlans", t => t.HasComment("طرح مراقبت بیمار مزمن — چرخه هدف/اقدام/بازبینی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DiseaseTitle).HasMaxLength(200).IsRequired().HasComment("عنوان بیماری مزمن");
        builder.Property(x => x.Icd10Code).HasMaxLength(10).HasComment("کد ICD-10");
        builder.Property(x => x.Goals).HasMaxLength(1000).HasComment("اهداف درمانی قابل اندازه‌گیری");
        builder.Property(x => x.Actions).HasMaxLength(1000).HasComment("اقدامات جاری");
        builder.Property(x => x.CareTeam).HasMaxLength(300).HasComment("اعضای تیم مراقبت");
        builder.Property(x => x.Notes).HasMaxLength(500).HasComment("یادداشت آزاد");

        builder.HasIndex(x => x.PatientId).HasDatabaseName("IX_ChronicCarePlans_Patient");
        builder.HasIndex(x => new { x.Status, x.NextReviewDate }).HasDatabaseName("IX_ChronicCarePlans_Review");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Clinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول FamilyHistories — سابقه پزشکی خانواده (گام R2)</summary>
public class FamilyHistoryConfiguration : IEntityTypeConfiguration<FamilyHistory>
{
    /// <summary>تعریف جدول، ایندکس‌ها و محدودیت‌ها</summary>
    public void Configure(EntityTypeBuilder<FamilyHistory> builder)
    {
        builder.ToTable("FamilyHistories", t => t.HasComment("سابقه پزشکی خانواده — خویشاوندان بیمار"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Disease).HasMaxLength(200).IsRequired().HasComment("عنوان بیماری");
        builder.Property(x => x.Icd10Code).HasMaxLength(10).HasComment("کد ICD-10");
        builder.Property(x => x.Notes).HasMaxLength(500).HasComment("یادداشت آزاد");

        builder.HasIndex(x => x.PatientId).HasDatabaseName("IX_FamilyHistories_Patient");
        builder.HasIndex(x => x.IsGenetic).HasDatabaseName("IX_FamilyHistories_Genetic");
    }
}

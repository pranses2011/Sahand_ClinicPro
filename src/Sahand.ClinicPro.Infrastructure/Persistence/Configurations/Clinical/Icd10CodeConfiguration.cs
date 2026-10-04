using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Clinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ICD10Codes — کد تشخیص ICD-10 - طبقه‌بندی بین‌المللی بیماری‌ها</summary>
public class Icd10CodeConfiguration : IEntityTypeConfiguration<Icd10Code>
{
    public void Configure(EntityTypeBuilder<Icd10Code> builder)
    {
        builder.ToTable("ICD10Codes", t => t.HasComment("کد تشخیص ICD-10 - طبقه‌بندی بین‌المللی بیماری‌ها"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(20).IsRequired().HasComment("کد ICD-10 مثل E11 - یکتا");
        builder.Property(x => x.Description).IsRequired().HasComment("شرح انگلیسی تشخیص");
        builder.Property(x => x.DescriptionFa).HasMaxLength(200).HasComment("شرح فارسی تشخیص");
        builder.Property(x => x.ParentCode).HasMaxLength(20).HasComment("کد والد در طبقه‌بندی");
        builder.Property(x => x.Category).HasMaxLength(200).HasComment("دسته‌بندی");
        builder.Property(x => x.Chapter).HasMaxLength(200).HasComment("فصل ICD-10");
        builder.Property(x => x.Block).HasMaxLength(200).HasComment("بلوک ICD-10");

        builder.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("IX_Icd10Code_Code");
    }
}

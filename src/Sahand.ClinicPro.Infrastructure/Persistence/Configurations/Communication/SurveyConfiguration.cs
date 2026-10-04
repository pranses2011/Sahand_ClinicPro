using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Communication;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Surveys — نظرسنجی - پرسشنامه رضایت بیماران</summary>
public class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> builder)
    {
        builder.ToTable("Surveys", t => t.HasComment("نظرسنجی - پرسشنامه رضایت بیماران"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired().HasComment("عنوان نظرسنجی");
        builder.Property(x => x.SurveyType).HasConversion<byte>().HasComment("نوع نظرسنجی");

        builder.HasIndex(x => new { x.Title }).HasDatabaseName("IX_Survey_Title");
    }
}

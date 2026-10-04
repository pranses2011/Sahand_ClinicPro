using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Communication;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول SurveyResponses — پاسخ نظرسنجی - ثبت امتیاز و نظرات بیمار</summary>
public class SurveyResponseConfiguration : IEntityTypeConfiguration<SurveyResponse>
{
    public void Configure(EntityTypeBuilder<SurveyResponse> builder)
    {
        builder.ToTable("SurveyResponses", t => t.HasComment("پاسخ نظرسنجی - ثبت امتیاز و نظرات بیمار"));
        builder.HasKey(x => x.Id);


    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Clinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول PrescriptionTemplates — قالب نسخه - نسخه‌های آماده پزشکان برای بیماری‌های پرتکرار</summary>
public class PrescriptionTemplateConfiguration : IEntityTypeConfiguration<PrescriptionTemplate>
{
    public void Configure(EntityTypeBuilder<PrescriptionTemplate> builder)
    {
        builder.ToTable("PrescriptionTemplates", t => t.HasComment("قالب نسخه - نسخه‌های آماده پزشکان برای بیماری‌های پرتکرار"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TemplateName).HasMaxLength(100).IsRequired().HasComment("نام قالب");
        builder.Property(x => x.Category).HasMaxLength(200).HasComment("دسته قالب");
        builder.Property(x => x.Items).HasMaxLength(200).HasComment("اقلام قالب - JSON");

    }
}

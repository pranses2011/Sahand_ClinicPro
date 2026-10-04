using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Communication;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول SmsTemplates — قالب پیامک - الگوی آماده با متغیرها</summary>
public class SmsTemplateConfiguration : IEntityTypeConfiguration<SmsTemplate>
{
    public void Configure(EntityTypeBuilder<SmsTemplate> builder)
    {
        builder.ToTable("SmsTemplates", t => t.HasComment("قالب پیامک - الگوی آماده با متغیرها"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired().HasComment("نام قالب");
        builder.Property(x => x.Code).HasMaxLength(20).HasComment("کد یکتای قالب");
        builder.Property(x => x.TemplateType).HasConversion<byte>().HasComment("نوع قالب");

        builder.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("IX_SmsTemplate_Code").HasFilter("[IsDeleted] = 0");
    }
}

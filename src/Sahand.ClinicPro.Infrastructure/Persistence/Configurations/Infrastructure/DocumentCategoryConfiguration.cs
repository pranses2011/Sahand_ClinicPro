using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول DocumentCategories — دسته سند - طبقه‌بندی درختی فایل‌ها</summary>
public class DocumentCategoryConfiguration : IEntityTypeConfiguration<DocumentCategory>
{
    public void Configure(EntityTypeBuilder<DocumentCategory> builder)
    {
        builder.ToTable("DocumentCategories", t => t.HasComment("دسته سند - طبقه‌بندی درختی فایل‌ها"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired().HasComment("نام دسته");
        builder.Property(x => x.Code).HasMaxLength(20).HasComment("کد یکتا");
        builder.Property(x => x.IconPath).HasMaxLength(200).HasComment("آیکون");

    }
}

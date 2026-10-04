using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ImagingFiles — فایل تصویربرداری - DICOM یا تصویر معمولی</summary>
public class ImagingFileConfiguration : IEntityTypeConfiguration<ImagingFile>
{
    public void Configure(EntityTypeBuilder<ImagingFile> builder)
    {
        builder.ToTable("ImagingFiles", t => t.HasComment("فایل تصویربرداری - DICOM یا تصویر معمولی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(200).HasComment("نام فایل");
        builder.Property(x => x.FilePath).HasMaxLength(500).HasComment("مسیر ذخیره‌سازی");
        builder.Property(x => x.FileType).HasMaxLength(20).HasComment("نوع فایل");
        builder.Property(x => x.ThumbnailPath).HasMaxLength(500).HasComment("مسیر پیش‌نمایش");

    }
}

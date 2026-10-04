using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Documents — سند - فایل پیوست هر رکورد سیستم</summary>
public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents", t => t.HasComment("سند - فایل پیوست هر رکورد سیستم"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired().HasComment("عنوان سند");
        builder.Property(x => x.ReferenceType).HasConversion<byte>().HasComment("نوع رکورد مرجع");
        builder.Property(x => x.FileName).HasMaxLength(200).HasComment("نام فایل");
        builder.Property(x => x.FilePath).HasMaxLength(500).HasComment("مسیر ذخیره‌سازی");
        builder.Property(x => x.FileType).HasMaxLength(20).HasComment("پسوند فایل");
        builder.Property(x => x.MimeType).HasMaxLength(100).HasComment("نوع MIME");
        builder.Property(x => x.Hash).HasMaxLength(100).HasComment("هش فایل برای بررسی یکپارچگی");
        builder.Property(x => x.Tags).HasMaxLength(500).HasComment("برچسب‌ها");

    }
}

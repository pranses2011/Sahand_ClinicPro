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
        // چرخه بیستم (گزارش چهاردهم مالک): ستون Items پیش‌تر nvarchar(200) بود و
        // JSON اقلام نسخه (نام دارو + دوز + مصرف + مدت + …) به‌راحتی از ۲۰۰ نویسه
        // عبور می‌کرد → خطای «رشته یا دادهٔ باینری قطع می‌شود» و پیام «خطای غیرمنتظره
        // در سرور» هنگام «ذخیره به‌عنوان تمپلیت». ستون به nvarchar(max) ارتقا یافت.
        builder.Property(x => x.Items).HasColumnType("nvarchar(max)").HasComment("اقلام قالب - JSON");

        // چرخه بیست‌وسوم (گزارش شانزدهم مالک): رابطهٔ پزشک↔تمپلیت پیش‌تر با قراردادِ
        // «FK الزامی» ساخته می‌شد و درج تمپلیت بدون پزشک (DoctorId=0 که در جدول
        // پزشکان وجود ندارد) با نقض کلید خارجی شکست می‌خورد و به خطای ۵۰۰
        // «غیر منتظره در سرور» منجر می‌شد. رابطهٔ صریحِ اختیاری (IsRequired=false)
        // پیکربندی شد تا تمپلیت عمومی بدون پزشک مجاز باشد. مهاجرت متناظر:
        // Cycle23TemplateDoctorNullable.
        builder.HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Reporting;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول PrintTemplates — قالب چاپ سفارشی (چرخه ۱۳، درخواست ۱-۶)</summary>
public class PrintTemplateConfiguration : IEntityTypeConfiguration<PrintTemplate>
{
    /// <summary>پیکربندی جدول و ایندکس‌ها</summary>
    public void Configure(EntityTypeBuilder<PrintTemplate> builder)
    {
        builder.ToTable("PrintTemplates", t => t.HasComment("قالب چاپ سفارشی — طراح آنلاین با متغیرهای جایگاه‌دار"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TemplateName).HasMaxLength(200).IsRequired().HasComment("نام نمایشی قالب");
        builder.Property(x => x.Type).HasConversion<byte>().HasComment("نوع سند — ۹ نوع چاپی");
        builder.Property(x => x.Description).HasMaxLength(500).HasComment("توضیح اختیاری");
        builder.Property(x => x.HtmlBody).IsRequired().HasComment("بدنه HTML با متغیرهای {{نام_متغیر}}");
        // چرخه سی‌ویکم: ۱۰ → ۲۰ نویسه برای کلیدهای بلندتر مثل «Custom:100x180»
        // (قالب سفارشی عرضxارتفاع) و «Thermal80» که پیش‌تر به‌زور در ۱۰ نویسه جا می‌شد
        builder.Property(x => x.PaperSize).HasMaxLength(20).HasComment("اندازه کاغذ — A4/A5/A6/Thermal80/Custom:WxH");
        builder.Property(x => x.Orientation).HasMaxLength(12).HasComment("جهت صفحه — Portrait/Landscape");
        builder.Property(x => x.FontSize).HasComment("اندازه فونت پایه (پیکسل)");
        builder.Property(x => x.FontFamily).HasMaxLength(100).HasComment("خانواده فونت");
        builder.Property(x => x.ShowClinicLogo).HasComment("نمایش لوگوی درمانگاه در سربرگ");
        builder.Property(x => x.AccentColor).HasMaxLength(9).HasComment("رنگ تأکیدی — هگز");
        builder.Property(x => x.IsActive).HasComment("فعال");
        builder.Property(x => x.IsDefault).HasComment("پیش‌فرض این نوع");

        // فقط یک قالب پیش‌فرض برای هر نوع — ایندکس فیلترشده
        builder.HasIndex(x => new { x.Type, x.IsDefault })
            .HasDatabaseName("IX_PrintTemplate_Type_Default")
            .HasFilter("[IsDefault] = 1 AND [IsDeleted] = 0");
    }
}

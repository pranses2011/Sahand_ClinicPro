using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Billing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول InvoiceItems — قلم فاکتور - یک خدمت با جزئیات سهم بیمه و پزشک</summary>
public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("InvoiceItems", t => t.HasComment("قلم فاکتور - یک خدمت با جزئیات سهم بیمه و پزشک"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity).HasPrecision(10, 2).HasComment("تعداد");
        builder.Property(x => x.UnitPrice).HasPrecision(18, 0).HasComment("قیمت واحد");
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 0).HasComment("تخفیف قلم");
        builder.Property(x => x.DiscountPercent).HasPrecision(5, 2).HasComment("درصد تخفیف");
        // ═══ چرخهٔ سی‌ودوم (گزارش مالک — بحرانی‌ترین ریشهٔ مالی) ═══
        // «پذیرش ثبت شد و صدور فاکتور شکست خورد — خطای غیرمنتظره»:
        // این دو ستون «مبلغ ریالی» سهم بیمه/بیمارند ولی به‌اشتباه از پیکربندی
        // تعرفه‌ها (که در آن‌ها همین نام‌ها «درصد»‌اند) با HasPrecision(5,2)
        // کپی شده بودند → سقف ۹۹۹٫۹۹ ریال! هر فاکتور واقعی (مثلاً ویزیت
        // ۸۰۰٬۰۰۰ ریالی) هنگام INSERT با SqlException «Arithmetic overflow
        // error converting numeric to data type numeric» شکست می‌خورد و
        // کل زنجیرهٔ مالی بی‌نتیجه می‌ماند؛ اکنون دقت ریالی استاندارد (18,2)
        // هم‌تراز بقیهٔ ستون‌های مبلغ؛ کسرهای درصدی (مثل ۳۳٬۳۳۳٫۳۳) هم بدون
        // گردشدن مخفی حفظ می‌شوند.
        builder.Property(x => x.InsuranceShare).HasPrecision(18, 2).HasComment("سهم ریالی بیمه از این قلم");
        builder.Property(x => x.InsurancePercent).HasPrecision(5, 2).HasComment("درصد بیمه");
        builder.Property(x => x.SupplementaryShare).HasPrecision(18, 0).HasComment("سهم بیمه تکمیلی");
        builder.Property(x => x.PatientShare).HasPrecision(18, 2).HasComment("سهم ریالی بیمار از این قلم");
        builder.Property(x => x.TotalPrice).HasPrecision(18, 0).HasComment("قیمت کل قلم");
        builder.Property(x => x.DoctorShareAmount).HasPrecision(18, 0).HasComment("سهم ریالی پزشک");
        builder.Property(x => x.DoctorSharePercent).HasPrecision(5, 2).HasComment("درصد سهم پزشک");
        builder.Property(x => x.UnitCost).HasPrecision(18, 0).HasComment("عکس‌فوری هزینه تمام‌شده هر واحد در لحظه صدور — مبنای سود این قلم");
        builder.Property(x => x.PerformedById).HasComment("شناسه پرسنل انجام‌دهنده خدمت — مبنای حقوق درصدی سود خدمات انجامی همان پرسنل");

        // رابطه اختیاری قلم→پرسنل انجام‌دهنده (حقوق درصدی از سود همین اقلام محاسبه می‌شود)
        builder.HasIndex(x => x.PerformedById).HasDatabaseName("IX_InvoiceItem_PerformedBy");
        builder.HasOne(x => x.Performer)
            .WithMany()
            .HasForeignKey(x => x.PerformedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

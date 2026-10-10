using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Medications — دارو - کاتالوگ مرجع داروها با کد IRC</summary>
public class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> builder)
    {
        builder.ToTable("Medications", t => t.HasComment("دارو - کاتالوگ مرجع داروها با کد IRC"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IrcCode).HasMaxLength(20).HasComment("کد IRC دارو - یکتا");
        builder.Property(x => x.GenericName).HasMaxLength(200).IsRequired().HasComment("نام ژنریک انگلیسی");
        builder.Property(x => x.GenericNameFa).HasMaxLength(200).HasComment("نام ژنریک فارسی");
        builder.Property(x => x.BrandName).HasMaxLength(200).HasComment("نام تجاری");
        builder.Property(x => x.AtcCode).HasMaxLength(20).HasComment("کد ATC طبقه‌بندی دارویی");
        builder.Property(x => x.Form).HasMaxLength(50).HasComment("فرم دارویی مثل قرص یا شربت");
        builder.Property(x => x.Strength).HasMaxLength(200).HasComment("غلظت مثل 500 میلی‌گرم");
        builder.Property(x => x.Manufacturer).HasMaxLength(200).HasComment("شرکت سازنده");
        builder.Property(x => x.Country).HasMaxLength(200).HasComment("کشور سازنده");
        builder.Property(x => x.Barcode).HasMaxLength(50).HasComment("بارکد");
        builder.Property(x => x.Category).HasMaxLength(200).HasComment("دسته دارویی");

        builder.HasIndex(x => new { x.IrcCode }).IsUnique().HasDatabaseName("IX_Medication_IrcCode").HasFilter("[IsDeleted] = 0");

        // ═══ چرخهٔ سی‌ونه‌م (گزارش مالک) — نوع قلم انبار: دارو یا لوازم مصرفی ═══
        // «سرنگ و سرم و گاز استریل و مواردی از این قبیل… خرید و انبار و قیمت‌های
        // این لوازم هم باید باشه» — همان جدول Medications با پرچم ItemKind به
        // دو دسته تقسیم می‌شود؛ مقدار پیش‌فرض ۱ (دارو) یعنی همهٔ ردیف‌های موجود
        // پس از مهاجرت بدون تغییر، دارو باقی می‌مانند.
        builder.Property(x => x.ItemKind)
            .HasConversion<byte>()
            .HasDefaultValue(MedicationItemKind.Drug)
            .HasComment("نوع قلم انبار — ۱: دارو، ۲: لوازم مصرفی (سرنگ/سرم/گاز و…) — چرخهٔ ۳۹");

        // ═══ چرخه ۱۳ — الگوریتم خطر تشخیصی: ستون‌های خطر بالینی ═══
        builder.Property(x => x.PregnancyCategory)
            .HasConversion<byte>()
            .HasComment("رده بارداری FDA (۰=نرده‌بندی ۱=A … ۵=X) — هشدار تجویز در بارداری");
        builder.Property(x => x.IsLactationRisk).HasComment("خطر مصرف در شیردهی");
        builder.Property(x => x.NeedsPediatricCaution).HasComment("احتیاط مصرف در کودکان زیر ۱۲ سال");
        builder.Property(x => x.NeedsGeriatricCaution).HasComment("احتیاط مصرف در سالمندان بالای ۶۵ سال (Beers)");
        builder.Property(x => x.NeedsRenalCaution).HasComment("احتیاط در نارسایی کلیه — تعدیل دوز");
        builder.Property(x => x.NeedsHepaticCaution).HasComment("احتیاط در نارسایی کبدی");
        builder.Property(x => x.MaxDailyDose).HasMaxLength(100).HasComment("حداکثر دوز روزانه مجاز — متن توضیحی");
        builder.Property(x => x.CausesSedation).HasComment("هشدار خواب‌آلودگی/رانندگی");
        builder.Property(x => x.HasAbusePotential).HasComment("خطر وابستگی/اعتیاد");
    }
}

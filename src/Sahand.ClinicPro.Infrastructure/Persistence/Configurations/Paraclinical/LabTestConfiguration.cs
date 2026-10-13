using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول LabTests — آزمایش - کاتالوگ تست‌های آزمایشگاهی</summary>
public class LabTestConfiguration : IEntityTypeConfiguration<LabTest>
{
    public void Configure(EntityTypeBuilder<LabTest> builder)
    {
        builder.ToTable("LabTests", t => t.HasComment("آزمایش - کاتالوگ تست‌های آزمایشگاهی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TestCode).HasMaxLength(20).HasComment("کد تست - یکتا");
        builder.Property(x => x.TestName).HasMaxLength(200).IsRequired().HasComment("نام آزمایش");
        builder.Property(x => x.TestNameEn).HasMaxLength(200).HasComment("نام انگلیسی");
        builder.Property(x => x.Category).HasMaxLength(200).HasComment("دسته آزمایش مثل بیوشیمی");
        builder.Property(x => x.SampleType).HasMaxLength(200).HasComment("نوع نمونه مثل خون یا ادرار");
        builder.Property(x => x.ContainerType).HasMaxLength(200).HasComment("نوع ظرف نمونه");
        builder.Property(x => x.NormalRangeMale).HasMaxLength(200).HasComment("حد طبیعی آقایان");
        builder.Property(x => x.NormalRangeFemale).HasMaxLength(200).HasComment("حد طبیعی خانم‌ها");

        // ═══ بازه‌های مرجع ساخت‌یافته + حدود بحرانی — چرخهٔ ۶۹ (فاز ۳ / F-28+F-29) ═══
        // دقت ۱۸,۴ برای پوشش هم آنالیت‌های اعشاری (TSH با ۰٫۴) و هم مقادیر بزرگ؛
        // همه تهی‌پذیر — تست بدون بازه تفسیر نمی‌شود.
        builder.Property(x => x.RefMinAdultMale).HasPrecision(18, 4).HasComment("کف بازهٔ مرجع بزرگسال مرد — چرخهٔ ۶۹ (F-28)");
        builder.Property(x => x.RefMaxAdultMale).HasPrecision(18, 4).HasComment("سقف بازهٔ مرجع بزرگسال مرد — چرخهٔ ۶۹ (F-28)");
        builder.Property(x => x.RefMinAdultFemale).HasPrecision(18, 4).HasComment("کف بازهٔ مرجع بزرگسال زن — چرخهٔ ۶۹ (F-28)");
        builder.Property(x => x.RefMaxAdultFemale).HasPrecision(18, 4).HasComment("سقف بازهٔ مرجع بزرگسال زن — چرخهٔ ۶۹ (F-28)");
        builder.Property(x => x.RefMinChild).HasPrecision(18, 4).HasComment("کف بازهٔ مرجع کودک — چرخهٔ ۶۹ (F-28)");
        builder.Property(x => x.RefMaxChild).HasPrecision(18, 4).HasComment("سقف بازهٔ مرجع کودک — چرخهٔ ۶۹ (F-28)");
        builder.Property(x => x.ChildAgeMaxYears).HasComment("سقف سن کودک به سال — چرخهٔ ۶۹ (F-28)");
        builder.Property(x => x.CriticalLow).HasPrecision(18, 4).HasComment("کف مقدار بحرانی — چرخهٔ ۶۹ (F-29)");
        builder.Property(x => x.CriticalHigh).HasPrecision(18, 4).HasComment("سقف مقدار بحرانی — چرخهٔ ۶۹ (F-29)");
        builder.Property(x => x.Unit).HasMaxLength(20).HasComment("واحد سنجش");
        builder.Property(x => x.Price).HasPrecision(18, 0).HasComment("قیمت");

        builder.HasIndex(x => new { x.TestCode }).IsUnique().HasDatabaseName("IX_LabTest_TestCode").HasFilter("[IsDeleted] = 0");
    }
}

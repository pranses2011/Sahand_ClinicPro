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
        builder.Property(x => x.Unit).HasMaxLength(20).HasComment("واحد سنجش");
        builder.Property(x => x.Price).HasPrecision(18, 0).HasComment("قیمت");

        builder.HasIndex(x => new { x.TestCode }).IsUnique().HasDatabaseName("IX_LabTest_TestCode").HasFilter("[IsDeleted] = 0");
    }
}

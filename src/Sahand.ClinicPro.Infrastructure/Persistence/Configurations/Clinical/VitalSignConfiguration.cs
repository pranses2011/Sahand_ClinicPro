using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Clinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول VitalSigns — علائم حیاتی - اندازه‌گیری‌های بالینی در جریان ویزیت</summary>
public class VitalSignConfiguration : IEntityTypeConfiguration<VitalSign>
{
    public void Configure(EntityTypeBuilder<VitalSign> builder)
    {
        builder.ToTable("VitalSigns", t => t.HasComment("علائم حیاتی - اندازه‌گیری‌های بالینی در جریان ویزیت"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Temperature).HasPrecision(4, 1).HasComment("دمای بدن به سلسیوس");
        builder.Property(x => x.Weight).HasPrecision(5, 2).HasComment("وزن به کیلوگرم");
        builder.Property(x => x.Height).HasPrecision(5, 2).HasComment("قد به سانتی‌متر");
        builder.Property(x => x.Bmi).HasPrecision(4, 2).HasComment("شاخص توده بدنی - محاسباتی");
        builder.Property(x => x.HeadCircumference).HasPrecision(5, 2).HasComment("دور سر به سانتی‌متر - برای نوزاد");

    }
}

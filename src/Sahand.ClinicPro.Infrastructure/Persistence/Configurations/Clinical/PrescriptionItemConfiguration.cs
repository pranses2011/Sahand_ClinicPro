using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Clinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول PrescriptionItems — قلم نسخه - یک دارو با دستور مصرف</summary>
public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.ToTable("PrescriptionItems", t => t.HasComment("قلم نسخه - یک دارو با دستور مصرف"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Dosage).HasMaxLength(100).HasComment("دوز مصرف مثل یک قرص");
        builder.Property(x => x.Frequency).HasMaxLength(100).HasComment("تعداد مصرف در روز");
        builder.Property(x => x.Duration).HasMaxLength(50).HasComment("مدت مصرف مثل هفت روز");
        builder.Property(x => x.Route).HasMaxLength(50).HasComment("مسیر مصرف - خوراکی یا تزریقی");
        builder.Property(x => x.Quantity).HasPrecision(10, 2).HasComment("تعداد کل");
        builder.Property(x => x.Unit).HasMaxLength(20).HasComment("واحد شمارش مثل عدد یا آمپول");
        builder.Property(x => x.DeliveredQuantity).HasPrecision(10, 2).HasComment("تعداد تحویل‌شده");

    }
}

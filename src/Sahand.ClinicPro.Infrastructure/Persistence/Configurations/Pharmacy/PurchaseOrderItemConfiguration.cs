using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Pharmacy;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول PurchaseOrderItems — قلم سفارش خرید - یک دارو با مقدار</summary>
public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("PurchaseOrderItems", t => t.HasComment("قلم سفارش خرید - یک دارو با مقدار"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity).HasPrecision(10, 2).HasComment("تعداد سفارش");
        builder.Property(x => x.UnitPrice).HasPrecision(18, 0).HasComment("قیمت واحد");
        builder.Property(x => x.TotalPrice).HasPrecision(18, 0).HasComment("قیمت کل");
        builder.Property(x => x.ReceivedQuantity).HasPrecision(10, 2).HasComment("تعداد دریافتی");

    }
}

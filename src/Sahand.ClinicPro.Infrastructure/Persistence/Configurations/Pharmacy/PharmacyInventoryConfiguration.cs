using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Pharmacy;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول PharmacyInventory — موجودی داروخانه - به تفکیک بچ و تاریخ انقضا</summary>
public class PharmacyInventoryConfiguration : IEntityTypeConfiguration<PharmacyInventory>
{
    public void Configure(EntityTypeBuilder<PharmacyInventory> builder)
    {
        builder.ToTable("PharmacyInventory", t => t.HasComment("موجودی داروخانه - به تفکیک بچ و تاریخ انقضا"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BatchNumber).HasMaxLength(50).HasComment("شماره بچ");
        builder.Property(x => x.Quantity).HasPrecision(10, 2).HasComment("موجودی فعلی");
        builder.Property(x => x.MinStock).HasPrecision(18, 0).HasComment("حداقل موجودی");
        builder.Property(x => x.MaxStock).HasPrecision(18, 0).HasComment("حداکثر موجودی");
        builder.Property(x => x.ReorderLevel).HasPrecision(18, 0).HasComment("نقطه سفارش مجدد");
        builder.Property(x => x.PurchasePrice).HasPrecision(18, 0).HasComment("قیمت خرید");
        builder.Property(x => x.SellPrice).HasPrecision(18, 0).HasComment("قیمت فروش");
        builder.Property(x => x.Location).HasMaxLength(50).HasComment("محل فیزیکی در قفسه");

        builder.HasIndex(x => new { x.ExpiryDate }).HasDatabaseName("IX_PharmacyInventory_ExpiryDate");
        // چرخه چهاردهم (پیوست P4 — گزارش مالک): فیلتر قبلی «[Quantity] < [ReorderLevel]»
        // مقایسه ستون‌به‌ستون است و SQL Server در فیلتر ایندکس فقط مقایسه ستون با
        // مقدار ثابت را مجاز می‌داند → خطای 10735 هنگام dotnet ef database update.
        // فیلتر به IsDeleted محدود شد؛ ایندکس همچنان جست‌وجهای موجودی صفر/کم را
        // پوشش می‌دهد (پرس‌وجوی شرط سفارش مجدد به‌هرحال مقایسه ستون‌به‌ستون است
        // و هیچ ایندکس فیلترداری نمی‌تواند مستقیماً آن را سیک کند).
        builder.HasIndex(x => new { x.Quantity }).HasDatabaseName("IX_PharmacyInventory_Quantity").HasFilter("[IsDeleted] = 0");
    }
}

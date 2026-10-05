using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Pharmacy;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>
/// پیکربندی EF Core برای جدول StockTakingItems — اقلام انبارگردانی — گام P2 (چرخه ششم)
/// </summary>
public class StockTakingItemConfiguration : IEntityTypeConfiguration<StockTakingItem>
{
    public void Configure(EntityTypeBuilder<StockTakingItem> builder)
    {
        builder.ToTable("StockTakingItems", t => t.HasComment("اقلام انبارگردانی - عکس فوری موجودی + شمارش واقعی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SystemQuantity).HasPrecision(18, 2).HasComment("موجودی سیستم هنگام شروع");
        builder.Property(x => x.CountedQuantity).HasPrecision(18, 2).HasComment("مقدار شمارش‌شده");
        builder.Property(x => x.IsCounted).HasComment("آیا شمارش شده؟");
        builder.Property(x => x.Notes).HasMaxLength(500).HasComment("یادداشت شمارنده");

        builder.HasOne(x => x.StockTaking)
            .WithMany(s => s.Items)
            .HasForeignKey(x => x.StockTakingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Inventory)
            .WithMany()
            .HasForeignKey(x => x.InventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // ایندکس پرس‌وجو: اقلام یک جلسه + یکتایی هر بچ در هر جلسه
        builder.HasIndex(x => x.StockTakingId);
        builder.HasIndex(x => new { x.StockTakingId, x.InventoryId }).IsUnique();
    }
}

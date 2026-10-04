using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Pharmacy;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول PurchaseOrders — سفارش خرید دارو - از تأمین‌کننده</summary>
public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders", t => t.HasComment("سفارش خرید دارو - از تأمین‌کننده"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber).HasMaxLength(20).HasDefaultValueSql("CONCAT('PO-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqPurchaseOrderNumber AS varchar(10)), 6))").HasComment("شماره سفارش - یکتا");
        builder.Property(x => x.TotalAmount).HasPrecision(18, 0).HasComment("مبلغ کل");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت سفارش");

        builder.HasIndex(x => new { x.OrderNumber }).IsUnique().HasDatabaseName("IX_PurchaseOrder_OrderNumber").HasFilter("[IsDeleted] = 0");
    }
}

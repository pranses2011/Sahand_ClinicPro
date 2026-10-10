using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Pharmacy;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول StockTakings — انبارگردانی - شمارش ادواری موجودی</summary>
public class StockTakingConfiguration : IEntityTypeConfiguration<StockTaking>
{
    public void Configure(EntityTypeBuilder<StockTaking> builder)
    {
        builder.ToTable("StockTakings", t => t.HasComment("انبارگردانی - شمارش ادواری موجودی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت");

        // ═══ چرخهٔ چهل‌وسوم — دامنهٔ جلسه: دارو / لوازم مصرفی / کل انبار (تهی) ═══
        builder.Property(x => x.ItemKind)
            .HasConversion<byte>()
            .HasComment("دامنهٔ جلسه انبارگردانی — ۱: دارو، ۲: لوازم مصرفی، تهی: کل انبار — چرخهٔ ۴۳");
        builder.Property(x => x.TakingNumber).HasMaxLength(30).HasComment("شماره جلسه انبارگردانی");
        builder.Property(x => x.ApprovedAt).HasComment("تاریخ تأیید و اعمال مغایرت — گام P2");

        // بارگذاری کامل اقلام جلسه
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

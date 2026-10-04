using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Billing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول InvoiceItems — قلم فاکتور - یک خدمت با جزئیات سهم بیمه و پزشک</summary>
public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("InvoiceItems", t => t.HasComment("قلم فاکتور - یک خدمت با جزئیات سهم بیمه و پزشک"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity).HasPrecision(10, 2).HasComment("تعداد");
        builder.Property(x => x.UnitPrice).HasPrecision(18, 0).HasComment("قیمت واحد");
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 0).HasComment("تخفیف قلم");
        builder.Property(x => x.DiscountPercent).HasPrecision(5, 2).HasComment("درصد تخفیف");
        builder.Property(x => x.InsuranceShare).HasPrecision(5, 2).HasComment("سهم بیمه");
        builder.Property(x => x.InsurancePercent).HasPrecision(5, 2).HasComment("درصد بیمه");
        builder.Property(x => x.SupplementaryShare).HasPrecision(18, 0).HasComment("سهم بیمه تکمیلی");
        builder.Property(x => x.PatientShare).HasPrecision(5, 2).HasComment("سهم بیمار");
        builder.Property(x => x.TotalPrice).HasPrecision(18, 0).HasComment("قیمت کل قلم");
        builder.Property(x => x.DoctorShareAmount).HasPrecision(18, 0).HasComment("سهم ریالی پزشک");
        builder.Property(x => x.DoctorSharePercent).HasPrecision(5, 2).HasComment("درصد سهم پزشک");

    }
}

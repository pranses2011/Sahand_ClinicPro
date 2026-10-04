using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Billing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Invoices — فاکتور - صورت‌حساب خدمات ارائه‌شده به بیمار</summary>
public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices", t => t.HasComment("فاکتور - صورت‌حساب خدمات ارائه‌شده به بیمار"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceNumber).HasMaxLength(20).HasDefaultValueSql("CONCAT('INV-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqInvoiceNumber AS varchar(10)), 6))").HasComment("شماره فاکتور - یکتا");
        builder.Property(x => x.InvoiceDateShamsi).HasMaxLength(200).HasComment("تاریخ شمسی فاکتور");
        builder.Property(x => x.InvoiceType).HasConversion<byte>().HasComment("نوع فاکتور");
        builder.Property(x => x.SubTotal).HasPrecision(18, 0).HasComment("جمع کل قبل از تخفیف");
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 0).HasComment("مبلغ تخفیف");
        builder.Property(x => x.DiscountPercent).HasPrecision(5, 2).HasComment("درصد تخفیف");
        builder.Property(x => x.InsuranceDiscount).HasPrecision(18, 0).HasComment("سهم بیمه اصلی");
        builder.Property(x => x.SupplementaryDiscount).HasPrecision(18, 0).HasComment("سهم بیمه تکمیلی");
        builder.Property(x => x.Tax).HasPrecision(18, 0).HasComment("مالیات");
        builder.Property(x => x.TotalAmount).HasPrecision(18, 0).HasComment("مبلغ نهایی");
        builder.Property(x => x.PaidAmount).HasPrecision(18, 0).HasComment("مبلغ پرداخت‌شده");
        builder.Property(x => x.RemainingAmount).HasPrecision(18, 0).HasComment("مانده حساب");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت فاکتور");
        builder.Property(x => x.PaymentStatus).HasConversion<byte>().HasComment("وضعیت پرداخت");

        builder.HasIndex(x => new { x.InvoiceNumber }).IsUnique().HasDatabaseName("IX_Invoice_InvoiceNumber").HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.InvoiceDate }).HasDatabaseName("IX_Invoice_InvoiceDate");
        builder.HasIndex(x => new { x.PatientId }).HasDatabaseName("IX_Invoice_PatientId");
        builder.HasIndex(x => new { x.PaymentStatus }).HasDatabaseName("IX_Invoice_PaymentStatus");
    }
}

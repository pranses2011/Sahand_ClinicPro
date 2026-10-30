using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Billing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Payments — پرداخت - دریافتی نقدی یا کارتی یا آنلاین از بیمار</summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", t => t.HasComment("پرداخت - دریافتی نقدی یا کارتی یا آنلاین از بیمار"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PaymentNumber).HasMaxLength(20).HasDefaultValueSql("CONCAT('PAY-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqPaymentNumber AS varchar(10)), 6))").HasComment("شماره سند پرداخت - یکتا");
        builder.Property(x => x.Amount).HasPrecision(18, 0).HasComment("مبلغ پرداختی به ریال");
        builder.Property(x => x.Method).HasConversion<byte>().HasComment("روش پرداخت");
        builder.Property(x => x.ReferenceNumber).HasMaxLength(50).HasComment("شماره ارجاع تراکنش");
        builder.Property(x => x.CardNumber).HasMaxLength(20).HasComment("چهار رقم آخر کارت");
        builder.Property(x => x.BankName).HasMaxLength(50).HasComment("نام بانک");
        builder.Property(x => x.TerminalId).HasMaxLength(20).HasComment("شناسه پایانه کارت‌خوان");
        builder.Property(x => x.TraceNumber).HasMaxLength(50).HasComment("شماره پیگیری");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت پرداخت");

        // حتی حذف نرم نیز نباید امکان استفاده مجدد از شناسه درخواست را فراهم کند.
        builder.HasIndex(x => x.RequestId).IsUnique().HasDatabaseName("IX_Payment_RequestId").HasFilter("[RequestId] IS NOT NULL");
        builder.HasIndex(x => new { x.PaymentNumber }).IsUnique().HasDatabaseName("IX_Payment_PaymentNumber").HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.InvoiceId }).HasDatabaseName("IX_Payment_InvoiceId");
    }
}

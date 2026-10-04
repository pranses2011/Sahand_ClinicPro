using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Billing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول DoctorPayments — کارانه پزشک - تسویه دوره‌ای سهم پزشک از ویزیت‌ها</summary>
public class DoctorPaymentConfiguration : IEntityTypeConfiguration<DoctorPayment>
{
    public void Configure(EntityTypeBuilder<DoctorPayment> builder)
    {
        builder.ToTable("DoctorPayments", t => t.HasComment("کارانه پزشک - تسویه دوره‌ای سهم پزشک از ویزیت‌ها"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GrossAmount).HasPrecision(18, 0).HasComment("مبلغ ناخالص");
        builder.Property(x => x.DeductionAmount).HasPrecision(18, 0).HasComment("کسورات");
        builder.Property(x => x.NetAmount).HasPrecision(18, 0).HasComment("مبلغ خالص قابل پرداخت");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت کارانه");

        builder.HasIndex(x => new { x.DoctorId, x.PeriodStart }).HasDatabaseName("IX_DoctorPayment_DoctorId_PeriodStart");
    }
}

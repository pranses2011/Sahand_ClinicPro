using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Billing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Tariffs — تعرفه - قیمت سالانه خدمات برای هر بیمه یا آزاد</summary>
public class TariffConfiguration : IEntityTypeConfiguration<Tariff>
{
    public void Configure(EntityTypeBuilder<Tariff> builder)
    {
        builder.ToTable("Tariffs", t => t.HasComment("تعرفه - قیمت سالانه خدمات برای هر بیمه یا آزاد"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Price).HasPrecision(18, 0).HasComment("قیمت تعرفه‌ای");
        builder.Property(x => x.InsuranceShare).HasPrecision(5, 2).HasComment("درصد سهم بیمه");
        builder.Property(x => x.PatientShare).HasPrecision(5, 2).HasComment("درصد سهم بیمار");
        builder.Property(x => x.InsuranceCeiling).HasPrecision(18, 0).HasComment("سقف بیمه");
        builder.Property(x => x.KValue).HasPrecision(10, 2).HasComment("مقدار K");

        builder.HasIndex(x => new { x.ServiceId, x.InsuranceCompanyId, x.Year }).HasDatabaseName("IX_Tariff_ServiceId_InsuranceCompanyId_Year");
    }
}

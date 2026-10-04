using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Clinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Medications — دارو - کاتالوگ مرجع داروها با کد IRC</summary>
public class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> builder)
    {
        builder.ToTable("Medications", t => t.HasComment("دارو - کاتالوگ مرجع داروها با کد IRC"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IrcCode).HasMaxLength(20).HasComment("کد IRC دارو - یکتا");
        builder.Property(x => x.GenericName).HasMaxLength(200).IsRequired().HasComment("نام ژنریک انگلیسی");
        builder.Property(x => x.GenericNameFa).HasMaxLength(200).HasComment("نام ژنریک فارسی");
        builder.Property(x => x.BrandName).HasMaxLength(200).HasComment("نام تجاری");
        builder.Property(x => x.AtcCode).HasMaxLength(20).HasComment("کد ATC طبقه‌بندی دارویی");
        builder.Property(x => x.Form).HasMaxLength(50).HasComment("فرم دارویی مثل قرص یا شربت");
        builder.Property(x => x.Strength).HasMaxLength(200).HasComment("غلظت مثل 500 میلی‌گرم");
        builder.Property(x => x.Manufacturer).HasMaxLength(200).HasComment("شرکت سازنده");
        builder.Property(x => x.Country).HasMaxLength(200).HasComment("کشور سازنده");
        builder.Property(x => x.Barcode).HasMaxLength(50).HasComment("بارکد");
        builder.Property(x => x.Category).HasMaxLength(200).HasComment("دسته دارویی");

        builder.HasIndex(x => new { x.IrcCode }).IsUnique().HasDatabaseName("IX_Medication_IrcCode").HasFilter("[IsDeleted] = 0");
    }
}

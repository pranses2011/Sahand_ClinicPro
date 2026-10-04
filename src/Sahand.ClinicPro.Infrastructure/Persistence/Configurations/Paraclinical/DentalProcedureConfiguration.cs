using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول DentalProcedures — عمل دندانپزشکی - کاتالوگ اعمال دندانپزشکی</summary>
public class DentalProcedureConfiguration : IEntityTypeConfiguration<DentalProcedure>
{
    public void Configure(EntityTypeBuilder<DentalProcedure> builder)
    {
        builder.ToTable("DentalProcedures", t => t.HasComment("عمل دندانپزشکی - کاتالوگ اعمال دندانپزشکی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired().HasComment("نام عمل");
        builder.Property(x => x.Code).HasMaxLength(20).HasComment("کد یکتا");
        builder.Property(x => x.Category).HasMaxLength(200).HasComment("دسته");
        builder.Property(x => x.DefaultPrice).HasPrecision(18, 0).HasComment("قیمت پیش‌فرض");

    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول DentalTreatmentPlans — طرح درمان دندانپزشکی - برنامه کامل مراحل درمان</summary>
public class DentalTreatmentPlanConfiguration : IEntityTypeConfiguration<DentalTreatmentPlan>
{
    public void Configure(EntityTypeBuilder<DentalTreatmentPlan> builder)
    {
        builder.ToTable("DentalTreatmentPlans", t => t.HasComment("طرح درمان دندانپزشکی - برنامه کامل مراحل درمان"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TotalCost).HasPrecision(18, 0).HasComment("هزینه کل");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت طرح");

    }
}

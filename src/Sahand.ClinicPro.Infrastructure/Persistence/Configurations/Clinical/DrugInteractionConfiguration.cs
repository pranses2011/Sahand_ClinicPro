using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Clinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول DrugInteractions — تداخل دارویی - جفت داروهای خطرناک با شدت تداخل</summary>
public class DrugInteractionConfiguration : IEntityTypeConfiguration<DrugInteraction>
{
    public void Configure(EntityTypeBuilder<DrugInteraction> builder)
    {
        builder.ToTable("DrugInteractions", t => t.HasComment("تداخل دارویی - جفت داروهای خطرناک با شدت تداخل"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Severity).HasConversion<byte>().HasComment("شدت تداخل");
        builder.Property(x => x.Mechanism).HasMaxLength(200).HasComment("سازوکار تداخل");

    }
}

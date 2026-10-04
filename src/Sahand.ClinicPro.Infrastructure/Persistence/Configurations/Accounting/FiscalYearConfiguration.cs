using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Accounting;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول FiscalYears — سال مالی - دوره حسابداری</summary>
public class FiscalYearConfiguration : IEntityTypeConfiguration<FiscalYear>
{
    public void Configure(EntityTypeBuilder<FiscalYear> builder)
    {
        builder.ToTable("FiscalYears", t => t.HasComment("سال مالی - دوره حسابداری"));
        builder.HasKey(x => x.Id);


        builder.HasIndex(x => new { x.Year }).IsUnique().HasDatabaseName("IX_FiscalYear_Year");
    }
}

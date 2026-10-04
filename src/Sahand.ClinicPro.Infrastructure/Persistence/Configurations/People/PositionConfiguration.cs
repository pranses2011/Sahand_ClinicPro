using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Positions — سمت شغلی - عنوان‌های سازمانی با دامنه حقوق</summary>
public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions", t => t.HasComment("سمت شغلی - عنوان‌های سازمانی با دامنه حقوق"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired().HasComment("عنوان سمت");
        builder.Property(x => x.Code).HasMaxLength(20).HasComment("کد یکتای سمت");
        builder.Property(x => x.MinSalary).HasPrecision(18, 0).HasComment("حداقل حقوق");
        builder.Property(x => x.MaxSalary).HasPrecision(18, 0).HasComment("حداکثر حقوق");

        builder.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("IX_Position_Code").HasFilter("[IsDeleted] = 0");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Scheduling;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Shifts — شیفت کاری - الگوی زمانی کار پرسنل</summary>
public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts", t => t.HasComment("شیفت کاری - الگوی زمانی کار پرسنل"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ShiftName).HasMaxLength(200).IsRequired().HasComment("نام شیفت");
        builder.Property(x => x.ShiftCode).HasMaxLength(20).HasComment("کد یکتای شیفت");
        builder.Property(x => x.ShiftType).HasConversion<byte>().HasComment("نوع شیفت");

        builder.HasIndex(x => new { x.ShiftCode }).IsUnique().HasDatabaseName("IX_Shift_ShiftCode").HasFilter("[IsDeleted] = 0");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول PatientHistories — سابقه پزشکی بیمار - جراحی‌ها و بستری‌ها و آلرژی‌های ثبت‌شده</summary>
public class PatientHistoryConfiguration : IEntityTypeConfiguration<PatientHistory>
{
    public void Configure(EntityTypeBuilder<PatientHistory> builder)
    {
        builder.ToTable("PatientHistories", t => t.HasComment("سابقه پزشکی بیمار - جراحی‌ها و بستری‌ها و آلرژی‌های ثبت‌شده"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.HistoryType).HasConversion<byte>().HasComment("نوع سابقه");
        builder.Property(x => x.Title).HasMaxLength(200).HasComment("عنوان سابقه");

    }
}

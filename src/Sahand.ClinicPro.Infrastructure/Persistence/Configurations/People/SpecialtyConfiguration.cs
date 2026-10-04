using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Specialties — تخصص پزشکی - درختی با ارجاع به والد برای فوق‌تخصص‌ها</summary>
public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("Specialties", t => t.HasComment("تخصص پزشکی - درختی با ارجاع به والد برای فوق‌تخصص‌ها"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired().HasComment("نام تخصص به فارسی");
        builder.Property(x => x.NameEn).HasMaxLength(200).HasComment("نام تخصص به انگلیسی");
        builder.Property(x => x.Code).HasMaxLength(20).HasComment("کد یکتای تخصص");
        builder.Property(x => x.IconPath).HasMaxLength(200).HasComment("مسیر آیکون");

        builder.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("IX_Specialty_Code").HasFilter("[IsDeleted] = 0");
    }
}

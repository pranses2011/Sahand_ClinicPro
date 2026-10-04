using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Branches — شعبه درمانگاه - زیرساخت چندشعبه‌ای برای آینده</summary>
public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches", t => t.HasComment("شعبه درمانگاه - زیرساخت چندشعبه‌ای برای آینده"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BranchName).HasMaxLength(200).IsRequired().HasComment("نام شعبه");
        builder.Property(x => x.BranchCode).HasMaxLength(20).HasComment("کد یکتای شعبه");
        builder.Property(x => x.Address).HasMaxLength(500).HasComment("آدرس");
        builder.Property(x => x.City).HasMaxLength(50).HasComment("شهر");
        builder.Property(x => x.Province).HasMaxLength(50).HasComment("استان");
        builder.Property(x => x.Phone).HasMaxLength(15).HasComment("تلفن");

        builder.HasIndex(x => new { x.BranchCode }).IsUnique().HasDatabaseName("IX_Branch_BranchCode").HasFilter("[IsDeleted] = 0");
    }
}

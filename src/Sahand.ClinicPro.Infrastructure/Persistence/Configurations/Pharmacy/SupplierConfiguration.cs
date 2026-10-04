using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Pharmacy;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Suppliers — تأمین‌کننده دارو - شرکت‌های پخش و سازنده</summary>
public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers", t => t.HasComment("تأمین‌کننده دارو - شرکت‌های پخش و سازنده"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompanyName).HasMaxLength(200).IsRequired().HasComment("نام شرکت");
        builder.Property(x => x.Code).HasMaxLength(20).HasComment("کد یکتا");
        builder.Property(x => x.SupplierType).HasConversion<byte>().HasComment("نوع تأمین‌کننده");
        builder.Property(x => x.ContactPerson).HasMaxLength(100).HasComment("شخص رابط");
        builder.Property(x => x.Phone).HasMaxLength(15).HasComment("تلفن");
        builder.Property(x => x.Mobile).HasMaxLength(11).HasComment("موبایل");
        builder.Property(x => x.Email).HasMaxLength(100).HasComment("ایمیل");
        builder.Property(x => x.Address).HasMaxLength(500).HasComment("نشانی");
        builder.Property(x => x.EconomicCode).HasMaxLength(20).HasComment("کد اقتصادی");
        builder.Property(x => x.NationalId).HasMaxLength(200).HasComment("شناسه ملی شرکت");
        builder.Property(x => x.BankAccount).HasMaxLength(50).HasComment("حساب بانکی");

        builder.HasIndex(x => new { x.Code }).IsUnique().HasDatabaseName("IX_Supplier_Code").HasFilter("[IsDeleted] = 0");
    }
}

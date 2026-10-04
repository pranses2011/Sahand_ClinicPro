using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Personnel;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Contracts — قرارداد پرسنلی - سند رسمی همکاری</summary>
public class PersonnelContractConfiguration : IEntityTypeConfiguration<PersonnelContract>
{
    public void Configure(EntityTypeBuilder<PersonnelContract> builder)
    {
        builder.ToTable("Contracts", t => t.HasComment("قرارداد پرسنلی - سند رسمی همکاری"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ContractNumber).HasMaxLength(20).HasDefaultValueSql("CONCAT('CTR-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqContractNumber AS varchar(10)), 6))").HasComment("شماره قرارداد - یکتا");
        builder.Property(x => x.ContractType).HasConversion<byte>().HasComment("نوع قرارداد");
        builder.Property(x => x.Salary).HasPrecision(18, 0).HasComment("حقوق قراردادی");
        builder.Property(x => x.DocumentPath).HasMaxLength(500).HasComment("مسیر فایل قرارداد");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت");

        builder.HasIndex(x => new { x.ContractNumber }).IsUnique().HasDatabaseName("IX_PersonnelContract_ContractNumber").HasFilter("[IsDeleted] = 0");
    }
}

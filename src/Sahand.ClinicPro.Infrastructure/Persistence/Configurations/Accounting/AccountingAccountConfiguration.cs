using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Accounting;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول AccountingAccounts — سرفصل حساب - چهارسطحی: گروه و کل و معین و تفصیلی</summary>
public class AccountingAccountConfiguration : IEntityTypeConfiguration<AccountingAccount>
{
    public void Configure(EntityTypeBuilder<AccountingAccount> builder)
    {
        builder.ToTable("AccountingAccounts", t => t.HasComment("سرفصل حساب - چهارسطحی: گروه و کل و معین و تفصیلی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountCode).HasMaxLength(20).IsRequired().HasComment("کد حساب - یکتا");
        builder.Property(x => x.AccountName).HasMaxLength(200).IsRequired().HasComment("نام حساب");
        builder.Property(x => x.AccountType).HasConversion<byte>().HasComment("نوع حساب - دارایی یا بدهی یا درآمد");
        builder.Property(x => x.Level).HasConversion<byte>().HasComment("سطح حساب");
        builder.Property(x => x.Nature).HasConversion<byte>().HasComment("طبیعت حساب");
        builder.Property(x => x.OpeningBalance).HasPrecision(18, 0).HasComment("تراز افتتاحیه");
        builder.Property(x => x.CurrentBalance).HasPrecision(18, 0).HasComment("تراز جاری");

        builder.HasIndex(x => new { x.AccountCode }).IsUnique().HasDatabaseName("IX_AccountingAccount_AccountCode").HasFilter("[IsDeleted] = 0");
    }
}

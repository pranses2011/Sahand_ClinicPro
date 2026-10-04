using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Billing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Expenses — هزینه - پرداخت‌های جاری درمانگاه</summary>
public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses", t => t.HasComment("هزینه - پرداخت‌های جاری درمانگاه"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExpenseNumber).HasMaxLength(20).HasDefaultValueSql("CONCAT('EXP-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqExpenseNumber AS varchar(10)), 6))").HasComment("شماره سند هزینه - یکتا");
        builder.Property(x => x.Category).HasConversion<byte>().HasComment("دسته هزینه");
        builder.Property(x => x.Amount).HasPrecision(18, 0).HasComment("مبلغ به ریال");
        builder.Property(x => x.Method).HasConversion<byte>().HasComment("روش پرداخت");

        builder.HasIndex(x => new { x.ExpenseNumber }).IsUnique().HasDatabaseName("IX_Expense_ExpenseNumber").HasFilter("[IsDeleted] = 0");
    }
}

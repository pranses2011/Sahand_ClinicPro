using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Personnel;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول EmployeeSalaries — فیش حقوقی - محاسبه ماهانه حقوق پرسنل</summary>
public class EmployeeSalaryConfiguration : IEntityTypeConfiguration<EmployeeSalary>
{
    public void Configure(EntityTypeBuilder<EmployeeSalary> builder)
    {
        builder.ToTable("EmployeeSalaries", t => t.HasComment("فیش حقوقی - محاسبه ماهانه حقوق پرسنل"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BaseSalary).HasPrecision(18, 0).HasComment("حقوق پایه");
        builder.Property(x => x.CommissionPercent).HasPrecision(5, 2).HasComment("عکس‌فوری درصد حقوق از سود خدمات در لحظه محاسبه");
        builder.Property(x => x.ServiceProfitBase).HasPrecision(18, 0).HasComment("جمع سود خدمات انجامی خود پرسنل در دوره — مبنا (نه کل دریافتی)");
        builder.Property(x => x.ServiceCommission).HasPrecision(18, 0).HasComment("حقوق درصدی دوره = سود خدمات × درصد ÷ ۱۰۰");
        builder.Property(x => x.Overtime).HasPrecision(18, 0).HasComment("اضافه‌کاری");
        builder.Property(x => x.Bonus).HasPrecision(18, 0).HasComment("پاداش");
        builder.Property(x => x.Deductions).HasPrecision(18, 0).HasComment("کسورات");
        builder.Property(x => x.Insurance).HasPrecision(18, 0).HasComment("بیمه");
        builder.Property(x => x.Tax).HasPrecision(18, 0).HasComment("مالیات");
        builder.Property(x => x.NetAmount).HasPrecision(18, 0).HasComment("خالص پرداختی");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت");

    }
}

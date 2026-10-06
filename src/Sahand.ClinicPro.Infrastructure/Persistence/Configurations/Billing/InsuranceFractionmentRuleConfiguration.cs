using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Billing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول InsuranceFractionmentRules — قاعده تسویه بیمه (چرخه ۱۳، درخواست ۲-۱)</summary>
public class InsuranceFractionmentRuleConfiguration : IEntityTypeConfiguration<InsuranceFractionmentRule>
{
    /// <summary>پیکربندی جدول و ایندکس‌ها</summary>
    public void Configure(EntityTypeBuilder<InsuranceFractionmentRule> builder)
    {
        builder.ToTable("InsuranceFractionmentRules",
            t => t.HasComment("قاعده تسویه بیمه — درصدی/اسمی به‌ازای خدمت + فرانشیز + سقف"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RuleType).HasConversion<byte>().HasComment("نوع قاعده — ۱=درصدی ۲=اسمی");
        builder.Property(x => x.InsurancePercent).HasPrecision(5, 2).HasComment("درصد سهم بیمه (وقتی درصدی)");
        builder.Property(x => x.NominalAmount).HasPrecision(18, 0).HasComment("مبلغ اسمی سهم بیمه (وقتی اسمی)");
        builder.Property(x => x.FranchiseAmount).HasPrecision(18, 0).HasComment("فرانشیز ثابت بیمار");
        builder.Property(x => x.PerItemCeiling).HasPrecision(18, 0).HasComment("سقف پرداخت بیمه به‌ازای قلم — صفر یعنی بدون سقف");
        builder.Property(x => x.Notes).HasMaxLength(500).HasComment("یادداشت قرارداد");

        // یک قاعده فعال برای هر (بیمه، خدمت) — خالی بودن ServiceId به‌عنوان NULL در ایندکس مجاز است
        builder.HasIndex(x => new { x.InsuranceCompanyId, x.ServiceId, x.IsActive })
            .HasDatabaseName("IX_FractionmentRule_Company_Service")
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");
    }
}

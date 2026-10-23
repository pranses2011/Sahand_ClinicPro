using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sahand.ClinicPro.Application.Features.Insurance.Fractionment;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests.Insurance;

/// <summary>
/// چرخه سیزدهم (B1): موتور تسویه حساب بیمه — درصدی/اسمی + فرانشیز + سقف + اولویت قواعد
/// </summary>
public class FractionmentCalculatorTests
{
    private static (FractionmentCalculator Calc, ClinicProDbContext Ctx) MakeCalc()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"fractionment-{Guid.NewGuid():N}")
            .Options;
        var ctx = new ClinicProDbContext(options);

        static Mock<IRepository<T>> Repo<T>(ClinicProDbContext c) where T : Sahand.ClinicPro.Domain.Common.BaseEntity
        {
            var mock = new Mock<IRepository<T>>();
            mock.Setup(r => r.QueryableNoTracking).Returns(c.Set<T>().AsNoTracking());
            mock.Setup(r => r.QueryableTracking).Returns(c.Set<T>());
            return mock;
        }

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<Invoice>()).Returns(Repo<Invoice>(ctx).Object);
        uow.Setup(u => u.Repository<InsuranceCompany>()).Returns(Repo<InsuranceCompany>(ctx).Object);
        uow.Setup(u => u.Repository<InsuranceFractionmentRule>()).Returns(Repo<InsuranceFractionmentRule>(ctx).Object);
        uow.Setup(u => u.Repository<Tariff>()).Returns(Repo<Tariff>(ctx).Object);

        return (new FractionmentCalculator(uow.Object), ctx);
    }

    private static Invoice InsuranceInvoice(long id, long companyId, params (string desc, decimal qty, decimal price)[] items)
    {
        var inv = new Invoice
        {
            Id = id,
            InvoiceNumber = $"INV-{id}",
            PatientId = 1,
            InsuranceCompanyId = companyId,
            InvoiceDate = DateTime.UtcNow,
            TotalAmount = items.Sum(i => i.qty * i.price)
        };
        foreach (var (desc, qty, price) in items)
            inv.Items.Add(new InvoiceItem
            {
                InvoiceId = id,
                Description = desc,
                Quantity = qty,
                UnitPrice = price,
                TotalPrice = qty * price,
                // ═══ چرخهٔ ۸۰ — snapshot صلاحیت بیمه در زمان صدور ═══
                // موتور تسویه از این چرخه اقلامی را که درصد بیمهٔ ذخیره‌شده‌شان
                // صفر است «معاف/غیربیمه» می‌داند و سهم بیمه را صفر می‌گذارد
                // (همان قاعده‌ای که CreateInvoice هنگام صدور فاکتور می‌نویسد).
                // بدون این مقدار، همهٔ این تست‌ها سهم بیمهٔ صفر می‌گرفتند.
                InsurancePercent = 100m
            });
        return inv;
    }

    private static Patient Patient1() => new()
    {
        Id = 1,
        MedicalRecordNo = "MRN-00000001",
        NationalCode = "0012345678",
        Mobile = "09120000001",
        FirstName = "سارا",
        LastName = "محمدی"
    };

    /// <summary>قاعده درصدی عمومی — ۸۰٪ بیمه بدون فرانشیز</summary>
    [Fact]
    public async Task PercentageGeneralRule_SplitsCorrectly()
    {
        var (calc, ctx) = MakeCalc();
        ctx.Patients.Add(Patient1());
        ctx.InsuranceCompanies.Add(new InsuranceCompany { Id = 5, Name = "تأمین اجتماعی" });
        ctx.InsuranceFractionmentRules.Add(new InsuranceFractionmentRule
        {
            InsuranceCompanyId = 5,
            ServiceId = null,
            RuleType = FractionmentRuleType.Percentage,
            InsurancePercent = 80m
        });
        ctx.Invoices.Add(InsuranceInvoice(100, 5, ("ویزیت", 1, 1_000_000m)));
        await ctx.SaveChangesAsync();

        var result = (await calc.CalculateAsync(100, persist: false)).Value;

        result.Lines.Should().HaveCount(1);
        result.Lines[0].InsuranceShare.Should().Be(800_000m);
        result.Lines[0].PatientShare.Should().Be(200_000m);
        result.Lines[0].RuleSource.Should().Contain("درصدی");
    }

    /// <summary>فرانشیز قبل از تقسیم کسر می‌شود — ۷۰٪ روی مبلغ پس از فرانشیز</summary>
    [Fact]
    public async Task Franchise_DeductedBeforeSplit()
    {
        var (calc, ctx) = MakeCalc();
        ctx.Patients.Add(Patient1());
        ctx.InsuranceCompanies.Add(new InsuranceCompany { Id = 5, Name = "آزاد" });
        ctx.InsuranceFractionmentRules.Add(new InsuranceFractionmentRule
        {
            InsuranceCompanyId = 5,
            RuleType = FractionmentRuleType.Percentage,
            InsurancePercent = 70m,
            FranchiseAmount = 100_000m
        });
        ctx.Invoices.Add(InsuranceInvoice(101, 5, ("خدمت", 1, 1_000_000m)));
        await ctx.SaveChangesAsync();

        var result = (await calc.CalculateAsync(101, persist: false)).Value;

        // (۱٬۰۰۰٬۰۰۰ − ۱۰۰٬۰۰۰ فرانشیز) × ۷۰٪ = ۶۳۰٬۰۰۰
        result.Lines[0].Franchise.Should().Be(100_000m);
        result.Lines[0].InsuranceShare.Should().Be(630_000m);
        result.Lines[0].PatientShare.Should().Be(370_000m);
    }

    /// <summary>قاعده اسمی — بیمه مبلغ ثابت می‌پردازد</summary>
    [Fact]
    public async Task NominalRule_PaysFixedAmount()
    {
        var (calc, ctx) = MakeCalc();
        ctx.Patients.Add(Patient1());
        ctx.InsuranceCompanies.Add(new InsuranceCompany { Id = 5, Name = "خدمات" });
        ctx.InsuranceFractionmentRules.Add(new InsuranceFractionmentRule
        {
            InsuranceCompanyId = 5,
            RuleType = FractionmentRuleType.Nominal,
            NominalAmount = 400_000m
        });
        ctx.Invoices.Add(InsuranceInvoice(102, 5, ("خدمت", 1, 1_000_000m)));
        await ctx.SaveChangesAsync();

        var result = (await calc.CalculateAsync(102, persist: false)).Value;

        result.Lines[0].InsuranceShare.Should().Be(400_000m);
        result.Lines[0].PatientShare.Should().Be(600_000m);
        result.Lines[0].RuleType.Should().Be("اسمی");
    }

    /// <summary>سقف قلم — سهم بیمه از سقف بیشتر نمی‌شود</summary>
    [Fact]
    public async Task PerItemCeiling_CapsInsuranceShare()
    {
        var (calc, ctx) = MakeCalc();
        ctx.Patients.Add(Patient1());
        ctx.InsuranceCompanies.Add(new InsuranceCompany { Id = 5, Name = "بیمه" });
        ctx.InsuranceFractionmentRules.Add(new InsuranceFractionmentRule
        {
            InsuranceCompanyId = 5,
            RuleType = FractionmentRuleType.Percentage,
            InsurancePercent = 90m,
            PerItemCeiling = 500_000m
        });
        ctx.Invoices.Add(InsuranceInvoice(103, 5, ("خدمت", 1, 1_000_000m)));
        await ctx.SaveChangesAsync();

        var result = (await calc.CalculateAsync(103, persist: false)).Value;

        result.Lines[0].InsuranceShare.Should().Be(500_000m);
    }

    /// <summary>قاعده اختصاصی خدمت بر قاعده عمومی مقدم است</summary>
    [Fact]
    public async Task SpecificRule_OverridesGeneral()
    {
        var (calc, ctx) = MakeCalc();
        ctx.Patients.Add(Patient1());
        ctx.InsuranceCompanies.Add(new InsuranceCompany { Id = 5, Name = "بیمه" });
        ctx.Services.Add(new Service { Id = 9, ServiceName = "ویزیت متخصص", ServiceCode = "S9" });
        ctx.InsuranceFractionmentRules.AddRange(
            new InsuranceFractionmentRule
            {
                InsuranceCompanyId = 5, ServiceId = null,
                RuleType = FractionmentRuleType.Percentage, InsurancePercent = 70m
            },
            new InsuranceFractionmentRule
            {
                InsuranceCompanyId = 5, ServiceId = 9,
                RuleType = FractionmentRuleType.Percentage, InsurancePercent = 100m
            });
        var inv = InsuranceInvoice(104, 5, ("ویزیت متخصص", 1, 500_000m));
        inv.Items.First().ServiceId = 9;
        ctx.Invoices.Add(inv);
        await ctx.SaveChangesAsync();

        var result = (await calc.CalculateAsync(104, persist: false)).Value;

        result.Lines[0].InsuranceShare.Should().Be(500_000m);
        result.Lines[0].RuleSource.Should().Contain("اختصاصی");
    }

    /// <summary>بدون قاعده و تعرفه — درصد پوشش پیش‌فرض شرکت</summary>
    [Fact]
    public async Task FallbackToCompanyDefault()
    {
        var (calc, ctx) = MakeCalc();
        ctx.Patients.Add(Patient1());
        ctx.InsuranceCompanies.Add(new InsuranceCompany
        {
            Id = 5, Name = "تکمیلی", DefaultCoveragePercent = 90m
        });
        ctx.Invoices.Add(InsuranceInvoice(105, 5, ("خدمت", 1, 1_000_000m)));
        await ctx.SaveChangesAsync();

        var result = (await calc.CalculateAsync(105, persist: false)).Value;

        result.Lines[0].InsuranceShare.Should().Be(900_000m);
        result.Lines[0].RuleSource.Should().Contain("پیش‌فرض");
    }

    /// <summary>فاکتور بدون بیمه — خطای NO_INSURANCE</summary>
    [Fact]
    public async Task NonInsuranceInvoice_Fails()
    {
        var (calc, ctx) = MakeCalc();
        ctx.Patients.Add(Patient1());
        var inv = InsuranceInvoice(106, 0, ("x", 1, 100m));
        inv.InsuranceCompanyId = null; // آزاد
        ctx.Invoices.Add(inv);
        await ctx.SaveChangesAsync();

        var result = await calc.CalculateAsync(106, persist: false);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NO_INSURANCE");
    }

    /// <summary>اعمال — اقلام فاکتور با سهم‌های محاسبه‌شده به‌روزرسانی می‌شوند</summary>
    [Fact]
    public async Task Apply_PersistsSharesOnItems()
    {
        var (calc, ctx) = MakeCalc();
        ctx.Patients.Add(Patient1());
        ctx.InsuranceCompanies.Add(new InsuranceCompany { Id = 5, Name = "بیمه" });
        ctx.InsuranceFractionmentRules.Add(new InsuranceFractionmentRule
        {
            InsuranceCompanyId = 5,
            RuleType = FractionmentRuleType.Percentage,
            InsurancePercent = 60m
        });
        ctx.Invoices.Add(InsuranceInvoice(107, 5, ("خدمت", 2, 250_000m)));
        await ctx.SaveChangesAsync();

        var result = (await calc.CalculateAsync(107, persist: true)).Value;

        // ۲×۲۵۰٬۰۰۰ = ۵۰۰٬۰۰۰ × ۶۰٪ = ۳۰۰٬۰۰۰
        result.TotalInsuranceShare.Should().Be(300_000m);
        var item = ctx.Invoices.Include(i => i.Items).First(i => i.Id == 107).Items.First();
        item.InsuranceShare.Should().Be(300_000m);
        item.PatientShare.Should().Be(200_000m);
    }
}

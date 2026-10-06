using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Features.Reports.Adhoc;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست‌های موتور گزارش‌ساز رایگانه (کوئری‌ساز گرافیکی) — چرخه سیزدهم (درخواست ۴-۳):
/// لیست سفید، فیلتر متنی/تاریخی، گروه‌بندی با شمارش/جمع،
/// رد تعریف خارج از لیست سفید و رفت‌وبرگشت JSON تعریف
/// </summary>
public class AdhocReportEngineTests
{
    /// <summary>ساخت موتور روی دیتابیس درون‌حافظه‌ای با داده نمونه</summary>
    private static (AdhocQueryEngine Engine, ClinicProDbContext Ctx) MakeEngine()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"adhoc-{Guid.NewGuid():N}")
            .Options;
        var ctx = new ClinicProDbContext(options);
        var mediator = new Moq.Mock<MediatR.IMediator>().Object;
        var uow = new UnitOfWork(ctx, mediator);
        return (new AdhocQueryEngine(uow), ctx);
    }

    /// <summary>بیمار نمونه</summary>
    private static Patient Patient(string first, string last, Gender gender, int year = 1370)
        => new()
        {
            FirstName = first,
            LastName = last,
            NationalCode = $"00{Random.Shared.Next(10000000, 99999999)}",
            MedicalRecordNo = $"MR-{Random.Shared.Next(1000, 9999)}",
            Mobile = "09140000000",
            Gender = gender,
            BirthDateShamsi = $"{year}/05/15",
        };

    /// <summary>فاکتور نمونه</summary>
    private static Invoice Invoice(long patientId, decimal total, DateTime date)
        => new()
        {
            PatientId = patientId,
            InvoiceDate = date,
            InvoiceNumber = $"INV-{Random.Shared.Next(1000, 9999)}",
            TotalAmount = total,
            PaidAmount = total,
        };

    [Fact]
    public void Schema_Contains_Seven_Whitelisted_Entities()
    {
        AdhocQueryEngine.Schema.Should().HaveCount(7, "هفت موجودیت لیست سفید است");
        AdhocQueryEngine.Schema.Select(s => s.Key).Should().BeEquivalentTo(
            new[] { "patient", "appointment", "visit", "invoice", "payment", "prescription", "laborder" });
        // هیچ تعریف SQL خام در طرح نیست
        AdhocQueryEngine.Schema.SelectMany(s => s.Fields).Should().OnlyContain(f => f.Name.All(c => char.IsLetter(c)));
    }

    [Fact]
    public async Task Run_PatientListing_FormatsGenderPersian_AndShamsiDates()
    {
        var (engine, ctx) = MakeEngine();
        ctx.Patients.Add(Patient("علی", "رضایی", Gender.Male));
        ctx.Patients.Add(Patient("مریم", "احمدی", Gender.Female));
        await ctx.SaveChangesAsync();

        var result = await engine.RunAsync(new AdhocDefinition(
            "patient",
            new() { "FirstName", "LastName", "Gender", "CreatedAt" },
            new(), null));

        result.Columns.Should().BeEquivalentTo(new[] { "نام", "نام خانوادگی", "جنسیت", "تاریخ ثبت" });
        result.Rows.Should().HaveCount(2);
        result.Rows.Select(r => r[2]).Should().Contain(new[] { "مرد", "زن" }, "شمارش‌ها با برچسب فارسی خروجی می‌شوند");
        // تاریخ ثبت شمسی است — حتماً با ۱۴ شروع می‌شود یا حداقل جداکننده اسلش دارد
        result.Rows.All(r => r[3].Contains('/')).Should().BeTrue("قالب شمسی ۱۴۰۴/۰۱/۰۱ است");
        result.Summary.Should().Contain(s => s.Key == "موجودیت" && s.Value == "بیماران");
    }

    [Fact]
    public async Task Run_TextContainsFilter_FiltersRows()
    {
        var (engine, ctx) = MakeEngine();
        ctx.Patients.Add(Patient("علی", "رضایی", Gender.Male));
        ctx.Patients.Add(Patient("مریم", "احمدی", Gender.Female));
        await ctx.SaveChangesAsync();

        var result = await engine.RunAsync(new AdhocDefinition(
            "patient",
            new() { "FirstName", "LastName" },
            new() { new AdhocFilter("FirstName", AdhocOp.Contains, "عل", null) },
            null));

        result.Rows.Should().HaveCount(1, "تنها بیمار منطبق فیلتر «شامل عل» خروجی می‌شود")
            .And.ContainSingle().Which.Should().Contain("علی");
    }

    [Fact]
    public async Task Run_DateBetweenFilter_ShamsiInput_MatchesGregorianDates()
    {
        var (engine, ctx) = MakeEngine();
        // بیمار مرجع — Include با رابطه اجباری در بانک درون‌حافظه‌ای نیازمند وجود بیمار است
        var p = Patient("علی", "رضایی", Gender.Male);
        ctx.Patients.Add(p);
        await ctx.SaveChangesAsync();
        // دو فاکتور: یکی فروردین ۱۴۰۴ و یکی مهر ۱۴۰۳
        ctx.Invoices.Add(Invoice(p.Id, 100_000, new DateTime(2024, 10, 1)));   // ~ مهر ۱۴۰۳
        ctx.Invoices.Add(Invoice(p.Id, 200_000, new DateTime(2025, 04, 10)));  // ~ فروردین ۱۴۰۴
        await ctx.SaveChangesAsync();

        var result = await engine.RunAsync(new AdhocDefinition(
            "invoice",
            new() { "InvoiceNumber", "TotalAmount", "InvoiceDate" },
            new() { new AdhocFilter("InvoiceDate", AdhocOp.Between, "1404/01/01", "1404/12/29") },
            null));

        result.Rows.Should().ContainSingle("فقط فاکتور فروردین ۱۴۰۴ در بازه شمسی می‌افتد");
    }

    [Fact]
    public async Task Run_GroupByGender_WithCount_ProducesTwoGroups()
    {
        var (engine, ctx) = MakeEngine();
        ctx.Patients.Add(Patient("علی", "۱", Gender.Male));
        ctx.Patients.Add(Patient("رضا", "۲", Gender.Male));
        ctx.Patients.Add(Patient("مریم", "۳", Gender.Female));
        await ctx.SaveChangesAsync();

        var result = await engine.RunAsync(new AdhocDefinition(
            "patient",
            new() { "Gender" },
            new(),
            "Gender",
            Aggregate: AdhocAggregateFunc.Count,
            Limit: 10,
            Chart: "doughnut"));

        result.Rows.Should().HaveCount(2, "دو گروه جنسیت");
        result.Rows.Should().Contain(r => r[0] == "مرد" && r[1] == "2");
        result.Rows.Should().Contain(r => r[0] == "زن" && r[1] == "1");
        result.ChartKind.Should().Be("doughnut");
        result.Summary.Should().Contain(s => s.Key == "جمع تعداد" && s.Value == "3");
    }

    [Fact]
    public async Task Run_GroupByPaymentMethod_WithSum_AggregatesMoney()
    {
        var (engine, ctx) = MakeEngine();
        var p = Patient("علی", "رضایی", Gender.Male);
        ctx.Patients.Add(p);
        await ctx.SaveChangesAsync();
        ctx.Payments.Add(new Payment { PatientId = p.Id, Amount = 100_000, Method = PaymentMethod.Cash, PaymentDate = DateTime.UtcNow });
        ctx.Payments.Add(new Payment { PatientId = p.Id, Amount = 250_000, Method = PaymentMethod.Cash, PaymentDate = DateTime.UtcNow });
        ctx.Payments.Add(new Payment { PatientId = p.Id, Amount = 50_000, Method = PaymentMethod.Card, PaymentDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var result = await engine.RunAsync(new AdhocDefinition(
            "payment",
            new() { "Method" },
            new(),
            "Method",
            Aggregate: AdhocAggregateFunc.Sum,
            AggregateField: "Amount"));

        result.Rows.Should().Contain(r => r[0] == "نقدی" && r[1] == "350,000", "جمع مبلغ نقدی با جداکننده");
        result.Rows.Should().Contain(r => r[0] == "کارت" && r[1] == "50,000");
        result.ChartUnit.Should().Be("ریال", "فیلد تجمیع مبلغی است");
    }

    [Fact]
    public async Task Run_UnknownEntity_IsRejected()
    {
        var (engine, _) = MakeEngine();
        var act = () => engine.RunAsync(new AdhocDefinition("sys_tables", new() { "name" }, new(), null));
        await act.Should().ThrowAsync<InvalidOperationException>("موجودیت خارج از لیست سفید پذیرفته نمی‌شود");
    }

    [Fact]
    public async Task Run_UnknownField_IsRejected()
    {
        var (engine, _) = MakeEngine();
        var act = () => engine.RunAsync(new AdhocDefinition(
            "patient", new() { "FirstName", "PasswordHash" }, new(), null));
        await act.Should().ThrowAsync<InvalidOperationException>("فیلد خارج از لیست سفید پذیرفته نمی‌شود");
    }

    [Fact]
    public async Task Run_FilterOnComputedField_IsRejected()
    {
        var (engine, _) = MakeEngine();
        var act = () => engine.RunAsync(new AdhocDefinition(
            "invoice",
            new() { "InvoiceNumber" },
            new() { new AdhocFilter("PatientName", AdhocOp.Contains, "علی", null) },
            null));
        await act.Should().ThrowAsync<InvalidOperationException>("فیلتر روی فیلد محاسبه‌شده ممنوع است");
    }

    [Fact]
    public async Task Run_AggregateWithoutGroupBy_IsRejected()
    {
        var (engine, _) = MakeEngine();
        var act = () => engine.RunAsync(new AdhocDefinition(
            "payment", new() { "Amount" }, new(), null, Aggregate: AdhocAggregateFunc.Sum, AggregateField: "Amount"));
        await act.Should().ThrowAsync<InvalidOperationException>("تجمیع بدون گروه‌بندی معنا ندارد");
    }

    [Fact]
    public void Definition_Json_RoundTrips()
    {
        var definition = new AdhocDefinition(
            "invoice",
            new() { "TotalAmount", "Status" },
            new() { new AdhocFilter("Status", AdhocOp.Eq, "صادره", null) },
            "InvoiceType",
            Aggregate: AdhocAggregateFunc.Sum,
            AggregateField: "TotalAmount",
            SortField: "InvoiceDate",
            SortDesc: true,
            Limit: 50,
            Chart: "hbar");

        var json = AdhocQueryEngine.Serialize(definition);
        var back = AdhocQueryEngine.Deserialize(json);

        back.Should().BeEquivalentTo(definition, "رفت‌وبرگشت JSON تعریف باید بدون تغییر باشد");
        AdhocQueryEngine.Deserialize("{{خراب}}").Should().BeNull("JSON خراب تهی برمی‌گردد");
    }

    [Fact]
    public async Task Run_Limit_IsClampedToMax()
    {
        var (engine, ctx) = MakeEngine();
        ctx.Patients.Add(Patient("علی", "۱", Gender.Male));
        await ctx.SaveChangesAsync();

        var result = await engine.RunAsync(new AdhocDefinition(
            "patient", new() { "FirstName" }, new(), null, Limit: 99_999));

        // سقف اعمال می‌شود بدون خطا — خروجی فقط یک ردیف است چون یک بیمار هست
        result.Rows.Should().HaveCount(1);
    }
}

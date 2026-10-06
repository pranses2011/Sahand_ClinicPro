using FluentAssertions;
using Sahand.ClinicPro.Application.Features.Reporting.PrintTemplates;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests.Reporting;

/// <summary>
/// چرخه سیزدهم (A4): موتور رندر قالب چاپ — جایگزینی متغیر + بسته‌بندی سند
/// </summary>
public class PrintTemplateRendererTests
{
    /// <summary>جایگزینی متغیر ساده</summary>
    [Fact]
    public void Render_ReplacesKnownVariables()
    {
        var html = "<div>{{patient_name}} — {{total}}</div>";
        var vars = new Dictionary<string, object?>
        {
            ["patient_name"] = "سارا محمدی",
            ["total"] = "۷۲۰٬۰۰۰ ریال"
        };

        var result = PrintTemplateRenderer.Render(html, vars);

        result.Should().Contain("سارا محمدی");
        result.Should().Contain("۷۲۰٬۰۰۰ ریال");
        result.Should().NotContain("{{");
    }

    /// <summary>متغیر بی‌مقدار — با برچسب «نام متغیر» مشخص می‌ماند</summary>
    [Fact]
    public void Render_UnknownVariable_ShowsLabel()
    {
        var result = PrintTemplateRenderer.Render("<b>{{missing_var}}</b>",
            new Dictionary<string, object?>());

        result.Should().Contain("«missing_var»");
    }

    /// <summary>متغیر با فاصله دور براکت هم شناسایی می‌شود</summary>
    [Fact]
    public void Render_ToleratesWhitespace()
    {
        var result = PrintTemplateRenderer.Render("{{  patient_name  }}",
            new Dictionary<string, object?> { ["patient_name"] = "علی" });

        result.Should().Be("علی");
    }

    /// <summary>سند کامل دارای سربرگ RTL و نام درمانگاه است</summary>
    [Fact]
    public void WrapDocument_ContainsHeaderAndRtl()
    {
        var html = PrintTemplateRenderer.WrapDocument(
            "<div class='x'>بدنه</div>", "درمانگاه سهند",
            "تبریز، خیابان آزادی", "۰۴۱-۳۳۳۳۴۴۴",
            logoUrl: null, accent: "#1B5E20", fontSize: 13,
            fontFamily: null, showLogo: true, paper: "A5");

        html.Should().Contain("dir=\"rtl\"");
        html.Should().Contain("درمانگاه سهند");
        html.Should().Contain("خیابان آزادی");
        html.Should().Contain("#1B5E20");
        html.Should().Contain("بدنه");
    }

    /// <summary>لوگوی خاموش — بدون تگ تصویر</summary>
    [Fact]
    public void WrapDocument_WithoutLogo_HasNoImageTag()
    {
        var html = PrintTemplateRenderer.WrapDocument(
            "بدنه", "درمانگاه", null, null, null, null, 13, null, showLogo: false);

        html.Should().NotContain("<img");
    }

    /// <summary>داده نمونه همه انواع — کلیدهای اصلی هر نوع موجود است</summary>
    [Theory]
    [InlineData("Invoice", "invoice_code")]
    [InlineData("Invoice", "total")]
    [InlineData("Prescription", "items_html")]
    [InlineData("LabOrder", "tests_html")]
    [InlineData("AppointmentTicket", "queue_number")]
    [InlineData("PayrollSlip", "net_pay")]
    public void SampleData_HasCoreVariables(string type, string expectedKey)
    {
        var data = PrintTemplateRenderer.SampleData(type);
        data.Should().ContainKey(expectedKey);
    }

    /// <summary>پالت متغیرها — ۹ نوع کامل</summary>
    [Fact]
    public void VariablesOf_CoversAllNineTypes()
    {
        var map = PrintTemplateRenderer.VariablesOf();
        map.Keys.Should().HaveCount(9);
        map["Invoice"].Should().NotBeEmpty();
        map["PayrollSlip"].Should().NotBeEmpty();
    }
}

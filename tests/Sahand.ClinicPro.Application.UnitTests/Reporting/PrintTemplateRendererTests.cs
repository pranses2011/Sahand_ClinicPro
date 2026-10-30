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

    [Fact]
    public void Legal_portrait_has_eight_and_half_inch_width()
    {
        var html = PrintTemplateRenderer.WrapDocument("body", "clinic", null, null, null, null, 13, null, false, "Legal");
        html.Should().Contain(".sheet { width:816px");
    }

    [Theory]
    [InlineData("A4", "1123px", "A4 landscape")]
    [InlineData("Custom:100x180", "680px", "180mm 100mm")]
    public void WrapDocument_landscape_uses_height_as_width_and_safe_page_size(string paper, string width, string cssSize)
    {
        var html = PrintTemplateRenderer.WrapDocument("body", "clinic", null, null, null, null, 13, null, false, paper, "Landscape");
        html.Should().Contain(".sheet { width:" + width).And.Contain("@page { size: " + cssSize + "; }");
    }

    [Fact]
    public void Landscape_rejects_thermal_and_malicious_sizes()
    {
        PrintTemplateRenderer.LandscapePaper("Thermal80").Should().BeNull();
        PrintTemplateRenderer.LandscapePaper("Custom:100x180; color:red").Should().BeNull();
        PrintTemplateRenderer.LandscapePaper("Custom:1x180").Should().BeNull();
        var html = PrintTemplateRenderer.WrapDocument("body", "clinic", null, null, null, null, 13, null, false, "A4;evil", "Landscape");
        html.Should().NotContain("@page").And.NotContain("evil");
    }

    /// <summary>لوگوی خاموش — بدون تگ تصویر</summary>
    [Fact]
    public void WrapDocument_WithoutLogo_HasNoImageTag()
    {
        var html = PrintTemplateRenderer.WrapDocument(
            "بدنه", "درمانگاه", null, null, null, null, 13, null, showLogo: false);

        html.Should().NotContain("<img");
    }

    [Fact]
    public void Render_escapes_plain_patient_values_and_does_not_treat_arbitrary_html_keys_as_markup()
    {
        var html = PrintTemplateRenderer.Render("<div>{{patient_name}} / {{other_html}}</div>",
            new Dictionary<string, object?> {
                ["patient_name"] = "علی <script>alert(1)</script> & شرکا",
                ["other_html"] = "<img src=x onerror=alert(1)>"
            });
        html.Should().Contain("علی &lt;script&gt;alert(1)&lt;/script&gt; &amp; شرکا");
        html.Should().NotContain("<script>").And.NotContain("<img");
    }

    [Theory]
    [InlineData("items_html")]
    [InlineData("tests_html")]
    [InlineData("results_html")]
    public void Render_sanitizes_table_fragments_but_preserves_table_and_text(string key)
    {
        var html = PrintTemplateRenderer.Render("{{" + key + "}}", new Dictionary<string, object?>
        {
            [key] = "<table class='pt' onclick='evil()'><tr><td style='background:url(https://evil.test)'>خدمت</td></tr></table>" +
                "<script>alert(1)</script><img src='https://evil.test/a' onerror='evil()'>"
        });
        html.Should().Contain("<table").And.Contain("<td").And.Contain("خدمت");
        html.Should().NotContain("<script").And.NotContain("<img").And.NotContain("onclick")
            .And.NotContain("onerror").And.NotContain("https://evil.test").And.NotContain("style=");
    }

    [Theory]
    [InlineData("<div title='{{patient_name}}'>بیمار</div>")]
    [InlineData("<div data-x={{patient_name}}>بیمار</div>")]
    [InlineData("<style>.x { color: {{patient_name}}; }</style>")]
    [InlineData("<script>var x = '{{patient_name}}';</script>")]
    [InlineData("<!-- {{patient_name}} --><div>بیمار</div>")]
    public void Render_rejects_variables_outside_text_nodes(string template)
    {
        PrintTemplateRenderer.HasUnsafeVariablePlacement(template).Should().BeTrue();
        var action = () => PrintTemplateRenderer.Render(template,
            new Dictionary<string, object?> { ["patient_name"] = "نام" });
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WrapDocument_encodes_clinic_fields_and_rejects_css_and_logo_injection()
    {
        var html = PrintTemplateRenderer.WrapDocument("<div>بدنه</div>",
            "درمانگاه <img src=x>", "آدرس <script>", "۱ & ۲", "javascript:alert(1)",
            "red;}</style><script>alert(1)</script>", 13, "Tahoma;}</style><script>alert(2)</script>", true);
        html.Should().Contain("درمانگاه &lt;img src=x&gt;").And.Contain("آدرس &lt;script&gt;").And.Contain("۱ &amp; ۲");
        html.Should().NotContain("<script>").And.NotContain("javascript:").And.NotContain("alert(2)");
        html.Should().Contain("#16355E").And.Contain("Vazirmatn");
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
        map["Invoice"].Should().Contain(new[] { "tax", "paid", "remaining", "payment_status" });
        var sample = PrintTemplateRenderer.SampleData("Invoice");
        foreach (var key in new[] { "tax", "paid", "remaining", "payment_status" }) sample.Should().ContainKey(key);
        map["PayrollSlip"].Should().NotBeEmpty();
    }
}

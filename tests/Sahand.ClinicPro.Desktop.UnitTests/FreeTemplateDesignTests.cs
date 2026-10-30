using FluentAssertions;
using Sahand.ClinicPro.Application.Features.Reporting.PrintTemplates;
using Sahand.ClinicPro.Desktop.Services;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class FreeTemplateDesignTests
{
    [Theory]
    [InlineData("A5", 507)]
    [InlineData("Thermal80", 250)]
    [InlineData("Custom:100x180", 326)]
    [InlineData("invalid", 0)]
    public void Width_accounts_for_server_sheet_padding(string paper, double expected)
        => FreeTemplateDesign.AvailableBodyWidth(paper).Should().Be(expected);

    [Fact]
    public void Canvas_roundtrips_and_renders_only_valid_variable_in_text_context()
    {
        var doc = new FreeTemplateDocument { Width = 500, Height = 600, Elements = new() {
            new() { Kind = "text", Text = "<script>{{patient_name}}", X = 10, Y = 20, Width = 200, Height = 40 },
            new() { Kind = "variable", Text = "بیمار:", Key = "patient_name", X = 10, Y = 80, Width = 200, Height = 40 },
            new() { Kind = "rectangle", X = 5, Y = 5, Width = 450, Height = 300 }
        } };
        var keys = new[] { "patient_name" };
        var html = FreeTemplateDesign.Build(doc, keys);
        FreeTemplateDesign.TryRead(html, keys, out var loaded).Should().BeTrue();
        FreeTemplateDesign.Build(loaded!, keys).Should().Be(html);
        html.Should().NotContain("<script>").And.Contain("&#123;&#123;patient_name&#125;&#125;");
        PrintTemplateRenderer.HasUnsafeVariablePlacement(html).Should().BeFalse();
        PrintTemplateRenderer.Render(html, new Dictionary<string, object?> { ["patient_name"] = "<بیمار>" }).Should().Contain("&lt;بیمار&gt;");
        FreeTemplateDesign.TryRead(html + "تغییر", keys, out _).Should().BeFalse();
        FreeTemplateDesign.TryRead(html, Array.Empty<string>(), out _).Should().BeFalse();
    }

    [Fact]
    public void Draft_preview_is_watermarked_and_rejects_modified_html()
    {
        var doc = new FreeTemplateDocument { Elements = new() { new() { Kind = "variable", Key = "patient_name", Text = "نام" } } };
        var keys = new[] { "patient_name" };
        var html = FreeTemplateDesign.Build(doc, keys);
        var preview = FreeTemplatePreview.Create(html, "Invoice", "A4", "#123456", 13, false, keys);
        preview.Should().Contain("پیش‌نویس").And.Contain("داده نمونه").And.Contain("ذخیره‌نشده");
        Action modified = () => FreeTemplatePreview.Create(html + "<script></script>", "Invoice", "A4", null, 13, false, keys);
        modified.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Out_of_bounds_and_injected_style_are_rejected()
    {
        var doc = new FreeTemplateDocument { Width = 500, Height = 600, Elements = new() { new() { X = 490, Width = 100 } } };
        Action build = () => FreeTemplateDesign.Build(doc, Array.Empty<string>());
        build.Should().Throw<ArgumentException>();
        doc.Elements[0].X = 0;
        doc.Elements[0].Color = "red;position:fixed";
        build.Should().Throw<ArgumentException>();
    }
}

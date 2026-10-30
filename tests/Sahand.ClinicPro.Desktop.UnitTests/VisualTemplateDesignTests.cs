using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Application.Features.Reporting.PrintTemplates;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class VisualTemplateDesignTests
{
    [Theory]
    [InlineData("A4", true)]
    [InlineData("Thermal80", true)]
    [InlineData("Custom:100x180", true)]
    [InlineData("Custom:20×1000mm", true)]
    [InlineData("Custom:0x50", false)]
    [InlineData("Custom:1001x100", false)]
    [InlineData("<style>", false)]
    public void Paper_sizes_are_validated(string paper, bool expected)
        => VisualTemplateDesign.IsValidPaperSize(paper).Should().Be(expected);

    [Fact]
    public void Round_trip_preserves_blocks_and_encodes_static_text_without_interpreting_tokens()
    {
        var rows = new List<VisualTemplateBlock> {
            new() { Kind = "text", Text = "<script> {{patient_name}}" },
            new() { Kind = "variable", Text = "بیمار:", Key = "patient_name" } };
        var allowed = new[] { "patient_name" };
        var html = VisualTemplateDesign.Build(rows, allowed);
        html.Should().Contain("&lt;script&gt;").And.NotContain("<script>");
        html.Should().Contain("&#123;&#123;patient_name&#125;&#125;").And.Contain("{{patient_name}}");
        VisualTemplateDesign.TryRead(html, allowed, out var loaded).Should().BeTrue();
        loaded.Select(x => x.Text).Should().Equal(rows.Select(x => x.Text));
        VisualTemplateDesign.Build(loaded, allowed).Should().Be(html);
    }
    [Fact]
    public void Generated_body_is_accepted_by_renderer_and_keeps_literal_tokens_literal()
    {
        var html = VisualTemplateDesign.Build(new[] {
            new VisualTemplateBlock { Kind = "text", Text = "{{patient_name}}" },
            new VisualTemplateBlock { Kind = "variable", Text = "نام:", Key = "patient_name" }
        }, new[] { "patient_name" });
        PrintTemplateRenderer.HasUnsafeVariablePlacement(html).Should().BeFalse();
        var output = PrintTemplateRenderer.Render(html, new Dictionary<string, object?> { ["patient_name"] = "<بیمار>" });
        output.Should().Contain("&#123;&#123;patient_name&#125;&#125;").And.Contain("&lt;بیمار&gt;");
    }
    [Fact]
    public void Existing_or_modified_html_never_silently_converts_and_unknown_variables_rejected()
    {
        var rows = new List<VisualTemplateBlock> { new() { Kind = "variable", Text = "نام", Key = "patient_name" } };
        var html = VisualTemplateDesign.Build(rows, new[] { "patient_name" });
        VisualTemplateDesign.HasVisualMarker(html).Should().BeTrue();
        VisualTemplateDesign.HasVisualMarker(html + "تغییر").Should().BeTrue();
        VisualTemplateDesign.HasVisualMarker("<p>قالب قدیمی</p>").Should().BeFalse();
        VisualTemplateDesign.TryRead("<p>قالب قدیمی</p>", new[] { "patient_name" }, out _).Should().BeFalse();
        VisualTemplateDesign.TryRead(html + "<p>تغییر دستی</p>", new[] { "patient_name" }, out _).Should().BeFalse();
        VisualTemplateDesign.TryRead(html, Array.Empty<string>(), out _).Should().BeFalse();
        Action build = () => VisualTemplateDesign.Build(rows, Array.Empty<string>());
        build.Should().Throw<ArgumentException>();
    }
}

using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class VisualTemplateDesignTests
{
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
    public void Existing_or_modified_html_never_silently_converts_and_unknown_variables_rejected()
    {
        var rows = new List<VisualTemplateBlock> { new() { Kind = "variable", Text = "نام", Key = "patient_name" } };
        var html = VisualTemplateDesign.Build(rows, new[] { "patient_name" });
        VisualTemplateDesign.TryRead("<p>قالب قدیمی</p>", new[] { "patient_name" }, out _).Should().BeFalse();
        VisualTemplateDesign.TryRead(html + "<p>تغییر دستی</p>", new[] { "patient_name" }, out _).Should().BeFalse();
        VisualTemplateDesign.TryRead(html, Array.Empty<string>(), out _).Should().BeFalse();
        Action build = () => VisualTemplateDesign.Build(rows, Array.Empty<string>());
        build.Should().Throw<ArgumentException>();
    }
}

using FluentAssertions;
using Sahand.ClinicPro.Application.Features.Reporting.PrintTemplates;
using Sahand.ClinicPro.Desktop.Services;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class FreeTemplateDesignTests
{
    [Theory]
    [InlineData("۱۲۳٫۵", 123.5)]
    [InlineData("١٢٣.٥", 123.5)]
    [InlineData("123.5", 123.5)]
    public void Canvas_numbers_accept_local_digits(string text, double expected)
    {
        FreeTemplateInput.TryNumber(text, out var actual).Should().BeTrue();
        actual.Should().Be(expected);
    }

    [Fact]
    public void Canvas_numbers_reject_ambiguous_and_non_finite_values()
    {
        FreeTemplateInput.TryNumber("1,200", out _).Should().BeFalse();
        FreeTemplateInput.TryNumber("NaN", out _).Should().BeFalse();
        FreeTemplateInput.TryNumber("1e999", out _).Should().BeFalse();
        FreeTemplateInput.TryInteger("۱۲", out var font).Should().BeTrue();
        font.Should().Be(12);
        FreeTemplateInput.TryInteger("۱۲٫۵", out _).Should().BeFalse();
    }

    [Fact]
    public void Layer_order_is_preserved_in_output_and_stops_at_edges()
    {
        var first = new FreeTemplateElement { Kind = "text", Text = "first" };
        var second = new FreeTemplateElement { Kind = "text", Text = "second" };
        var doc = new FreeTemplateDocument { Elements = new() { first, second } };
        FreeTemplateDesign.MoveLayer(doc, first, -1).Should().BeFalse();
        FreeTemplateDesign.MoveLayer(doc, first, 1).Should().BeTrue();
        doc.Elements.Should().Equal(second, first);
        var html = FreeTemplateDesign.Build(doc, Array.Empty<string>());
        html.IndexOf("second", StringComparison.Ordinal).Should().BeLessThan(html.IndexOf("first", StringComparison.Ordinal));
        FreeTemplateDesign.TryRead(html, Array.Empty<string>(), out var loaded).Should().BeTrue();
        loaded!.Elements.Select(x => x.Text).Should().Equal("second", "first");
        FreeTemplateDesign.MoveLayer(doc, first, 1).Should().BeFalse();
    }

    [Fact]
    public void Drag_stays_inside_fractional_canvas_after_rounding()
    {
        var doc = new FreeTemplateDocument { Width = 600.001, Height = 800.001 };
        var item = new FreeTemplateElement { Width = 100.004, Height = 200.004 };
        var position = FreeTemplateDesign.MoveWithinCanvas(doc, item, 900, 900);
        position.Should().Be((499.99, 599.99));
        (position.X + item.Width).Should().BeLessThanOrEqualTo(doc.Width);
        (position.Y + item.Height).Should().BeLessThanOrEqualTo(doc.Height);
        FreeTemplateDesign.MoveWithinCanvas(doc, item, -10, -5).Should().Be((0, 0));
        Action invalid = () => FreeTemplateDesign.MoveWithinCanvas(doc, item, double.NaN, 0);
        invalid.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Resize_stays_inside_canvas_and_preserves_minimum_size()
    {
        var doc = new FreeTemplateDocument { Width = 600, Height = 800 };
        var item = new FreeTemplateElement { X = 100, Y = 200 };
        FreeTemplateDesign.ResizeWithinCanvas(doc, item, 700, 900).Should().Be((500, 600));
        FreeTemplateDesign.ResizeWithinCanvas(doc, item, -20, 0).Should().Be((5, 5));
        FreeTemplateDesign.ResizeWithinCanvas(doc, item, 120.126, 43.124).Should().Be((120.13, 43.12));
        doc.Width = 600.001; doc.Height = 800.001;
        item.X = 100.004; item.Y = 200.004;
        var bounded = FreeTemplateDesign.ResizeWithinCanvas(doc, item, 900, 900);
        bounded.Should().Be((499.99, 599.99));
        (item.X + bounded.Width).Should().BeLessThanOrEqualTo(doc.Width);
        (item.Y + bounded.Height).Should().BeLessThanOrEqualTo(doc.Height);
        Action invalid = () => FreeTemplateDesign.ResizeWithinCanvas(doc, item, double.NaN, 20);
        invalid.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("A5", 507)]
    [InlineData("Legal", 764)]
    [InlineData("Thermal80", 250)]
    [InlineData("Custom:100x180", 326)]
    [InlineData("invalid", 0)]
    public void Width_accounts_for_server_sheet_padding(string paper, double expected)
        => FreeTemplateDesign.AvailableBodyWidth(paper).Should().Be(expected);

    [Theory]
    [InlineData("A4", 1071)]
    [InlineData("Custom:100x180", 628)]
    [InlineData("Thermal80", 0)]
    public void Landscape_width_accounts_for_orientation(string paper, double expected)
        => FreeTemplateDesign.AvailableBodyWidth(paper, "Landscape").Should().Be(expected);

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
    public void Text_style_roundtrips_and_rejects_arbitrary_css()
    {
        var doc = new FreeTemplateDocument { Elements = new() { new() { Kind = "text", Text = "عنوان", Bold = true, Alignment = "center" } } };
        var html = FreeTemplateDesign.Build(doc, Array.Empty<string>());
        html.Should().Contain("font-weight:bold").And.Contain("text-align:center");
        FreeTemplateDesign.TryRead(html, Array.Empty<string>(), out var loaded).Should().BeTrue();
        loaded!.Elements[0].Bold.Should().BeTrue();
        loaded.Elements[0].Alignment.Should().Be("center");
        doc.Elements[0].Alignment = "left;background:url(https://example.com)";
        Action invalid = () => FreeTemplateDesign.Build(doc, Array.Empty<string>());
        invalid.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Embedded_image_roundtrips_and_never_accepts_remote_or_unlimited_content()
    {
        const string png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/aXQAAAAASUVORK5CYII=";
        var doc = new FreeTemplateDocument { Elements = new() { new() { Kind = "image", ImageData = png } } };
        var html = FreeTemplateDesign.Build(doc, Array.Empty<string>());
        html.Should().Contain("src='data:image/png;base64," + png + "'");
        FreeTemplateDesign.TryRead(html, Array.Empty<string>(), out var loaded).Should().BeTrue();
        loaded!.Elements[0].ImageData.Should().Be(png);
        PrintTemplateRenderer.HasUnsafeVariablePlacement(html).Should().BeFalse();
        FreeTemplateDesign.ImageMimeType("https://example.com/picture.png").Should().BeNull();
        FreeTemplateDesign.ImageMimeType(Convert.ToBase64String(new byte[32769])).Should().BeNull();
        doc.Elements.AddRange(Enumerable.Range(0, 4).Select(_ => new FreeTemplateElement { Kind = "image", ImageData = png }));
        Action tooMany = () => FreeTemplateDesign.Build(doc, Array.Empty<string>());
        tooMany.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Optional_sheet_padding_roundtrips_and_changes_available_width()
    {
        var doc = new FreeTemplateDocument { Width = 500, Height = 600, SheetPadding = 12, Elements = new() { new() { Kind = "text", Text = "نمونه" } } };
        var html = FreeTemplateDesign.Build(doc, Array.Empty<string>());
        html.Should().Contain(".sheet{padding:12px !important;}");
        FreeTemplateDesign.TryRead(html, Array.Empty<string>(), out var loaded).Should().BeTrue();
        loaded!.SheetPadding.Should().Be(12);
        FreeTemplateDesign.AvailableBodyWidth("A5", "Portrait", 12).Should().Be(535);
        doc.SheetPadding = 101;
        Action invalid = () => FreeTemplateDesign.Build(doc, Array.Empty<string>());
        invalid.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Default_padding_does_not_change_legacy_canvas_metadata()
    {
        var doc = new FreeTemplateDocument { Elements = new() { new() { Kind = "text" } } };
        var html = FreeTemplateDesign.Build(doc, Array.Empty<string>());
        var metadata = html["<!--clinicpro-canvas-v1:".Length..html.IndexOf("-->", StringComparison.Ordinal)];
        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(metadata));
        json.Should().NotContain("SheetPadding").And.NotContain("ImageData").And.NotContain("Bold").And.NotContain("Alignment");
        html.Should().NotContain(".sheet{padding:");
        FreeTemplateDesign.TryRead(html, Array.Empty<string>(), out _).Should().BeTrue();
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

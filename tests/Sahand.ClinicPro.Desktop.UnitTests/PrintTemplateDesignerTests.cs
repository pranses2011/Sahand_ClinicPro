using FluentAssertions;
using Sahand.ClinicPro.Desktop.ViewModels;
using Sahand.ClinicPro.Domain.Entities.Reporting;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class PrintTemplateDesignerTests
{
    private static (ComplementaryViewModel Vm, FakeApiClient Api, FakeSnackbar Snackbar) Create(string body)
    {
        var api = new FakeApiClient();
        var snackbar = new FakeSnackbar();
        api.CannedResponses["print-templates"] = new { data = new[] { new { id = 12, type = "ReceptionReceipt", templateName = "قبض", isActive = true, isDefault = true } } };
        api.CannedResponses["print-templates/12"] = new { data = new { id = 12, templateName = "قبض", htmlBody = body, paperSize = "Thermal80", fontSize = 11, isActive = true, isDefault = true } };
        api.CannedResponses["surveys"] = new { data = Array.Empty<object>() };
        return (new ComplementaryViewModel(api, snackbar), api, snackbar);
    }

    [Fact]
    public async Task Original_preview_uses_internal_document_instead_of_html_endpoint()
    {
        var (vm, api, _) = Create(BuiltInPrintTemplates.ReceptionReceiptMarker);
        var requested = 0;
        vm.OriginalReceiptPreviewRequested += () => requested++;
        vm.SelectedTemplate!.IsOriginalReceipt.Should().BeTrue();
        vm.SelectedTemplate.IsHtmlTemplate.Should().BeFalse();
        await vm.PreviewTemplateCommand.ExecuteAsync(null);
        requested.Should().Be(1);
        api.Calls.Should().NotContain(c => c.Endpoint.EndsWith("/preview"));
    }

    [Fact]
    public async Task Unsaved_or_failed_save_never_previews_old_version()
    {
        var (vm, api, snackbar) = Create("<div>نسخه قبلی</div>");
        vm.SelectedTemplate!.Title = "نسخه تغییر یافته";
        api.NextBoolResult = false;
        await vm.SaveTemplateCommand.ExecuteAsync(null);
        await vm.PreviewTemplateCommand.ExecuteAsync(null);
        vm.SelectedTemplate.IsDirty.Should().BeTrue();
        vm.PreviewHtml.Should().BeNull();
        api.Calls.Should().NotContain(c => c.Endpoint.EndsWith("/preview"));
        snackbar.Messages.Should().Contain(m => m.Message.Contains("ابتدا تغییرات"));
    }

    [Fact]
    public async Task Html_preview_clears_when_template_changes()
    {
        var (vm, api, _) = Create("<div>نسخه قبلی</div>");
        api.CannedResponses["print-templates/12/preview"] = new { data = new { html = "<html>نمونه</html>" } };
        await vm.PreviewTemplateCommand.ExecuteAsync(null);
        vm.PreviewHtml.Should().Contain("نمونه");
        vm.SelectedTemplate!.Body = "<div>نسخه تازه</div>";
        vm.PreviewHtml.Should().BeNull();
    }
}

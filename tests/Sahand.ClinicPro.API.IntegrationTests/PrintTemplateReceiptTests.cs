using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.API.Controllers;
using Sahand.ClinicPro.Domain.Entities.Reporting;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class PrintTemplateReceiptTests
{
    [Fact]
    public async Task Receipt_render_checks_print_permission_and_uses_selected_default()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var original = new PrintTemplate { TemplateName = "قبض اصلی", Type = PrintTemplateType.ReceptionReceipt,
            HtmlBody = BuiltInPrintTemplates.ReceptionReceiptMarker, IsActive = true, IsDefault = true };
        var html = new PrintTemplate { TemplateName = "قبض سفارشی", Type = PrintTemplateType.ReceptionReceipt,
            HtmlBody = "<div>بیمار {{patient_name}}</div>{{items_html}}", IsActive = true, IsDefault = false };
        db.PrintTemplates.AddRange(original, html);
        await db.SaveChangesAsync();
        var controller = new PrintTemplatesController(scope.ServiceProvider.GetRequiredService<IUnitOfWork>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var auth = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var request = new RenderRequest(new Dictionary<string, object?> {
            ["patient_name"] = "نام نمونه <script>alert(1)</script>",
            ["items_html"] = "<table><tr><td>خدمت</td></tr></table><img src=x onerror=alert(1)>" });
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("permission", "receptions.view") }, "test"));
        (await controller.Render("ReceptionReceipt", request, auth)).Should().BeOfType<ForbidResult>();
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("permission", "receptions.print") }, "test"));
        (await controller.Render("Invoice", request, auth)).Should().BeOfType<ForbidResult>();
        var preview = (OkObjectResult)await controller.Preview(original.Id);
        var previewData = JsonSerializer.SerializeToElement(preview.Value).GetProperty("Data");
        previewData.GetProperty("useOriginalReceptionReceipt").GetBoolean().Should().BeTrue();
        previewData.GetProperty("html").GetString().Should().BeEmpty();
        var first = (OkObjectResult)await controller.Render("ReceptionReceipt", request, auth);
        JsonSerializer.SerializeToElement(first.Value).GetProperty("Data").GetProperty("useOriginalReceptionReceipt").GetBoolean().Should().BeTrue();
        await controller.SetDefault(html.Id);
        var customPreview = (OkObjectResult)await controller.Preview(html.Id);
        var customPreviewHtml = JsonSerializer.SerializeToElement(customPreview.Value).GetProperty("Data").GetProperty("html").GetString();
        customPreviewHtml.Should().NotContain("__PRINT_DATE__");
        var second = (OkObjectResult)await controller.Render("ReceptionReceipt", request, auth);
        var data = JsonSerializer.SerializeToElement(second.Value).GetProperty("Data");
        data.GetProperty("templateId").GetInt64().Should().Be(html.Id);
        data.GetProperty("html").GetString().Should().Contain("نام نمونه").And.Contain("&lt;script&gt;")
            .And.Contain("<table").And.Contain("خدمت").And.NotContain("<script>")
            .And.NotContain("<img").And.NotContain("__PRINT_DATE__");
        var invalid = new SavePrintTemplateDto("قبض غیرفعال", "ReceptionReceipt", null, "<div>متن قبض</div>", "A5", "Portrait", 12, null, false, null, false, true);
        (await controller.Create(invalid)).Should().BeOfType<BadRequestObjectResult>();
        var badContext = new SavePrintTemplateDto("قالب نامعتبر", "ReceptionReceipt", null,
            "<div title='{{patient_name}}'>بیمار</div>", "A5", "Portrait", 12, null, false, null, true, false);
        (await controller.Create(badContext)).Should().BeOfType<BadRequestObjectResult>();
        html.HtmlBody = badContext.HtmlBody;
        await db.SaveChangesAsync();
        (await controller.Preview(html.Id)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.Render("ReceptionReceipt", request, auth)).Should().BeOfType<BadRequestObjectResult>();
    }
}

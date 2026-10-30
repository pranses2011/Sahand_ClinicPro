using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        // راه‌اندازی کارخانه، قبض اصلی را واقعاً بذر می‌کند؛ یک پیش‌فرض
        // تکراری نسازید، چون InMemory برخلاف SQL Server ایندکس یکتا ندارد.
        var original = await db.PrintTemplates.SingleAsync(t => t.Type == PrintTemplateType.ReceptionReceipt
            && t.HtmlBody == BuiltInPrintTemplates.ReceptionReceiptMarker);
        var html = new PrintTemplate { TemplateName = "قبض سفارشی", Type = PrintTemplateType.ReceptionReceipt,
            HtmlBody = "<div>بیمار {{patient_name}}</div>{{items_html}}", IsActive = true, IsDefault = false };
        db.PrintTemplates.Add(html);
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
        (await controller.SetDefault(html.Id)).Should().BeOfType<OkObjectResult>();
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

    [Fact]
    public async Task Paper_validation_and_landscape_preview_follow_stored_orientation()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var controller = new PrintTemplatesController(scope.ServiceProvider.GetRequiredService<IUnitOfWork>())
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var template = new PrintTemplate { TemplateName = "فاکتور افقی", Type = PrintTemplateType.Invoice,
            HtmlBody = "<div>{{patient_name}}</div>", PaperSize = "A4", Orientation = "Landscape", IsActive = true };
        db.PrintTemplates.Add(template);
        await db.SaveChangesAsync();
        var preview = (OkObjectResult)await controller.Preview(template.Id);
        var html = JsonSerializer.SerializeToElement(preview.Value).GetProperty("Data").GetProperty("html").GetString();
        html.Should().Contain("@page { size: A4 landscape; }").And.Contain(".sheet { width:1123px");
        var invalidSize = new SavePrintTemplateDto("قالب نامعتبر", "Invoice", null, "<div>بدنه نمونه</div>", "Custom:1x180", "Portrait", 12, null, false, null, true, false);
        (await controller.Create(invalidSize)).Should().BeOfType<BadRequestObjectResult>();
        var thermal = new SavePrintTemplateDto("قالب نامعتبر", "Invoice", null, "<div>بدنه نمونه</div>", "Thermal80", "Landscape", 12, null, false, null, true, false);
        (await controller.Create(thermal)).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Appointment_template_render_requires_appointment_create_or_settings_permission()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var template = new PrintTemplate { TemplateName = "نوبت", Type = PrintTemplateType.AppointmentTicket,
            HtmlBody = "<div>{{patient_name}} {{tracking_code}} {{queue_number}}</div>", IsActive = true, IsDefault = false };
        db.PrintTemplates.Add(template);
        await db.SaveChangesAsync();
        var controller = new PrintTemplatesController(scope.ServiceProvider.GetRequiredService<IUnitOfWork>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        (await controller.SetDefault(template.Id)).Should().BeOfType<OkObjectResult>();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var request = new RenderRequest(new Dictionary<string, object?> {
            ["patient_name"] = "بیمار <script>", ["tracking_code"] = "APT-12", ["queue_number"] = "7" });
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("permission", "appointments.view") }, "test"));
        (await controller.Render("AppointmentTicket", request, auth)).Should().BeOfType<ForbidResult>();
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("permission", "appointments.create") }, "test"));
        (await controller.Render("ReceptionReceipt", request, auth)).Should().BeOfType<ForbidResult>();
        var result = (OkObjectResult)await controller.Render("AppointmentTicket", request, auth);
        var data = JsonSerializer.SerializeToElement(result.Value).GetProperty("Data");
        data.GetProperty("templateId").GetInt64().Should().Be(template.Id);
        var html = data.GetProperty("html").GetString();
        html.Should().Contain("APT-12").And.Contain("7").And.Contain("&lt;script&gt;").And.NotContain("<script>");
    }

    [Fact]
    public async Task Lab_view_permission_can_render_only_lab_result_template()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var template = new PrintTemplate { TemplateName = "جواب", Type = PrintTemplateType.LabResult,
            HtmlBody = "<div>{{patient_name}}</div>{{results_html}}", IsActive = true, IsDefault = false };
        db.PrintTemplates.Add(template);
        await db.SaveChangesAsync();
        var controller = new PrintTemplatesController(scope.ServiceProvider.GetRequiredService<IUnitOfWork>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var auth = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        (await controller.SetDefault(template.Id)).Should().BeOfType<OkObjectResult>();
        var request = new RenderRequest(new Dictionary<string, object?> {
            ["patient_name"] = "بیمار <script>", ["results_html"] = "<table><tr><td>12.55</td></tr></table><img src=x onerror=alert(1)>" });
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("permission", "lab.view") }, "test"));
        (await controller.Render("ReceptionReceipt", request, auth)).Should().BeOfType<ForbidResult>();
        var result = (OkObjectResult)await controller.Render("LabResult", request, auth);
        var data = JsonSerializer.SerializeToElement(result.Value).GetProperty("Data");
        data.GetProperty("templateId").GetInt64().Should().Be(template.Id);
        var html = data.GetProperty("html").GetString();
        html.Should().Contain("12.55").And.Contain("&lt;script&gt;").And.NotContain("<img").And.NotContain("<script>");
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("permission", "appointments.view") }, "test"));
        (await controller.Render("LabResult", request, auth)).Should().BeOfType<ForbidResult>();
    }
    [Fact]
    public async Task Invoice_view_permission_renders_invoice_only_and_escapes_variables()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var template = new PrintTemplate { TemplateName = "فاکتور", Type = PrintTemplateType.Invoice,
            HtmlBody = "<p>{{patient_name}} {{paid}} {{remaining}}</p>{{items_html}}", IsActive = true, IsDefault = false };
        db.PrintTemplates.Add(template);
        await db.SaveChangesAsync();
        var controller = new PrintTemplatesController(scope.ServiceProvider.GetRequiredService<IUnitOfWork>())
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var auth = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        (await controller.SetDefault(template.Id)).Should().BeOfType<OkObjectResult>();
        var request = new RenderRequest(new Dictionary<string, object?> { ["patient_name"] = "<script>bad</script>",
            ["paid"] = "20", ["remaining"] = "55", ["items_html"] = "<table><tr><td>خدمت</td></tr></table><img src=x>" });
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("permission", "invoices.view") }, "test"));
        (await controller.Render("ReceptionReceipt", request, auth)).Should().BeOfType<ForbidResult>();
        var result = (OkObjectResult)await controller.Render("Invoice", request, auth);
        var data = JsonSerializer.SerializeToElement(result.Value).GetProperty("Data");
        data.GetProperty("templateId").GetInt64().Should().Be(template.Id);
        var html = data.GetProperty("html").GetString();
        html.Should().Contain("&lt;script&gt;").And.Contain("20").And.Contain("55").And.NotContain("<script>").And.NotContain("<img");
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("permission", "appointments.view") }, "test"));
        (await controller.Render("Invoice", request, auth)).Should().BeOfType<ForbidResult>();
    }

}

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.API.Middleware;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class AuditLogIsolationTests
{
    [Fact]
    public async Task Audit_write_does_not_save_failed_request_changes()
    {
        using var factory = new InMemoryApiFactory();
        using var requestScope = factory.Services.CreateScope();
        var db = requestScope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var context = new DefaultHttpContext { RequestServices = requestScope.ServiceProvider };
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/dental/plans";
        context.Response.Body = new MemoryStream();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Name, "کاربر") }, "test"));
        var middleware = new AuditLogMiddleware(http =>
        {
            // شبیه موجودیت باقی‌مانده در ردیاب پس از rollback یا شکست ذخیره
            db.Patients.Add(new Patient { MedicalRecordNo = "AUDIT-UNSAVED", FirstName = "نباید", LastName = "ثبت شود", Mobile = "09120000000" });
            http.Response.StatusCode = StatusCodes.Status409Conflict;
            return Task.CompletedTask;
        });
        await middleware.InvokeAsync(context, db);
        using var verifyScope = factory.Services.CreateScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        (await verify.Patients.AnyAsync(p => p.MedicalRecordNo == "AUDIT-UNSAVED")).Should().BeFalse();
        (await verify.AuditLogs.AnyAsync(a => a.UserId == 1)).Should().BeTrue();
    }
}

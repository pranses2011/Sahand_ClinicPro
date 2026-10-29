using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Sahand.ClinicPro.API.Controllers;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application.Features.Paraclinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class DentalPatientSafetyTests
{
    [Fact]
    public async Task Plan_requires_existing_patient_and_nonnegative_values()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "DENTAL-1", NationalCode = "0010000021", FirstName = "بیمار", LastName = "آزمایشی", Mobile = "09120000001" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        var handler = new CreateDentalPlanCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        (await handler.Handle(new(999999, null, 0, 0, "{}"), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(patient.Id, null, -1, 0, "{}"), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(patient.Id, null, 0, -1, "{}"), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(patient.Id, null, 0, 0, "{}"), default)).IsSuccess.Should().BeTrue();
        db.Set<Sahand.ClinicPro.Domain.Entities.Paraclinical.DentalTreatmentPlan>().Should().ContainSingle();
    }
    [Theory]
    [InlineData(null)]
    [InlineData("متن طرح قدیمی")]
    [InlineData("[]")]
    [InlineData("{\"teeth\":{},\"rows\":[],\"future\":123}")]
    [InlineData("{\"teeth\":{\"11\":7},\"rows\":[]}")]
    [InlineData("{\"teeth\":{\"11\":1,\"11\":2},\"rows\":[]}")]
    [InlineData("{\"teeth\":{},\"rows\":[{\"tooth\":11,\"price\":10}]}")]
    [InlineData("{\"teeth\":{},\"rows\":[],\"extras\":{\"future\":\"مهم\"}}")]
    public void Unsupported_dental_chart_is_not_safe_to_rewrite(string? details)
    {
        DentalChartCompatibility.CanEdit(details).Should().BeFalse();
    }

    [Fact]
    public void Current_dental_chart_structure_is_editable()
    {
        var json = "{\"teeth\":{\"11\":2,\"51\":1},\"rows\":[{\"tooth\":11,\"procedure\":\"ترمیم\",\"price\":10,\"status\":\"پیشنهاد\",\"surface\":\"O\",\"schedule\":\"بدون نوبت\"}],\"extras\":{\"clinical\":\"یادداشت\",\"consent\":false,\"isChildChart\":false}}";
        DentalChartCompatibility.CanEdit(json).Should().BeTrue();
    }

    [Theory]
    [InlineData("طرح قدیمی", false)]
    [InlineData("{\"teeth\":[],\"rows\":[]}", false)]
    [InlineData("{\"teeth\":{\"11\":2},\"rows\":[]}", true)]
    public async Task Chart_endpoint_preserves_original_when_existing_plan_is_unsupported(string details, bool editable)
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "DENTAL-CHART", FirstName = "بیمار", LastName = "تست", Mobile = "09120000001" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        var plan = new DentalTreatmentPlan { PatientId = patient.Id, PlanDetails = details };
        db.Add(plan);
        await db.SaveChangesAsync();
        var controller = new ParaclinicalController(Mock.Of<IMediator>(), scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            Mock.Of<IWebHostEnvironment>(), Mock.Of<IConfiguration>());
        var response = (OkObjectResult)await controller.GetDentalChart(patient.Id);
        var result = JsonSerializer.SerializeToElement(response.Value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var data = result.GetProperty("data");
        data.GetProperty("canEdit").GetBoolean().Should().Be(editable);
        if (!editable) data.GetProperty("note").GetString().Should().Be(details);
        else data.GetProperty("teeth").GetProperty("11").GetInt32().Should().Be(2);
        plan.PlanDetails.Should().Be(details);
    }

}

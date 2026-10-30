using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application.Features.Paraclinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class ImagingOrderPatientSafetyTests
{
    [Fact]
    public async Task Order_requires_existing_patient_supported_modality_and_correct_body_part()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "IMG-SAFETY-01", FirstName = "بیمار", LastName = "آزمایشی", Mobile = "09120000001" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        var handler = new CreateImagingOrderCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        (await handler.Handle(new(new(999999, null, ImagingModality.MRI, "زانو")), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(new(patient.Id, null, (ImagingModality)6, "زانو")), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(new(patient.Id, null, ImagingModality.MRI, "  ")), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(new(patient.Id, 999999, ImagingModality.MRI, "زانو")), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(new(patient.Id, null, ImagingModality.MRI, " مچ دست چپ ")), default)).IsSuccess.Should().BeTrue();
        var saved = db.Set<ImagingOrder>().Single();
        saved.PatientId.Should().Be(patient.Id);
        saved.Modality.Should().Be(ImagingModality.MRI);
        saved.BodyPart.Should().Be("مچ دست چپ");
    }
    [Fact]
    public async Task Status_transitions_do_not_reopen_reported_or_cancelled_orders()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "IMG-STATUS-01", FirstName = "بیمار", LastName = "تست", Mobile = "09120000001" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        var order = new ImagingOrder { PatientId = patient.Id, BodyPart = "زانو", Modality = ImagingModality.MRI, Status = ImagingOrderStatus.Ordered };
        db.Add(order);
        await db.SaveChangesAsync();
        var handler = new UpdateImagingOrderStatusCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        (await handler.Handle(new(order.Id, ImagingOrderStatus.Reported), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(order.Id, (ImagingOrderStatus)99), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(order.Id, ImagingOrderStatus.Performed), default)).IsSuccess.Should().BeTrue();
        order.PerformedAt.Should().NotBeNull();
        (await handler.Handle(new(order.Id, ImagingOrderStatus.Scheduled), default)).IsSuccess.Should().BeFalse();
        order.Status = ImagingOrderStatus.Reported;
        await db.SaveChangesAsync();
        (await handler.Handle(new(order.Id, ImagingOrderStatus.Performed), default)).IsSuccess.Should().BeFalse();
        order.Status.Should().Be(ImagingOrderStatus.Reported);
        order.Status = ImagingOrderStatus.Cancelled;
        await db.SaveChangesAsync();
        (await handler.Handle(new(order.Id, ImagingOrderStatus.Performed), default)).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Final_reports_cannot_be_empty_or_overwritten_and_cancelled_orders_reject_reports()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "IMG-REPORT-01", FirstName = "بیمار", LastName = "تست", Mobile = "09120000001" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        var order = new ImagingOrder { PatientId = patient.Id, BodyPart = "زانو", Modality = ImagingModality.MRI, Status = ImagingOrderStatus.Performed };
        db.Add(order);
        await db.SaveChangesAsync();
        var handler = new SubmitImagingReportCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        ImagingReportExpectedState Expected() => new(order.Status, order.Technique, order.Findings, order.Impression, order.Recommendation, order.ReportDetails);
        order.Status = ImagingOrderStatus.Ordered;
        (await handler.Handle(new(new(order.Id, null, "پیش‌نویس", null, null, IsFinal: false, ExpectedState: Expected())), default)).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ImagingOrderStatus.Ordered);
        order.PerformedAt.Should().BeNull();
        (await handler.Handle(new(new(order.Id, null, "یافته", null, null, ExpectedState: Expected())), default)).IsSuccess.Should().BeFalse();
        var stale = Expected();
        order.Findings = "تغییر کاربر دیگر";
        (await handler.Handle(new(new(order.Id, null, "بازنویسی قدیمی", null, null, IsFinal: false, ExpectedState: stale)), default)).IsSuccess.Should().BeFalse();
        order.Findings.Should().Be("تغییر کاربر دیگر");
        (await handler.Handle(new(new(order.Id, null, "بدون نسخه", null, null, IsFinal: false)), default)).IsSuccess.Should().BeFalse();
        order.Status = ImagingOrderStatus.Performed;
        (await handler.Handle(new(new(order.Id, null, null, null, null, ExpectedState: Expected())), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(new(order.Id, null, "یافته اصلی", null, null, ExpectedState: Expected())), default)).IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ImagingOrderStatus.Reported);
        order.PerformedAt.Should().BeNull(); // گزارش تاریخ انجام گمشده را جعل نمی‌کند.
        (await handler.Handle(new(new(order.Id, null, "بازنویسی", null, null, IsFinal: false, ExpectedState: Expected())), default)).IsSuccess.Should().BeFalse();
        order.Findings.Should().Be("یافته اصلی");
        order.Status = ImagingOrderStatus.Cancelled;
        await db.SaveChangesAsync();
        (await handler.Handle(new(new(order.Id, null, "گزارش", null, null, ExpectedState: Expected())), default)).IsSuccess.Should().BeFalse();
    }

}

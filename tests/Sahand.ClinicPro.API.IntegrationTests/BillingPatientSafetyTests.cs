using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Application.Features.Billing.Commands.RecordPayment;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class BillingPatientSafetyTests
{
    [Fact]
    public async Task Payment_rejects_wrong_patient_invalid_method_and_nonpayable_invoice()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var owner = new Patient { MedicalRecordNo = "PAY-OWNER", FirstName = "اول", LastName = "بیمار", Mobile = "09120000001" };
        var other = new Patient { MedicalRecordNo = "PAY-OTHER", FirstName = "دوم", LastName = "بیمار", Mobile = "09120000002" };
        db.AddRange(owner, other);
        await db.SaveChangesAsync();
        var invoice = new Invoice { PatientId = owner.Id, TotalAmount = 100, RemainingAmount = 100, Status = InvoiceStatus.Issued };
        db.Add(invoice);
        await db.SaveChangesAsync();
        var handler = new RecordPaymentCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            Mock.Of<ICurrentUserService>(), NullLogger<RecordPaymentCommandHandler>.Instance);
        var wrongPatient = new RecordPaymentDto(invoice.Id, other.Id, 100, PaymentMethod.Cash);
        (await handler.Handle(new(wrongPatient), default)).IsSuccess.Should().BeFalse();
        var badMethod = new RecordPaymentDto(invoice.Id, owner.Id, 100, (PaymentMethod)255);
        (await handler.Handle(new(badMethod), default)).IsSuccess.Should().BeFalse();
        invoice.Status = InvoiceStatus.Cancelled;
        (await handler.Handle(new(new RecordPaymentDto(invoice.Id, owner.Id, 100, PaymentMethod.Cash)), default)).IsSuccess.Should().BeFalse();
        db.Set<Payment>().Should().BeEmpty();
        invoice.PaidAmount.Should().Be(0);
        invoice.RemainingAmount.Should().Be(100);
    }
}

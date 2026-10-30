using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sahand.ClinicPro.Application.Features.Receptions.Queries.GetReceptionReceipt;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

public class ReceptionReceiptDataTests
{
    [Fact]
    public async Task Receipt_uses_stored_totals_and_full_numbers_without_double_deducting_insurance()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"ReceiptData_{Guid.NewGuid():N}").Options;
        using var db = new ClinicProDbContext(options);
        var patient = new Patient { FirstName = "بیمار", LastName = "نمونه", Mobile = "09123456789" };
        var employee = new Employee { FirstName = "انجام", LastName = "دهنده" };
        db.AddRange(patient, employee);
        await db.SaveChangesAsync();
        var reception = new Reception { PatientId = patient.Id, ReceptionNumber = "RCP-000123", ServiceName = "خدمت",
            PerformedByEmployeeId = employee.Id, ReceptionDate = new DateTime(2026, 1, 2, 10, 0, 0) };
        db.Add(reception);
        await db.SaveChangesAsync();
        db.Add(new Invoice { PatientId = patient.Id, ReceptionId = reception.Id, InvoiceNumber = "INV-2026/001",
            SubTotal = 1000, DiscountAmount = 100, InsuranceDiscount = 300, SupplementaryDiscount = 50,
            Tax = 40, TotalAmount = 590, PaidAmount = 200,
            Items = new List<InvoiceItem> { new() { Description = "خدمت", Quantity = 1, UnitPrice = 1000 } },
            Payments = new List<Payment> { new() { PatientId = patient.Id, Amount = 200, Method = PaymentMethod.Card,
                Status = PaymentRecordStatus.Completed, ReferenceNumber = "AB-123" } } });
        await db.SaveChangesAsync();
        var handler = new GetReceptionReceiptQueryHandler(new UnitOfWork(db, new Mock<IMediator>().Object));
        var result = await handler.Handle(new GetReceptionReceiptQuery(reception.Id), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        var receipt = result.Value!;
        receipt.ReceptionNumber.Should().Be("RCP-000123");
        receipt.InvoiceNumbers.Should().Be("INV-2026/001");
        receipt.DoctorName.Should().Be("انجام دهنده");
        receipt.ItemsTotal.Should().Be(1000);
        receipt.Discount.Should().Be(100);
        receipt.InsuranceShare.Should().Be(350);
        receipt.Tax.Should().Be(40);
        receipt.Payable.Should().Be(590);
        receipt.PatientShare.Should().Be(590);
        receipt.Paid.Should().Be(200);
        receipt.PaymentMethod.Should().Be(2);
        receipt.ReferenceNumber.Should().Be("AB-123");
    }

    [Fact]
    public async Task Unpaid_invoice_is_not_claimed_as_cash_payment()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"ReceiptUnpaid_{Guid.NewGuid():N}").Options;
        using var db = new ClinicProDbContext(options);
        var patient = new Patient { FirstName = "بیمار", LastName = "نمونه", Mobile = "09123456789" };
        db.Add(patient); await db.SaveChangesAsync();
        var reception = new Reception { PatientId = patient.Id, ReceptionDate = new DateTime(2026, 1, 1) };
        db.Add(reception); await db.SaveChangesAsync();
        db.Add(new Invoice { PatientId = patient.Id, ReceptionId = reception.Id, SubTotal = 100, TotalAmount = 100 });
        await db.SaveChangesAsync();
        var handler = new GetReceptionReceiptQueryHandler(new UnitOfWork(db, new Mock<IMediator>().Object));
        var result = await handler.Handle(new GetReceptionReceiptQuery(reception.Id), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentMethod.Should().Be(0);
        result.Value.InvoiceNumbers.Should().Be("—");
        result.Value.Paid.Should().Be(0);
    }
}

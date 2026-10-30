using FluentAssertions;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Application.Features.Billing.Commands.CreateInvoice;
using Sahand.ClinicPro.Domain.Enums;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests.Billing;

public class InvoiceAmountSafetyTests
{
    [Fact]
    public void Rejects_out_of_range_precision_and_mixed_consumable_identity()
    {
        var item = new InvoiceItemDto(1, "خدمت", 1, 100, 0, null, 0);
        CreateInvoiceDto Invoice(InvoiceItemDto i) => new(1, null, null, InvoiceType.Visit, null, 0, new() { i });
        InvoiceAmountSafety.Valid(Invoice(item)).Should().BeTrue();
        InvoiceAmountSafety.Valid(Invoice(item with { Quantity = 1.001m })).Should().BeFalse();
        InvoiceAmountSafety.Valid(Invoice(item with { UnitPrice = 100.5m })).Should().BeFalse();
        InvoiceAmountSafety.Valid(Invoice(item with { DiscountPercent = 101 })).Should().BeFalse();
        InvoiceAmountSafety.Valid(Invoice(item with { DoctorSharePercent = -1 })).Should().BeFalse();
        InvoiceAmountSafety.Valid(Invoice(item with { MedicationId = 3 })).Should().BeFalse();
        InvoiceAmountSafety.Valid(Invoice(item with { Quantity = 2, UnitPrice = InvoiceAmountSafety.MaxRial })).Should().BeFalse();
    }
}

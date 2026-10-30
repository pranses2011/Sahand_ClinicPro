using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Application.Features.Billing.Commands.RecordPayment;
using Sahand.ClinicPro.Application.Features.Billing.Commands.CreateInvoice;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.Accounting;
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
        invoice.Status = InvoiceStatus.Issued;
        invoice.IsDeleted = true;
        (await handler.Handle(new(new RecordPaymentDto(invoice.Id, owner.Id, 100, PaymentMethod.Cash)), default)).IsSuccess.Should().BeFalse();
        db.Set<Payment>().Should().BeEmpty();
        invoice.PaidAmount.Should().Be(0);
        invoice.RemainingAmount.Should().Be(100);
    }
    [Fact]
    public async Task Invoice_rejects_reception_or_visit_belonging_to_another_patient()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var owner = new Patient { MedicalRecordNo = "INV-OWNER", FirstName = "اول", LastName = "بیمار", Mobile = "09120000001" };
        var other = new Patient { MedicalRecordNo = "INV-OTHER", FirstName = "دوم", LastName = "بیمار", Mobile = "09120000002" };
        db.AddRange(owner, other);
        await db.SaveChangesAsync();
        var reception = new Reception { PatientId = owner.Id };
        var visit = new Visit { PatientId = owner.Id, DoctorId = 1 };
        db.AddRange(reception, visit);
        await db.SaveChangesAsync();
        var handler = new CreateInvoiceCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            NullLogger<CreateInvoiceCommandHandler>.Instance);
        var items = new List<InvoiceItemDto> { new(null, "خدمت", 1, 100, 0, null, 0) };
        CreateInvoiceDto Make(long? receptionId, long? visitId) => new(other.Id, receptionId, visitId, (InvoiceType)1, null, 0, items);
        (await handler.Handle(new(Make(reception.Id, null)), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(Make(null, visit.Id)), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(Make(999999, null)), default)).IsSuccess.Should().BeFalse();
        db.Set<Invoice>().Should().BeEmpty();
    }

    [Fact]
    public async Task Payment_journal_references_the_saved_payment()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "PAY-JOURNAL", FirstName = "بیمار", LastName = "تست", Mobile = "09120000004" };
        // کارخانه API کدینگ پایه را هنگام راه‌اندازی بذر می‌کند؛ ساخت حساب
        // تکراری در InMemory می‌تواند خطای ایندکس یکتای SQL را پنهان کند.
        db.Add(patient);
        await db.SaveChangesAsync();
        var invoice = new Invoice { PatientId = patient.Id, TotalAmount = 100, RemainingAmount = 100, Status = InvoiceStatus.Issued };
        db.Add(invoice);
        await db.SaveChangesAsync();
        var handler = new RecordPaymentCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            Mock.Of<ICurrentUserService>(), NullLogger<RecordPaymentCommandHandler>.Instance);
        var result = await handler.Handle(new(new RecordPaymentDto(invoice.Id, patient.Id, 100, PaymentMethod.Cash)), default);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
        var journal = await db.Set<JournalEntry>().SingleAsync(e => e.ReferenceType == JournalReferenceType.Payment);
        journal.ReferenceId.Should().Be(result.Value);
    }

    [Fact]
    public async Task Consumables_cannot_be_invoiced_again_when_any_prior_invoice_is_ambiguous()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "INV-AMBIG", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000001" };
        db.Add(patient);
        await db.SaveChangesAsync();
        var reception = new Reception { PatientId = patient.Id };
        db.Add(reception);
        await db.SaveChangesAsync();
        db.Add(new Invoice { PatientId = patient.Id, ReceptionId = reception.Id, Status = InvoiceStatus.Cancelled, IsDeleted = true });
        await db.SaveChangesAsync();
        var handler = new CreateInvoiceCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), NullLogger<CreateInvoiceCommandHandler>.Instance);
        var items = new List<InvoiceItemDto> { new(null, "مصرفی", 1, 100, 0, null, 0, MedicationId: 123) };
        var result = await handler.Handle(new(new CreateInvoiceDto(patient.Id, reception.Id, null, (InvoiceType)1, null, 0, items)), default);
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("BILLING.CONSUMABLE_PREVIOUS_INVOICE");
        (await db.Set<Invoice>().IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Payment_rejects_fractional_or_inconsistent_balance_before_writing()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "PAY-BALANCE", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000001" };
        db.Add(patient); await db.SaveChangesAsync();
        var invoice = new Invoice { PatientId = patient.Id, TotalAmount = 100, PaidAmount = 20, RemainingAmount = 90, Status = InvoiceStatus.Issued };
        db.Add(invoice); await db.SaveChangesAsync();
        var handler = new RecordPaymentCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), Mock.Of<ICurrentUserService>(), NullLogger<RecordPaymentCommandHandler>.Instance);
        (await handler.Handle(new(new RecordPaymentDto(invoice.Id, patient.Id, 1.5m, PaymentMethod.Cash)), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(new RecordPaymentDto(invoice.Id, patient.Id, 10m, PaymentMethod.Cash)), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(new RecordPaymentDto(null, patient.Id, 10m, PaymentMethod.Cash)), default)).IsSuccess.Should().BeFalse();
        db.Set<Payment>().Should().BeEmpty();
    }

    [Fact]
    public async Task Payment_rejects_a_tracked_invoice_from_another_branch()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "BRANCH-PAY", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000003" };
        db.Add(patient); await db.SaveChangesAsync();
        var invoice = new Invoice { PatientId = patient.Id, BranchId = 2, TotalAmount = 100, RemainingAmount = 100, Status = InvoiceStatus.Issued };
        db.Add(invoice); await db.SaveChangesAsync();
        db.CurrentBranchId = 1;
        var handler = new RecordPaymentCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            Mock.Of<ICurrentUserService>(), NullLogger<RecordPaymentCommandHandler>.Instance);
        var result = await handler.Handle(new RecordPaymentCommand(new RecordPaymentDto(invoice.Id, patient.Id, 100, PaymentMethod.Cash)), default);
        result.IsFailure.Should().BeTrue();
        invoice.PaidAmount.Should().Be(0);
        db.Set<Payment>().Should().BeEmpty();
    }
    [Fact]
    public async Task Payment_request_id_replays_same_payment_and_rejects_different_payload()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "PAY-IDEMPOTENT", FirstName = "اول", LastName = "بیمار", Mobile = "09120000001" };
        db.Add(patient); await db.SaveChangesAsync();
        var invoice = new Invoice { PatientId = patient.Id, TotalAmount = 100, RemainingAmount = 100, Status = InvoiceStatus.Issued };
        db.Add(invoice); await db.SaveChangesAsync();
        var handler = new RecordPaymentCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            Mock.Of<ICurrentUserService>(), NullLogger<RecordPaymentCommandHandler>.Instance);
        var dto = new RecordPaymentDto(invoice.Id, patient.Id, 100, PaymentMethod.Cash, RequestId: Guid.NewGuid());
        var first = await handler.Handle(new RecordPaymentCommand(dto), default);
        first.IsSuccess.Should().BeTrue();
        (await db.Set<AccountingAccount>().SingleAsync(a => a.AccountCode == "1101")).IsActive = false;
        await db.SaveChangesAsync();
        var replay = await handler.Handle(new RecordPaymentCommand(dto), default);
        replay.IsSuccess.Should().BeTrue(); replay.Value.Should().Be(first.Value);
        var conflict = await handler.Handle(new RecordPaymentCommand(dto with { Amount = 50 }), default);
        conflict.Error.Code.Should().Be("PAYMENT.REQUEST_CONFLICT");
        (await db.Set<Payment>().CountAsync()).Should().Be(1);
        invoice.PaidAmount.Should().Be(100); invoice.RemainingAmount.Should().Be(0);
        var empty = await handler.Handle(new RecordPaymentCommand(dto with { RequestId = Guid.Empty }), default);
        empty.IsFailure.Should().BeTrue();
    }
    [Fact]
    public async Task Idempotent_payment_endpoint_requires_a_nonempty_key_before_dispatch()
    {
        var mediator = new Mock<MediatR.IMediator>(MockBehavior.Strict);
        var controller = new Sahand.ClinicPro.API.Controllers.PaymentsController(mediator.Object);
        var dto = new RecordPaymentDto(1, 1, 100, PaymentMethod.Cash);
        (await controller.RecordIdempotent(dto)).Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>();
        (await controller.RecordIdempotent(dto with { RequestId = Guid.Empty })).Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>();
        mediator.VerifyNoOtherCalls();
    }
    [Fact]
    public async Task Invoice_search_can_find_exact_id_for_payment_recovery()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "SEARCH-INV", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000004" };
        db.Add(patient); await db.SaveChangesAsync();
        var invoice = new Invoice { PatientId = patient.Id, InvoiceNumber = "PAY-RECOVERY", TotalAmount = 100, RemainingAmount = 100, Status = InvoiceStatus.Issued };
        db.Add(invoice); await db.SaveChangesAsync();
        var handler = new Sahand.ClinicPro.Application.Features.Billing.Queries.GetInvoicesQueryHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        var page = new Sahand.ClinicPro.Application.Common.Models.PagedRequest { PageNumber = 1, PageSize = 15, Search = invoice.Id.ToString() };
        var result = await handler.Handle(new Sahand.ClinicPro.Application.Features.Billing.Queries.GetInvoicesQuery(page), default);
        result.IsSuccess.Should().BeTrue();
        result.Value.Data.Should().Contain(x => x.Id == invoice.Id);
        page.Search = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToPersianDigits(invoice.Id.ToString());
        var persianResult = await handler.Handle(new Sahand.ClinicPro.Application.Features.Billing.Queries.GetInvoicesQuery(page), default);
        persianResult.Value.Data.Should().Contain(x => x.Id == invoice.Id);
    }
    [Fact]
    public async Task Invoice_preview_and_creation_reject_tracked_patient_from_another_branch()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "BRANCH-INVOICE", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000003", BranchId = 2 };
        db.Add(patient); await db.SaveChangesAsync();
        db.CurrentBranchId = 1;
        var handler = new CreateInvoiceCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), NullLogger<CreateInvoiceCommandHandler>.Instance);
        var dto = new CreateInvoiceDto(patient.Id, null, null, InvoiceType.Other, null, 0,
            new() { new InvoiceItemDto(null, "خدمت", 1, 100, 0, null, 0) });
        (await handler.PreviewAsync(new PreviewInvoiceQuery(dto), default)).IsFailure.Should().BeTrue();
        (await handler.Handle(new CreateInvoiceCommand(dto), default)).IsFailure.Should().BeTrue();
        db.Set<Invoice>().Should().BeEmpty();
    }
    [Fact]
    public async Task Reception_rejects_tracked_patient_from_another_branch()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "BRANCH-RECEPTION", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000003", BranchId = 2 };
        db.Add(patient); await db.SaveChangesAsync();
        db.CurrentBranchId = 1;
        var handler = new Sahand.ClinicPro.Application.Features.Receptions.Commands.CreateReception.CreateReceptionCommandHandler(
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            NullLogger<Sahand.ClinicPro.Application.Features.Receptions.Commands.CreateReception.CreateReceptionCommandHandler>.Instance,
            Mock.Of<IRealtimeNotifier>());
        var dto = new Sahand.ClinicPro.Application.DTOs.Receptions.CreateReceptionDto(patient.Id, null, null, null, null, null, ServiceId: 1);
        var result = await handler.Handle(new Sahand.ClinicPro.Application.Features.Receptions.Commands.CreateReception.CreateReceptionCommand(dto), default);
        result.IsFailure.Should().BeTrue();
        db.Set<Reception>().Should().BeEmpty();
        patient.LastVisitDate.Should().BeNull();
    }
    [Fact]
    public async Task Invoice_search_prioritizes_exact_id_before_similar_numbers_and_paging()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "SEARCH-RANK", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000005" };
        db.Add(patient); await db.SaveChangesAsync();
        var target = new Invoice { PatientId = patient.Id, InvoiceNumber = "UNIQUE", InvoiceDate = new DateTime(2020, 1, 1) };
        db.Add(target); await db.SaveChangesAsync();
        for (var n = 0; n < 20; n++) db.Add(new Invoice { PatientId = patient.Id, InvoiceNumber = $"INV-{target.Id}-{n}", InvoiceDate = new DateTime(2025, 1, 1) });
        await db.SaveChangesAsync();
        var handler = new Sahand.ClinicPro.Application.Features.Billing.Queries.GetInvoicesQueryHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        var page = new Sahand.ClinicPro.Application.Common.Models.PagedRequest { PageNumber = 1, PageSize = 15, Search = target.Id.ToString() };
        var result = await handler.Handle(new Sahand.ClinicPro.Application.Features.Billing.Queries.GetInvoicesQuery(page), default);
        result.Value.Data!.First().Id.Should().Be(target.Id);
    }
    [Fact]
    public async Task Invoice_details_identify_insurance_recorded_on_invoice_not_current_patient_policy()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var oldInsurance = new InsuranceCompany { Name = "بیمه فاکتور" };
        var currentInsurance = new InsuranceCompany { Name = "بیمه فعلی بیمار" };
        db.AddRange(oldInsurance, currentInsurance); await db.SaveChangesAsync();
        var patient = new Patient { MedicalRecordNo = "PRINT-INSURANCE", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000008", InsuranceCompanyId = currentInsurance.Id };
        db.Add(patient); await db.SaveChangesAsync();
        var invoice = new Invoice { PatientId = patient.Id, InsuranceCompanyId = oldInsurance.Id };
        db.Add(invoice); await db.SaveChangesAsync();
        var handler = new Sahand.ClinicPro.Application.Features.Billing.Queries.GetInvoiceByIdQueryHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        var result = await handler.Handle(new(invoice.Id), default);
        result.IsSuccess.Should().BeTrue();
        result.Value.InsuranceCompanyId.Should().Be(oldInsurance.Id);
        result.Value.InsuranceName.Should().Be("بیمه فاکتور");
    }

}

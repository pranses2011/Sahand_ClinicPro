using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sahand.ClinicPro.Application.DTOs.Insurance;
using Sahand.ClinicPro.Application.Features.Insurance;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests.Insurance;

public class InsuranceInquiryIdentityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Missing_national_code_does_not_contact_insurer_or_record_inquiry(string? nationalCode)
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"insurance-identity-{Guid.NewGuid():N}").Options;
        await using var context = new ClinicProDbContext(options);
        context.Patients.Add(new Patient { Id = 1, MedicalRecordNo = "MR-1", FirstName = "بیمار", LastName = "آزمایشی", Mobile = "09120000000", NationalCode = nationalCode });
        context.InsuranceCompanies.Add(new InsuranceCompany { Id = 5, Name = "بیمه", IsActive = true, IsContracted = true });
        await context.SaveChangesAsync();
        var unitOfWork = new UnitOfWork(context, new Mock<MediatR.IMediator>().Object);
        var insurer = new Mock<IInsuranceEligibilityService>(MockBehavior.Strict);
        var handler = new RecordInquiryCommandHandler(unitOfWork, insurer.Object);

        var result = await handler.Handle(new RecordInquiryCommand(new InsuranceInquiryDto(1, 5, null)), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INSURANCE_NATIONAL_CODE_REQUIRED");
        insurer.Verify(x => x.CheckAsync(It.IsAny<InsuranceEligibilityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        (await context.InsuranceInquiries.CountAsync()).Should().Be(0);
    }
}

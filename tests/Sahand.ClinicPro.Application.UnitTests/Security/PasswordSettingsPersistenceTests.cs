using System.Linq.Expressions;
using Moq;
using Sahand.ClinicPro.Application.Common.Caching;
using Sahand.ClinicPro.Application.Features.Settings;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Interfaces;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests.Security;

public class PasswordSettingsPersistenceTests
{
    [Fact]
    public async Task SavingAnExistingSetting_UpdatesItsCategoryForCategoryScopedReaders()
    {
        var setting = new SystemSetting
        {
            Key = "MinPasswordLength",
            Value = "8",
            Category = "General"
        };
        var repository = new Mock<IRepository<SystemSetting>>();
        repository
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<SystemSetting, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(setting);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.Repository<SystemSetting>()).Returns(repository.Object);
        unitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var cache = new Mock<IReferenceCache>();
        var handler = new SaveSettingCommandHandler(unitOfWork.Object, cache.Object);

        await handler.Handle(new SaveSettingCommand("MinPasswordLength", "4", "Security"), CancellationToken.None);

        Assert.Equal("4", setting.Value);
        Assert.Equal("Security", setting.Category);
        cache.Verify(x => x.EvictByPrefix(It.IsAny<string>()), Times.Once);
    }
}

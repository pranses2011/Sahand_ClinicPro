using System.ComponentModel.DataAnnotations;
using Sahand.ClinicPro.Application.DTOs.Auth;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests.Security;

public class ChangePasswordDtoTests
{
    [Fact]
    public void NewPassword_DoesNotEnforceAStaticMinimumLength()
    {
        var property = typeof(ChangePasswordRequestDto).GetProperty(nameof(ChangePasswordRequestDto.NewPassword));

        Assert.NotNull(property);
        Assert.Empty(property!.GetCustomAttributes(typeof(MinLengthAttribute), inherit: true));
        Assert.DoesNotContain(
            property.GetCustomAttributes(typeof(StringLengthAttribute), inherit: true),
            attribute => ((StringLengthAttribute)attribute).MinimumLength > 0);
    }
}

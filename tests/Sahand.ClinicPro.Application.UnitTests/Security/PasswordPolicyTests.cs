using Xunit;
using Sahand.ClinicPro.Application.Common.Security;

namespace Sahand.ClinicPro.Application.UnitTests.Security;

public class PasswordPolicyTests
{
    [Fact]
    public void Validate_AllowsFourCharacterPasswordWhenConfiguredRequirementsAreDisabled()
    {
        var policy = PasswordPolicy.FromSettings(new Dictionary<string, string?>
        {
            ["MinPasswordLength"] = "4",
            ["RequireMixedCase"] = "false",
            ["RequireDigit"] = "false",
            ["RequireSpecialChar"] = "false"
        });

        Assert.Null(policy.Validate("abcd"));
        Assert.Equal(4, policy.MinPasswordLength);
    }

    [Fact]
    public void Validate_EnforcesOnlyEnabledCompositionRequirements()
    {
        var policy = PasswordPolicy.FromSettings(new Dictionary<string, string?>
        {
            ["MinPasswordLength"] = "4",
            ["RequireMixedCase"] = "false",
            ["RequireDigit"] = "true",
            ["RequireSpecialChar"] = "false"
        });

        Assert.Equal("رمز عبور باید دست‌کم یک عدد داشته باشد", policy.Validate("abcd"));
        Assert.Null(policy.Validate("abc1"));
    }

    [Fact]
    public void InsuranceBillingPolicy_UsesIndependentVisitAndServiceFlags()
    {
        var policy = InsuranceBillingPolicy.FromSettings(new Dictionary<string, string?>
        {
            ["ApplyToDoctorVisits"] = "false",
            ["ApplyToMedicalServices"] = "true"
        });

        Assert.False(policy.ApplyToDoctorVisits);
        Assert.True(policy.ApplyToMedicalServices);
    }
}

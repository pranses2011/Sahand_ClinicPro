using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class FixedCanvasPrintGuardTests
{
    [Fact]
    public void All_fixed_elements_must_be_measurable_and_fit()
    {
        FixedCanvasPrintGuard.Evaluate(new[] {
            new FixedCanvasPrintGuard.Measurement(100, 40, 100, 40),
            new FixedCanvasPrintGuard.Measurement(200, 100, 120, 50)
        }).Should().Be(FixedCanvasPrintGuard.Result.Clear);
    }

    [Theory]
    [InlineData(101, 40)]
    [InlineData(100, 41)]
    public void Horizontal_or_vertical_overflow_blocks_print(int width, int height)
        => FixedCanvasPrintGuard.Evaluate(new[] { new FixedCanvasPrintGuard.Measurement(100, 40, width, height) })
            .Should().Be(FixedCanvasPrintGuard.Result.Overflow);

    [Fact]
    public void Missing_or_invalid_measurement_is_not_success()
    {
        FixedCanvasPrintGuard.Evaluate(Array.Empty<FixedCanvasPrintGuard.Measurement>()).Should().Be(FixedCanvasPrintGuard.Result.Unavailable);
        FixedCanvasPrintGuard.Evaluate(new[] { new FixedCanvasPrintGuard.Measurement(0, 40, 0, 40) }).Should().Be(FixedCanvasPrintGuard.Result.Unavailable);
        FixedCanvasPrintGuard.Evaluate(new[] { new FixedCanvasPrintGuard.Measurement(100, 40, 100, -1) }).Should().Be(FixedCanvasPrintGuard.Result.Unavailable);
    }
}

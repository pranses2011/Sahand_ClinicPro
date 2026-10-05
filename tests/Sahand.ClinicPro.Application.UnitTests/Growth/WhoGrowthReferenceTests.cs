using FluentAssertions;
using Sahand.ClinicPro.Shared.Growth;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests.Growth;

/// <summary>
/// تست‌های داده مرجع رشد WHO — گام R2
/// صحت z-score، بازه سنی، درون‌یابی و طبقه‌بندی وضعیت
/// </summary>
public class WhoGrowthReferenceTests
{
    /// <summary>z-score صفر یعنی دقیقاً روی میانه</summary>
    [Fact]
    public void ZScore_AtMedian_IsZero()
    {
        // میانه وزن پسر تازه‌متولد WHO حدود ۳/۳ کیلوگرم است
        var z = WhoGrowthReference.ZScore(GrowthIndicator.WeightForAge, true, 0, 3.3);

        z.Should().NotBeNull();
        z!.Value.Should().BeInRange(-0.05, 0.05, "مقدار روی میانه باید z نزدیک صفر بدهد");
    }

    /// <summary>دو انحراف معیار بالای میانه باید z≈+۲ بدهد</summary>
    [Fact]
    public void ZScore_TwoSdAboveMedian_IsAboutTwo()
    {
        var reference = WhoGrowthReference.Reference(GrowthIndicator.WeightForAge, true, 0);
        reference.Should().NotBeNull();

        var z = WhoGrowthReference.ZScore(GrowthIndicator.WeightForAge, true, 0,
            reference!.Value.Median + 2 * reference.Value.Sd);

        z.Should().NotBeNull();
        z!.Value.Should().BeInRange(1.9, 2.1);
    }

    /// <summary>قد دختر ۲۴ ماهه روی میانه WHO (حدود ۸۶/۴) z نزدیک صفر است</summary>
    [Fact]
    public void ZScore_GirlsHeightAtMedian_IsZero()
    {
        var z = WhoGrowthReference.ZScore(GrowthIndicator.HeightForAge, false, 24, 86.4);

        z.Should().NotBeNull();
        z!.Value.Should().BeInRange(-0.1, 0.1);
    }

    /// <summary>محیط سر خارج بازه ۳۶ ماه باید تهی باشد</summary>
    [Fact]
    public void ZScore_HeadCircumference_After36Months_IsNull()
    {
        var z = WhoGrowthReference.ZScore(GrowthIndicator.HeadCircumferenceForAge, true, 48, 50);
        z.Should().BeNull("شاخص محیط سر فقط تا ۳۶ ماه تعریف شده است");
    }

    /// <summary>درون‌یابی بین لنگرها باید پیوسته و بی‌جهش باشد</summary>
    [Fact]
    public void Reference_InterpolatesSmoothly_BetweenAnchors()
    {
        var at5 = WhoGrowthReference.Reference(GrowthIndicator.WeightForAge, true, 5);
        var at6 = WhoGrowthReference.Reference(GrowthIndicator.WeightForAge, true, 6);

        at5.Should().NotBeNull();
        at6.Should().NotBeNull();
        at5!.Value.Median.Should().BeLessThan(at6!.Value.Median, "میانه وزن با سن صعودی است");

        // z-score در سن لنگر روی میانه دقیقاً صفر است
        var z = WhoGrowthReference.ZScore(GrowthIndicator.WeightForAge, true, 5, at5.Value.Median);
        z.Should().NotBeNull().And.BeInRange(-0.05, 0.05);
    }

    /// <summary>طبقه‌بندی وضعیت — مرزهای چهارگانه</summary>
    [Theory]
    [InlineData(0.5, GrowthStatus.Normal)]
    [InlineData(1.5, GrowthStatus.Borderline)]
    [InlineData(2.5, GrowthStatus.Alert)]
    [InlineData(3.5, GrowthStatus.Critical)]
    public void Classify_CoversFourZones(double z, GrowthStatus expected)
    {
        var (status, _, _) = WhoGrowthReference.Classify(z, GrowthIndicator.WeightForAge);
        status.Should().Be(expected);
    }

    /// <summary>BMI مثبت شدید باید «چاقی» گزارش کند نه «بالاتر از حد»</summary>
    [Fact]
    public void Classify_HighBmi_ReportsObesity()
    {
        var (status, text, _) = WhoGrowthReference.Classify(2.8, GrowthIndicator.BmiForAge);
        status.Should().Be(GrowthStatus.Alert);
        text.Should().Contain("اضافه‌وزن");
    }

    /// <summary>بازه سنی شاخص‌ها — محور افقی نمودار</summary>
    [Fact]
    public void AgeRange_MatchesIndicatorDefinition()
    {
        WhoGrowthReference.AgeRange(GrowthIndicator.HeadCircumferenceForAge).ToMonth.Should().Be(36);
        WhoGrowthReference.AgeRange(GrowthIndicator.WeightForAge).ToMonth.Should().Be(60);
        WhoGrowthReference.AgeRange(GrowthIndicator.BmiForAge).ToMonth.Should().Be(60);
    }

    /// <summary>جنسیت‌ها جدایند — میانه پسر و دختر تازه‌متولد متفاوت است</summary>
    [Fact]
    public void Reference_DistinguishesSexes()
    {
        var boys = WhoGrowthReference.Reference(GrowthIndicator.WeightForAge, true, 0);
        var girls = WhoGrowthReference.Reference(GrowthIndicator.WeightForAge, false, 0);

        boys.Should().NotBeNull();
        girls.Should().NotBeNull();
        boys!.Value.Median.Should().BeGreaterThan(girls!.Value.Median,
            "میانه وزن هنگام تولد پسران WHO بالاتر از دختران است");
    }
}

using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

/// <summary>
/// تست‌های کاتالوگ بسته آموزش آنلاین تعاملی — چرخه سیزدهم (درخواست ۸-۶):
/// ۱۰ درس کامل با HTML راست‌به‌چپ گام‌به‌گام و ۸ ایستگاه تور معتبر
/// </summary>
public class TrainingCatalogTests
{
    [Fact]
    public void Lessons_HaveTenCompleteInteractiveCourses()
    {
        TrainingCatalog.Lessons.Should().HaveCount(10, "بسته آموزش ۱۰ درس دارد");

        foreach (var lesson in TrainingCatalog.Lessons)
        {
            lesson.Id.Should().NotBeNullOrWhiteSpace();
            lesson.Title.Should().NotBeNullOrWhiteSpace();
            lesson.Minutes.Should().BeInRange(2, 15, "مدت درس‌ها منطقی است");
            lesson.Html.Should().NotBeNullOrWhiteSpace();
            // HTML درس گام‌به‌گام است — حداقل دو گام و یک نکته/هشدار دارد
            lesson.Html.Should().Contain("<!DOCTYPE html>").And.Contain("dir='rtl'")
                .And.Contain("گام ۱").And.Contain("گام ۲",
                because: "هر درس راست‌به‌چپ و گام‌به‌گام است");
            lesson.Html.Should().Contain("class='tip'", "هر درس دست‌کم یک نکته کارگاهی دارد");
        }

        // شناسه‌ها یکتا
        TrainingCatalog.Lessons.Select(l => l.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Lessons_CoverAllCoreWorkflows()
    {
        var ids = TrainingCatalog.Lessons.Select(l => l.Id).ToHashSet();
        // پذیرش بیمار، ثبت نوبت، پذیرش حضوری، ویزیت، نسخه‌نویسی، صورتحساب، پرداخت، گزارش‌ها، بکاپ، لایسنس
        ids.Should().BeEquivalentTo(new[]
        {
            "reception", "appointment", "reception-desk", "visit", "prescription",
            "billing", "payment", "reports", "backup", "license",
        });
    }

    [Fact]
    public void Find_LooksUpById()
    {
        var lesson = TrainingCatalog.Find("prescription");
        lesson.Should().NotBeNull();
        lesson!.Title.Should().Contain("نسخه");
        TrainingCatalog.Find("nonexistent").Should().BeNull();
    }

    [Fact]
    public void TourSteps_HaveEightStations_WithContent()
    {
        TourCatalog.Steps.Should().HaveCount(8, "تور هدایت‌شده ۸ ایستگاه دارد");
        TourCatalog.Steps.Should().OnlyContain(s =>
            !string.IsNullOrWhiteSpace(s.Title) &&
            !string.IsNullOrWhiteSpace(s.Message));

        // ایستگاه نخست خوش‌آمد بدون هدف است؛ بقیه به عناصر اصلی اشاره می‌کنند
        TourCatalog.Steps[0].TargetName.Should().BeNull();
        TourCatalog.Steps.Count(s => s.TargetName is not null).Should().Be(7,
            "هفت ایستگاه دارای هایلایت نقطه‌ای روی عنصر هدف‌اند");
    }
}

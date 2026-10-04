using FluentAssertions;
using Sahand.ClinicPro.Application.Common.Helpers;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests;

/// <summary>
/// تست‌های راهنمای تقویم شمسی — دقت تبدیل دوطرفه جلالی و میلادی
/// نقاط مرزی: کبیسه، نوروز، پایان اسفند
/// </summary>
public class PersianDateHelperTests
{
    /// <summary>تاریخ‌های مرجع شمسی شناخته‌شده — از تقویم رسمی</summary>
    public static readonly TheoryData<string, DateTime> KnownDates = new()
    {
        // نوروز ۱۴۰۳ برابر با اعتدال بهاری ۲۰۲۴
        { "1403/01/01", new DateTime(2024, 3, 20) },
        // روز میانی سال
        { "1403/07/12", new DateTime(2024, 10, 3) },
        // پایان اسفند ۱۴۰۳ — سال کبیسه ۳۶۶ روزه شمسی
        { "1403/12/30", new DateTime(2025, 3, 20) },
        // نوروز ۱۴۰۴
        { "1404/01/01", new DateTime(2025, 3, 21) },
        // نمونه سال گذشته
        { "1402/05/15", new DateTime(2023, 8, 6) },
        // نمونه دهه ۹۰ شمسی
        { "1399/11/22", new DateTime(2021, 2, 10) }
    };

    [Theory]
    [MemberData(nameof(KnownDates))]
    public void ToShamsi_KnownDates_Should_Match(string expected, DateTime gregorian)
    {
        // عملیات: تبدیل میلادی به شمسی
        var result = PersianDateHelper.ToShamsi(gregorian);

        // نتیجه: تطابق با تقویم رسمی
        result.Should().Be(expected,
            because: $"تاریخ {gregorian:yyyy-MM-dd} باید {expected} باشد");
    }

    [Theory]
    [MemberData(nameof(KnownDates))]
    public void ToGregorian_KnownDates_Should_Match(string shamsi, DateTime expected)
    {
        // عملیات: تبدیل رشته شمسی به میلادی
        var result = PersianDateHelper.ToGregorian(shamsi);

        // نتیجه: تطابق با تقویم رسمی
        result.Should().Be(expected,
            because: $"تاریخ شمسی {shamsi} باید {expected:yyyy-MM-dd} باشد");
    }

    [Fact]
    public void RoundTrip_Should_Preserve_Date()
    {
        // آماده‌سازی: بازه ده سال اخیر روز به روز
        var start = new DateTime(2016, 3, 20);

        for (var day = 0; day < 3650; day += 37)
        {
            var date = start.AddDays(day);

            // عملیات: تبدیل به شمسی و بازگشت
            var shamsi = PersianDateHelper.ToShamsi(date);
            var back = PersianDateHelper.ToGregorian(shamsi);

            // نتیجه: تاریخ اولیه بازگشته شود
            back.Should().Be(date,
                because: $"رفت و برگشت برای {date:yyyy-MM-dd} باید بدون تغییر باشد");
        }
    }

    [Fact]
    public void ToGregorian_InvalidInput_Should_Return_Null()
    {
        // عملیات و نتیجه: ورودی‌های نامعتبر تهی برمی‌گردند
        PersianDateHelper.ToGregorian(null).Should().BeNull();
        PersianDateHelper.ToGregorian("").Should().BeNull();
        PersianDateHelper.ToGregorian("1403/13/01").Should().BeNull(because: "ماه سیزدهم وجود ندارد");
        PersianDateHelper.ToGregorian("abc").Should().BeNull();
    }

    [Fact]
    public void CalculateAge_Should_Compute_Correct_Age()
    {
        // آماده‌سازی: تاریخ تولد دقیقاً ۳۰ سال پیش
        var birth = DateTime.UtcNow.AddYears(-30).AddDays(1);

        // عملیات: محاسبه سن
        var age = PersianDateHelper.CalculateAge(birth);

        // نتیجه: سن ۲۹ سال (یک روز تا تولد ۳۰ سالگی مانده)
        age.Should().Be(29);
    }
}

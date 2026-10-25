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

    // ═══════════════════════════════════════════════════════════
    // بازه ماه شمسی — چرخه دهم (+ رفع باگ فرمول سال چرخه نهم)
    // ═══════════════════════════════════════════════════════════

    [Theory]
    [InlineData(1404, 1, 2025, 3)]  // فروردین → مارس ۲۰۲۵
    [InlineData(1404, 7, 2025, 9)]  // مهر → سپتامبر ۲۰۲۵
    [InlineData(1404, 10, 2025, 12)] // دی → دسامبر ۲۰۲۵
    [InlineData(1404, 11, 2026, 1)] // بهمن → ژانویه سال بعد
    [InlineData(1403, 12, 2025, 2)] // اسفند ۱۴۰۳ → فوریه ۲۰۲۵
    public void MonthRange_Should_Map_JalaliMonth_To_GregorianMonth(
        int jalaliYear, int jalaliMonth, int expectedGYear, int expectedGMonth)
    {
        var (from, to) = PersianDateHelper.MonthRange(jalaliYear, jalaliMonth);

        from.Year.Should().Be(expectedGYear,
            because: $"ماه {jalaliMonth} سال {jalaliYear} شمسی باید در {expectedGYear} میلادی بیفتد");
        from.Month.Should().Be(expectedGMonth);

        // چرخهٔ ۸۱: بازه [از، تا) دقیقاً «اول این ماه شمسی تا اول ماه شمسی بعد»
        // است؛ معادلِ «from.AddMonths(1)» فقط یک تقریب میلادی بود و در
        // ماه‌هایی که مرزشان با مرز ماه میلادی یکی نیست (اسفند/دی/بهمن) شکست
        // می‌خورد — طول ماه شمسی ۲۹ تا ۳۱ روز است، نه ۲۸ تا ۳۱ روز میلادی.
        var nextMonth = jalaliMonth == 12 ? 1 : jalaliMonth + 1;
        var nextYear = jalaliMonth == 12 ? jalaliYear + 1 : jalaliYear;
        to.Should().Be(DateOnly.FromDateTime(PersianDateHelper.ToGregorian(nextYear, nextMonth, 1)),
            because: "پایان بازه، اولِ ماه شمسی بعد است");
        (to.DayNumber - from.DayNumber).Should().BeInRange(29, 31,
            because: "هر ماه شمسی ۲۹ تا ۳۱ روز است");
    }

    [Fact]
    public void MonthRange_Mehr1404_Should_Use_ExactPersianMonthBoundaries()
    {
        // مهر ۱۴۰۴ دقیقاً از ۲۳ سپتامبر تا ۲۳ اکتبر ۲۰۲۵ است.
        var (from, to) = PersianDateHelper.MonthRange(1404, 7);

        from.Should().Be(new DateOnly(2025, 9, 23));
        to.Should().Be(new DateOnly(2025, 10, 23));

        // فاکتور ۵ اکتبر داخل بازه و ۱۵ اوت خارج از آن است
        var sample = new DateTime(2025, 10, 5);
        (sample >= from.ToDateTime(TimeOnly.MinValue) &&
         sample < to.ToDateTime(TimeOnly.MinValue)).Should().BeTrue();
    }

    [Fact]
    public void MonthRange_Should_Produce_SensibleGregorianYears()
    {
        // رگرسیون باگ چرخه نهم: فرمول «سال − ۶۲۲» سال ۷۸۲ میلادی می‌ساخت؛
        // هر سال شمسی معتبر باید به بازه میلادی قرن ۲۱/۲۲ بیفتد
        for (var year = 1400; year <= 1410; year++)
        {
            for (var month = 1; month <= 12; month++)
            {
                var (from, _) = PersianDateHelper.MonthRange(year, month);
                from.Year.Should().BeInRange(2021, 2032,
                    because: $"بازه {year}/{month} شمسی باید معنادار باشد");
            }
        }
    }

    [Fact]
    public void MonthRange_InvalidMonthOrYear_Should_Throw()
    {
        var act1 = () => PersianDateHelper.MonthRange(1404, 13);
        var act2 = () => PersianDateHelper.MonthRange(1200, 7);

        act1.Should().Throw<ArgumentOutOfRangeException>(because: "ماه سیزدهم وجود ندارد");
        act2.Should().Throw<ArgumentOutOfRangeException>(because: "سال خارج از بازه معتبر");
    }

    /// <summary>
    /// رگرسیون باگ تقویم نوبت‌دهی: رشته‌ای که از قبل ارقام فارسی دارد
    /// (خروجی ToShamsiFa) نباید با تبدیل دوبارهٔ ارقام خطا بدهد — پیش‌تر
    /// char.IsDigit ارقام فارسی را هم رقم می‌دانست و ایندکس آرایه بیرون
    /// می‌زد و خانه‌های تقویم بی‌صدا به حالت جایگزین بدون کلیک می‌افتادند.
    /// </summary>
    [Fact]
    public void ToPersianDigits_AlreadyPersianDigits_Should_NotThrow_And_StayUnchanged()
    {
        var fa = PersianDateHelper.ToShamsiFa(new DateTime(2026, 10, 24));

        var act = () => PersianDateHelper.ToPersianDigits(fa);
        act.Should().NotThrow();
        PersianDateHelper.ToPersianDigits(fa).Should().Be(fa);
    }

    [Fact]
    public void ToPersianDigits_MixedLatinDigits_Should_Convert_OnlyLatin()
    {
        PersianDateHelper.ToPersianDigits("1405/08/03 — 2 نوبت از 21")
            .Should().Be("۱۴۰۵/۰۸/۰۳ — ۲ نوبت از ۲۱");
    }
}

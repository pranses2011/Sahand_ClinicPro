using FluentAssertions;
using Sahand.ClinicPro.Application.Common.Helpers;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests;

/// <summary>
/// آزمون‌های کمکی تقویم شمسیِ انتخابگر تاریخ سه‌نمایی (چرخهٔ ۸۱)
/// </summary>
/// <remarks>
/// چک‌لیست پذیرش طراحی: تطابق شمسی/میلادی در هر خانه، شروع هفته از شنبه،
/// برچسب بازهٔ میلادی ماه‌ها با عدد ماه در پرانتز، بازهٔ ثابت ۱۲سالهٔ سال‌ها
/// و طول ماه‌ها/سال کبیسه از <see cref="System.Globalization.PersianCalendar"/>.
/// </remarks>
public class JalaliCalendarTests
{
    /// <summary>نمونهٔ کنترلی طراحی: ۱ آبان ۱۴۰۵ = جمعه ۲۳ اکتبر ۲۰۲۶</summary>
    [Fact]
    public void Aban1_1405_Should_Be_Friday_23_October_2026()
    {
        var gregorian = JalaliCalendar.ToGregorian(1405, 8, 1);

        gregorian.Should().Be(new DateTime(2026, 10, 23),
            because: "نمونهٔ کنترلی مشخصات طراحی است");
        JalaliCalendar.IsFriday(gregorian).Should().BeTrue(
            because: "۱ آبان ۱۴۰۵ جمعه است و باید در ستون قرمز جمعه بیفتد");
    }

    /// <summary>ستون هفته راست‌به‌چپ: شنبه = ۰ … جمعه = ۶</summary>
    [Fact]
    public void WeekColumn_Should_Start_Saturday_At_Zero()
    {
        // ۱۴۰۵/۰۱/۰۱ = شنبه ۲۱ مارس ۲۰۲۶ (نوروز)
        JalaliCalendar.WeekColumn(new DateTime(2026, 3, 21)).Should().Be(0);
        JalaliCalendar.WeekColumn(new DateTime(2026, 3, 22)).Should().Be(1);
        JalaliCalendar.WeekColumn(new DateTime(2026, 10, 23)).Should().Be(6);
    }

    /// <summary>طول ماه‌ها از تقویم مرجع — اسفند ۱۴۰۳ سی‌روزه و اسفند ۱۴۰۵ بیست‌ونه‌روزه</summary>
    [Theory]
    [InlineData(1403, 12, 30)]
    [InlineData(1405, 12, 29)]
    [InlineData(1405, 1, 31)]
    [InlineData(1405, 7, 30)]
    public void MonthLength_Should_Come_From_PersianCalendar(int year, int month, int expected)
    {
        JalaliCalendar.GetMonthLength(year, month).Should().Be(expected);
    }

    /// <summary>کبیسه‌بودن سال از تقویم مرجع خوانده می‌شود</summary>
    [Fact]
    public void LeapYear_Should_Match_PersianCalendar()
    {
        JalaliCalendar.IsLeapYear(1403).Should().BeTrue("اسفند ۱۴۰۳ سی روز دارد");
        JalaliCalendar.IsLeapYear(1405).Should().BeFalse("اسفند ۱۴۰۵ بیست‌ونه روز دارد");
    }

    /// <summary>برچسب بازهٔ میلادیِ نمای روز — ماهِ داخل یک سال میلادی</summary>
    [Fact]
    public void MonthRangeLabel_Should_Include_Gregorian_Month_Numbers()
    {
        JalaliCalendar.MonthRangeLabel(1405, 8)
            .Should().Be("Oct (10) – Nov (11) 2026");
    }

    /// <summary>برچسب بازهٔ میلادیِ نمای روز — ماهی که از مرز سال میلادی می‌گذرد</summary>
    [Fact]
    public void MonthRangeLabel_Should_Split_On_Gregorian_Year_Change()
    {
        JalaliCalendar.MonthRangeLabel(1405, 10)
            .Should().Be("Dec (12) 2026 – Jan (1) 2027");
    }

    /// <summary>زیرنویس ثابت هر ماه شمسی — همان جدول مشخصات طراحی</summary>
    [Fact]
    public void MonthSubtitles_Should_Match_Design_Table()
    {
        JalaliCalendar.MonthGregorianSubtitles.Should().HaveCount(12);
        JalaliCalendar.MonthGregorianSubtitles[0].Should().Be("Mar (3)–Apr (4)");
        JalaliCalendar.MonthGregorianSubtitles[7].Should().Be("Oct (10)–Nov (11)");
        JalaliCalendar.MonthGregorianSubtitles[11].Should().Be("Feb (2)–Mar (3)");
    }

    /// <summary>بازهٔ نمایش سال — «2026 – 2027» برای ۱۴۰۵</summary>
    [Fact]
    public void YearRangeLabel_Should_Span_Two_Gregorian_Years()
    {
        JalaliCalendar.YearRangeLabel(1405).Should().Be("2026 – 2027");
    }

    /// <summary>بازهٔ ثابت ۱۲ساله: شروع = سال − ((سال − ۱۴۰۰) mod 12)</summary>
    [Theory]
    [InlineData(1400, 1400)]
    [InlineData(1405, 1400)]
    [InlineData(1411, 1400)]
    [InlineData(1412, 1412)]
    [InlineData(1424, 1424)]
    [InlineData(1399, 1388)]
    public void DecadeStart_Should_Follow_Formula(int year, int expected)
    {
        JalaliCalendar.DecadeStart(year).Should().Be(expected);
    }

    /// <summary>عنوان بازهٔ میلادیِ نمای سال — «2021 – 2033» برای ۱۴۰۰ تا ۱۴۱۱</summary>
    [Fact]
    public void DecadeRangeLabel_Should_Cover_Twelve_Years()
    {
        JalaliCalendar.DecadeRangeLabel(1400).Should().Be("2021 – 2033");
    }

    /// <summary>معادل میلادیِ زیر فیلد — «Oct 23, 2026  (2026-10-23)»</summary>
    [Fact]
    public void GregorianCaption_Should_Show_Both_Human_And_Iso_Form()
    {
        JalaliCalendar.GregorianCaption(new DateTime(2026, 10, 23))
            .Should().Be("Oct 23, 2026  (2026-10-23)");
    }

    /// <summary>نمایش شمسی با ارقام فارسی</summary>
    [Fact]
    public void ToShamsiFa_Should_Use_Persian_Digits()
    {
        JalaliCalendar.ToShamsiFa(new DateTime(2026, 2, 28)).Should().Be("۱۴۰۴/۱۲/۰۹");
        JalaliCalendar.ToShamsiFa(new DateTime(2026, 10, 23)).Should().Be("۱۴۰۵/۰۸/۰۱");
    }

    /// <summary>رفت‌وبرگشت همهٔ روزهای یک سال — سازگاری طول ماه و کبیسه</summary>
    [Fact]
    public void RoundTrip_Should_Be_Stable_For_Whole_Leap_Year()
    {
        var start = DateTime.Today;
        for (var i = 0; i < 400; i++)
        {
            var date = start.AddDays(i);
            var (jy, jm, jd) = JalaliCalendar.ToJalali(date);
            JalaliCalendar.ToGregorian(jy, jm, jd).Should().Be(date.Date);
        }
    }
    [Fact]
    public void Iran_day_bounds_include_iran_midnight_and_exclude_the_next_midnight()
    {
        var instant = new DateTime(2026, 1, 1, 21, 0, 0, DateTimeKind.Utc);
        var (start, end) = PersianDateHelper.IranDayUtcBounds(instant);
        start.Should().Be(new DateTime(2026, 1, 1, 20, 30, 0, DateTimeKind.Utc));
        end.Should().Be(new DateTime(2026, 1, 2, 20, 30, 0, DateTimeKind.Utc));
        PersianDateHelper.IranDayUtcBounds(start.AddTicks(-1)).End.Should().Be(start);
        PersianDateHelper.IranDayUtcBounds(end).Start.Should().Be(end);
    }

    [Fact]
    public void Stored_utc_dates_use_iran_day_even_when_sql_loses_datetime_kind()
    {
        var beforeMidnight = new DateTime(2026, 3, 20, 20, 29, 0, DateTimeKind.Unspecified);
        var afterMidnight = new DateTime(2026, 3, 20, 20, 30, 0, DateTimeKind.Unspecified);
        PersianDateHelper.StoredUtcToIranShamsi(beforeMidnight).Should().Be("1404/12/29");
        PersianDateHelper.StoredUtcToIranShamsi(afterMidnight).Should().Be("1405/01/01");
        PersianDateHelper.IranTimeFromStoredUtc(afterMidnight).Date.Should().Be(new DateTime(2026, 3, 21));
    }

}

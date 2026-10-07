using FluentAssertions;
using Sahand.ClinicPro.Application.Common.Helpers;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests;

/// <summary>
/// تست‌های گارد چرخه هجدهم (گزارش دوازدهم مالک) —
/// ۱) نشانی سرور لایسنس: پیش‌فرض رسمی + نرمال‌سازی ریشه ← api.php + اعتبارسنجی
/// ۲) ارقام فارسی: ToShamsiFa و ToClockFa و بازگشت‌پذیری ToShamsi (پارس‌شونده)
/// </summary>
public class LicensePortalHelperTests
{
    [Fact]
    public void پیش‌فرض_نشانی_سرور_لایسنس_نشانی_رسمی_محصول_است()
    {
        // عملیات: نرمال‌سازی تهی → پیش‌فرض
        var result = LicensePortalHelper.NormalizeApiUrl("");

        // نتیجه: endpoint کامل پیش‌فرض رسمی — رفع خطای «PortalUrl تنظیم نشده»
        result.Should().Be("https://clinic-license.ea-fixer.ir/api.php");
        result.Should().StartWith(LicensePortalHelper.DefaultPortalRoot,
            because: "پیش‌فرض رسمی مالک است");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("https://clinic-license.ea-fixer.ir")]
    [InlineData("https://clinic-license.ea-fixer.ir/")]
    public void نشانی_ریشه_خودکار_به_api_php_کامل_می‌شود(string? input)
    {
        // عملیات: نرمال‌سازی نشانی ریشه
        var result = LicensePortalHelper.NormalizeApiUrl(input);

        // نتیجه: api.php الصاق شده — کاربر لازم نیست endpoint کامل بداند
        result.Should().Be("https://clinic-license.ea-fixer.ir/api.php");
    }

    [Theory]
    [InlineData("clinic-license.ea-fixer.ir")]
    [InlineData("clinic-license.ea-fixer.ir/")]
    public void نشانی_بدون_طرح_https_می‌گیرد(string input)
    {
        // عملیات: نرمال‌سازی بدون طرح
        var result = LicensePortalHelper.NormalizeApiUrl(input);

        // نتیجه: طرح https ضمیمه شده و api.php هم دارد
        result.Should().Be("https://clinic-license.ea-fixer.ir/api.php");
    }

    [Fact]
    public void نشانی_کامل_api_php_دست_نخورده_برمی‌گردد()
    {
        // عملیات: نشانی کامل endpoint
        var result = LicensePortalHelper.NormalizeApiUrl("https://license.example.com/api.php");

        // نتیجه: بدون تغییر
        result.Should().Be("https://license.example.com/api.php");
    }

    [Theory]
    [InlineData("https://valid.example.com", true)]
    [InlineData("http://valid.example.com/api.php", true)]
    [InlineData("ftp://invalid.example.com", false)]
    [InlineData("not a url at all", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void اعتبارسنجی_نشانی_http_ها(string? input, bool expected)
    {
        // عملیات + نتیجه
        LicensePortalHelper.IsValidHttpUrl(input).Should().Be(expected);
    }
}

/// <summary>تست‌های ارقام فارسی — چرخه هجدهم (گزارش دوازدهم مالک)</summary>
public class PersianDigitsTests
{
    [Fact]
    public void تاریخ_شمسی_فارسی_رقم_همه_ارقام_فارسی_دارند()
    {
        // عملیات: 2024-10-03 = ۱۴۰۳/۰۷/۱۲
        var result = PersianDateHelper.ToShamsiFa(new DateTime(2024, 10, 3));

        // نتیجه: هیچ رقم لاتینی باقی نمانده
        result.Should().Be("۱۴۰۳/۰۷/۱۲");
        result.Any(c => c is >= '0' and <= '9').Should().BeFalse(
            because: "هیچ رقم لاتینی نباید باقی بماند");
    }

    [Fact]
    public void ساعت_فارسی_رقم_دو_نقطه_و_ارقام_فارسی_است()
    {
        // عملیات: ساعت مشخص UTC → تبدیل به وقت ایران
        var utc = new DateTime(2024, 10, 3, 10, 30, 5, DateTimeKind.Utc);
        var result = PersianDateHelper.ToClockFa(utc);

        // نتیجه: ۱۴:۰۰:۰۵ (ایران UTC+3:30) — بدون رقم لاتین
        result.Should().Be("۱۴:۰۰:۰۵");
        result.Any(c => c is >= '0' and <= '9').Should().BeFalse(
            because: "هیچ رقم لاتینی نباید باقی بماند");
    }

    [Fact]
    public void ساعت_بدون_ثانیه_فقط_ساعت_و_دقیقه_است()
    {
        // عملیات
        var utc = new DateTime(2024, 10, 3, 10, 30, 5, DateTimeKind.Utc);
        var result = PersianDateHelper.ToClockFa(utc, withSeconds: false);

        // نتیجه: ۱۴:۰۰
        result.Should().Be("۱۴:۰۰");
    }

    [Fact]
    public void خروجی_فارسی_رقم_برگشت‌پذیر_با_پارس_لایتن_نیست_پس_ToShamsi_دست_نخورده_ماند()
    {
        // عملیات: ToShamsi لاتین می‌ماند (ورودی فیلترهای تاریخ و ToGregorian)
        var latin = PersianDateHelper.ToShamsi(new DateTime(2024, 10, 3));
        var fa = PersianDateHelper.ToShamsiFa(new DateTime(2024, 10, 3));

        // نتیجه: دو خروجی جدا — پارس‌شونده لاتین، نمایشی فارسی
        latin.Should().Be("1403/07/12");
        PersianDateHelper.ToGregorian(latin).Should().Be(new DateTime(2024, 10, 3),
            because: "ورودی‌های تاریخ همچنان لاتین پارس می‌شوند");
        fa.Should().NotBe(latin);
    }

    [Theory]
    [InlineData("09:30", "۰۹:۳۰")]
    [InlineData("1403/07/12", "۱۴۰۳/۰۷/۱۲")]
    [InlineData("abc", "abc")]
    public void تبدیل_متن_به_ارقام_فارسی(string input, string expected)
    {
        // عملیات + نتیجه
        PersianDateHelper.ToPersianDigits(input).Should().Be(expected);
    }
}

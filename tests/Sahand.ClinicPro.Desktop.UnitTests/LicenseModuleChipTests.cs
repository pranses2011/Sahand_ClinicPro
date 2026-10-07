using FluentAssertions;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

/// <summary>
/// تست گارد چیپس ماژول‌های لایسنس — چرخه هجدهم (گزارش دوازدهم مالک):
/// «همه چیپس ها نوشته غیر فعال رو نشون میدن که باید اسم ماژول رو هم نشون بدن
/// و با فعال شدنشون رنگشون هم سبز بشه»
/// </summary>
public class LicenseModuleChipTests
{
    [Fact]
    public void چیپس_فعال_نام_ماژول_و_وضعیت_فعال_را_نشان_می‌دهد()
    {
        // عملیات: ردیف ماژول فعال
        var row = new LicenseModuleRow { Title = "بیماران", IsEnabled = true };

        // نتیجه: نام + وضعیت + رنگ سبز
        row.DisplayText.Should().Be("بیماران • ✅ فعال");
        row.ChipBackground.Should().Be("#E8F5E9");
        row.ChipForeground.Should().Be("#1B5E20");
        row.ChipBorder.Should().Be("#A5D6A7");
    }

    [Fact]
    public void چیپس_غیرفعال_نام_ماژول_و_وضعیت_غیرفعال_را_نشان_می‌دهد()
    {
        // عملیات: ردیف ماژول غیرفعال
        var row = new LicenseModuleRow { Title = "داروخانه", IsEnabled = false };

        // نتیجه: نام + وضعیت + رنگ خاکستری (نه فقط «غیرفعال» بی‌نام مثل قبل)
        row.DisplayText.Should().Be("داروخانه • غیرفعال");
        row.ChipBackground.Should().Be("#F5F5F5");
        row.ChipForeground.Should().Be("#9E9E9E");
    }

    [Fact]
    public void تغییر_وضعیت_رنگ_و_متن_چیپس_را_تازه_می‌کند()
    {
        // عملیات: ردیف ساخته‌شده و بعد فعال می‌شود
        var row = new LicenseModuleRow { Title = "آزمایشگاه", IsEnabled = false };
        var before = row.DisplayText;
        row.IsEnabled = true;

        // نتیجه: متن و رنگ تازه شده‌اند (به‌روزرسانی PropertyChanged)
        before.Should().Be("آزمایشگاه • غیرفعال");
        row.DisplayText.Should().Be("آزمایشگاه • ✅ فعال");
        row.ChipBackground.Should().Be("#E8F5E9");
    }
}

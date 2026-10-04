using FluentAssertions;
using Xunit;

namespace Sahand.ClinicPro.Domain.UnitTests;

/// <summary>
/// تست‌های دودی اسمبلی لایه دامنه — گام ۱.
/// این تست‌ها صحت اتصال پروژه تست به لایه دامنه را تأیید می‌کنند؛
/// تست‌های واقعی موجودیت‌ها و قواعد تجاری در گام ۳۰ نوشته می‌شوند.
/// </summary>
public class DomainAssemblyTests
{
    /// <summary>
    /// اسمبلی دامنه باید با نام و هویت صحیح در دسترس باشد.
    /// </summary>
    [Fact]
    public void Domain_Assembly_Should_Be_Referenced_With_Correct_Name()
    {
        // عملیات: خواندن نام اسمبلی از روی نوع نشانگر لایه دامنه
        var assemblyName = typeof(DomainAssemblyMarker).Assembly.GetName().Name;

        // نتیجه مورد انتظار: نام دقیق پروژه با پیشوند Sahand.ClinicPro
        assemblyName.Should().Be("Sahand.ClinicPro.Domain",
            because: "قاعده نام‌گذاری پروژه رعایت شده است");
    }

    /// <summary>
    /// لایه دامنه نباید هیچ وابستگی پکیج خارجی نداشته باشد (قانون معماری تمیز).
    /// تنها اسمبلیهای مجاز: خود دامنه و اسمبلیهای سیستمی .NET.
    /// </summary>
    [Fact]
    public void Domain_Should_Not_Reference_External_Packages()
    {
        // عملیات: فهرست اسمبلی‌های ارجاع‌شده در لایه دامنه
        var referencedAssemblies = typeof(DomainAssemblyMarker).Assembly.GetReferencedAssemblies();

        // نتیجه مورد انتظار: هیچ ارجاعی به پکیج‌های شخص ثالث وجود نداشته باشد
        // (فقط اسمبلیهای سیستمی مثل System.Runtime یا netstandard مجازند)
        referencedAssemblies
            .Should()
            .OnlyContain(name => name.Name!.StartsWith("System") ||
                                 name.Name == "netstandard" ||
                                 name.Name == "Microsoft.CSharp" ||
                                 name.Name == "Sahand.ClinicPro.Domain",
                because: "لایه دامنه در معماری تمیز نباید وابستگی خارجی داشته باشد");
    }
}

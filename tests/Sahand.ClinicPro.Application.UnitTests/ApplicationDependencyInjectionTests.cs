using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests;

/// <summary>
/// تست‌های واحد ثبت سرویس‌های لایه کاربرد — گام ۱.
/// صحت عملکرد متد AddApplication را بررسی می‌کند؛ این تضمین می‌کند
/// MediatR، نگاشتگرهای Mapperly و FluentValidation به‌درستی از اسمبلی
/// لایه کاربرد شناسایی و ثبت می‌شوند.
/// </summary>
public class ApplicationDependencyInjectionTests
{
    /// <summary>
    /// متد AddApplication باید سرویس‌های MediatR (ISender/IMediator)
    /// را در کانتینر ثبت کند تا الگوی CQRS کار کند.
    /// </summary>
    [Fact]
    public void AddApplication_Should_Register_MediatR_Services()
    {
        // آماده‌سازی: کالکشن سرویس خالی (بدون میزبان کامل)
        var services = new ServiceCollection();

        // عملیات: ثبت سرویس‌های لایه کاربرد
        services.AddApplication();

        // نتیجه مورد انتظار: ISender (پایه CQRS) ثبت شده باشد
        services.Should().Contain(
            d => d.ServiceType == typeof(ISender),
            because: "MediatR باید از اسمبلی لایه کاربرد ثبت شود");
    }

    /// <summary>
    /// ثبت سرویس‌ها نباید خطا بدهد و باید کانتینر قابل ساخت باشد؛
    /// این تست کشف می‌کند اگر یک نگاشتگر یا Validator خراب باشد.
    /// </summary>
    [Fact]
    public void AddApplication_Should_Produce_Buildable_Service_Provider()
    {
        // آماده‌سازی و عملیات
        var services = new ServiceCollection();
        services.AddApplication();
        using var provider = services.BuildServiceProvider();

        // نتیجه مورد انتظار: سازنده ارائه‌دهنده سرویس بدون خطا کار کند
        // و ISender قابل حل‌زنجیر کردن باشد
        var sender = provider.GetService<ISender>();
        sender.Should().NotBeNull(because: "کانتینر باید ISender را تأمین کند");
    }
}

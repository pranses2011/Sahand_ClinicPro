using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

/// <summary>
/// مجموعه تست API — کارخانه مشترک برای همه کلاس‌ها تا برنامه فقط یک‌بار ساخته شود
/// (مهم برای سازگاری Serilog دو‌مرحله‌ای که فقط یک‌بار قابل انجماد است)
/// گام V12: کارخانه به InMemory تغییر کرد تا کش مدل EF با کارخانه جریان یکدست باشد.
/// </summary>
[CollectionDefinition("Api")]
public class ApiCollectionFixture : ICollectionFixture<InMemoryApiFactory>
{
}
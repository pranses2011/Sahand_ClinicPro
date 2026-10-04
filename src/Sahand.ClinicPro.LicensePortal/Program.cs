using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Sahand.ClinicPro.LicensePortal.Components;
using Sahand.ClinicPro.LicensePortal.Services;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════
// سرویس‌ها — گام V9
// پنل به‌عنوان سامانه صادرکننده مستقیماً از DbContext استفاده می‌کند؛
// رشته اتصال در استقرار عملیاتی به دیتابیس مجزای لایسنس اشاره می‌کند.
// ═══════════════════════════════════════════════════════════
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<ClinicProDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("LicensePortalDatabase")
            ?? builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=SahandClinicProLicenseDb;Trusted_Connection=True;TrustServerCertificate=True"));

builder.Services.AddScoped<LicenseAdminService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
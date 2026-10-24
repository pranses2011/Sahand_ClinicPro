using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// بذر دسترسی‌ها — مجوزهای اتمی بر اساس ماژول و عملیات
/// </summary>
public class PermissionSeeder : IDataSeeder
{
    private readonly ClinicProDbContext _context;

    /// <summary>ترتیب اجرای بذر</summary>
    public int Order => 20;

    /// <summary>سازنده با تزریق کانتکست</summary>
    public PermissionSeeder(ClinicProDbContext context) => _context = context;

    /// <summary>ایجاد دسترسی‌های ماژول‌های اصلی</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var modules = new[]
        {
            "patients", "doctors", "appointments", "receptions", "visits", "prescriptions",
            "invoices", "payments", "accounting", "insurance", "lab", "imaging", "dental",
            "eye", "physiotherapy", "pharmacy", "sms", "reports", "users", "roles",
            "settings", "backup", "auditlogs", "license", "dashboard", "attendance"
        };
        var actions = new[] { "view", "create", "edit", "delete", "print", "export" };
        var persianModules = new Dictionary<string, string>
        {
            ["patients"] = "بیماران", ["doctors"] = "پزشکان", ["appointments"] = "نوبت‌دهی",
            ["receptions"] = "پذیرش", ["visits"] = "ویزیت", ["prescriptions"] = "نسخه‌نویسی",
            ["invoices"] = "صندوق و فاکتور", ["payments"] = "پرداخت‌ها", ["accounting"] = "حسابداری",
            ["insurance"] = "بیمه", ["lab"] = "آزمایشگاه", ["imaging"] = "تصویربرداری",
            ["dental"] = "دندانپزشکی", ["eye"] = "چشم‌پزشکی", ["physiotherapy"] = "فیزیوتراپی",
            ["pharmacy"] = "داروخانه", ["sms"] = "پیامک", ["reports"] = "گزارش‌ها",
            ["users"] = "کاربران", ["roles"] = "نقش‌ها", ["settings"] = "تنظیمات",
            ["backup"] = "پشتیبان‌گیری", ["auditlogs"] = "لاگ عملیات", ["license"] = "لایسنس",
            ["dashboard"] = "داشبورد", ["attendance"] = "حضور و غیاب"
        };
        var persianActions = new Dictionary<string, string>
        {
            ["view"] = "مشاهده", ["create"] = "ایجاد", ["edit"] = "ویرایش",
            ["delete"] = "حذف", ["print"] = "چاپ", ["export"] = "خروجی"
        };

        foreach (var module in modules)
        {
            foreach (var action in actions)
            {
                var name = $"{module}.{action}";
                var exists = await _context.Permissions.AnyAsync(p => p.Name == name, cancellationToken);
                if (!exists)
                {
                    _context.Permissions.Add(new Permission
                    {
                        Name = name,
                        Module = module,
                        Action = action,
                        Category = "ماژول اصلی",
                        Description = $"{persianActions[action]} {persianModules[module]}"
                    });
                }
            }
        }

        // دسترسی‌های ویژه خارج از الگوی ماژول×عملیات (گام R1 — پرکردن خلأ قبلی همزمان)
        var specials = new (string Name, string Module, string Action, string Category, string Description)[]
        {
            ("sms.bulksend", "sms", "bulksend", "ویژه", "ارسال دسته‌ای پیامک"),
            ("notifications.manage", "notifications", "manage", "ویژه", "مدیریت و ایجاد اعلان درون‌برنامه‌ای"),
            ("emr.advanced", "emr", "advanced", "ویژه", "پرونده بالینی پیشرفته (رشد/سابقه خانوادگی/مراقبت مزمن)"),
            ("payroll.manage", "payroll", "manage", "ویژه", "مدیریت حقوق و فیش حقوقی پرسنل"),
            ("attendance.devices", "attendance", "devices", "ویژه", "مدیریت دستگاه‌های حضور و غیاب و همگام‌سازی"),
            ("attendance.shifts", "attendance", "shifts", "ویژه", "تعریف شیفت‌ها و تخصیص به پرسنل"),
            ("attendance.approve", "attendance", "approve", "ویژه", "تأیید درخواست‌های اصلاح کارت حضور"),
            ("pharmacy.stocktake", "pharmacy", "stocktake", "ویژه", "انبارگردانی داروخانه — شمارش، تکمیل و تأیید مغایرت"),
            ("surveys.manage", "surveys", "manage", "ویژه", "مدیریت نظرسنجی‌های رضایت بیماران — ایجاد، ویرایش، فعال‌سازی و ارسال لینک پیامکی"),
            ("surveys.view", "surveys", "view", "ویژه", "مشاهده فهرست نظرسنجی‌ها و نتایج و آمار آنها")
        };
        foreach (var (name, module, action, category, description) in specials)
        {
            var exists = await _context.Permissions.AnyAsync(p => p.Name == name, cancellationToken);
            if (!exists)
            {
                _context.Permissions.Add(new Permission
                {
                    Name = name, Module = module, Action = action,
                    Category = category, Description = description
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

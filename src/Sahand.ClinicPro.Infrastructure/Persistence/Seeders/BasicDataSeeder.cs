using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// بذر داده‌های پایه — تخصص‌ها، کدهای ICD-10 پرکاربرد، بیمه‌ها، ماژول‌ها و تنظیمات
/// </summary>
public class BasicDataSeeder : IDataSeeder
{
    private readonly ClinicProDbContext _context;

    /// <summary>ترتیب اجرای بذر</summary>
    public int Order => 40;

    /// <summary>سازنده با تزریق کانتکست</summary>
    public BasicDataSeeder(ClinicProDbContext context) => _context = context;

    /// <summary>ایجاد داده‌های پایه در صورت نبود</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedSpecialtiesAsync(cancellationToken);
        await SeedIcd10Async(cancellationToken);
        await SeedInsuranceCompaniesAsync(cancellationToken);
        await SeedModulesAsync(cancellationToken);
        await SeedSmsTemplatesAsync(cancellationToken);
        await SeedClinicInfoAsync(cancellationToken);
        await SeedFiscalYearAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>بذر تخصص‌های پزشکی</summary>
    private async Task SeedSpecialtiesAsync(CancellationToken ct)
    {
        if (await _context.Specialties.AnyAsync(ct)) return;
        var list = new (string Fa, string En, string Code)[]
        {
            ("پزشکی عمومی", "General Practice", "GP"),
            ("داخلی", "Internal Medicine", "INT"),
            ("قلب و عروق", "Cardiology", "CAR"),
            ("اطفال", "Pediatrics", "PED"),
            ("زنان و زایمان", "Obstetrics and Gynecology", "OBG"),
            ("جراحی عمومی", "General Surgery", "SUR"),
            ("ارتوپدی", "Orthopedics", "ORT"),
            ("پوست و مو", "Dermatology", "DER"),
            ("چشم‌پزشکی", "Ophthalmology", "OPH"),
            ("گوش حلق بینی", "Otolaryngology", "ENT"),
            ("روان‌پزشکی", "Psychiatry", "PSY"),
            ("دندانپزشکی", "Dentistry", "DENT"),
            ("فیزیوتراپی", "Physiotherapy", "PHY"),
            ("رادیولوژی", "Radiology", "RAD"),
        };
        foreach (var (fa, en, code) in list)
        {
            _context.Specialties.Add(new Specialty { Name = fa, NameEn = en, Code = code, IsActive = true });
        }
    }

    /// <summary>بذر کدهای ICD-10 پرکاربرد</summary>
    private async Task SeedIcd10Async(CancellationToken ct)
    {
        if (await _context.ICD10Codes.AnyAsync(ct)) return;
        var list = new (string Code, string En, string Fa)[]
        {
            ("A09", "Infectious gastroenteritis and colitis", "اسهال و گاستروانتریت عفونی"),
            ("B54", "Malaria, unspecified", "مالاریای نامشخص"),
            ("E11", "Type 2 diabetes mellitus", "دیابت نوع دو"),
            ("E03", "Other hypothyroidism", "کم‌کاری تیروئید"),
            ("E78", "Disorders of lipoprotein metabolism", "اختلال متابولیسم لیپوپروتئین"),
            ("G43", "Migraine", "میگرن"),
            ("H10", "Acute conjunctivitis", "ملتحیه حاد"),
            ("H52", "Disorders of accommodation and refraction", "اختلالات انکسار چشم"),
            ("I10", "Essential hypertension", "فشار خون بالا"),
            ("I50", "Heart failure", "نارسایی قلب"),
            ("J06", "Acute upper respiratory infection", "عفونتهای حاد دستگاه تنفس فوقانی"),
            ("J20", "Acute bronchitis", "برونشیت حاد"),
            ("J45", "Asthma", "آسم"),
            ("K29", "Gastritis and duodenitis", "گاستریت و دوودنیت"),
            ("K35", "Acute appendicitis", "آپاندیسیت حاد"),
            ("L50", "Urticaria", "کهیر"),
            ("M17", "Gonarthrosis", "آرتروز زانو"),
            ("M54", "Dorsalgia", "درد پشت"),
            ("N39", "Urinary tract infection", "عفونت مجاری ادراری"),
            ("O80", "Encounter for full-term uncomplicated delivery", "زایمان طبیعی بدون عارضه"),
            ("R05", "Cough", "سرفه"),
            ("R51", "Headache", "سردرد"),
            ("Z00", "General medical examination", "معاینه عمومی پزشکی"),
        };
        foreach (var (code, en, fa) in list)
        {
            _context.ICD10Codes.Add(new Icd10Code { Code = code, Description = en, DescriptionFa = fa });
        }
    }

    /// <summary>بذر شرکت‌های بیمه</summary>
    private async Task SeedInsuranceCompaniesAsync(CancellationToken ct)
    {
        if (await _context.InsuranceCompanies.AnyAsync(ct)) return;
        var list = new (string Name, string Code, InsuranceType Type)[]
        {
            ("تأمین اجتماعی", "TAMIN", InsuranceType.Public),
            ("سلامت", "SALAMAT", InsuranceType.Salamat),
            ("نیروهای مسلح", "ARTESH", InsuranceType.Artesh),
            ("خدمات درمانی آزاد", "AZAD", InsuranceType.Private),
            ("بیمه ایران", "IRAN", InsuranceType.Private),
            ("بیمه آسیا", "ASIA", InsuranceType.Private),
            ("بیمه دانا", "DANA", InsuranceType.Private),
            ("بیمه پاسارگاد", "PASARGAD", InsuranceType.Supplementary),
        };
        foreach (var (name, code, type) in list)
        {
            _context.InsuranceCompanies.Add(new InsuranceCompany
            {
                Name = name, Code = code, Type = type, IsActive = true
            });
        }
    }

    /// <summary>بذر ماژول‌های سامانه</summary>
    private async Task SeedModulesAsync(CancellationToken ct)
    {
        if (await _context.Modules.AnyAsync(ct)) return;
        var list = new (string Name, string Code, bool Core)[]
        {
            ("پذیرش و نوبت‌دهی", "Appointments", true),
            ("پرونده بیمار", "Patients", true),
            ("ویزیت و معاینه", "Visits", true),
            ("نسخه‌نویسی", "Prescriptions", true),
            ("مالی و صندوق", "Billing", true),
            ("حسابداری دوبل", "Accounting", false),
            ("آزمایشگاه", "Laboratory", false),
            ("تصویربرداری", "Imaging", false),
            ("دندانپزشکی", "Dental", false),
            ("چشم‌پزشکی", "EyeCare", false),
            ("فیزیوتراپی", "Physiotherapy", false),
            ("داروخانه", "Pharmacy", false),
            ("سامانه پیامک", "Sms", false),
            ("گزارش‌ساز", "Reports", false),
            ("منابع انسانی", "HR", false),
            ("یکپارچه‌سازی دولتی", "GovIntegration", false),
            ("نسخه الکترونیک", "EPrescription", false),
            ("بیمه و مطالبات", "Insurance", false),
            ("مدیریت کاربران", "Users", true),
            ("تنظیمات سامانه", "Settings", true),
            ("پشتیبان‌گیری", "Backup", true),
            ("لاگ عملیات", "Audit", true),
            ("داشبورد مدیریت", "Dashboard", true),
        };
        var order = 1;
        foreach (var (name, code, core) in list)
        {
            _context.Modules.Add(new SystemModule
            {
                Name = name, Code = code, IsCore = core, IsEnabled = core,
                SortOrder = order++, Version = "1.0.0"
            });
        }
    }

    /// <summary>بذر قالب‌های پیامک آماده</summary>
    private async Task SeedSmsTemplatesAsync(CancellationToken ct)
    {
        if (await _context.SmsTemplates.AnyAsync(ct)) return;
        var list = new (string Name, string Code, SmsTemplateType Type, string Content)[]
        {
            ("یادآوری نوبت فردا", "APPT_REMINDER", SmsTemplateType.Reminder,
             "{patient_name} عزیز، نوبت شما با {doctor_name} در تاریخ {date} ساعت {time} در درمانگاه سهند ثبت شده است. کد پیگیری: {code}"),
            ("تأیید ثبت نوبت", "APPT_CONFIRM", SmsTemplateType.Confirmation,
             "{patient_name} عزیز، نوبت شما با موفقیت ثبت شد. لغو نوبت تا ۲۴ ساعت قبل امکان‌پذیر است."),
            ("آماده شدن نتیجه آزمایش", "LAB_READY", SmsTemplateType.Result,
             "{patient_name} عزیز، نتیجه آزمایش شما آماده دریافت است."),
            ("رسید پرداخت", "PAY_RECEIPT", SmsTemplateType.Receipt,
             "{patient_name} عزیز، پرداخت شما به مبلغ {amount} ریال با شماره رسید {ref_no} ثبت شد."),
            ("خوش‌آمدگویی بیمار جدید", "PATIENT_WELCOME", SmsTemplateType.Custom,
             "{patient_name} عزیز، به درمانگاه سهند خوش آمدید. شماره پرونده شما: {mrn}"),
        };
        foreach (var (name, code, type, content) in list)
        {
            _context.SmsTemplates.Add(new SmsTemplate
            {
                Name = name, Code = code, TemplateType = type, Content = content, IsActive = true
            });
        }
    }

    /// <summary>بذر اطلاعات پایه درمانگاه</summary>
    private async Task SeedClinicInfoAsync(CancellationToken ct)
    {
        if (await _context.ClinicInfo.AnyAsync(ct)) return;
        _context.ClinicInfo.Add(new ClinicInfo
        {
            ClinicName = "درمانگاه سهند",
            Phone = "04112345678",
            Email = "info@sahandclinic.ir",
            Address = "تبریز، خیابان آبرسان"
        });
    }

    /// <summary>بذر سال مالی جاری</summary>
    private async Task SeedFiscalYearAsync(CancellationToken ct)
    {
        if (await _context.FiscalYears.AnyAsync(ct)) return;
        var now = DateTime.UtcNow;
        _context.FiscalYears.Add(new FiscalYear
        {
            Year = now.Year,
            StartDate = new DateOnly(now.Year, 1, 1),
            EndDate = new DateOnly(now.Year, 12, 31),
            IsActive = true,
            IsClosed = false
        });
    }
}

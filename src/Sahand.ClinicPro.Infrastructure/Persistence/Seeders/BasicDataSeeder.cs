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

    /// <summary>
    /// بذر تخصص‌های پزشکی — چرخه بیست‌ویکم (گزارش پانزدهم مالک):
    /// «لیست فوق‌تخصص رو خالی میاره و چیزی برای انتخاب کردن نیست» — ریشه آن‌بود که
    /// بذر قبلی فقط ۱۴ تخصص والد می‌کاشت و هیچ فرزندی (فوق‌تخصص) وجود نداشت؛ به‌علاوه
    /// شرط «اگر جدول پر است برگرد» مانع تکمیل دیتابیس‌های موجود می‌شد.
    /// این نسخه: الف) upsert بر اساس Code (دیتابیس موجود هم کامل می‌شود)
    ///           ب) ۲ تخصص والد جدید + ۵۰ فوق‌تخصص فرزند برای ۱۲ تخصص اصلی
    /// </summary>
    private async Task SeedSpecialtiesAsync(CancellationToken ct)
    {
        // ── تخصص‌های والد (اصلی) — کد یکتا مبنای upsert ──
        var parents = new (string Fa, string En, string Code)[]
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
            // چرخه بیست‌ویکم — دو تخصص والد پرکاربردِ جاافتاده
            ("مغز و اعصاب", "Neurology", "NEU"),
            ("اورولوژی", "Urology", "URO"),
        };

        var existing = await _context.Specialties
            .IgnoreQueryFilters()
            .ToListAsync(ct);
        var byCode = existing.ToDictionary(s => s.Code, s => s, StringComparer.OrdinalIgnoreCase);

        // افزودن والدین تازه (دیتابیس موجود فقط موارد نوی را می‌گیرد)
        foreach (var (fa, en, code) in parents)
        {
            if (!byCode.ContainsKey(code))
            {
                var entity = new Specialty { Name = fa, NameEn = en, Code = code, IsActive = true };
                _context.Specialties.Add(entity);
                byCode[code] = entity;
            }
        }
        // والدین تازه باید Id بگیرند تا فرزندان به آن‌ها وصل شوند
        await _context.SaveChangesAsync(ct);

        // ── فوق‌تخصص‌ها (فرزندان) — چرخه بیست‌ویکم ──
        // (کد والد، نام فارسی، نام انگلیسی، کد یکتای فرزند)
        var subs = new (string ParentCode, string Fa, string En, string Code)[]
        {
            // داخلی
            ("INT", "عفونی", "Infectious Diseases", "INT-INF"),
            ("INT", "گوارش و کبد", "Gastroenterology", "INT-GAS"),
            ("INT", "غدد و متابولیسم", "Endocrinology", "INT-END"),
            ("INT", "کلیه", "Nephrology", "INT-NEP"),
            ("INT", "ریه", "Pulmonology", "INT-PUL"),
            ("INT", "خون و سرطان", "Hematology and Oncology", "INT-HEM"),
            ("INT", "روماتولوژی", "Rheumatology", "INT-RHE"),
            // قلب و عروق
            ("CAR", "قلب مداخله‌ای", "Interventional Cardiology", "CAR-ICD"),
            ("CAR", "الکتروفیزیولوژی قلب", "Cardiac Electrophysiology", "CAR-EPS"),
            ("CAR", "اکوکاردیوگرافی", "Echocardiography", "CAR-ECH"),
            ("CAR", "قلب اطفال", "Pediatric Cardiology", "CAR-PED"),
            // اطفال
            ("PED", "نوزادان", "Neonatology", "PED-NEO"),
            ("PED", "نورولوژی اطفال", "Pediatric Neurology", "PED-NEU"),
            ("PED", "گوارش اطفال", "Pediatric Gastroenterology", "PED-GAS"),
            // زنان و زایمان
            ("OBG", "پریناتولوژی", "Maternal-Fetal Medicine", "OBG-MFM"),
            ("OBG", "ناباروری و IVF", "Reproductive Endocrinology", "OBG-IVF"),
            ("OBG", "ژنیکولوژی آنکولوژی", "Gynecologic Oncology", "OBG-GON"),
            // جراحی عمومی
            ("SUR", "جراحی لاپاراسکوپیک", "Laparoscopic Surgery", "SUR-LAP"),
            ("SUR", "جراحی سرطان", "Surgical Oncology", "SUR-ONC"),
            ("SUR", "جراحی عروق", "Vascular Surgery", "SUR-VAS"),
            // ارتوپدی
            ("ORT", "ستون فقرات", "Spine Surgery", "ORT-SPI"),
            ("ORT", "جراحی دست", "Hand Surgery", "ORT-HND"),
            ("ORT", "تعویض مفصل", "Joint Replacement", "ORT-JNT"),
            ("ORT", "ارتوپدی ورزشی", "Sports Medicine", "ORT-SPT"),
            // پوست و مو
            ("DER", "پوست ترمیمی و زیبایی", "Cosmetic Dermatology", "DER-COS"),
            ("DER", "مو و ناخن", "Trichology", "DER-TRI"),
            // چشم‌پزشکی
            ("OPH", "شبکیه", "Retina", "OPH-RET"),
            ("OPH", "آب‌سیاه", "Glaucoma", "OPH-GLA"),
            ("OPH", "قرنیه", "Cornea", "OPH-COR"),
            ("OPH", "قرارگیری عدسی (لیزیک)", "Refractive Surgery", "OPH-RFR"),
            // گوش حلق بینی
            ("ENT", "شنوایی‌شناسی", "Audiology", "ENT-AUD"),
            ("ENT", "جراحی سر و گردن", "Head and Neck Surgery", "ENT-HNS"),
            ("ENT", "آلرژی", "Rhinology and Allergy", "ENT-ALG"),
            // روان‌پزشکی
            ("PSY", "کودک و نوجوان", "Child Psychiatry", "PSY-CHD"),
            ("PSY", "روان‌درمانی", "Psychotherapy", "PSY-PSY"),
            ("PSY", "اعتیاد", "Addiction Medicine", "PSY-ADD"),
            // دندانپزشکی
            ("DENT", "ایمپلنت", "Implantology", "DENT-IMP"),
            ("DENT", "ارتودنسی", "Orthodontics", "DENT-ORT"),
            ("DENT", "جراحی فک و صورت", "Maxillofacial Surgery", "DENT-MFS"),
            ("DENT", "دندان‌پزشکی کودکان", "Pediatric Dentistry", "DENT-PED"),
            ("DENT", "درمان ریشه", "Endodontics", "DENT-END"),
            ("DENT", "پروتز", "Prosthodontics", "DENT-PRO"),
            // رادیولوژی
            ("RAD", "سونوگرافی", "Ultrasonography", "RAD-USG"),
            ("RAD", "ام‌آرآی", "MRI", "RAD-MRI"),
            ("RAD", "سی‌تی‌اسکن", "CT Scan", "RAD-CT"),
            ("RAD", "رادیولوژی مداخله‌ای", "Interventional Radiology", "RAD-INT"),
            // مغز و اعصاب
            ("NEU", "تشنج و صرع", "Epileptology", "NEU-EPI"),
            ("NEU", "سکته مغزی", "Stroke", "NEU-STR"),
            ("NEU", "دستگاه عصبی محیطی", "Neuromuscular", "NEU-NMD"),
            // اورولوژی
            ("URO", "جراحی کلیه", "Endourology", "URO-END"),
            ("URO", "ناباروری مردان", "Male Infertility", "URO-INF"),
            ("URO", "اورو-آنکولوژی", "Urologic Oncology", "URO-ONC"),
        };

        foreach (var (parentCode, fa, en, code) in subs)
        {
            // upsert بر اساس کد یکتا؛ دیتابیس‌های موجود فقط ردیف‌های جدید را می‌گیرند
            if (byCode.ContainsKey(code))
                continue;
            if (!byCode.TryGetValue(parentCode, out var parent))
                continue; // والد ناشناخته — از مسیر امن عبور کن

            var child = new Specialty
            {
                Name = fa,
                NameEn = en,
                Code = code,
                ParentId = parent.Id,
                IsActive = true
            };
            _context.Specialties.Add(child);
            byCode[code] = child;
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

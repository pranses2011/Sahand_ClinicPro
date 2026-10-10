using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Lookups;

/// <summary>
/// فرمان ثبت قلم جدید انبار داروخانه — چرخهٔ سی‌ونه‌م (گزارش مالک)
/// </summary>
/// <remarks>
/// «خرید و انبار و قیمت‌های این لوازم هم باید باشه» — تا پیش از این چرخه،
/// کاتالوگ اقلام (جدول Medications) فقط از بذر اولیه پر می‌شد و اندپوینت
/// «ساخت قلم» وجود نداشت؛ یعنی افزودن سرنگ/سرم/گاز استریل به انبار از خود
/// برنامه ممکن نبود. این فرمان قلم جدید را با نوع صریح (دارو یا لوازم
/// مصرفی) می‌سازد؛ فیلدهای بالینی اختیاری‌اند چون برای لوازم مصرفی
/// بی‌معنا هستند (مثلاً رده بارداری FDA برای گاز استریل!) و فقط برای داروها
/// پر می‌شوند.
/// </remarks>
public record CreatePharmacyItemCommand(
    // نام فارسی قلم — برای لوازم مصرفی همان نام نمایشی است (مثلاً «سرنگ ۵ سی‌سی»)
    // [نکتهٔ CS1587 چرخهٔ ۳۶: کامنت روی پارامترهای پرانتزی رکورد باید عادی باشد]
    string NameFa,
    // نام انگلیسی/ژنریک — الزامی طبق قرارداد جدول؛ برای لوازم مصرفی می‌تواند همان نام فارسی باشد
    string GenericName,
    // نوع قلم: ۱ دارو، ۲ لوازم مصرفی
    MedicationItemKind ItemKind,
    // فرم/جنس — اختیاری (مثلاً «ست تزریقی» یا «قرص»)
    string? Form = null,
    // اندازه/غلظت — اختیاری (مثلاً «۵cc» یا «۵۰۰ میلی‌گرم»)
    string? Strength = null,
    // شرکت سازنده — اختیاری
    string? Manufacturer = null,
    // بارکد — اختیاری
    string? Barcode = null) : IRequest<Result<long>>;

/// <summary>اعتبارسنج قلم جدید — نام و نوع الزامی‌اند</summary>
public class CreatePharmacyItemCommandValidator : AbstractValidator<CreatePharmacyItemCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreatePharmacyItemCommandValidator()
    {
        RuleFor(x => x.NameFa).NotEmpty().MaximumLength(200)
            .WithMessage("نام فارسی قلم الزامی است (حداکثر ۲۰۰ نویسه)");
        RuleFor(x => x.GenericName).NotEmpty().MaximumLength(200)
            .WithMessage("نام ژنریک/انگلیسی الزامی است — برای لوازم مصرفی می‌توانید همان نام فارسی را وارد کنید");
        // ═══ چرخهٔ ۴۳ (گزارش مالک: «سرنگ ۲ سی سی رو ثبت بکنم ولی گفت در لیست
        // اقلام موجود است») — نام ژنریک تک‌نویسه‌ای تقریباً همیشه از رونوشت
        // ناقص خودکار فرم می‌آید؛ رد صریح با پیام راهنما از ثبت دادهٔ معیوب
        // پیشگیری می‌کند و گارد تکراری را هم از تطبیق‌های کاذب خلاص می‌کند.
        RuleFor(x => x.GenericName).MinimumLength(2).When(x => !string.IsNullOrWhiteSpace(x.GenericName))
            .WithMessage("نام ژنریک حداقل ۲ نویسه است — اگر نام فارسی به‌طور خودکار کپی شده، آن را کامل کنید");
        RuleFor(x => x.ItemKind).IsInEnum().WithMessage("نوع قلم نامعتبر است (دارو یا لوازم مصرفی)");
    }
}

/// <summary>گردانندهٔ ثبت قلم جدید انبار</summary>
public class CreatePharmacyItemCommandHandler : IRequestHandler<CreatePharmacyItemCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreatePharmacyItemCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>
    /// اجرای ثبت — نام تکراریِ فعال رد می‌شود تا فهرست‌ها دوزبانگی نداشته باشند.
    /// ═══ چرخهٔ ۴۳ (گزارش مالک: «سرنگ ۲ سی سی رو ثبت بکنم ولی گفت در لیست
    /// اقلام موجود است… در حالی که در لیست نبود») ═══ ریشهٔ تکراری کاذب:
    /// فرم دسکتاپ نام ژنریک را فقط با «نخستین نویسهٔ» نام فارسی پر می‌کرد (مثل
    /// «س» برای سرنگ)؛ چون گارد تکراری نام ژنریک را هم تطبیق می‌داد، هر قلم
    /// دوم با حرف آغازین یکسان رد می‌شد. اکنون تطبیق نام‌ها با «نرمال‌سازی
    /// فارسی» انجام می‌شود (ی/ي عربی، ک/ك عربی و فاصله‌های ابتدایی/انتهایی)
    /// تا فقط نام‌های «واقعاً یکسان» تکراری شمرده شوند.
    /// </summary>
    public async Task<Result<long>> Handle(CreatePharmacyItemCommand request,
        CancellationToken cancellationToken)
    {
        // مقدار خامِ بریده‌شده برای ذخیره + نسخهٔ نرمال‌شده فقط برای مقایسه
        // (نرمال‌سازی هرگز روی دادهٔ ذخیره‌شده اعمال نمی‌شود تا ورودی کاربر دست‌نخورده بماند)
        var rawNameFa = request.NameFa.Trim();
        var rawGeneric = request.GenericName.Trim();
        var nameFa = NormalizeFa(rawNameFa);
        var genericName = NormalizeFa(rawGeneric);

        // گارد نام تکراری — قلم فعالِ هم‌نام (دارو یا مصرفی) نباید دوباره ساخته شود؛
        // اگر قبلاً نرم‌حذف شده باشد، ردیف جدید بدون تداخل با یکتایی IrcCode ساخته می‌شود.
        // همهٔ نام‌های فعال خوانده و در حافظه با نرمال‌سازی فارسی مقایسه می‌شوند —
        // چون SQL تبدیل نویسه‌های عربی/فارسی (ي/ی، ك/ک) را انجام نمی‌دهد و پالایش
        // پیشینی روی متن خام، جفت‌های هم‌نام با صفحه‌کلید متفاوت را از دست می‌داد.
        // سقف مقرون‌به‌صرفه: کاتالوگ دارو چند هزار ردیف است و این یک بار در هر
        // ثبت قلم اتفاق می‌افتد؛ فقط دو ستون متنی خوانده می‌شود.
        var existing = await _unitOfWork.Repository<Medication>().QueryableNoTracking
            .Where(m => !m.IsDeleted && m.IsActive)
            .Select(m => new { m.GenericNameFa, m.GenericName })
            .ToListAsync(cancellationToken);
        var duplicate = existing.Any(m =>
            NormalizeFa(m.GenericNameFa ?? "") == nameFa ||
            NormalizeFa(m.GenericName ?? "") == genericName);
        if (duplicate)
            return Result<long>.Failure(new Error("PHARMACY.ITEM.DUPLICATE",
                $"قلم «{rawNameFa}» از قبل در انبار ثبت شده است — از همان استفاده کنید"));

        var item = new Medication
        {
            GenericName = rawGeneric,
            GenericNameFa = rawNameFa,
            ItemKind = request.ItemKind,
            Form = request.Form?.Trim(),
            Strength = request.Strength?.Trim(),
            Manufacturer = request.Manufacturer?.Trim(),
            Barcode = request.Barcode?.Trim(),
            IsActive = true
        };

        await _unitOfWork.Repository<Medication>().AddAsync(item, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<long>.Success(item.Id);
    }

    /// <summary>
    /// نرمال‌سازی فارسی برای «مقایسهٔ» نام‌ها — نویسه‌های هم‌ارز عربی/فارسی و
    /// فاصله‌ها یکسان‌سازی می‌شوند تا دو نام که فقط تفاوت صفحه‌کلیدی دارند
    /// («سرنگ» با ي عربی و «سرنگ» با ی فارسی) به‌درستی تکراری شمرده شوند.
    /// فقط برای مقایسه است؛ مقدار اصلی کاربر بدون تغییر ذخیره می‌شود.
    /// </summary>
    internal static string NormalizeFa(string text) => text
        .Replace('ي', 'ی')   // ي عربی → ی فارسی
        .Replace('ك', 'ک')   // ك عربی → ک فارسی
        .Replace('ۀ', 'ه')
        .Replace('\u200c', ' ') // نیم‌فاصله → فاصله (برای برابری)
        .Trim();
}

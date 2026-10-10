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
        RuleFor(x => x.ItemKind).IsInEnum().WithMessage("نوع قلم نامعتبر است (دارو یا لوازم مصرفی)");
    }
}

/// <summary>گردانندهٔ ثبت قلم جدید انبار</summary>
public class CreatePharmacyItemCommandHandler : IRequestHandler<CreatePharmacyItemCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreatePharmacyItemCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت — نام تکراریِ فعال رد می‌شود تا فهرست‌ها دوزبانگی نداشته باشند</summary>
    public async Task<Result<long>> Handle(CreatePharmacyItemCommand request,
        CancellationToken cancellationToken)
    {
        var nameFa = request.NameFa.Trim();

        // گارد نام تکراری — قلم فعالِ هم‌نام (دارو یا مصرفی) نباید دوباره ساخته شود؛
        // اگر قبلاً نرم‌حذف شده باشد، ردیف جدید بدون تداخل با یکتایی IrcCode ساخته می‌شود
        var duplicate = await _unitOfWork.Repository<Medication>().QueryableNoTracking
            .AnyAsync(m => !m.IsDeleted && m.IsActive
                && (m.GenericNameFa == nameFa || m.GenericName == request.GenericName.Trim()),
                cancellationToken);
        if (duplicate)
            return Result<long>.Failure(new Error("PHARMACY.ITEM.DUPLICATE",
                $"قلم «{nameFa}» از قبل در انبار ثبت شده است — از همان استفاده کنید"));

        var item = new Medication
        {
            GenericName = request.GenericName.Trim(),
            GenericNameFa = nameFa,
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
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Application.Common.Security;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Security;
using Sahand.ClinicPro.Domain.Interfaces;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر مدیریت کاربران — فرم ۲۴ پرامپت UI بخش ۴
/// فهرست با فیلتر، ثبت/ویرایش دو‌تبی، ریست رمز، قفل/آزادسازی و غیرفعال‌سازی
/// مسیرها: /api/v1/users
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _hasher;
    private readonly IPasswordPolicyProvider _passwordPolicyProvider;

    /// <summary>سازنده با تزریق واحد کار و هشکننده رمز</summary>
    public UsersController(IUnitOfWork unitOfWork, IPasswordHasher hasher, IPasswordPolicyProvider passwordPolicyProvider)
    {
        _unitOfWork = unitOfWork;
        _hasher = hasher;
        _passwordPolicyProvider = passwordPolicyProvider;
    }

    /// <summary>سیاست جاری رمز عبور برای فرم‌های کاربری</summary>
    [HttpGet("password-policy")]
    public async Task<IActionResult> GetPasswordPolicy(CancellationToken cancellationToken)
    {
        var policy = await _passwordPolicyProvider.GetCurrentAsync(cancellationToken);
        return Ok(ApiResponse<PasswordPolicyDto>.Success(new PasswordPolicyDto(
            policy.MinPasswordLength, policy.RequireMixedCase, policy.RequireDigit, policy.RequireSpecialChar)));
    }

    /// <summary>فهرست کاربران — جستجو و فیلتر نقش و وضعیت با صفحه‌بندی</summary>
    [HttpGet]
    [HasPermission("users.view")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? role,
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        IQueryable<User> query = _unitOfWork.Repository<User>().QueryableNoTracking
            .Include(u => u.Employee)
            .Include(u => u.Roles).ThenInclude(r => r.Role);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search) ||
                (u.Employee != null && (u.Employee.FirstName + " " + u.Employee.LastName).Contains(search)));

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Roles.Any(r => r.Role != null && r.Role.Name == role));

        // وضعیت: active فعال | locked قفل | inactive غیرفعال
        query = status switch
        {
            "active" => query.Where(u => u.IsActive && (u.LockoutEnd == null || u.LockoutEnd <= DateTime.UtcNow)),
            "locked" => query.Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTime.UtcNow),
            "inactive" => query.Where(u => !u.IsActive),
            _ => query
        };

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UserListItemDto(
                u.Id,
                u.Username,
                u.Email,
                u.Employee == null ? null : u.Employee.FirstName + " " + u.Employee.LastName,
                u.Roles.Where(r => r.Role != null).Select(r => r.Role!.Name).ToList(),
                u.LastLoginAt,
                u.LockoutEnd,
                u.IsActive,
                u.MustChangePassword))
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new PagedResult<UserListItemDto>(items, total, page, pageSize)));
    }

    /// <summary>بررسی یکتایی نام کاربری — اعتبارسنجی برخط فرم</summary>
    [HttpGet("check-username")]
    [HasPermission("users.view")]
    public async Task<IActionResult> CheckUsername([FromQuery] string username, [FromQuery] long? excludeId)
    {
        var exists = await _unitOfWork.Repository<User>().QueryableNoTracking
            .AnyAsync(u => u.Username == username && (excludeId == null || u.Id != excludeId));
        return Ok(ApiResponse<object>.Success(new { available = !exists }));
    }

    /// <summary>
    /// مشخصات کاربر جاری — چرخهٔ بیست‌وهفتم (گزارش هجدهم مالک):
    /// «صفحهٔ ویزیت پزشک» باید بداند کاربرِ واردشده کدام پزشک است. این نقطهٔ
    /// پایانی عمومی (بدون مجوز ویژه — هر کاربرِ واردشده مشخصات خودش را
    /// می‌بیند) شناسهٔ کاربر/پرسنل/پزشکِ مرتبط و نقش‌ها را برمی‌گرداند تا
    /// میزکار پزشک، خودش را خودکار شناسایی کند.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        // شناسهٔ کاربر از ادعای NameIdentifier توکن (همان الگوی AuditLogMiddleware)
        var idText = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!long.TryParse(idText, out var userId))
            return Unauthorized(ApiResponse<object>.Failure("نشست نامعتبر است — دوباره وارد شوید"));

        var user = await _unitOfWork.Repository<User>().QueryableNoTracking
            .Include(u => u.Employee)
            .Include(u => u.Roles).ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return NotFound(ApiResponse<object>.Failure("کاربر یافت نشد"));

        // پزشکِ مرتبط از مسیر پرسنل — User.EmployeeId → Doctor.EmployeeId
        var doctorId = user.EmployeeId is { } empId
            ? await _unitOfWork.Repository<Doctor>().QueryableNoTracking
                .Where(d => d.EmployeeId == empId && !d.IsDeleted)
                .Select(d => (long?)d.Id)
                .FirstOrDefaultAsync()
            : null;

        // ═══ چرخهٔ سی‌وهشتم (گزارش مالک — سطح دسترسی و پروفایل کاربر) ═══
        // «فقط منوها و فرم‌هایی که کاربر دسترسی دارد نمایش داده شود» +
        // «اگر نام کامل وجود داشت همان نوشته شود وگرنه نام کاربری» + «تصویر
        // کاربر هم نمایش داده شود». این نقطهٔ پایانی علاوه بر داده‌های قبلی،
        // فهرست دسترسی‌های «زندهٔ» توکن (ادعاهای permission) هم برمی‌گرداند
        // تا پوستهٔ دسکتاپ منو را بر اساس همان کلیدهایی که سرور خودش با
        // HasPermission اجرا می‌کند فیلتر کند — نه بر پایهٔ دادهٔ کهنهٔ ورود.
        // displayName تهی یعنی کاربر پرسنل متصل ندارد → کلاینت به نام
        // کاربری برمی‌گردد (قانون مالک: نام کامل در صورت وجود، وگرنه نام کاربری).
        var permissions = User.Claims
            .Where(c => string.Equals(c.Type, "permission", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .Distinct()
            .ToList();

        var response = new
        {
            id = user.Id,
            username = user.Username,
            displayName = user.Employee is null ? (string?)null
                : $"{user.Employee.FirstName} {user.Employee.LastName}".Trim(),
            fullName = user.Employee is null ? user.Username
                : $"{user.Employee.FirstName} {user.Employee.LastName}",
            employeeId = user.EmployeeId,
            doctorId, // تهی یعنی این کاربر پزشک نیست
            roles = user.Roles.Where(r => r.Role != null).Select(r => r.Role!.Name).ToList(),
            permissions
        };
        return Ok(ApiResponse<object>.Success(response, "کاربر جاری"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // چرخهٔ چهل‌ویکم (گزارش مالک) — میانبرهای سریع داشبورد در دیتابیس
    // «پنل میانبرهای سریع داشبورد در دیتابیس ذخیره بشه تا مشکلی براش پیش
    // نیاد» — پیش‌تر این چیدمان شخصی فقط در %AppData% همان ایستگاه زندگی
    // می‌کرد؛ دو اندپوینت زیر حقیقت مرجع را به سرور می‌آورند تا با تعویض
    // ایستگاه/پروفایل از بین نرود و در پشتیبان‌های سرور هم پوشش داده شود.
    // مجوز ویژه‌ای لازم نیست (الگوی me): هر کاربرِ واردشده فقط میانبرهای
    // «خودش» را می‌بیند و ذخیره می‌کند.
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// میانبرهای داشبورد کاربر جاری — اگر رکوردی وجود نداشته باشد Data تهی
    /// برمی‌گردد (کلاینت در آن حالت سراغ کش محلی و سپس پیش‌فرض‌ها می‌رود)؛
    /// لیست خالی معنای «کاربر همه را برداشته» دارد و مبنا قرار می‌گیرد.
    /// </summary>
    [HttpGet("me/shortcuts")]
    public async Task<IActionResult> GetMyShortcuts()
    {
        var userId = ResolveCurrentUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Failure("نشست نامعتبر است — دوباره وارد شوید"));

        // فیلتر نرم‌حذفی خودکار اعمال می‌شود (HasQueryFilter سراسری)
        var record = await _unitOfWork.Repository<UserShortcut>().QueryableNoTracking
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (record is null)
            return Ok(ApiResponse<UserShortcutsDto>.Success(new UserShortcutsDto(null), "میانبری ثبت نشده است"));

        // JSON رکورد به لیست کلیدها تجزیه می‌شود — خرابیِ محتوا هرگز نباید
        // اندپوینت را خطا بدهد؛ لیست خالی، حالتِ محافظه‌کارانهٔ همان رکورد است
        var keys = TryParseKeys(record.KeysJson);
        return Ok(ApiResponse<UserShortcutsDto>.Success(new UserShortcutsDto(keys), "میانبرهای داشبورد"));
    }

    /// <summary>
    /// ذخیرهٔ میانبرهای داشبورد کاربر جاری — Upsert ردیف یکتای UserId؛
    /// ردیف نرم‌حذف‌شدهٔ قبلی (در صورت وجود) با IgnoreQueryFilters بازگردانده
    /// می‌شود تا ایندکس یکتای فیلترشده هرگز درجِ دوم را قفل نکند.
    /// </summary>
    /// <param name="request">فهرست کلید صفحات به ترتیب چیدمان — خالی مجاز است</param>
    [HttpPut("me/shortcuts")]
    public async Task<IActionResult> SaveMyShortcuts([FromBody] SaveUserShortcutsRequest request)
    {
        var userId = ResolveCurrentUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Failure("نشست نامعتبر است — دوباره وارد شوید"));

        // ─── اعتبارسنجی دفاعی: کلیدها کوتاه، محدود و بی‌نویسهٔ مهار هستند تا
        // ستون JSON (۴۰۰۰ نویسه) سرریز نشود و محتوای مخرب ذخیره نشود ───
        var keys = (request.Keys ?? new List<string>())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Where(k => k.Length <= 64)
            .Take(24)
            .ToList();

        var json = System.Text.Json.JsonSerializer.Serialize(keys);

        // Upsert با دور زدن فیلتر نرم‌حذفی — ردیف مردهٔ قبلی همان‌جا زنده می‌شود
        var existing = await _unitOfWork.Repository<UserShortcut>().QueryableTracking
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (existing is null)
        {
            await _unitOfWork.Repository<UserShortcut>().AddAsync(new UserShortcut
            {
                UserId = userId.Value,
                KeysJson = json,
                IsDeleted = false
            });
        }
        else
        {
            existing.KeysJson = json;
            // بازگرداندن ردیف نرم‌حذف‌شده (لایهٔ دوم دفاع در برابر قفل ایندکس یکتا)
            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;
        }

        try
        {
            await _unitOfWork.SaveEntitiesAsync();
        }
        catch (DbUpdateException)
        {
            // دو ایستگاه هم‌زمان برای کاربرِ تازه insert زدند → برندهٔ ایندکس
            // یکتا ردیف ساخت؛ بازنده دوباره می‌خواند و به‌روزرسانی می‌کند
            var winner = await _unitOfWork.Repository<UserShortcut>().QueryableTracking
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.UserId == userId);
            if (winner is null) throw; // خطای دیگری بود — بالا بیاید

            winner.KeysJson = json;
            winner.IsDeleted = false;
            winner.DeletedAt = null;
            winner.DeletedBy = null;
            await _unitOfWork.SaveEntitiesAsync();
        }

        return Ok(ApiResponse<object>.Success(new { keys.Count }, "میانبرهای داشبورد ذخیره شد"));
    }

    /* ═══════════ منوی کناری شخصی کاربر — چرخهٔ ۷۴ (گزارش مالک) ═══════════
       «امکانی رو طراحی و پیاده سازی بکن که بتونم منوهای کناری رو به سلیقه
       خودم مرتب بکنم و حتی بتونم منوی اصلی بسازم که صفه های مورد نظرم رو در
       زیر منوی اون قرار بدم و همچنین دکمه برگشت به تنظیمات پیشفرض هم داشته
       باشه و این تنظیمات به ازای هر کاربر باشه و در دیتابیس ذخیره بشه».
       ذخیره‌سازی: جدول عمومی SystemSetting با کلید nav-layout:{userId}
       (همان الگوی اثبات‌شدهٔ چیدمان داشبورد) — بدون مهاجرت جدید. */

    /// <summary>کلید تنظیم چیدمان منوی کناری کاربر جاری</summary>
    private string NavLayoutKey(long userId) => $"nav-layout:{userId}";

    /// <summary>
    /// چیدمان منوی کناری کاربر جاری — JSON ساختار (آیتم تکی/گروه دلخواه با
    /// زیرمنو). Data تهی یعنی «چیدمان ذخیره‌شده‌ای نیست — منوی پیش‌فرض».
    /// </summary>
    [HttpGet("me/nav-layout")]
    public async Task<IActionResult> GetMyNavLayout()
    {
        var userId = ResolveCurrentUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Failure("نشست نامعتبر است — دوباره وارد شوید"));

        var setting = await _unitOfWork.Repository<Domain.Entities.Infrastructure.SystemSetting>()
            .QueryableNoTracking
            .FirstOrDefaultAsync(x => x.Key == NavLayoutKey(userId.Value) && !x.IsDeleted);

        // خرابی محتوا هرگز اندپوینت را نمی‌شکند — تهی برمی‌گردد و کلاینت منوی
        // پیش‌فرض را می‌سازد (محافظه‌کارانه)
        string? layout = null;
        if (setting?.Value is { Length: > 0 } raw)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    layout = raw; // ساختار معتبر — عیناً پاس می‌شود
            }
            catch
            {
                layout = null;
            }
        }

        return Ok(ApiResponse<object>.Success(new { layout },
            layout is null ? "چیدمان منو ذخیره نشده است" : "چیدمان منوی کناری"));
    }

    /// <summary>
    /// ذخیرهٔ چیدمان منوی کناری کاربر جاری — هر کاربر ساختار مستقل خودش را
    /// دارد؛ سقف ۸۰۰۰ نویسه (منو با گروه‌های دلخواه طولانی‌تر از چیدمان
    /// داشبورد است) و اعتبارسنجی ساختارِ آرایه.
    /// </summary>
    /// <param name="request">JSON چیدمان — آرایهٔ آیتم/گروه</param>
    [HttpPut("me/nav-layout")]
    public async Task<IActionResult> SaveMyNavLayout([FromBody] SaveNavLayoutRequest request)
    {
        var userId = ResolveCurrentUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Failure("نشست نامعتبر است — دوباره وارد شوید"));

        if (string.IsNullOrWhiteSpace(request.Layout) || request.Layout.Length > 8000)
            return BadRequest(ApiResponse<object>.Failure("چیدمان منو نامعتبر است (حداکثر ۸۰۰۰ نویسه)"));

        // اعتبارسنجی ساختار — باید آرایهٔ JSON معتبر باشد
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(request.Layout);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                return BadRequest(ApiResponse<object>.Failure("ساختار چیدمان منو نامعتبر است"));
        }
        catch
        {
            return BadRequest(ApiResponse<object>.Failure("JSON چیدمان منو قابل خواندن نیست"));
        }

        var setting = await _unitOfWork.Repository<Domain.Entities.Infrastructure.SystemSetting>()
            .QueryableTracking
            .FirstOrDefaultAsync(x => x.Key == NavLayoutKey(userId.Value) && !x.IsDeleted);

        if (setting is null)
        {
            setting = new Domain.Entities.Infrastructure.SystemSetting
            {
                Category = "NavLayout",
                Key = NavLayoutKey(userId.Value),
                Value = request.Layout,
                DataType = Domain.Enums.SettingDataType.Json,
                Description = "چیدمان منوی کناری شخصی کاربر"
            };
            await _unitOfWork.Repository<Domain.Entities.Infrastructure.SystemSetting>().AddAsync(setting);
        }
        else
        {
            setting.Value = request.Layout;
        }

        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("چیدمان منوی کناری ذخیره شد"));
    }

    /// <summary>
    /// شناسهٔ کاربر جاری از ادعای NameIdentifier توکن — تهی یعنی نشست نامعتبر
    /// (همان الگوی GetCurrentUser؛ به‌صورت متد مشترک برای دو اندپوینت بالا).
    /// </summary>
    private long? ResolveCurrentUserId()
    {
        var idText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(idText, out var id) ? id : null;
    }

    /// <summary>
    /// تجزیهٔ امن JSON کلیدها — هر خرابیِ محتوا به «لیست خالی» فرومی‌نشیند؛
    /// فقط آرایهٔ رشته‌ای پذیرفته می‌شود و ردیف‌های غیررشته‌ای نادیده می‌مانند.
    /// </summary>
    private static List<string>? TryParseKeys(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                return new List<string>();
            return doc.RootElement.EnumerateArray()
                .Where(e => e.ValueKind == System.Text.Json.JsonValueKind.String)
                .Select(e => e.GetString() ?? string.Empty)
                .Where(s => s.Length > 0)
                .ToList();
        }
        catch (System.Text.Json.JsonException)
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// یافتن حساب کاربری متصل به یک پرسنل — چرخهٔ بیست‌ونهم (گزارش هجدهم مالک):
    /// کلید «مشخصات کاربری» در لیست پزشکان/پرسنل باید حسابِ متصل را برای ویرایش
    /// باز کند و اگر پرسنل هنوز حساب ندارد، فرم ثبتِ کاربرِ جدیدِ متصل به
    /// «همان» رکورد پرسنل را نشان دهد. این نقطهٔ پایانی فقط شناسه/نام‌کاربری/
    /// فعال‌بودن را برمی‌گرداند — تهی یعنی این پرسنل حساب ورود ندارد.
    /// اتصالِ دقیق با EmployeeId از تطبیقِ نامی (ResolveEmployeeIdAsync) امن‌تر
    /// است چون تطبیق نامی با ثبت پرسنل تکراریِ هم‌نام، حساب را به رکورد اشتباه
    /// می‌بست.
    /// </summary>
    [HttpGet("by-employee/{employeeId:long}")]
    [HasPermission("users.view")]
    public async Task<IActionResult> GetUserByEmployee(long employeeId)
    {
        var user = await _unitOfWork.Repository<User>().QueryableNoTracking
            .Where(u => u.EmployeeId == employeeId && !u.IsDeleted)
            .OrderByDescending(u => u.IsActive) // حسابِ فعال در اولویت
            .ThenBy(u => u.Id)
            .Select(u => new { u.Id, u.Username, u.IsActive })
            .FirstOrDefaultAsync();

        // بدون حساب — دادهٔ تهی؛ کلاینت با Data تهی «فهرستِ ثبت حساب جدید» را باز می‌کند
        if (user is null)
            return Ok(ApiResponse<object>.SuccessMessage("این پرسنل هنوز حساب کاربری ندارد"));
        return Ok(ApiResponse<object>.Success(user, "حساب متصل به پرسنل"));
    }

    /// <summary>ثبت کاربر جدید — با هش BCrypt و نقش‌های انتخابی</summary>
    [HttpPost]
    [HasPermission("users.create")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // اعتبارسنجی‌ها — نام کاربری یکتا و رمز مطابق سیاست
        var duplicate = await _unitOfWork.Repository<User>().QueryableNoTracking
            .AnyAsync(u => u.Username == request.Username);
        if (duplicate)
            return BadRequest(ApiResponse<object>.Failure("این نام کاربری قبلاً استفاده شده است"));

        var policy = await _passwordPolicyProvider.GetCurrentAsync(HttpContext.RequestAborted);
        var policyError = policy.Validate(request.Password);
        if (policyError is not null)
            return BadRequest(ApiResponse<object>.Failure(policyError));

        // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — «فیلد نام کامل وجود ندارد» ═══
        // نام کامل به رکورد پرسنل متصل می‌شود (رکورد موجود هم‌نام یا ایجاد
        // رکورد حداقلی) تا «نام کامل» در لیست کاربران و لاگ عملیات دیده شود
        var employeeId = await ResolveEmployeeIdAsync(request.FullName);

        var user = new User
        {
            Username = request.Username.Trim(),
            Email = (request.Email ?? $"{request.Username}@clinic.local").Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            PhoneNumber = request.Phone,
            EmployeeId = request.EmployeeId ?? employeeId,
            MustChangePassword = request.MustChangePassword,
            TwoFactorEnabled = request.TwoFactorEnabled,
            IsActive = request.IsActive,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            PasswordChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await SyncRolesAsync(user, request.RoleIds);
        await _unitOfWork.Repository<User>().AddAsync(user);
        await _unitOfWork.SaveEntitiesAsync();

        return Ok(ApiResponse<object>.SuccessMessage($"کاربر {user.Username} ایجاد شد"));
    }

    /// <summary>ویرایش کاربر — ایمیل/موبایل/نقش‌ها/وضعیت</summary>
    [HttpPut("{id:long}")]
    [HasPermission("users.edit")]
    public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserRequest request)
    {
        var user = await _unitOfWork.Repository<User>().QueryableTracking
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return BadRequest(ApiResponse<object>.Failure("کاربر یافت نشد"));

        if (!string.IsNullOrWhiteSpace(request.Email))
            user.Email = request.Email.Trim();
        user.PhoneNumber = request.Phone;
        // نام کامل — اتصال به پرسنل (گزارش هفدهم مالک — چرخه ۲۴)
        var employeeId = await ResolveEmployeeIdAsync(request.FullName);
        user.EmployeeId = request.EmployeeId ?? employeeId ?? user.EmployeeId;
        user.MustChangePassword = request.MustChangePassword;
        user.TwoFactorEnabled = request.TwoFactorEnabled;
        user.IsActive = request.IsActive;

        // ═══ چرخه بیست‌وچهارم — ریشهٔ «نقش و دسترسی ذخیره نمی‌شود» ═══
        // پیش‌تر Roles.Clear() + افزودن مجدد ردیف‌ها زده می‌شد؛ چون UserRole
        // کلید مرکب (UserId, RoleId) دارد، افزودن ردیفی با همان کلیدِ ردیفِ
        // تازه‌حذف‌شده (که هنوز Track شده است) باعث تداخل کلید در EF می‌شد و
        // کل ذخیره‌سازی با خطا می‌شکست. همگام‌سازیِ تفاوتی جایگزین شد: فقط
        // نقش‌های حذف‌شده پاک و فقط نقش‌های تازه افزوده می‌شوند.
        await SyncRolesAsync(user, request.RoleIds);

        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("کاربر به‌روزرسانی شد"));
    }

    /// <summary>ریست رمز عبور — تولید رمز تصادفی قوی و بازگشت برای نمایش یک‌باره</summary>
    [HttpPost("{id:long}/reset-password")]
    [HasPermission("users.edit")]
    public async Task<IActionResult> ResetPassword(long id)
    {
        var user = await _unitOfWork.Repository<User>().QueryableTracking.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return BadRequest(ApiResponse<object>.Failure("کاربر یافت نشد"));

        // رمز تصادفی ۱۲ کاراکتری با حروف بزرگ/کوچک/رقم/علامت — مطابق سیاست
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%-_*+?";
        var random = RandomNumberGenerator.Create();
        string Next(string set)
        {
            var bytes = new byte[1];
            random.GetBytes(bytes);
            return set[bytes[0] % set.Length].ToString();
        }
        var chars = new List<string> { Next(upper), Next(lower), Next(digits), Next(symbols) };
        var all = upper + lower + digits + symbols;
        while (chars.Count < 12) chars.Add(Next(all));
        var password = string.Concat(chars.OrderBy(_ => Guid.NewGuid()));

        user.PasswordHash = _hasher.Hash(password);
        user.MustChangePassword = true;
        user.PasswordChangedAt = DateTime.UtcNow;
        user.SecurityStamp = Guid.NewGuid().ToString("N"); // ابطال نشست‌های قبلی
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.Success(new { newPassword = password },
            "رمز جدید تولید شد — در اولین ورود باید تغییر کند"));
    }

    /// <summary>قفل حساب کاربر — ۳۰ دقیقه</summary>
    [HttpPost("{id:long}/lock")]
    [HasPermission("users.edit")]
    public async Task<IActionResult> LockUser(long id)
    {
        var user = await _unitOfWork.Repository<User>().QueryableTracking.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return BadRequest(ApiResponse<object>.Failure("کاربر یافت نشد"));

        user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
        user.FailedLoginAttempts = 5;
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("حساب کاربر به مدت ۳۰ دقیقه قفل شد"));
    }

    /// <summary>آزادسازی حساب کاربر قفل‌شده</summary>
    [HttpPost("{id:long}/unlock")]
    [HasPermission("users.edit")]
    public async Task<IActionResult> UnlockUser(long id)
    {
        var user = await _unitOfWork.Repository<User>().QueryableTracking.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return BadRequest(ApiResponse<object>.Failure("کاربر یافت نشد"));

        user.LockoutEnd = null;
        user.FailedLoginAttempts = 0;
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("حساب کاربر آزاد شد"));
    }

    /// <summary>غیرفعال‌سازی (حذف نرم) کاربر</summary>
    [HttpDelete("{id:long}")]
    [HasPermission("users.delete")]
    public async Task<IActionResult> DeactivateUser(long id)
    {
        var user = await _unitOfWork.Repository<User>().QueryableTracking.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return BadRequest(ApiResponse<object>.Failure("کاربر یافت نشد"));

        user.IsActive = false;
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = user.Id; // توسط خود کاربر فعلی — AuditableInterceptor دقت می‌کند
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("کاربر غیرفعال شد"));
    }

    /// <summary>
    /// همگام‌سازی تفاوتی نقش‌های کاربر — چرخه بیست‌وچهارم (گزارش هفدهم مالک):
    /// «در تعریف کاربر جدید، نقش و دسترسی ذخیره نمی‌شود». جای الگوی مخرب
    /// Clear+Add (که با کلید مرکب UserRole تداخل می‌کرد و استثنا می‌انداخت)،
    /// فقط تفاوت‌ها اعمال می‌شود: نقش‌های برداشته‌شده حذف و تازه‌ها افزوده
    /// می‌شوند؛ ردیف‌های مشترک دست نمی‌خورند. برای کاربر نو هم بی‌نقص کار
    /// می‌کند (مجموعهٔ خالی = همهٔ انتخاب‌ها افزودنی‌اند).
    /// </summary>
    private async Task SyncRolesAsync(User user, List<long>? roleIds)
    {
        if (roleIds is null)
            return; // تهی = بدون تغییر در نقش‌ها

        // شناسه‌های نقش‌های موجود در دیتابیس (نقش حذف‌شده نادیده گرفته می‌شود)
        var validIds = (await _unitOfWork.Repository<Role>().QueryableNoTracking
            .Where(r => roleIds.Contains(r.Id)).Select(r => r.Id).ToListAsync())
            .ToHashSet();

        // حذف نقش‌هایی که دیگر انتخاب نشده‌اند
        foreach (var existing in user.Roles.ToList())
            if (!validIds.Contains(existing.RoleId))
                user.Roles.Remove(existing);

        // افزودن نقش‌های تازه — ردیف موجود دست نمی‌خورد
        foreach (var roleId in validIds)
            if (user.Roles.All(ur => ur.RoleId != roleId))
                user.Roles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow
                });
    }

    /// <summary>
    /// یافتن/ایجاد رکورد پرسنل بر اساس نام کامل — چرخه بیست‌وچهارم:
    /// نام کامل کاربر در فیلد «نام کامل» فرم وارد می‌شود؛ اگر پرسنل هم‌نام
    /// موجود باشد به همان متصل می‌شویم، وگرنه رکورد حداقلی پرسنل با همان نام
    /// ساخته می‌شود تا نام کامل در لیست کاربران و لاگ عملیات دیده شود.
    /// خروجی تهی یعنی نامی ارسال نشده (بدون تغییر رکورد فعلی).
    /// </summary>
    private async Task<long?> ResolveEmployeeIdAsync(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return null;

        var name = fullName.Trim();
        // تجزیهٔ نام: نخستین واژه = نام، بقیه = نام‌خانوادگی
        var parts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var first = parts[0];
        var last = parts.Length > 1 ? parts[1].Trim() : "—";

        // پرسنل هم‌نام موجود؟
        var existing = await _unitOfWork.Repository<Employee>().QueryableNoTracking
            .FirstOrDefaultAsync(e => e.FirstName == first && e.LastName == last);
        if (existing is not null)
            return existing.Id;

        // ایجاد رکورد حداقلی پرسنل — بقیهٔ فیلدها بعداً از فرم پرسنل تکمیل می‌شود
        var employee = new Employee
        {
            FirstName = first,
            LastName = last,
            Mobile = null,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<Employee>().AddAsync(employee);
        await _unitOfWork.SaveEntitiesAsync();
        return employee.Id;
    }

    /* ─── مدل‌های درخواست/پاسخ ─── */

    /// <summary>ردیف کاربر در فهرست</summary>
    public record UserListItemDto(long Id, string Username, string? Email, string? FullName,
        List<string> Roles, DateTime? LastLoginAt, DateTime? LockoutEnd, bool IsActive, bool MustChangePassword);

    /// <summary>نتیجه صفحه‌بندی‌شده</summary>
    public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);

    /// <summary>درخواست ثبت کاربر — FullName نام کامل (چرخه ۲۴)</summary>
    public record PasswordPolicyDto(int MinPasswordLength, bool RequireMixedCase, bool RequireDigit, bool RequireSpecialChar);

    public record CreateUserRequest(string Username, string? Email, string Password, string? Phone,
        string? FullName, long? EmployeeId, List<long>? RoleIds,
        bool MustChangePassword, bool TwoFactorEnabled, bool IsActive);

    /// <summary>درخواست ویرایش کاربر — FullName نام کامل (چرخه ۲۴)</summary>
    public record UpdateUserRequest(string? Email, string? Phone, string? FullName, long? EmployeeId,
        List<long>? RoleIds, bool MustChangePassword, bool TwoFactorEnabled, bool IsActive);

    // ═══ چرخهٔ چهل‌ویکم — میانبرهای داشبورد در دیتابیس ═══

    /// <summary>
    /// درخواست ذخیرهٔ میانبرهای داشبورد — Keys همان کلیدهای PageCatalog دسکتاپ
    /// به ترتیب چیدمان کاربر است (اولین عنصر = نخستین کاشی).
    /// </summary>
    public record SaveUserShortcutsRequest(List<string>? Keys);

    /// <summary>درخواست ذخیرهٔ چیدمان منوی کناری — چرخهٔ ۷۴</summary>
    public record SaveNavLayoutRequest(string Layout);

    /// <summary>
    /// میانبرهای ذخیره‌شدهٔ کاربر جاری — تهی بودن Keys یعنی هنوز رکوردی
    /// ثبت نشده (کلاینت باید سراغ کش محلی/پیش‌فرض‌ها برود)؛ لیست خالی یعنی
    /// کاربر عمداً همهٔ میانبرها را برداشته و نباید پیش‌فرض بازگردد.
    /// </summary>
    public record UserShortcutsDto(List<string>? Keys);
}

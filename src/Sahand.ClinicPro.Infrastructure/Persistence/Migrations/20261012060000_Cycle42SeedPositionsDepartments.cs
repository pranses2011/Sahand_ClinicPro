using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// چرخهٔ چهل‌ودوم (گزارش مالک) — بذر فهرست‌های مرجع سمت و دپارتمان پرسنل:
    /// «در ویرایش پرسنل، سمت و دپارتمان رو اصلاح نمیکنه. و همچنین لیست سمت‌ها
    /// و لیست دپارتمانها رو خودت پر بکن تا از بینشون انتخاب بکنیم. هر دو تکمیل
    /// و مرتبط با درمانگاه باشند» — جداول Positions/Departions تا امروز فقط در
    /// صورت تایپِ مقدار تازه از داخل فرم پرسنل پر می‌شدند؛ درمانگاهِ تازه‌نصب
    /// کامبوهای خالی می‌دید و ردیف‌های «—» در فهرست پرسنل.
    ///
    /// این مهاجرت مجموعهٔ استانداردِ نقش‌ها و بخش‌های یک درمانگاه عمومی-تخصصی
    /// را به‌صورت بذر ثبت می‌کند: ۳۰ سمت (مدیریت/پزشکی/پرستاری/پاراکلینیک/
    /// داروخانه/اداری-مالی/پشتیبانی) و ۱۷ دپارتمان. تمام درج‌ها گارد NOT EXISTS
    /// دارند؛ نصب‌هایی که قبلاً مقدار هم‌نام ساخته‌اند هیچ ردیف تکراری نمی‌گیرند
    /// و شناسه‌های موجودشان دست نمی‌خورد. Code عمداً NULL می‌ماند (ایندکس یکتای
    /// فیلترشدهٔ Code فقط مقدار غیرتهی را می‌شمارد) و RowVersion خودکار تولید
    /// می‌شود؛ MinSalary/MaxSalary اجباری‌اند و با صفر بذر می‌شوند تا مدیر بعداً
    /// در صورت نیاز پُرشان کند.
    ///
    /// سبک مهاجرت: SQL خامِ گاردشده (هم‌سبک Cycle24/۳۹/۴۱) تا روی هر نصب و هر
    /// وضعیت قبلی، تکرارپذیر (idempotent) اجرا شود.
    /// </summary>
    [global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Sahand.ClinicPro.Infrastructure.Persistence.ClinicProDbContext))]
    [global::Microsoft.EntityFrameworkCore.Migrations.Migration("20261012060000_Cycle42SeedPositionsDepartments")]
    public partial class Cycle42SeedPositionsDepartments : Migration
    {
        /// <summary>
        /// اجرای مهاجرت — بذر ۱۷ دپارتمان و ۳۰ سمت استاندارد درمانگاهی.
        /// مقادیر عمداً BranchId تهی دارند (سراسری برای همهٔ شعبه‌ها) و
        /// CreatedAt با GETUTCDATE ثبت می‌شود تا با ردیف‌های دستی هم‌سنتی باشد.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─── دپارتمان‌ها: بخش‌های سازمانی درمانگاه ───
            migrationBuilder.Sql(@"
INSERT INTO Departments (Name, Code, CreatedAt, IsDeleted)
SELECT s.Name, NULL, GETUTCDATE(), 0
FROM (VALUES
    (N'مدیریت درمانگاه'),
    (N'پذیرش'),
    (N'اورژانس'),
    (N'درمانگاه عمومی'),
    (N'درمانگاه تخصصی'),
    (N'اتاق عمل'),
    (N'بخش بستری'),
    (N'آزمایشگاه'),
    (N'تصویربرداری و رادیولوژی'),
    (N'داروخانه'),
    (N'فیزیوتراپی'),
    (N'صندوق و مالی'),
    (N'پروندهٔ پزشکی'),
    (N'منابع انسانی'),
    (N'انبار و تدارکات'),
    (N'فناوری اطلاعات'),
    (N'پشتیبانی و خدمات')
) AS s(Name)
WHERE NOT EXISTS (SELECT 1 FROM Departments d WHERE d.Name = s.Name);
");

            // ─── سمت‌ها: عناوین شغلی سراسر درمانگاه ───
            migrationBuilder.Sql(@"
INSERT INTO Positions (Title, Code, MinSalary, MaxSalary, CreatedAt, IsDeleted)
SELECT s.Title, NULL, 0, 0, GETUTCDATE(), 0
FROM (VALUES
    (N'مدیر درمانگاه'),
    (N'معاون درمانگاه'),
    (N'مدیر پرستاری'),
    (N'سرپرست'),
    (N'پزشک عمومی'),
    (N'پزشک متخصص'),
    (N'پزشک فوق‌تخصص'),
    (N'پزشک اورژانس'),
    (N'دستیار پزشک'),
    (N'پرستار'),
    (N'مسئول پرستاری'),
    (N'بهیار'),
    (N'فیزیوتراپیست'),
    (N'اپتومتریست'),
    (N'روان‌شناس'),
    (N'کارشناس تغذیه'),
    (N'کارشناس آزمایشگاه'),
    (N'تکنسین آزمایشگاه'),
    (N'تکنسین رادیولوژی'),
    (N'داروساز'),
    (N'مسئول انبار دارو'),
    (N'منشی پذیرش'),
    (N'مسئول صندوق'),
    (N'حسابدار'),
    (N'کارشناس پروندهٔ پزشکی'),
    (N'کارشناس منابع انسانی'),
    (N'مسئول انبار و تدارکات'),
    (N'کارشناس فناوری اطلاعات'),
    (N'مسئول خدمات و پشتیبانی'),
    (N'نگهبان')
) AS s(Title)
WHERE NOT EXISTS (SELECT 1 FROM Positions p WHERE p.Title = s.Title);
");
        }

        /// <summary>
        /// بازگشت — فقط ردیف‌های بذریِ همین فهرست (که هنوز به هیچ پرسنلی
        /// متصل نشده‌اند) نرم‌حذف می‌شوند؛ ردیف‌های ساخت‌شده توسط کاربر و
        /// ردیف‌های متصل به پرسنل دست‌نخورده می‌مانند تا سابقه آسیب نبیند.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // دپارتمان‌های بذریِ بدون پرسنلِ متصل
            migrationBuilder.Sql(@"
UPDATE d SET d.IsDeleted = 1, d.DeletedAt = GETUTCDATE()
FROM Departments d
WHERE d.IsDeleted = 0
  AND d.Name IN (
    N'مدیریت درمانگاه', N'پذیرش', N'اورژانس', N'درمانگاه عمومی',
    N'درمانگاه تخصصی', N'اتاق عمل', N'بخش بستری', N'آزمایشگاه',
    N'تصویربرداری و رادیولوژی', N'داروخانه', N'فیزیوتراپی', N'صندوق و مالی',
    N'پروندهٔ پزشکی', N'منابع انسانی', N'انبار و تدارکات',
    N'فناوری اطلاعات', N'پشتیبانی و خدمات')
  AND NOT EXISTS (SELECT 1 FROM Employees e WHERE e.DepartmentId = d.Id);
");

            // سمت‌های بذریِ بدون پرسنلِ متصل
            migrationBuilder.Sql(@"
UPDATE p SET p.IsDeleted = 1, p.DeletedAt = GETUTCDATE()
FROM Positions p
WHERE p.IsDeleted = 0
  AND p.Title IN (
    N'مدیر درمانگاه', N'معاون درمانگاه', N'مدیر پرستاری', N'سرپرست',
    N'پزشک عمومی', N'پزشک متخصص', N'پزشک فوق‌تخصص', N'پزشک اورژانس',
    N'دستیار پزشک', N'پرستار', N'مسئول پرستاری', N'بهیار',
    N'فیزیوتراپیست', N'اپتومتریست', N'روان‌شناس', N'کارشناس تغذیه',
    N'کارشناس آزمایشگاه', N'تکنسین آزمایشگاه', N'تکنسین رادیولوژی',
    N'داروساز', N'مسئول انبار دارو', N'منشی پذیرش', N'مسئول صندوق',
    N'حسابدار', N'کارشناس پروندهٔ پزشکی', N'کارشناس منابع انسانی',
    N'مسئول انبار و تدارکات', N'کارشناس فناوری اطلاعات',
    N'مسئول خدمات و پشتیبانی', N'نگهبان')
  AND NOT EXISTS (SELECT 1 FROM Employees e WHERE e.PositionId = p.Id);
");
        }
    }
}

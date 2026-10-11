using Microsoft.AspNetCore.Http;
using Sahand.ClinicPro.API.Middleware;
using Xunit;

namespace Sahand.ClinicPro.API.Tests.Middleware;

/// <summary>
/// تست‌های واحد استخراج نام موجودیت و شناسه از مسیرهای API — چرخهٔ چهل‌وچهارم.
/// ═══ زمینه (گزارش مالک: «لاگ عملیات هنوز هم مقادیر قبلی رو نشون نمیده») ═══
/// ریشهٔ کهنه: نام موجودیت همیشه «قطعهٔ سوم» مسیر فرض می‌شد و مسیرهای
/// زیرمنبعی (hr/employees، insurance/companies، lookups/rooms و…) هرگز به
/// جدول دیتابیسی نگاشت نمی‌شدند ← ستون OldValues لاگ همیشه تهی می‌ماند.
/// این تست‌ها الگوی جدید («قطعهٔ نزدیک‌ترین به شناسهٔ عددی») را قفل می‌کنند:
/// ۱) مسیرهای سادهٔ «/ patients/5» مثل پیش کار کنند؛
/// ۲) مسیرهای زیرمنبعی نامِ درست (نه نام کنترلر) بدهند؛
/// ۳) مسیرهای «زیرعملیات» مثل «doctors/5/schedule» به خودِ پزشک برگردند؛
/// ۴) مسیرهای بدون شناسه (users/me/shortcuts) رفتار پیشین را حفظ کنند.
/// </summary>
public class AuditLogEntityNameExtractionTests
{
    /// <summary>سازندهٔ کمکی PathString از رشتهٔ مسیر</summary>
    private static PathString PathOf(string path) => new(path);

    /// <summary>ادعای کمکی: نام موجودیت استخراج‌شده از مسیر باید مقدار مورد انتظار باشد</summary>
    private static void AssertEntity(string path, string expected)
    {
        var actual = AuditLogEntityNameProbe.ExtractEntityName(PathOf(path));
        Assert.True(expected == actual,
            $"مسیر «{path}»: انتظار «{expected}» ولی «{actual ?? "تهی"}» برگشت");
    }

    [Fact] // مسیر سادهٔ قدیمی — نام موجودیت همان قطعهٔ سوم است
    public void Simple_Route_Returns_Controller_Segment()
    {
        AssertEntity("/api/v1/patients/5", "patients");
        AssertEntity("/api/v1/users/12", "users");
        AssertEntity("/api/v1/roles/3", "roles");
    }

    [Fact] // مسیر زیرمنبعی — قطعهٔ «نزدیک به شناسه» نه نام کنترلر (چرخهٔ ۴۴)
    public void Sub_Resource_Route_Returns_Segment_Near_Id()
    {
        // پیش‌تر هر سه «hr» می‌دادند و به هیچ جدولی نگاشت نمی‌شدند
        AssertEntity("/api/v1/hr/employees/5", "employees");
        AssertEntity("/api/v1/insurance/companies/7", "companies");
        AssertEntity("/api/v1/lookups/rooms/3", "rooms");
        AssertEntity("/api/v1/lookups/services/9", "services");
        AssertEntity("/api/v1/insurance/fractionment-rules/4", "fractionment-rules");
        AssertEntity("/api/v1/prescriptions/templates/11", "templates");
        AssertEntity("/api/v1/reports/adhoc/definitions/6", "definitions");
    }

    [Fact] // مسیر «شناسه/زیرعملیات» — قطعهٔ پس از شناسه، نام موجودیت را عوض نمی‌کند
    public void Id_Then_Action_Route_Returns_Segment_Before_Id()
    {
        AssertEntity("/api/v1/doctors/5/schedule", "doctors");
        AssertEntity("/api/v1/roles/3/permissions", "roles");
        AssertEntity("/api/v1/receptions/8/status", "receptions");
        AssertEntity("/api/v1/employees/5/photo", "employees");
        AssertEntity("/api/v1/emr/care-plans/2/status", "care-plans");
    }

    [Fact] // بدون شناسه — رفتار پیشین (قطعهٔ سوم) حفظ می‌شود
    public void No_Id_Route_Falls_Back_To_Controller_Segment()
    {
        AssertEntity("/api/v1/users/me/shortcuts", "users");
        AssertEntity("/api/v1/pharmacy/consume", "pharmacy");
    }

    [Fact] // شناسه — آخرین قطعهٔ عددی از انتها (رفتار چرخهٔ ۳۵ حفظ شده)
    public void ExtractId_Returns_Last_Numeric_Segment()
    {
        Assert.Equal(5, AuditLogEntityNameProbe.ExtractId(PathOf("/api/v1/doctors/5/schedule")));
        Assert.Equal(7, AuditLogEntityNameProbe.ExtractId(PathOf("/api/v1/insurance/companies/7")));
        Assert.Equal(12, AuditLogEntityNameProbe.ExtractId(PathOf("/api/v1/users/12")));
        Assert.Null(AuditLogEntityNameProbe.ExtractId(PathOf("/api/v1/users/me/shortcuts")));
    }
}

/// <summary>
/// پل عمومی برای تست — متدهای خصوصی میدلور باید برای پروژهٔ تست در دسترس
/// باشند. ExtractEntityName و ExtractId بدون حالت‌اند و صرفاً رشته‌ای
/// کار می‌کنند؛ نمایاندنشان خطری ندارد و فقط همین دو متدِ «محض» را پوشش
/// می‌دهد (TryCaptureOldValues به DbContext نیاز دارد و در تست‌های یکپارچگی
/// بررسی می‌شود).
/// </summary>
public static partial class AuditLogEntityNameProbe
{
    /// <summary>نمایش عمومی ExtractEntityName میدلور — فقط برای تست</summary>
    public static string? ExtractEntityName(PathString path)
        => AuditLogMiddleware.ExtractEntityNameForTest(path);

    /// <summary>نمایش عمومی ExtractId میدلور — فقط برای تست</summary>
    public static long? ExtractId(PathString path)
        => AuditLogMiddleware.ExtractIdForTest(path);
}

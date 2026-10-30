using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

/// <summary>
/// کارخانه تست جریان نظرسنجی — دیتابیس درون‌حافظه‌ای اختصاصی
/// </summary>
public class SurveyApiFactory : InMemoryApiFactory
{
    private readonly Lazy<Task<string>> _adminToken;

    /// <summary>یک نشست واقعی برای مجموعهٔ تست، مانند استفادهٔ معمول کلاینت.</summary>
    public SurveyApiFactory() => _adminToken = new Lazy<Task<string>>(AuthenticateAsync);

    /// <summary>نام دیتابیس درون‌حافظه‌ای</summary>
    protected override string DatabaseName => "SurveyFlowDb";

    /// <summary>توکن ورود مشترک؛ سیاست محدودسازی واقعی API تغییر نمی‌کند.</summary>
    public Task<string> AdminTokenAsync() => _adminToken.Value;

    private async Task<string> AuthenticateAsync()
    {
        using var client = CreateClient();
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { usernameOrEmail = "admin", password = "Admin@123" });
        login.StatusCode.Should().Be(HttpStatusCode.OK,
            because: $"بذر ادمین باید امکان ورود دهد — بدنه: {await login.Content.ReadAsStringAsync()}");
        using var packet = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return packet.RootElement.GetProperty("data").GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("توکن ادمین در پاسخ ورود وجود ندارد");
    }
}

/// <summary>مجموعه تست جریان نظرسنجی</summary>
[CollectionDefinition("SurveyFlow")]
public class SurveyCollectionFixture : ICollectionFixture<SurveyApiFactory>
{
}

/// <summary>
/// تست جریان کامل نظرسنجی — گام V4 چرخه هفتم:
/// ایجاد → فهرست → ثبت پاسخ‌های چندپزشکه → نتایج و آمار با میانگین صحیح
/// → فعال/غیرفعال → ارسال لینک پیامکی به ویزیت‌های امروز
/// </summary>
[Collection("SurveyFlow")]
public class SurveyFlowTests
{
    private readonly SurveyApiFactory _factory;
    private static int _seedSequence;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>سازنده تست — تزریق کارخانه</summary>
    public SurveyFlowTests(SurveyApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// گام ۱ تا ۳: ایجاد نظرسنجی + فهرست + ویرایش باید بدون خطا طی شود
    /// </summary>
    [Fact]
    public async Task Create_List_Update_Survey_Should_Succeed()
    {
        var client = await LoginAsync();

        // ── ایجاد نظرسنجی با سوال JSON ──
        var create = await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            title = "نظرسنجی رضایت مهرماه",
            description = "سنجش کیفیت خدمات",
            questions = "[{\"text\":\"میزان رضایت کلی؟\",\"type\":\"rating\"}]",
            surveyType = 1
        });
        create.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "ادمین مجوز surveys.manage و داده معتبر دارد");
        var created = await ReadAsync<JsonElementPacket>(create);
        created.Data.GetProperty("surveyId").GetInt64().Should().BeGreaterThan(0);
        var surveyId = created.Data.GetProperty("surveyId").GetInt64();

        // ── فهرست شامل نظرسنجی جدید ──
        var list = await client.GetAsync("/api/v1/surveys");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await ReadAsync<ListPacket<SurveyListDto>>(list);
        rows.Data.Should().ContainSingle(s => s.Id == surveyId, "نظرسنجی ایجادشده در فهرست است");
        var row = rows.Data.Single(s => s.Id == surveyId);
        row.IsActive.Should().BeTrue("ایجاد با وضعیت فعال");
        row.ResponseCount.Should().Be(0);

        // ── ویرایش عنوان و غیرفعال‌سازی از طریق PUT ──
        var update = await client.PutAsJsonAsync($"/api/v1/surveys/{surveyId}", new
        {
            title = "نظرسنجی رضایت — ویرایش‌شده",
            description = "نسخه دوم",
            questions = (string?)null,
            surveyType = 2,
            isActive = false,
            editVersion = row.EditVersion
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "ویرایش با مجوز manage مجاز است");

        var list2 = await client.GetAsync("/api/v1/surveys");
        var rows2 = await ReadAsync<ListPacket<SurveyListDto>>(list2);
        rows2.Data.Single(s => s.Id == surveyId).IsActive.Should().BeFalse(
            "isActive=false در ویرایش اعمال می‌شود");
    }

    /// <summary>
    /// ثبت پاسخ‌های چندپزشکه و صحت محاسبات نتایج:
    /// میانگین کل + توزیع ستاره + میانگین تفکیکی هر پزشک
    /// </summary>
    [Fact]
    public async Task SubmitResponses_Results_Should_Compute_Averages_Correctly()
    {
        var (client, doctorA, doctorB, patientId) = await SeedDoctorsAndPatientAsync();

        // ── ایجاد نظرسنجی فعال ──
        var create = await client.PostAsJsonAsync("/api/v1/surveys", new
        {
            title = "نظرسنجی ارزیابی پزشکان",
            surveyType = 3
        });
        var created = await ReadAsync<JsonElementPacket>(create);
        var surveyId = created.Data.GetProperty("surveyId").GetInt64();

        // ── سه پاسخ: پزشک الف امتیاز ۵ و ۴، پزشک ب امتیاز ۱ ──
        await SubmitAsync(client, surveyId, patientId, doctorA, rating: 5, comment: "برخورد عالی بود");
        await SubmitAsync(client, surveyId, patientId, doctorB, rating: 1, comment: "انتظار بیش از این را داشتم");

        await SubmitAsync(client, surveyId, patientId, doctorA, rating: 4, comment: null as string);

        // ── نتایج: کل ۳ پاسخ، میانگین (5+4+1)/3 ≈ 3.33 ──
        var results = await client.GetAsync($"/api/v1/surveys/{surveyId}/results");
        results.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "ادمین مجوز surveys.view دارد");
        var payload = await ReadAsync<ResultsPacket>(results);

        payload.Data.Summary.TotalResponses.Should().Be(3);
        payload.Data.Summary.AverageRating.Should().BeApproximately(3.33, 0.01);

        // توزیع ستاره: ۵→۱، ۴→۱، ۱→۱
        payload.Data.StarDistribution.Should().BeEquivalentTo(new[]
        {
            new { Star = 5, Count = 1 },
            new { Star = 4, Count = 1 },
            new { Star = 3, Count = 0 },
            new { Star = 2, Count = 0 },
            new { Star = 1, Count = 1 }
        });

        // میانگین تفکیکی: پزشک الف (5+4)/2=4.5 — پزشک ب 1
        payload.Data.DoctorRatings.Should().HaveCount(2);
        payload.Data.DoctorRatings.Should().Contain(d =>
            d.DoctorId == doctorA && d.Average.Equals(4.5));
        payload.Data.DoctorRatings.Should().Contain(d =>
            d.DoctorId == doctorB && d.Average.Equals(1.0));

        // نظرات آزاد — تازه‌ترین ۲۰ مورد
        payload.Data.RecentComments.Should().OnlyContain(c => !string.IsNullOrWhiteSpace(c.Comment));
        payload.Data.RecentComments.Should().Contain(c => c.Comment!.Contains("برخورد عالی"));
    }

    /// <summary>
    /// ثبت پاسخ روی نظرسنجی غیرفعال باید رد شود
    /// </summary>
    [Fact]
    public async Task SubmitResponse_To_Inactive_Survey_Should_Fail()
    {
        var client = await LoginAsync();

        var create = await client.PostAsJsonAsync("/api/v1/surveys", new { title = "غیرفعال‌شده" });
        var created = await ReadAsync<JsonElementPacket>(create);
        var surveyId = created.Data.GetProperty("surveyId").GetInt64();

        // غیرفعال‌سازی با toggle
        var toggle = await client.PostAsJsonAsync($"/api/v1/surveys/{surveyId}/toggle-active", new { editVersion = await GetEditVersion(client, surveyId), desiredActive = false });
        toggle.StatusCode.Should().Be(HttpStatusCode.OK);

        var submit = await client.PostAsJsonAsync($"/api/v1/surveys/{surveyId}/responses",
            new { rating = (byte)5 });
        submit.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "ثبت پاسخ فقط روی نظرسنجی فعال مجاز است");
    }

    /// <summary>
    /// ارسال لینک پیامکی: ویزیت امروزِ بیمارِ موبایل‌دار باید یک ردیف Queued در صف پیامک بسازد
    /// </summary>
    [Fact]
    public async Task SendLinks_Should_Queue_Sms_For_Today_Visits()
    {
        var (client, doctorId, _, patientId) = await SeedDoctorsAndPatientAsync();

        // ویزیت امروز برای بیمار
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
            context.Visits.Add(new Visit
            {
                PatientId = patientId,
                DoctorId = doctorId,
                VisitDate = DateTime.UtcNow,
                Status = Domain.Enums.VisitStatus.Completed
            });
            var futurePatient = new Patient
            {
                MedicalRecordNo = "MRN-SURVEY-FUTURE", NationalCode = "0055500099",
                FirstName = "آینده", LastName = "آزمایشی", Mobile = "09129990099",
                Gender = Domain.Enums.Gender.Female, IsActive = true
            };
            var blankMobile = new Patient
            {
                MedicalRecordNo = "MRN-SURVEY-BLANK", NationalCode = "0055500088",
                FirstName = "بدون شماره", LastName = "آزمایشی", Mobile = "   ",
                Gender = Domain.Enums.Gender.Female, IsActive = true
            };
            context.Patients.AddRange(futurePatient, blankMobile);
            await context.SaveChangesAsync();
            context.Visits.Add(new Visit { PatientId = blankMobile.Id, DoctorId = doctorId,
                VisitDate = DateTime.UtcNow, Status = Domain.Enums.VisitStatus.Completed });
            context.Visits.Add(new Visit { PatientId = futurePatient.Id, DoctorId = doctorId,
                VisitDate = DateTime.UtcNow.AddDays(3), Status = Domain.Enums.VisitStatus.Completed });
            await context.SaveChangesAsync();
        }

        // نظرسنجی فعال + ارسال لینک
        var create = await client.PostAsJsonAsync("/api/v1/surveys", new { title = "امروزی" });
        var created = await ReadAsync<JsonElementPacket>(create);
        var surveyId = created.Data.GetProperty("surveyId").GetInt64();

        // بدون نشانی صریح، پیامک به دامنهٔ فرضی و بررسی‌نشده ارسال نمی‌شود.
        var missingUrl = await client.PostAsJsonAsync($"/api/v1/surveys/{surveyId}/send-links",
            new { baseUrl = (string?)null });
        missingUrl.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var send = await client.PostAsJsonAsync($"/api/v1/surveys/{surveyId}/send-links",
            new { baseUrl = "https://example.com" });
        send.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "نظرسنجی فعال و ویزیت امروز موجود است");
        var payload = await ReadAsync<SendLinksPacket>(send);
        payload.Data.QueuedCount.Should().BeGreaterThanOrEqualTo(1,
            "دست‌کم ویزیت امروزِ همین بیمار در صف می‌نشیند");
        payload.Data.Link.Should().Contain($"/s/{surveyId}");

        // صحت صف پیامک — وضعیت Queued و متن شامل لینک
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
            var queued = await context.SmsQueue
                .Where(q => q.Status == Domain.Enums.SmsStatus.Queued)
                .ToListAsync();
            queued.Should().Contain(q => q.Recipient == "09129990001");
            queued.Should().NotContain(q => q.Recipient == "09129990099");
            queued.Should().NotContain(q => string.IsNullOrWhiteSpace(q.Recipient));
            queued.Should().Contain(q => q.Message != null && q.Message.Contains($"/s/{surveyId}"));
        }
    }

    [Fact]
    public async Task Invalid_or_empty_responses_are_rejected_and_audit_does_not_copy_answers()
    {
        var client = await LoginAsync();
        var create = await client.PostAsJsonAsync("/api/v1/surveys", new { title = "محرمانگی پاسخ" });
        var surveyId = (await ReadAsync<JsonElementPacket>(create)).Data.GetProperty("surveyId").GetInt64();
        (await client.PostAsJsonAsync($"/api/v1/surveys/{surveyId}/responses", new { rating = 6 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync($"/api/v1/surveys/{surveyId}/responses", new { comments = new string('x', 2001) })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync($"/api/v1/surveys/{surveyId}/responses", new { comments = " " })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        const string privateComment = "PRIVATE-SURVEY-COMMENT-83BD";
        (await client.PostAsJsonAsync($"/api/v1/surveys/{surveyId}/responses", new { rating = 4, comments = privateComment })).StatusCode.Should().Be(HttpStatusCode.OK);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        (await db.SurveyResponses.AnyAsync(r => r.SurveyId == surveyId && r.Comments == privateComment)).Should().BeTrue();
        (await db.AuditLogs.AnyAsync(a => a.NewValues != null && a.NewValues.Contains(privateComment))).Should().BeFalse();
    }

    [Fact]
    public async Task Structured_answers_are_validated_and_keep_original_question_text()
    {
        var client = await LoginAsync();
        const string questions = "[{\"text\":\"کیفیت؟\",\"type\":\"rating\"},{\"text\":\"انتخاب؟\",\"type\":\"choice\",\"options\":[\"الف\",\"ب\"]},{\"text\":\"توضیح؟\",\"type\":\"text\"}]";
        var create = await client.PostAsJsonAsync("/api/v1/surveys", new { title = "پاسخ پویا", questions, surveyType = 1 });
        var created = await ReadAsync<JsonElementPacket>(create);
        var surveyId = created.Data.GetProperty("surveyId").GetInt64();

        var invalid = await client.PostAsJsonAsync($"/api/v1/surveys/{surveyId}/responses", new
        { questionsSnapshot = questions, answers = new[] { "6", "الف", "متن" } });
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var ok = await client.PostAsJsonAsync($"/api/v1/surveys/{surveyId}/responses", new
        { questionsSnapshot = questions, answers = new[] { "5", "ب", "پاسخ آزاد" }, rating = 4 });
        ok.StatusCode.Should().Be(HttpStatusCode.OK);

        const string changed = "[{\"text\":\"پرسش تغییر یافته\",\"type\":\"text\"}]";
        var update = await client.PutAsJsonAsync($"/api/v1/surveys/{surveyId}", new { title = "پاسخ پویا", questions = changed, surveyType = 1, editVersion = await GetEditVersion(client, surveyId) });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var stale = await client.PostAsJsonAsync($"/api/v1/surveys/{surveyId}/responses", new
        { questionsSnapshot = questions, answers = new[] { "5", "ب", "متن" } });
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var results = await client.GetAsync($"/api/v1/surveys/{surveyId}/results");
        var payload = await ReadAsync<JsonElementPacket>(results);
        var recent = payload.Data.GetProperty("recentAnswers");
        recent.GetArrayLength().Should().Be(1);
        using var snapshot = JsonDocument.Parse(recent[0].GetProperty("answers").GetString()!);
        snapshot.RootElement[0].GetProperty("text").GetString().Should().Be("کیفیت؟");
        snapshot.RootElement[1].GetProperty("answer").GetString().Should().Be("ب");
        snapshot.RootElement[2].GetProperty("answer").GetString().Should().Be("پاسخ آزاد");
    }

    [Fact]
    public async Task Survey_question_definitions_are_validated_before_create_or_change()
    {
        var client = await LoginAsync();
        var invalidDefinitions = new[]
        {
            "{invalid", "{}", "[{\"text\":\"\",\"type\":\"text\"}]",
            "[{\"text\":\"سؤال\",\"type\":\"unknown\"}]",
            "[{\"text\":\"سؤال\",\"type\":\"choice\",\"options\":[\"تنها\"]}]",
            "[{\"text\":\"سؤال\",\"q\":\"دیگر\"}]",
            "[{\"text\":\"سؤال\",\"type\":\"rating\",\"options\":[\"x\",\"y\"]}]",
            "[" + string.Join(",", Enumerable.Repeat("{\"text\":\"سؤال\",\"type\":\"text\"}", 101)) + "]"
        };
        foreach (var questions in invalidDefinitions)
            (await client.PostAsJsonAsync("/api/v1/surveys", new { title = "فرم نامعتبر", questions }))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        const string valid = "[{\"q\":\"انتخاب؟\",\"type\":\"choice\",\"options\":\"الف|ب\"}]";
        var create = await client.PostAsJsonAsync("/api/v1/surveys", new { title = "فرم معتبر", questions = valid });
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = (await ReadAsync<JsonElementPacket>(create)).Data.GetProperty("surveyId").GetInt64();
        var invalidUpdate = await client.PutAsJsonAsync($"/api/v1/surveys/{id}", new { title = "نباید ذخیره شود", questions = invalidDefinitions[0] });
        invalidUpdate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var stored = await db.Surveys.SingleAsync(s => s.Id == id);
        stored.Title.Should().Be("فرم معتبر");
        stored.Questions.Should().Be(valid);
    }

    [Fact]
    public async Task Existing_invalid_questions_can_be_preserved_unchanged_when_disabling_survey()
    {
        var client = await LoginAsync();
        long id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
            var legacy = new Survey { Title = "فرم قدیمی", Questions = "invalid legacy", IsActive = true };
            db.Surveys.Add(legacy);
            await db.SaveChangesAsync();
            id = legacy.Id;
        }
        var update = await client.PutAsJsonAsync($"/api/v1/surveys/{id}", new
        { title = "فرم قدیمی", questions = "invalid legacy", isActive = false, editVersion = await GetEditVersion(client, id) });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        using var verify = _factory.Services.CreateScope();
        var saved = await verify.ServiceProvider.GetRequiredService<ClinicProDbContext>().Surveys.SingleAsync(s => s.Id == id);
        saved.Questions.Should().Be("invalid legacy");
        saved.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Response_rejects_invalid_or_mismatched_patient_doctor_and_visit()
    {
        var (client, doctorA, doctorB, patientId) = await SeedDoctorsAndPatientAsync();
        long visitId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
            var visit = new Visit { PatientId = patientId, DoctorId = doctorA, VisitDate = DateTime.UtcNow };
            db.Visits.Add(visit);
            await db.SaveChangesAsync();
            visitId = visit.Id;
        }
        var create = await client.PostAsJsonAsync("/api/v1/surveys", new { title = "ارتباط پاسخ با ویزیت" });
        var surveyId = (await ReadAsync<JsonElementPacket>(create)).Data.GetProperty("surveyId").GetInt64();
        var url = $"/api/v1/surveys/{surveyId}/responses";
        (await client.PostAsJsonAsync(url, new { patientId = long.MaxValue, rating = 4 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync(url, new { doctorId = long.MaxValue, rating = 4 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync(url, new { visitId = long.MaxValue, rating = 4 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync(url, new { patientId, doctorId = doctorB, visitId, rating = 4 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync(url, new { patientId, doctorId = doctorA, visitId, rating = 4 })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync(url, new { rating = 5 })).StatusCode.Should().Be(HttpStatusCode.OK);
        using var verify = _factory.Services.CreateScope();
        var dbVerify = verify.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        (await dbVerify.SurveyResponses.CountAsync(r => r.SurveyId == surveyId)).Should().Be(2);
    }

    [Fact]
    public async Task Result_dates_and_times_use_iran_day_even_across_midnight()
    {
        var client = await LoginAsync();
        var create = await client.PostAsJsonAsync("/api/v1/surveys", new { title = "تاریخ پاسخ" });
        var id = (await ReadAsync<JsonElementPacket>(create)).Data.GetProperty("surveyId").GetInt64();
        // ۲۱:۰۰ UTC برابر ۰۰:۳۰ روز بعد در تهران است. Kind مانند دادهٔ SQL نامشخص است.
        var storedUtc = new DateTime(2026, 1, 1, 21, 0, 0, DateTimeKind.Unspecified);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
            db.SurveyResponses.Add(new SurveyResponse
            {
                SurveyId = id, Comments = "نظر آزمایشی", Answers = "[]", SubmittedAt = storedUtc, CreatedAt = storedUtc
            });
            await db.SaveChangesAsync();
        }
        var result = await client.GetAsync($"/api/v1/surveys/{id}/results");
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await ReadAsync<JsonElementPacket>(result)).Data;
        var expectedDay = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(new DateTime(2026, 1, 2));
        data.GetProperty("recentComments")[0].GetProperty("submittedShamsi").GetString().Should().Be(expectedDay + " — 00:30");
        data.GetProperty("recentAnswers")[0].GetProperty("submittedShamsi").GetString().Should().Be(expectedDay);
    }

    /* ─── کمکی‌ها ─── */

    /// <summary>ورود ادمین و کلاینت احرازهویت‌شده</summary>
    private async Task<HttpClient> LoginAsync()
    {
        var token = await _factory.AdminTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>بذر دو پزشک + یک بیمار موبایل‌دار — شناسه‌ها را برمی‌گرداند</summary>
    private async Task<(HttpClient client, long doctorA, long doctorB, long patientId)>
        SeedDoctorsAndPatientAsync()
    {
        var client = await LoginAsync();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var sequence = Interlocked.Increment(ref _seedSequence);

        // دو پزشک مستقل با پرسنل مجزا و شناسه‌های غیرتکراری در کارخانه مشترک
        var ids = new long[2];
        for (var i = 0; i < 2; i++)
        {
            var emp = new Employee
            {
                FirstName = "پزشک",
                LastName = $"تست‌{i + 1}",
                NationalCode = $"7{sequence * 2 + i:000000000}",
                Mobile = "09120000001",
                IsActive = true
            };
            context.Employees.Add(emp);
            await context.SaveChangesAsync();
            var doc = new Doctor { EmployeeId = emp.Id, IsActive = true, ConsultationFee = 100000m };
            context.Doctors.Add(doc);
            await context.SaveChangesAsync();
            ids[i] = doc.Id;
        }

        // بیمار موبایل‌دار — شماره یکتا برای ادعای صف پیامک
        var patient = new Patient
        {
            MedicalRecordNo = $"MRN-SURVEY-{sequence:000000}",
            NationalCode = $"8{sequence:000000000}",
            FirstName = "سارا",
            LastName = "آزمایشی",
            Mobile = "09129990001",
            Gender = Domain.Enums.Gender.Female,
            IsActive = true
        };
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        return (client, ids[0], ids[1], patient.Id);
    }

    /// <summary>ثبت یک پاسخ</summary>
    private static async Task SubmitAsync(HttpClient client, long surveyId, long patientId,
        long doctorId, byte rating, string? comment)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/surveys/{surveyId}/responses", new
        {
            patientId,
            doctorId,
            rating,
            comments = comment
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: $"پاسخ با امتیاز {rating} روی نظرسنجی فعال معتبر است");
    }

    /// <summary>خواندن پاسخ با نوع عمومی سبک</summary>
    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(json, Json);
        result.Should().NotBeNull("پاسخ باید JSON معتبر باشد");
        return result!;
    }

    /* ─── بسته‌های پاسخ ─── */

    private static async Task<string> GetEditVersion(HttpClient client, long id)
    {
        var response = await client.GetAsync("/api/v1/surveys");
        var rows = await ReadAsync<ListPacket<SurveyListDto>>(response);
        return rows.Data.Single(s => s.Id == id).EditVersion;
    }

    [Fact]
    public async Task Stale_or_unversioned_survey_editor_cannot_overwrite_newer_changes()
    {
        var client = await LoginAsync();
        var created = await client.PostAsJsonAsync("/api/v1/surveys", new { title = "نسخه اول", questions = "[]" });
        var id = (await ReadAsync<JsonElementPacket>(created)).Data.GetProperty("surveyId").GetInt64();
        var version = await GetEditVersion(client, id);
        var saved = await client.PutAsJsonAsync($"/api/v1/surveys/{id}", new { title = "تغییر اول", questions = "[]", editVersion = version });
        saved.StatusCode.Should().Be(HttpStatusCode.OK);
        var stale = await client.PutAsJsonAsync($"/api/v1/surveys/{id}", new { title = "تغییر کهنه", questions = "[]", editVersion = version });
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var oldClient = await client.PutAsJsonAsync($"/api/v1/surveys/{id}", new { title = "کلاینت قدیمی", questions = "[]" });
        oldClient.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var rows = await ReadAsync<ListPacket<SurveyListDto>>(await client.GetAsync("/api/v1/surveys"));
        rows.Data.Single(s => s.Id == id).Title.Should().Be("تغییر اول");
    }


    [Fact]
    public async Task Unsupported_type_is_rejected_and_omitted_type_does_not_change_existing_type()
    {
        var client = await LoginAsync();
        (await client.PostAsJsonAsync("/api/v1/surveys", new { title = "نوع نامعتبر", surveyType = 99 }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var created = await client.PostAsJsonAsync("/api/v1/surveys", new { title = "خدمات", surveyType = 2 });
        var id = (await ReadAsync<JsonElementPacket>(created)).Data.GetProperty("surveyId").GetInt64();
        var version = await GetEditVersion(client, id);
        (await client.PutAsJsonAsync($"/api/v1/surveys/{id}", new { title = "تغییر", surveyType = 99, editVersion = version }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PutAsJsonAsync($"/api/v1/surveys/{id}", new { title = "تغییر", editVersion = version }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        using var scope = _factory.Services.CreateScope();
        var survey = await scope.ServiceProvider.GetRequiredService<ClinicProDbContext>().Surveys.SingleAsync(x => x.Id == id);
        survey.SurveyType.Should().Be(Sahand.ClinicPro.Domain.Enums.SurveyType.Service);
    }


    [Fact]
    public async Task Stale_toggle_does_not_invert_status_or_repeat()
    {
        var client = await LoginAsync();
        var created = await client.PostAsJsonAsync("/api/v1/surveys", new { title = "تغییر وضعیت امن" });
        var id = (await ReadAsync<JsonElementPacket>(created)).Data.GetProperty("surveyId").GetInt64();
        var version = await GetEditVersion(client, id);
        var first = await client.PostAsJsonAsync($"/api/v1/surveys/{id}/toggle-active", new { editVersion = version, desiredActive = false });
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var retry = await client.PostAsJsonAsync($"/api/v1/surveys/{id}/toggle-active", new { editVersion = version, desiredActive = false });
        retry.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var rows = await ReadAsync<ListPacket<SurveyListDto>>(await client.GetAsync("/api/v1/surveys"));
        rows.Data.Single(x => x.Id == id).IsActive.Should().BeFalse();
    }


    [Fact]
    public async Task Delete_requires_the_observed_version_and_keeps_response_rows()
    {
        var client = await LoginAsync();
        var created = await client.PostAsJsonAsync("/api/v1/surveys", new { title = "حذف امن" });
        var id = (await ReadAsync<JsonElementPacket>(created)).Data.GetProperty("surveyId").GetInt64();
        var old = await GetEditVersion(client, id);
        (await client.PostAsJsonAsync($"/api/v1/surveys/{id}/responses", new { rating = 4 }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync($"/api/v1/surveys/{id}/toggle-active", new { editVersion = old, desiredActive = false }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.DeleteAsync($"/api/v1/surveys/{id}?editVersion={old}")).StatusCode.Should().Be(HttpStatusCode.Conflict);
        var current = await GetEditVersion(client, id);
        (await client.DeleteAsync($"/api/v1/surveys/{id}?editVersion={current}")).StatusCode.Should().Be(HttpStatusCode.OK);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var stored = await db.Surveys.IgnoreQueryFilters().SingleAsync(x => x.Id == id);
        stored.IsDeleted.Should().BeTrue();
        (await db.SurveyResponses.IgnoreQueryFilters().CountAsync(x => x.SurveyId == id)).Should().Be(1);
    }

    private sealed record JsonElementPacket(bool IsSuccess, JsonElement Data);

    private sealed record ListPacket<T>(bool IsSuccess, List<T> Data);

    private sealed record SurveyListDto(long Id, string Title, bool IsActive, int ResponseCount, string EditVersion);

    private sealed record ResultsPacket(bool IsSuccess, ResultsData Data);

    private sealed class ResultsData
    {
        public ResultsSummary Summary { get; set; } = new();
        public List<StarRow> StarDistribution { get; set; } = new();
        public List<DoctorRow> DoctorRatings { get; set; } = new();
        public List<CommentRow> RecentComments { get; set; } = new();
    }

    private sealed class ResultsSummary
    {
        public int TotalResponses { get; set; }
        public double AverageRating { get; set; }
        public int ResponsesLast30Days { get; set; }
    }

    private sealed record StarRow(int Star, int Count);

    private sealed record DoctorRow(long DoctorId, string DoctorName, int ResponseCount, double Average);

    private sealed class CommentRow
    {
        public long Id { get; set; }
        public string PatientName { get; set; } = "";
        public byte? Rating { get; set; }
        public string? Comment { get; set; }
    }

    private sealed record SendLinksPacket(bool IsSuccess, SendLinksData Data);

    private sealed class SendLinksData
    {
        public int QueuedCount { get; set; }
        public string Link { get; set; } = "";
    }
}

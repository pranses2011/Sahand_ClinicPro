using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Infrastructure.Persistence;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>پاسخ‌گویی عمومی اختیاری؛ دعوت در fragment است و در مسیر وب ارسال نمی‌شود.</summary>
[ApiController]
[Route("s")]
[AllowAnonymous]
[EnableRateLimiting("public-survey")]
public class PublicSurveyController : ControllerBase
{
    private readonly ClinicProDbContext _db;
    private readonly IConfiguration _config;

    /// <summary>سازنده.</summary>
    public PublicSurveyController(ClinicProDbContext db, IConfiguration config) { _db = db; _config = config; }
    private bool Enabled => _config.GetValue<bool>("Surveys:EnableBuiltInPublic");

    private void SecurityHeaders()
    {
        Response.Headers.CacheControl = "no-store, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["X-Frame-Options"] = "DENY";
    }

    /// <summary>صفحه پاسخ مستقل، بدون منابع خارجی و بدون اطلاعات بیمار.</summary>
    [HttpGet]
    public IActionResult Page()
    {
        if (!Enabled) return NotFound();
        SecurityHeaders();
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
        Response.Headers["Content-Security-Policy"] = $"default-src 'none'; script-src 'nonce-{nonce}'; style-src 'unsafe-inline'; connect-src 'self'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";
        return Content(Html.Replace("{{nonce}}", nonce), "text/html; charset=utf-8");
    }

    /// <summary>خواندن فرم تنها با دعوت معتبر و مصرف‌نشده.</summary>
    [HttpPost("read")]
    [RequestSizeLimit(4096)]
    public async Task<IActionResult> Read([FromBody] TokenRequest request, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        SecurityHeaders();
        var invitation = await FindInvitation(request.Token, false, ct);
        if (invitation is null) return Unavailable();
        var survey = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == invitation.SurveyId && s.IsActive && !s.IsDeleted, ct);
        if (survey is null || !SurveysController.ValidQuestions(survey.Questions)) return Unavailable();
        return Ok(new { survey.Title, survey.Description, survey.Questions });
    }

    /// <summary>ثبت پاسخ ناشناس و مصرف همزمان دعوت، در یک SaveChanges تراکنشی.</summary>
    [HttpPost("submit")]
    // JSON حاوی عکس سؤال‌ها و پاسخ‌های فارسی/Unicode ممکن است چند برابر
    // تعداد نویسه بایت داشته باشد؛ سقف همچنان محدود است، نه ۲۵۰ هزار بایت.
    [RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> Submit([FromBody] PublicAnswerRequest request, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        SecurityHeaders();
        var invitation = await FindInvitation(request.Token, true, ct);
        if (invitation is null) return Unavailable();
        var survey = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == invitation.SurveyId && s.IsActive && !s.IsDeleted, ct);
        if (survey is null || !SurveysController.ValidQuestions(survey.Questions)) return Unavailable();
        if (request.Rating is not null && request.Rating is not (>= 1 and <= 5) || request.Comments?.Trim().Length > 2000)
            return BadRequest(new { message = "امتیاز یا نظر واردشده معتبر نیست." });
        if (!string.Equals(survey.Questions, request.QuestionsSnapshot, StringComparison.Ordinal))
            return Conflict(new { message = "سؤال‌ها تغییر کرده‌اند؛ لینک را دوباره باز کنید." });
        if (!SurveysController.TryBuildAnswerSnapshot(survey.Questions, request.Answers ?? new(), out var answers))
            return BadRequest(new { message = "لطفاً همه سؤال‌ها را به‌درستی پاسخ دهید." });
        if ((request.Answers is null || request.Answers.Count == 0) && request.Rating is null && string.IsNullOrWhiteSpace(request.Comments))
            return BadRequest(new { message = "حداقل یک پاسخ، امتیاز یا نظر وارد کنید." });

        var now = DateTime.UtcNow;
        // UsedAt یک concurrency token است: دو ارسال همزمان با مقدار اولیهٔ تهی
        // نمی‌توانند هر دو update کنند. EF درج پاسخ و update را در یک تراکنش
        // SaveChanges قرار می‌دهد و در تعارض، درج پاسخ نیز برمی‌گردد.
        invitation.UsedAt = now;
        _db.SurveyResponses.Add(new SurveyResponse
        {
            SurveyId = invitation.SurveyId, Answers = answers, Rating = request.Rating,
            Comments = request.Comments?.Trim(), SubmittedAt = now, CreatedAt = now
        });
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Unavailable(); }
        return Ok(new { message = "پاسخ شما ثبت شد. سپاس از همراهی شما." });
    }

    private async Task<SurveyInvitation?> FindInvitation(string? token, bool tracked, CancellationToken ct)
    {
        var hash = SurveyPublicToken.Hash(token);
        if (hash is null) return null;
        var query = tracked ? _db.SurveyInvitations.AsTracking() : _db.SurveyInvitations.AsNoTracking();
        var now = DateTime.UtcNow;
        return await query.SingleOrDefaultAsync(i => i.TokenHash == hash && i.UsedAt == null && i.ExpiresAt > now, ct);
    }

    private IActionResult Unavailable() => StatusCode(410, new { message = "این پیوند نامعتبر، منقضی یا قبلاً استفاده شده است." });

    /// <summary>توکن دعوت.</summary>
    public record TokenRequest(string? Token);
    /// <summary>پاسخ عمومی بدون شناسه بیمار، پزشک یا ویزیت.</summary>
    public record PublicAnswerRequest(string? Token, string? QuestionsSnapshot, List<string?>? Answers, byte? Rating, string? Comments);

    private const string Html = """
<!doctype html><html lang="fa" dir="rtl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>نظرسنجی درمانگاه</title>
<style>body{font-family:Tahoma,sans-serif;background:#f4f8fb;color:#18334b;margin:0;padding:24px}main{max-width:650px;margin:auto;background:white;padding:24px;border-radius:16px;box-shadow:0 4px 20px #2342}label{display:block;margin-top:20px;font-weight:bold}input,select,textarea{box-sizing:border-box;width:100%;padding:12px;margin-top:8px;border:1px solid #aac;border-radius:8px;font:inherit}button{background:#167c91;color:white;padding:12px 28px;border:0;border-radius:8px;margin-top:20px;font:inherit;cursor:pointer}button:disabled{opacity:.5}#message{line-height:2}small{display:block;margin-top:12px;color:#555}</style></head><body><main><h1 id="title">نظرسنجی درمانگاه</h1><p id="description"></p><p id="message">در حال دریافت فرم…</p><form id="form" hidden><div id="questions"></div><label>امتیاز کلی (اختیاری)<select id="rating"><option value="">انتخاب کنید</option><option>1</option><option>2</option><option>3</option><option>4</option><option>5</option></select></label><label>نظر آزاد (اختیاری)<textarea id="comments" maxlength="2000"></textarea></label><small>از وارد کردن اطلاعات شناسایی یا اطلاعات پزشکی حساس در متن آزاد خودداری کنید.</small><button id="submit" type="submit">ثبت پاسخ</button></form></main>
<script nonce="{{nonce}}">
'use strict';
const token = location.hash.slice(1), base = location.pathname.replace(/\/?$/, '/');
const el = id => document.getElementById(id); let snapshot = null, fields = [];
async function post(path, body) { const response = await fetch(base + path, {method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body),cache:'no-store',credentials:'omit'}); let data={}; try{data=await response.json()}catch{} if(!response.ok) throw Error(data.message || 'ارتباط با سرور برقرار نشد.'); return data; }
async function load(){ try { const data = await post('read',{token}); snapshot=data.questions; el('title').textContent=data.title; el('description').textContent=data.description||''; const qs=JSON.parse(snapshot||'[]');
for(const q of qs){const label=document.createElement('label'); label.textContent=q.text||q.q; const type=q.type||'rating'; let field;
if(type==='text'){field=document.createElement('textarea');field.maxLength=2000;}else{field=document.createElement('select');const blank=document.createElement('option');blank.value='';blank.textContent='انتخاب کنید';field.append(blank);const options=type==='rating'?['1','2','3','4','5']:(Array.isArray(q.options)?q.options:q.options.split('|').map(x=>x.trim()).filter(Boolean));for(const value of options){const opt=document.createElement('option');opt.value=value;opt.textContent=value;field.append(opt)}}
field.required=true;label.append(field);el('questions').append(label);fields.push(field)} el('message').textContent='';el('form').hidden=false;
}catch(e){el('message').textContent=e.message} }
el('form').addEventListener('submit',async e=>{e.preventDefault(); const button=el('submit');button.disabled=true;el('message').textContent='در حال ثبت…'; try{const data=await post('submit',{token,questionsSnapshot:snapshot,answers:fields.map(x=>x.value),rating:el('rating').value?Number(el('rating').value):null,comments:el('comments').value});el('form').hidden=true;el('message').textContent=data.message; history.replaceState(null,'',location.pathname);}catch(err){el('message').textContent=err.message+' اگر ارتباط قطع شده است، پیش از تلاش مجدد احتمال ثبت پاسخ را در نظر بگیرید.';button.disabled=false}});
load();
</script></body></html>
""";
}

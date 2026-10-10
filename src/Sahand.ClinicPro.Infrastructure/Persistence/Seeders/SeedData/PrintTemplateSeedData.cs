using System.Collections.Generic;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders.SeedData;

/// <summary>
/// قالب‌های چاپ پیش‌فرض — چرخه ۱۳ (درخواست ۱-۶)
/// ۹ قالب استاندارد HTML با متغیرهای {{...}} — هر درمانگاه می‌تواند ویرایش/کپی کند
/// </summary>
public static class PrintTemplateSeedData
{
    /// <summary>ردیف قالب بذر</summary>
    public sealed record TemplateRow(
        string TemplateName,
        int Type, // PrintTemplateType عددی
        string? Description,
        string HtmlBody,
        string PaperSize = "A5",
        string Orientation = "Portrait",
        int FontSize = 13,
        string AccentColor = "#16355E");

    /// <summary>فهرست ۹ قالب پیش‌فرض</summary>
    public static readonly IReadOnlyList<TemplateRow> Rows = new TemplateRow[]
    {
        new("فاکتور رسمی — استاندارد", 1,
            "فاکتور و قبض پرداخت با تفکیک سهم بیمه و بیمار",
            @"<div style='font-size:15px;font-weight:800;margin-bottom:12px;color:#16355E'>فاکتور خدمات پزشکی</div>
<table style='width:100%;font-size:12px;margin-bottom:12px'>
  <tr><td style='width:70px;opacity:.65'>شماره:</td><td><b>{{invoice_code}}</b></td>
      <td style='width:70px;opacity:.65'>تاریخ:</td><td>{{invoice_date}}</td></tr>
  <tr><td style='opacity:.65'>بیمار:</td><td>{{patient_name}}</td>
      <td style='opacity:.65'>بیمه:</td><td>{{insurance_name}}</td></tr>
</table>
{{items_html}}
<table style='width:100%;font-size:12px;margin-top:12px'>
  <tr><td>جمع کل:</td><td>{{subtotal}}</td></tr>
  <tr><td>تخفیف:</td><td>{{discount}}</td></tr>
  <tr><td style='color:#2E7D32'>سهم بیمه:</td><td style='color:#2E7D32'>{{insurance_share}}</td></tr>
  <tr style='font-weight:800;border-top:2px solid #16355E'><td>قابل پرداخت بیمار:</td><td>{{patient_share}}</td></tr>
</table>
<div class='sig'><div class='box'><div class='line'></div>مهر و امضای مسئول مالی</div>
<div class='box'><div class='line'></div>امضای بیمار</div></div>"),

        new("نسخه پزشکی — استاندارد", 2,
            "نسخه با جدول اقلام و دستور مصرف",
            @"<div style='display:flex;justify-content:space-between;font-size:12px;margin-bottom:10px'>
  <div><b>بیمار:</b> {{patient_name}} — {{patient_age}}</div>
  <div><b>نسخه:</b> {{prescription_code}}</div>
</div>
<div style='font-size:12px;margin-bottom:10px'><b>پزشک:</b> {{doctor_name}} — <b>تاریخ:</b> {{visit_date}}</div>
{{items_html}}
<div style='font-size:11.5px;margin-top:10px;background:#F5F7FB;border-radius:8px;padding:9px'>
  <b>توضیحات:</b> {{notes}}
</div>
<div class='sig'><div class='box'><div class='line'></div>امضا و مهر پزشک</div></div>"),

        new("درخواست آزمایش — استاندارد", 3,
            "فرم درخواست آزمایش با فهرست آزمون‌ها",
            @"<div style='font-size:15px;font-weight:800;margin-bottom:12px'>درخواست آزمایش {{order_number}}</div>
<div style='font-size:12px;margin-bottom:10px'><b>بیمار:</b> {{patient_name}} — <b>پزشک:</b> {{doctor_name}} — <b>تاریخ:</b> {{order_date}}</div>
<div style='font-size:11.5px;opacity:.75;margin-bottom:8px'><b>شرح حال:</b> {{clinical_info}}</div>
{{tests_html}}"),

        new("نتیجه آزمایش — استاندارد", 4,
            "گزارش نتیجه با جدول مقادیر و محدوده طبیعی",
            @"<div style='font-size:15px;font-weight:800;margin-bottom:12px'>نتیجه آزمایش {{order_number}}</div>
<div style='font-size:12px;margin-bottom:10px'><b>بیمار:</b> {{patient_name}} — <b>تاریخ نتیجه:</b> {{result_date}}</div>
{{results_html}}
<div style='font-size:10.5px;opacity:.7;margin-top:10px'>{{verified_by}}</div>"),

        new("درخواست تصویربرداری — استاندارد", 5,
            "فرم درخواست تصویربرداری",
            @"<div style='font-size:15px;font-weight:800;margin-bottom:12px'>درخواست {{modality}}</div>
<div style='font-size:12px;margin-bottom:10px'><b>بیمار:</b> {{patient_name}} — <b>شماره:</b> {{order_number}} — <b>تاریخ:</b> {{order_date}}</div>
<div style='font-size:11.5px;opacity:.75'><b>اطلاعات بالینی:</b> {{clinical_info}}</div>"),

        new("نوبت‌نامه — کارت کوچک", 6,
            "نوبت‌نامه جیبی بیمار",
            @"<div style='text-align:center'>
  <div style='font-size:18px;font-weight:800;color:#16355E'>نوبت‌نامه</div>
  <div style='font-size:28px;font-weight:800;margin:10px 0'>نوبت {{queue_number}}</div>
  <table style='width:100%;font-size:12.5px;margin-top:12px'>
    <tr><td style='opacity:.6'>بیمار</td><td>{{patient_name}}</td></tr>
    <tr><td style='opacity:.6'>پزشک</td><td>{{doctor_name}}</td></tr>
    <tr><td style='opacity:.6'>تاریخ</td><td>{{appointment_date}}</td></tr>
    <tr><td style='opacity:.6'>ساعت</td><td>{{appointment_time}}</td></tr>
    <tr><td style='opacity:.6'>کد رهگیری</td><td>{{tracking_code}}</td></tr>
  </table>
  <div style='font-size:10px;opacity:.6;margin-top:12px'>لطفاً ۱۵ دقیقه زودتر حضور داشته باشید</div>
</div>", "A6", "Portrait", 12),

        new("رسید پذیرش — استاندارد", 7,
            "رسید پذیرش حضوری با مبلغ و روش پرداخت",
            @"<div style='font-size:15px;font-weight:800;margin-bottom:12px'>رسید پذیرش {{reception_code}}</div>
<table style='width:100%;font-size:12.5px'>
  <tr><td style='opacity:.6;width:80px'>بیمار</td><td>{{patient_name}}</td></tr>
  <tr><td style='opacity:.6'>تاریخ</td><td>{{reception_date}}</td></tr>
  <tr><td style='opacity:.6'>خدمت</td><td>{{service_name}}</td></tr>
  <tr><td style='opacity:.6'>مبلغ</td><td><b>{{amount}}</b></td></tr>
  <tr><td style='opacity:.6'>پرداخت</td><td>{{payment_method}}</td></tr>
</table>"),

        new("گواهی پزشکی — استاندارد", 8,
            "گواهی سلامت/استراحت با امضای پزشک",
            @"<div style='font-size:15px;font-weight:800;margin-bottom:14px'>گواهی پزشکی</div>
<div style='font-size:12px;margin-bottom:12px'><b>بیمار:</b> {{patient_name}} — <b>تاریخ:</b> {{issue_date}}</div>
<div style='font-size:12.5px;line-height:2'>{{content}}</div>
<div style='font-size:12.5px;margin-top:10px'><b>مدت:</b> {{duration}}</div>
<div class='sig'><div class='box'><div class='line'></div>{{doctor_name}}</div></div>"),

        new("فیش حقوقی — استاندارد", 9,
            "فیش حقوقی پرسنل با تفکیک کسورات",
            @"<div style='font-size:15px;font-weight:800;margin-bottom:4px'>فیش حقوقی</div>
<div style='font-size:12px;opacity:.7;margin-bottom:12px'>دوره {{period}}</div>
<div style='font-size:13px;font-weight:700;margin-bottom:10px'>{{employee_name}}</div>
<table style='width:100%;font-size:12px'>
  <tr><td>حقوق پایه:</td><td>{{base_salary}}</td></tr>
  <tr><td>اضافه‌کاری:</td><td>{{overtime_pay}}</td></tr>
  <tr><td style='color:#C62828'>بیمه:</td><td style='color:#C62828'>-{{insurance}}</td></tr>
  <tr><td style='color:#C62828'>مالیات:</td><td style='color:#C62828'>-{{tax}}</td></tr>
  <tr style='font-weight:800;border-top:2px solid #16355E'><td>خالص پرداختی:</td><td>{{net_pay}}</td></tr>
</table>
<div class='sig'><div class='box'><div class='line'></div>امضای دریافت‌کننده</div></div>", "A5", "Portrait", 12),
        // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — «چندین قالب پیش‌فرض اولیه برای
        // همهٔ انواع موجودیت‌ها طراحی شود» — طرح دومِ «مدرن» برای هر نوع + طرح
        // «فیش حرارتی» برای اسناد نوبت/رسید (کاغذ ۸۰ میلی‌متری) افزوده شد ═══

        new("فاکتور مدرن — نوار رنگی", 1,
            "فاکتور با نوار رنگی بالا و کادر مشخصات",
            @"<div style='background:#1565C0;color:#fff;border-radius:10px;padding:12px 14px;font-size:15px;font-weight:800'>فاکتور خدمات پزشکی</div>
<div style='background:#F0F6FC;border:1px solid #C5D9F2;border-radius:10px;padding:10px 12px;font-size:12px;margin:10px 0'>
  <b>شماره:</b> {{invoice_code}} &nbsp;|&nbsp; <b>تاریخ:</b> {{invoice_date}} &nbsp;|&nbsp; <b>بیمه:</b> {{insurance_name}}<br/>
  <b>بیمار:</b> {{patient_name}}
</div>
{{items_html}}
<div style='background:#E8F5E9;border-radius:10px;padding:10px 12px;font-size:12.5px;margin-top:12px'>
  جمع: {{subtotal}} — تخفیف: {{discount}} — سهم بیمه: {{insurance_share}}<br/>
  <span style='font-size:14px;font-weight:800;color:#1B5E20'>قابل پرداخت: {{patient_share}}</span>
</div>"),

        new("نسخه پزشکی مدرن — کادر مشخصات", 2,
            "نسخه با کادر مشخصات رنگی و جدول اقلام",
            @"<div style='background:#00695C;color:#fff;border-radius:10px;padding:10px 14px;font-size:14px;font-weight:800'>نسخه الکترونیک {{prescription_code}}</div>
<div style='border:1px solid #B2DFDB;border-radius:10px;padding:9px 12px;font-size:12px;margin:10px 0'>
  <b>بیمار:</b> {{patient_name}} ({{patient_age}}) — <b>کد ملی:</b> {{patient_national_code}}<br/>
  <b>پزشک:</b> {{doctor_name}} — <b>تاریخ:</b> {{visit_date}}
</div>
{{items_html}}
<div style='font-size:11.5px;margin-top:10px;opacity:.8'>{{notes}}</div>
<div class='sig'><div class='box'><div class='line'></div>امضا و مهر پزشک</div></div>"),

        new("درخواست آزمایش مدرن", 3,
            "درخواست با نوار سبز و خلاصه بالینی",
            @"<div style='background:#2E7D32;color:#fff;border-radius:10px;padding:10px 14px;font-size:14px;font-weight:800'>درخواست آزمایش {{order_number}}</div>
<div style='font-size:12px;margin:10px 0'><b>بیمار:</b> {{patient_name}} — <b>پزشک:</b> {{doctor_name}} — <b>تاریخ:</b> {{order_date}}</div>
<div style='background:#F1F8E9;border-radius:8px;padding:8px 12px;font-size:11.5px'><b>شرح حال:</b> {{clinical_info}}</div>
{{tests_html}}"),

        new("نتیجه آزمایش مدرن", 4,
            "نتیجه با سربرگ رنگی و تأییدکننده",
            @"<div style='background:#6A1B9A;color:#fff;border-radius:10px;padding:10px 14px;font-size:14px;font-weight:800'>نتیجه آزمایش {{order_number}}</div>
<div style='font-size:12px;margin:10px 0'><b>بیمار:</b> {{patient_name}} — <b>تاریخ نتیجه:</b> {{result_date}}</div>
{{results_html}}
<div style='font-size:11px;opacity:.75;margin-top:12px;border-top:1px dashed #999;padding-top:8px'>تأییدکننده: {{verified_by}}</div>"),

        new("درخواست تصویربرداری مدرن", 5,
            "درخواست با نوار آبی و اطلاعات بالینی",
            @"<div style='background:#0277BD;color:#fff;border-radius:10px;padding:10px 14px;font-size:14px;font-weight:800'>درخواست {{modality}} — {{order_number}}</div>
<div style='font-size:12px;margin:10px 0'><b>بیمار:</b> {{patient_name}} — <b>تاریخ:</b> {{order_date}}</div>
<div style='background:#E1F5FE;border-radius:8px;padding:8px 12px;font-size:11.5px'><b>اطلاعات بالینی:</b> {{clinical_info}}</div>"),

        new("نوبت‌نامه فیش حرارتی ۸۰ میلی‌متری", 6,
            "فیش کوچک مناسب پرینترهای حرارتی کاسه‌ای",
            @"<div style='text-align:center'>
  <div style='font-size:14px;font-weight:800'>نوبت‌دهی درمانگاه</div>
  <div style='font-size:24px;font-weight:800;margin:8px 0'>نوبت {{queue_number}}</div>
  <div style='font-size:12px'>{{patient_name}}<br/>{{doctor_name}}<br/>{{appointment_date}} — ساعت {{appointment_time}}</div>
  <div style='font-size:11px;opacity:.7;margin-top:8px'>کد: {{tracking_code}}</div>
</div>", "Thermal80", "Portrait", 11),

        new("نوبت‌نامه مدرن — کارت رنگی", 6,
            "کارت نوبت با نوار رنگی و شماره درشت",
            @"<div style='background:#E65100;color:#fff;border-radius:12px;padding:12px;font-size:15px;font-weight:800;text-align:center'>نوبت شما</div>
<div style='text-align:center;font-size:32px;font-weight:800;margin:12px 0;color:#E65100'>{{queue_number}}</div>
<div style='font-size:12.5px;text-align:center'>
  {{patient_name}} — {{doctor_name}}<br/>
  {{appointment_date}} — ساعت {{appointment_time}}<br/>
  <span style='opacity:.7'>کد رهگیری {{tracking_code}}</span>
</div>", "A6", "Portrait", 12),

        new("رسید پذیرش فیش حرارتی ۸۰ میلی‌متری", 7,
            "رسید کوتاه مناسب پرینتر حرارتی",
            @"<div style='text-align:center'>
  <div style='font-size:14px;font-weight:800'>رسید پرداخت</div>
  <div style='font-size:11.5px;margin-top:6px'>{{reception_code}} — {{reception_date}}</div>
</div>
<div style='font-size:12px;margin-top:8px'>
  {{patient_name}}<br/>
  {{service_name}}<br/>
  مبلغ: <b>{{amount}}</b> ({{payment_method}})
</div>", "Thermal80", "Portrait", 11),

        new("رسید پذیرش مدرن", 7,
            "رسید با نوار رنگی",
            @"<div style='background:#4527A0;color:#fff;border-radius:10px;padding:10px 14px;font-size:14px;font-weight:800'>رسید پذیرش {{reception_code}}</div>
<div style='font-size:12.5px;margin:10px 0'>
  <b>بیمار:</b> {{patient_name}} — <b>تاریخ:</b> {{reception_date}}<br/>
  <b>خدمت:</b> {{service_name}}
</div>
<div style='background:#EDE7F6;border-radius:10px;padding:10px 12px;font-size:13px;font-weight:800'>
  مبلغ پرداختی: {{amount}} — {{payment_method}}
</div>"),

        new("گواهی پزشکی مدرن", 8,
            "گواهی با سربرگ رسمی",
            @"<div style='border-bottom:3px double #16355E;padding-bottom:8px;font-size:16px;font-weight:800;text-align:center'>گواهی پزشکی</div>
<div style='font-size:12px;margin:12px 0'><b>بیمار:</b> {{patient_name}} — <b>تاریخ صدور:</b> {{issue_date}}</div>
<div style='font-size:12.5px;line-height:2.2'>{{content}}</div>
<div style='font-size:12.5px;margin-top:10px'><b>مدت استراحت:</b> {{duration}}</div>
<div class='sig'><div class='box'><div class='line'></div>{{doctor_name}}</div></div>"),

        new("فیش حقوقی مدرن", 9,
            "فیش حقوقی با نوار رنگی و تفکیک مشمول/کسورات",
            @"<div style='background:#283593;color:#fff;border-radius:10px;padding:10px 14px;font-size:14px;font-weight:800'>فیش حقوقی — دوره {{period}}</div>
<div style='font-size:13.5px;font-weight:700;margin:10px 0'>{{employee_name}}</div>
<table style='width:100%;font-size:12px'>
  <tr style='background:#E8EAF6'><td colspan='2' style='padding:6px'><b>مشمول</b></td></tr>
  <tr><td>حقوق پایه:</td><td>{{base_salary}}</td></tr>
  <tr><td>اضافه‌کاری:</td><td>{{overtime_pay}}</td></tr>
  <tr style='background:#FFEBEE'><td colspan='2' style='padding:6px'><b>کسورات</b></td></tr>
  <tr><td style='color:#C62828'>بیمه:</td><td style='color:#C62828'>-{{insurance}}</td></tr>
  <tr><td style='color:#C62828'>مالیات:</td><td style='color:#C62828'>-{{tax}}</td></tr>
  <tr style='font-weight:800;border-top:2px solid #283593'><td>خالص پرداختی:</td><td>{{net_pay}}</td></tr>
</table>
<div class='sig'><div class='box'><div class='line'></div>امضای دریافت‌کننده</div></div>"),

        // ═══ چرخه سی‌ویکم (گزارش مالک): «برای همهٔ انواع موجودیت‌های قابل چاپ چندین
        // قالب پیش‌فرض اولیه طراحی شود» — طرح سومِ «کلاسیک» برای انواعی که فقط دو
        // طرح داشتند؛ اکنون هر ۹ نوع دست‌کم ۳ قالب آمادهٔ اولیه دارند ═══

        new("فاکتور کلاسیک — خطوط ظریف", 1,
            "فاکتور کم‌رنگ با خطوط افقی ظریف و کلاسیک",
            @"<div style='border-bottom:1px solid #333;padding-bottom:10px;font-size:15px;font-weight:800'>فاکتور خدمات پزشکی</div>
<table style='width:100%;font-size:12px;margin:10px 0'>
  <tr><td style='width:90px;opacity:.7'>شماره فاکتور:</td><td>{{invoice_code}}</td>
      <td style='width:70px;opacity:.7'>تاریخ:</td><td>{{invoice_date}}</td></tr>
  <tr><td style='opacity:.7'>بیمار:</td><td>{{patient_name}}</td>
      <td style='opacity:.7'>بیمه:</td><td>{{insurance_name}}</td></tr>
</table>
{{items_html}}
<table style='width:100%;font-size:12px;margin-top:10px;border-top:1px solid #333'>
  <tr><td style='padding-top:6px'>جمع کل:</td><td style='padding-top:6px'>{{subtotal}}</td></tr>
  <tr><td>تخفیف:</td><td>{{discount}}</td></tr>
  <tr><td>سهم بیمه:</td><td>{{insurance_share}}</td></tr>
  <tr style='font-weight:800;border-top:1px solid #333'><td style='padding-top:6px'>قابل پرداخت:</td><td style='padding-top:6px'>{{patient_share}}</td></tr>
</table>
<div class='sig'><div class='box'><div class='line'></div>مهر و امضای مسئول مالی</div>
<div class='box'><div class='line'></div>امضای بیمار</div></div>"),

        new("نسخه پزشکی کلاسیک — سربرگ دارویی", 2,
            "نسخه با لوگوی دارو و فهرست ساده ردیفی",
            @"<div style='font-size:16px;font-weight:800;text-align:center;border-bottom:2px solid #555;padding-bottom:10px'>℞ نسخه پزشکی</div>
<table style='width:100%;font-size:12px;margin:10px 0'>
  <tr><td><b>بیمار:</b> {{patient_name}} ({{patient_age}})</td><td><b>کد نسخه:</b> {{prescription_code}}</td></tr>
  <tr><td><b>پزشک:</b> {{doctor_name}}</td><td><b>تاریخ:</b> {{visit_date}}</td></tr>
</table>
{{items_html}}
<div style='font-size:11.5px;margin-top:10px;border-top:1px dashed #999;padding-top:8px'><b>توضیحات:</b> {{notes}}</div>
<div class='sig'><div class='box'><div class='line'></div>امضا و مهر پزشک</div></div>"),

        new("درخواست آزمایش کلاسیک — فهرست نقطه‌چین", 3,
            "درخواست با فهرست نقطه‌چین آزمون‌ها و امضای دوبل",
            @"<div style='border:1.5px solid #333;padding:10px 12px'>
<div style='font-size:15px;font-weight:800;border-bottom:1px solid #333;padding-bottom:8px'>درخواست آزمایش {{order_number}}</div>
<table style='width:100%;font-size:12px;margin:8px 0'>
  <tr><td><b>بیمار:</b> {{patient_name}}</td><td><b>تاریخ:</b> {{order_date}}</td></tr>
  <tr><td><b>پزشک:</b> {{doctor_name}}</td><td></td></tr>
</table>
<div style='font-size:11.5px;opacity:.8;margin-bottom:6px'><b>شرح حال:</b> {{clinical_info}}</div>
{{tests_html}}
</div>
<div class='sig'><div class='box'><div class='line'></div>امضای پزشک</div>
<div class='box'><div class='line'></div>امضای بیمار</div></div>"),

        new("نتیجه آزمایش کلاسیک — گزارش رسمی", 4,
            "گزارش نتیجه با قاب رسمی و مهر تأیید",
            @"<div style='border:2px double #333;padding:12px 14px'>
<div style='font-size:15px;font-weight:800;text-align:center;border-bottom:1px solid #333;padding-bottom:8px'>گزارش نتیجه آزمایش</div>
<table style='width:100%;font-size:12px;margin:8px 0'>
  <tr><td><b>شماره سفارش:</b> {{order_number}}</td><td><b>تاریخ نتیجه:</b> {{result_date}}</td></tr>
  <tr><td><b>بیمار:</b> {{patient_name}}</td><td></td></tr>
</table>
{{results_html}}
<div style='font-size:11px;margin-top:12px;text-align:left;border-top:1px solid #333;padding-top:8px'>{{verified_by}}</div>
</div>"),

        new("درخواست تصویربرداری کلاسیک — فرم رسمی", 5,
            "درخواست با قاب رسمی و کادر اطلاعات بالینی",
            @"<div style='border:1.5px solid #333;padding:10px 12px'>
<div style='font-size:15px;font-weight:800;border-bottom:1px solid #333;padding-bottom:8px'>درخواست {{modality}}</div>
<table style='width:100%;font-size:12px;margin:8px 0'>
  <tr><td><b>بیمار:</b> {{patient_name}}</td><td><b>شماره:</b> {{order_number}}</td></tr>
  <tr><td><b>تاریخ:</b> {{order_date}}</td><td></td></tr>
</table>
<div style='font-size:11.5px;background:#FAFAFA;border:1px solid #DDD;padding:8px 10px'><b>اطلاعات بالینی:</b> {{clinical_info}}</div>
</div>
<div class='sig'><div class='box'><div class='line'></div>امضای پزشک معالج</div></div>"),

        new("گواهی پزشکی کلاسیک — فرم اداری", 8,
            "گواهی با قاب اداری و متن وسط‌چین رسمی",
            @"<div style='border:2px solid #333;padding:16px 18px'>
<div style='font-size:17px;font-weight:800;text-align:center'>گواهی پزشکی</div>
<div style='font-size:12px;text-align:center;opacity:.7;margin:4px 0 12px'>شماره {{issue_code}} — تاریخ {{issue_date}}</div>
<div style='font-size:12.5px;line-height:2.2;text-align:justify'>{{content}}</div>
<div style='font-size:12.5px;margin-top:12px'>مدت استراحت توصیه‌شده: <b>{{duration}}</b></div>
<div style='font-size:12.5px;margin-top:14px'>بیمار: <b>{{patient_name}}</b></div>
<div class='sig'><div class='box'><div class='line'></div>{{doctor_name}}</div></div>
</div>", "A5", "Portrait", 13),

        new("فیش حقوقی کلاسیک — جدول رسمی", 9,
            "فیش حقوقی با جدول خط‌کشی‌شده رسمی",
            @"<div style='font-size:15px;font-weight:800;text-align:center;border-bottom:2px solid #333;padding-bottom:8px'>فیش حقوقی — دوره {{period}}</div>
<div style='font-size:13.5px;font-weight:700;margin:10px 0 6px'>{{employee_name}}</div>
<table style='width:100%;font-size:12px;border-collapse:collapse;border:1px solid #555'>
  <tr style='background:#EEE'><td colspan='2' style='padding:6px;border:1px solid #999'><b>مشمول حقوق</b></td></tr>
  <tr><td style='padding:5px 8px;border:1px solid #999'>حقوق پایه</td><td style='padding:5px 8px;border:1px solid #999'>{{base_salary}}</td></tr>
  <tr><td style='padding:5px 8px;border:1px solid #999'>اضافه‌کاری</td><td style='padding:5px 8px;border:1px solid #999'>{{overtime_pay}}</td></tr>
  <tr style='background:#EEE'><td colspan='2' style='padding:6px;border:1px solid #999'><b>کسورات</b></td></tr>
  <tr><td style='padding:5px 8px;border:1px solid #999'>بیمه</td><td style='padding:5px 8px;border:1px solid #999'>-{{insurance}}</td></tr>
  <tr><td style='padding:5px 8px;border:1px solid #999'>مالیات</td><td style='padding:5px 8px;border:1px solid #999'>-{{tax}}</td></tr>
  <tr style='font-weight:800;background:#F5F5F5'><td style='padding:6px 8px;border:1px solid #999'>خالص پرداختی</td><td style='padding:6px 8px;border:1px solid #999'>{{net_pay}}</td></tr>
</table>
<div class='sig'><div class='box'><div class='line'></div>امضای دریافت‌کننده</div></div>", "A5", "Portrait", 12),

        // ═══ چرخهٔ سی‌وپنجم (گزارش مالک) — «برای هر موجودیت ۲ طرح دیگه هم بساز» ═══
        // دو خانوادهٔ تازه برای هر ۹ نوع سند — «مینیمال» (سفید و پاک، بدون نوار رنگی،
        // فضای سفید زیاد، خطوط ظریف خاکستری) و «فشرده» (چیدمان دوستونهٔ کوتاه برای
        // کاغذ کوچک/ایкономیک، فونت ریزتر، همه‌چیز در نیمی از ارتفاع معمول).
        // نام‌ها یکتاست و بذر idempotent است → در اولین اجرای API روی هر دیتابیسِ
        // موجودی این ۱۸ طرح تازه هم افزوده می‌شوند (بدون دست‌زدن به ویرایش‌های
        // خود درمانگاه) — جمع هر موجودیت به ۵ طرح می‌رسد.

        // ─────────────── خانوادهٔ «مینیمال» — سفیدِ پاک ───────────────

        new("فاکتور مینیمال — سفید پاک", 1,
            "فاکتور بدون نوار رنگی؛ فقط تیتر باریک و خط ظریف — مناسب پرینتر لیزری اقتصادی",
            @"<div style='font-size:16px;font-weight:300;letter-spacing:1px;color:#333'>فاکتور {{invoice_code}}</div>
<div style='border-bottom:1px solid #DDD;margin:8px 0 10px'></div>
<div style='font-size:11.5px;color:#777;margin-bottom:10px'>{{invoice_date}} — بیمار: {{patient_name}} — بیمه: {{insurance_name}}</div>
{{items_html}}
<div style='font-size:12.5px;margin-top:10px;line-height:2'>
جمع: {{subtotal}} <span style='color:#999'>|</span> تخفیف: {{discount}} <span style='color:#999'>|</span> سهم بیمه: {{insurance_share}}<br/>
<b style='font-size:14px'>قابل پرداخت بیمار: {{patient_share}}</b>
</div>", "A5", "Portrait", 12, "#444444"),

        new("نسخه پزشکی مینیمال — بدون حاشیه", 2,
            "نسخه سبک با تایپوگرافی هوادار؛ مناسب نسخه‌های کم‌قلم",
            @"<div style='font-size:16px;font-weight:300;color:#333'>نسخه {{prescription_code}}</div>
<div style='border-bottom:1px solid #DDD;margin:8px 0 10px'></div>
<div style='font-size:11.5px;color:#777;margin-bottom:10px'>بیمار: {{patient_name}} ({{patient_age}}) — پزشک: {{doctor_name}} — {{visit_date}}</div>
{{items_html}}
<div style='font-size:11px;color:#888;margin-top:9px'>{{notes}}</div>
<div style='margin-top:18px;font-size:11.5px;color:#555'>امضا و مهر پزشک ..............................</div>", "A5", "Portrait", 12, "#444444"),

        new("درخواست آزمایش مینیمال", 3,
            "درخواست آزمایش سبک با فهرست آزمون‌ها",
            @"<div style='font-size:16px;font-weight:300'>درخواست آزمایش {{order_number}}</div>
<div style='border-bottom:1px solid #DDD;margin:8px 0 10px'></div>
<div style='font-size:11.5px;color:#777;margin-bottom:10px'>بیمار: {{patient_name}} — پزشک: {{doctor_name}} — {{order_date}}</div>
<div style='font-size:11.5px;color:#666;margin-bottom:8px'>شرح حال: {{clinical_info}}</div>
{{tests_html}}", "A5", "Portrait", 12, "#444444"),

        new("نتیجه آزمایش مینیمال", 4,
            "گزارش نتیجه با کمترین عناصر گرافیکی",
            @"<div style='font-size:16px;font-weight:300'>نتیجه آزمایش {{order_number}}</div>
<div style='border-bottom:1px solid #DDD;margin:8px 0 10px'></div>
<div style='font-size:11.5px;color:#777;margin-bottom:10px'>بیمار: {{patient_name}} — {{result_date}}</div>
{{results_html}}
<div style='font-size:10.5px;color:#888;margin-top:10px'>{{verified_by}}</div>", "A5", "Portrait", 12, "#444444"),

        new("درخواست تصویربرداری مینیمال", 5,
            "درخواست تصویربرداری سبک و خوانا",
            @"<div style='font-size:16px;font-weight:300'>درخواست {{modality}}</div>
<div style='border-bottom:1px solid #DDD;margin:8px 0 10px'></div>
<div style='font-size:11.5px;color:#777;margin-bottom:10px'>بیمار: {{patient_name}} — شماره: {{order_number}} — {{order_date}}</div>
<div style='font-size:11.5px;color:#666'>اطلاعات بالینی: {{clinical_info}}</div>", "A5", "Portrait", 12, "#444444"),

        new("نوبت‌نامه مینیمال — رزروشیت", 6,
            "نوبت‌نامه‌ی کاغذ A6 با حداقل جوهر",
            @"<div style='font-size:15px;font-weight:300;text-align:center'>نوبت {{queue_number}}</div>
<div style='border-bottom:1px solid #DDD;margin:7px 0 9px'></div>
<div style='font-size:12px;line-height:2'>
بیمار: {{patient_name}}<br/>
پزشک: {{doctor_name}}<br/>
{{appointment_date}} — ساعت {{appointment_time}}<br/>
<span style='color:#888;font-size:10.5px'>رهگیری: {{tracking_code}}</span>
</div>", "A6", "Portrait", 12, "#444444"),

        new("رسید پذیرش مینیمال", 7,
            "رسید ساده بدون کادر — مناسب پرینتر سوزنی",
            @"<div style='font-size:15px;font-weight:300'>رسید {{reception_code}}</div>
<div style='border-bottom:1px solid #DDD;margin:7px 0 9px'></div>
<div style='font-size:12px;line-height:2'>
بیمار: {{patient_name}}<br/>
تاریخ: {{reception_date}}<br/>
خدمت: {{service_name}}<br/>
مبلغ: <b>{{amount}}</b> ({{payment_method}})
</div>", "A5", "Portrait", 12, "#444444"),

        new("گواهی پزشکی مینیمال", 8,
            "گواهی با متن رسمی وسط‌چین و حاشیه سفید",
            @"<div style='font-size:16px;font-weight:300;text-align:center;margin-bottom:4px'>گواهی پزشکی</div>
<div style='font-size:11px;color:#888;text-align:center;margin-bottom:14px'>{{issue_date}}</div>
<div style='font-size:12.5px;line-height:2.3;text-align:justify;color:#333'>{{content}}</div>
<div style='font-size:12px;margin-top:12px'>مدت استراحت: <b>{{duration}}</b> — بیمار: <b>{{patient_name}}</b></div>
<div style='margin-top:20px;font-size:11.5px;color:#555'>{{doctor_name}}</div>", "A5", "Portrait", 12, "#444444"),

        new("فیش حقوقی مینیمال", 9,
            "فیش حقوقی سبک با ردیف‌های خط‌فاصله‌دار",
            @"<div style='font-size:15px;font-weight:300'>فیش حقوقی — {{period}}</div>
<div style='border-bottom:1px solid #DDD;margin:8px 0 10px'></div>
<div style='font-size:13px;font-weight:700;margin-bottom:8px'>{{employee_name}}</div>
<div style='font-size:12px;line-height:2.2'>
حقوق پایه ................ {{base_salary}}<br/>
اضافه‌کاری ................ {{overtime_pay}}<br/>
بیمه ................ −{{insurance}}<br/>
مالیات ................ −{{tax}}
</div>
<div style='border-top:1px solid #DDD;margin-top:8px;padding-top:8px;font-size:13px'><b>خالص پرداختی: {{net_pay}}</b></div>", "A5", "Portrait", 12, "#444444"),

        // ─────────────── خانوادهٔ «فشرده» — دوستونه روی کاغذ کوچک ───────────────

        new("فاکتور فشرده — فیش A6", 1,
            "فاکتور جمع‌وجور دوستونه برای کاغذ A6 — صرفه‌جویی در کاغذ",
            @"<div style='font-size:13px;font-weight:800;border-bottom:2px solid #16355E;padding-bottom:4px'>فاکتور {{invoice_code}}</div>
<div style='font-size:10.5px;margin:5px 0;line-height:1.8'>بیمار: {{patient_name}} — {{invoice_date}} — بیمه: {{insurance_name}}</div>
{{items_html}}
<div style='font-size:11px;margin-top:5px;line-height:1.8'>جمع {{subtotal}} — تخفیف {{discount}} — بیمه {{insurance_share}}</div>
<div style='font-size:12.5px;font-weight:800;margin-top:4px'>پرداخت بیمار: {{patient_share}}</div>", "A6", "Portrait", 11),

        new("نسخه پزشکی فشرده — نصف صفحه", 2,
            "نسخه دوستونه فشرده — دو نسخه در یک برگ A5",
            @"<div style='font-size:12.5px;font-weight:800;border-bottom:1px solid #999;padding-bottom:3px'>نسخه {{prescription_code}} — {{visit_date}}</div>
<div style='font-size:10.5px;margin:4px 0'>بیمار: {{patient_name}} ({{patient_age}}) — پزشک: {{doctor_name}}</div>
{{items_html}}
<div style='font-size:10px;color:#666;margin-top:4px'>{{notes}}</div>
<div style='font-size:10.5px;margin-top:8px'>امضا: ....................</div>", "A5", "Portrait", 11),

        new("درخواست آزمایش فشرده", 3,
            "درخواست آزمایش کوتاه برای کاغذ A6",
            @"<div style='font-size:12.5px;font-weight:800'>آزمایش {{order_number}}</div>
<div style='font-size:10.5px;margin:4px 0'>{{patient_name}} — {{doctor_name}} — {{order_date}}</div>
<div style='font-size:10px;color:#666;margin-bottom:4px'>{{clinical_info}}</div>
{{tests_html}}", "A6", "Portrait", 11),

        new("نتیجه آزمایش فشرده", 4,
            "نتیجه آزمایش فشرده با جدول ریز",
            @"<div style='font-size:12.5px;font-weight:800'>نتیجه {{order_number}}</div>
<div style='font-size:10.5px;margin:4px 0'>{{patient_name}} — {{result_date}}</div>
{{results_html}}
<div style='font-size:9.5px;color:#777;margin-top:5px'>{{verified_by}}</div>", "A6", "Portrait", 11),

        new("درخواست تصویربرداری فشرده", 5,
            "درخواست تصویربرداری کوتاه A6",
            @"<div style='font-size:12.5px;font-weight:800'>{{modality}} — {{order_number}}</div>
<div style='font-size:10.5px;margin:4px 0'>{{patient_name}} — {{order_date}}</div>
<div style='font-size:10px;color:#666'>{{clinical_info}}</div>", "A6", "Portrait", 11),

        new("نوبت‌نامه فشرده — سه‌قیمت", 6,
            "سه نوبت‌نامه در یک برگ A5 — برش با قیچی",
            @"<div style='border:1px dashed #999;padding:7px 9px;margin-bottom:6px'>
<div style='font-size:12px;font-weight:800'>نوبت {{queue_number}} <span style='font-weight:400;font-size:10.5px'>— {{appointment_date}} {{appointment_time}}</span></div>
<div style='font-size:10.5px;line-height:1.7'>بیمار: {{patient_name}} — پزشک: {{doctor_name}} — رهگیری {{tracking_code}}</div>
</div>
<div style='border:1px dashed #999;padding:7px 9px;margin-bottom:6px'>
<div style='font-size:12px;font-weight:800'>نوبت {{queue_number}} <span style='font-weight:400;font-size:10.5px'>— {{appointment_date}} {{appointment_time}}</span></div>
<div style='font-size:10.5px;line-height:1.7'>بیمار: {{patient_name}} — پزشک: {{doctor_name}} — رهگیری {{tracking_code}}</div>
</div>
<div style='border:1px dashed #999;padding:7px 9px'>
<div style='font-size:12px;font-weight:800'>نوبت {{queue_number}} <span style='font-weight:400;font-size:10.5px'>— {{appointment_date}} {{appointment_time}}</span></div>
<div style='font-size:10.5px;line-height:1.7'>بیمار: {{patient_name}} — پزشک: {{doctor_name}} — رهگیری {{tracking_code}}</div>
</div>", "A5", "Portrait", 11),

        new("رسید پذیرش فشرده — فیش A6", 7,
            "رسید کوتاه پذیرش روی کاغذ A6",
            @"<div style='font-size:12.5px;font-weight:800'>رسید {{reception_code}}</div>
<div style='font-size:10.5px;line-height:1.8;margin-top:3px'>{{patient_name}} — {{reception_date}}<br/>{{service_name}}</div>
<div style='font-size:12px;font-weight:800;margin-top:4px'>{{amount}} ({{payment_method}})</div>", "A6", "Portrait", 11),

        new("گواهی پزشکی فشرده", 8,
            "گواهی کوتاه برای کاغذ A6",
            @"<div style='font-size:13px;font-weight:800;text-align:center'>گواهی پزشکی</div>
<div style='font-size:10px;color:#777;text-align:center;margin:2px 0 8px'>{{issue_date}}</div>
<div style='font-size:11px;line-height:1.9;text-align:justify'>{{content}}</div>
<div style='font-size:10.5px;margin-top:8px'>استراحت {{duration}} — {{patient_name}}</div>
<div style='font-size:10.5px;margin-top:6px'>{{doctor_name}}</div>", "A6", "Portrait", 11),

        new("فیش حقوقی فشرده — کیف پارک", 9,
            "فیش حقوقی جمع‌وجور روی کاغذ A6",
            @"<div style='font-size:13px;font-weight:800'>فیش حقوقی — {{period}}</div>
<div style='font-size:12px;font-weight:700;margin:3px 0'>{{employee_name}}</div>
<div style='font-size:10.5px;line-height:1.8'>
پایه {{base_salary}} — اضافه‌کاری {{overtime_pay}}<br/>
بیمه −{{insurance}} — مالیات −{{tax}}
</div>
<div style='border-top:1px solid #999;margin-top:4px;padding-top:4px;font-size:12px;font-weight:800'>خالص: {{net_pay}}</div>", "A6", "Portrait", 11)
    };
}

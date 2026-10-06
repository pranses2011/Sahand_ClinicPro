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
<div class='sig'><div class='box'><div class='line'></div>امضای دریافت‌کننده</div></div>", "A5", "Portrait", 12)
    };
}

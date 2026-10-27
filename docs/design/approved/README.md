# 🎨 طرحهای تأییدشدهٔ شش صفحه — اعمالشده روی XAML

این پوشه مرجع نهایی ظاهر شش صفحهای است که مالک خواست «دوباره یکییکی از اول»
طراحی شوند. طرحها دقیقاً در همان سبک صفحهای تصویربرداری ساخته شدهاند که مالک
قبلاً با اسکرینشات واقعی تأیید کرد: پنج کارت آماری رنگی تمامعرض، چیپهای
کپسولی، دکمههای تخت، تبهای زیرخطدار، جداول راهراه و پالت آبی/سبز/بنفش/
نارنجی/قرمز.

> ✅ **وضعیت فعلی:** مالک طرحها را تأیید کرد، شش صفحه یکییکی روی XAML اعمال
> شدند و چهار دور اسکرینشات واقعی WPF گرفته و مغایرتها رفع گردید. اکنون
> مدرک کنارهم (طرح + عکس واقعی برنامه) در `../compare.html` و
> `../comparison-proof.png` قابلمشاهده است — منتظر **تأیید نهایی** مالک.

## فایلها

| کلید صفحه | منبع طرح | رندر PNG طرح | اسکرینشات واقعی فعلی |
|---|---|---|---|
| `dentistry` | `dentistry.html` | `dentistry-design.png` | `../../screenshots/real/dentistry.png` |
| `ophthalmology` | `ophthalmology.html` | `ophthalmology-design.png` | `../../screenshots/real/ophthalmology.png` |
| `physiotherapy` | `physiotherapy.html` | `physiotherapy-design.png` | `../../screenshots/real/physiotherapy.png` |
| `survey` | `survey.html` | `survey-design.png` | `../../screenshots/real/survey.png` |
| `survey-results` | `survey-results.html` | `survey-results-design.png` | `../../screenshots/real/survey-results.png` |
| `print-template` | `print-template.html` | `print-template-design.png` | `../../screenshots/real/print-template.png` |
| `print-designer-window` | `print-designer-window.html` | `print-designer-window-design.png` | — (تب دوم همان صفحه) |
| `print-settings` | `print-settings.html` | `print-settings-design.png` | `../../screenshots/real/print-settings.png` |

- `base.css` — سیستم طراحی مشترک (بوم ثابت **۱۰۳۰×۷۷۴ پیکسل** = همان اندازهٔ
  اسکرینشات واقعی WPF، تا مقایسه تا حد امکان پیکسلبهپیکسل باشد).
- فایلهای HTML توسط `scripts/generate_page_designs.py` ساخته میشوند؛ تغییر
  طرح را در همان اسکریپت انجام دهید و دوباره اجرا کنید.
- فایلهای `*-design.png` خروجی رندر Edge headless روی رانر ویندوزی
  (`.github/workflows/screenshot-pages.yml`) هستند و به مخزن برگردانده میشوند.

## صفحات بررسی

- **گالری تمام طرحها:** [`../gallery.html`](../gallery.html) — مناسب برای تأیید
  اولیهٔ ظاهر (طرحها در قابهای جداگانه نمایش داده میشوند).
- **مقایسهٔ کنارهم:** [`../compare.html`](../compare.html) — «طرح» در ستون راست،
  «اسکرینشات واقعی WPF» در ستون چپ، لغزندهٔ همپوشانی برای یافتن مغایرتها و
  فهرست مغایرتها زیر هر صفحه.

چرخهٔ انجامشده: «اسکرینشات واقعی ← مقایسهٔ پیکسلی ← اصلاح» چهار بار تکرار شد
(حذف تولتیپ شناور، حذف حاشیهٔ پنجره، اصلاح سرستونها، فشردهسازی پنلها،
هماندازه کردن بوم ۱۰۳۰×۷۷۴ و حذف نوار سیاه ۶پیکسلی). میانگین اختلاف پیکسلی
نهایی ۱۳٫۵ تا ۲۸٫۵ از ۲۵۵ (حدود ۵ تا ۱۱ درصد) است.

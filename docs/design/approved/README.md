# 🎨 طرحهای تازهٔ شش صفحه — منتظر تأیید مالک

این پوشه مرجع نهایی ظاهر شش صفحهای است که مالک خواست «دوباره یکییکی از اول»
طراحی شوند. طرحها دقیقاً در همان سبک صفحهای تصویربرداری ساخته شدهاند که مالک
قبلاً با اسکرینشات واقعی تأیید کرد: پنج کارت آماری رنگی تمامعرض، چیپهای
کپسولی، دکمههای تخت، تبهای زیرخطدار، جداول راهراه و پالت آبی/سبز/بنفش/
نارنجی/قرمز.

> ⚠️ **وضعیت فعلی:** طرحها ساخته و رندر شدهاند ولی **هنوز تأیید مالک را
> نگرفتهاند**؛ تا زمان تأیید، هیچ تغییری بر اساس آنها روی XAML اعمال نشده و
> هیچ صفحهای «منطبق» اعلام نشده است.

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

پس از تأیید مالک، مغایرتها یکییکی در XAML رفع و چرخهٔ
«اسکرینشات واقعی ← مقایسه ← اصلاح» تا رسیدن به بیشترین تطابق تکرار میشود.

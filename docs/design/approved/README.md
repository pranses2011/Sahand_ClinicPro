# 📐 طرحهای تأییدی مالک — مرجع مقایسه

این پوشه **مرجع نهایی ظاهر** شش صفحهای است که مالک تأیید کرده است. چرخهٔ
راستیآزمایی تصویری، «اسکرینشات واقعی رندر WPF روی ویندوز» را دقیقاً با این
طرحها مقایسه میکند و مغایرتها را یکییکی رفع مینماید.

> ⚠️ **وضعیت فعلی:** سه فایل `survey.png`، `survey-results.png` و
> `print-template.png` (بههمراه `print-designer-window.png`) فعلاً فقط
> **مراجع موجود در مخزن** هستند که از `docs/screenshots/forms/` بریده شدهاند
> تا مقایسهٔ اولیه ممکن شود. هنوز تأیید نشدهاند که همان طرحهای مالکند؛
> تا زمان تأیید ایشان، هیچ تغییر طراحی نمایشی بر اساس آنها نباید «منطبق»
> اعلام شود. چهار صفحهٔ دندانپزشکی، چشمپزشکی، فیزیوتراپی و تنظیمات چاپگر
> و چاپ هنوز هیچ فایل مرجعی ندارند و عمداً خالی ماندهاند.

## نامگذاری فایلها

| کلید صفحه | فایل طرح تأییدی | فایل اسکرینشات واقعی |
|---|---|---|
| `dentistry` | `dentistry.png` | `../../screenshots/real/dentistry.png` |
| `ophthalmology` | `ophthalmology.png` | `../../screenshots/real/ophthalmology.png` |
| `physiotherapy` | `physiotherapy.png` | `../../screenshots/real/physiotherapy.png` |
| `survey` | `survey.png` | `../../screenshots/real/survey.png` |
| `survey-results` | `survey-results.png` | `../../screenshots/real/survey-results.png` |
| `print-template` | `print-template.png` | `../../screenshots/real/print-template.png` |
| `print-settings` | `print-settings.png` | `../../screenshots/real/print-settings.png` |

- طرح میتواند **PNG/JPG** باشد، یا **HTML** همنام (مثلاً `dentistry.html`) —
  workflow ویندوزی فایل HTML را با Edge headless به `dentistry-design.png`
  رندر میکند و همان بهعنوان مرجع استفاده میشود.
- اسکرینشاتهای واقعی توسط `.github/workflows/screenshot-pages.yml` ساخته و به
  `docs/screenshots/real/` برگردانده میشوند (رندر واقعی WPF، نه شبیهسازی HTML).
- صفحهٔ مقایسهٔ کنارهم: [`../compare.html`](../compare.html).

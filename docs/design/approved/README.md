# 📐 طرحهای تأییدی مالک — مرجع مقایسه

این پوشه **مرجع نهایی ظاهر** شش صفحهای است که مالک تأیید کرده است. چرخهٔ
راستیآزمایی تصویری، «اسکرینشات واقعی رندر WPF روی ویندوز» را دقیقاً با این
طرحها مقایسه میکند و مغایرتها را یکییکی رفع مینماید.

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

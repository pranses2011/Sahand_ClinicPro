#!/usr/bin/env python3
# ═══════════════════════════════════════════════════════════════════
# چرخهٔ ۴۳ — ممیزی دقیق فیلدهای بدون عنوان (نسخهٔ ۲):
#   • تگ‌های چندخطی کامل استخراج می‌شوند (تا > واقعیِ همان تگ)
#   • Hint هم به‌صورت صفت و هم «عنصر فرزند» <materialDesign:HintAssist.Hint>
#     شناسایی می‌شود
#   • برچسب متنی بالای فیلد (TextBlock با Text ثابت در ۳ نویسه‌پیش) پذیرفته
# ═══════════════════════════════════════════════════════════════════
import re, glob

files = sorted(glob.glob('src/Sahand.ClinicPro.Desktop/Views/*.xaml') +
               ['src/Sahand.ClinicPro.Desktop/MainWindow.xaml'])
total = 0
for f in files:
    s = open(f, encoding='utf-8').read()
    clean = re.sub(r'<!--.*?-->', '', s, flags=re.S)
    findings = []
    # توجه: (?=[\s>]) و نه \b — وگرنه <TextBox.InputBindings> و <ComboBox.ItemTemplate>
    # نیز عنصر شمرده می‌شدند (کامنت‌های دروغین ممیزی نسخهٔ ۱)
    for m in re.finditer(r'<(TextBox|PasswordBox|ComboBox)(?=[\s>])', clean):
        # انتهای تگ باز (اولین > که داخل نقل‌قول نیست)
        i = m.end()
        in_q = None
        while i < len(clean):
            c = clean[i]
            if in_q:
                if c == in_q:
                    in_q = None
            elif c in '"\'':
                in_q = c
            elif c == '>':
                break
            i += 1
        opening = clean[m.start():i]
        # بدنهٔ عنصر تا بسته‌شدن هم‌نوع (فقط عمق ۱ برای یافتن Hint عنصری)
        close = clean.find(f'</{m.group(1)}>', i)
        body = clean[i:close if close > 0 else i + 500]
        whole = opening + body[:800]
        if 'HintAssist.Hint' in whole:
            continue
        if 'IsReadOnly="True"' in opening or 'IsEnabled="False"' in opening:
            continue
        # برچسب متنی بالای تگ؟ (TextBlock با Text ثابت در ~۵۵۰ نویسهٔ پیش —
        # برچسب مجاز است: «یا برای همشون عنوان بزار» — فیلدِ برچسب‌دار کافی است)
        before = clean[max(0, m.start() - 550):m.start()]
        if re.search(r'<TextBlock[^>]*\sText="[^{"\n]+"', before):
            continue
        line = clean.count('\n', 0, m.start()) + 1
        bind = re.search(r'(?:Text|SelectedItem|SelectedIndex|SelectedValue|NumericValue|DateShamsiText|Password)="(?:\{Binding\s+(?:Path=)?)(\w+)', whole)
        name = bind.group(1) if bind else '?'
        findings.append((line, m.group(1), name))
    if findings:
        print(f"═ {f.split('/')[-1]} ═")
        for line, tag, name in findings:
            print(f"   خط {line}: <{tag}> ← {name}")
        total += len(findings)
print(f"\nجمع فیلدهای بی‌عنوان: {total}")

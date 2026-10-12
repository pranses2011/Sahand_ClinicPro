#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
ساختِ «راهنمای جایگزینی» — سندی گام‌به‌گام برای جایگزینیِ ۸۱ View و ۸۱
ViewModelِ تازه با نمونه‌های کنونیِ پروژه.

جدولِ نگاشت به‌صورت خودکار از فهرستِ فایل‌های واقعیِ مخزن ساخته می‌شود تا
هیچ نامی از قلم نیفتد.
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import spec_core as S  # noqa: E402
from gen_views import vm_name  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
REPO = ROOT.parent
OLD_VIEWS = REPO / "src" / "Sahand.ClinicPro.Desktop" / "Views"
OLD_VMS = REPO / "src" / "Sahand.ClinicPro.Desktop" / "ViewModels"


def old_view_names() -> set[str]:
    if not OLD_VIEWS.exists():
        return set()
    names = {p.stem for p in OLD_VIEWS.glob("*.xaml")}
    # MainWindow در ریشه‌ی پروژه است، نه در پوشه‌ی Views
    if (REPO / "src" / "Sahand.ClinicPro.Desktop" / "MainWindow.xaml").exists():
        names.add("MainWindow")
    return names


def old_vm_names() -> set[str]:
    if not OLD_VMS.exists():
        return set()
    return {p.stem for p in OLD_VMS.glob("*.cs")}


def git_head() -> str:
    try:
        return subprocess.check_output(
            ["git", "rev-parse", "--short", "HEAD"], cwd=REPO, text=True
        ).strip()
    except Exception:
        return "نامشخص"


def build() -> str:
    old_views, old_vms = old_view_names(), old_vm_names()
    matched = sorted(old_views & {f["name"] for f in S.FORMS})
    unmatched = sorted(old_views - {f["name"] for f in S.FORMS})

    rows = []
    for f in sorted(S.FORMS, key=lambda x: x["name"]):
        n = f["name"]
        vm = vm_name(f)
        has_old_view = "✅" if n in old_views else "➕ جدید"
        has_old_vm = "✅" if vm in old_vms else "➕ جدید"
        rows.append(
            f"| `{n}` | {has_old_view} | {has_old_vm} "
            f"| `03-views/{n}.xaml` | `04-viewmodels/{vm}.cs` |"
        )

    L = [
        "# 🔄 راهنمای جایگزینی — از رابطِ کنونی به `redesign2`",
        "",
        f"> این سند برای مخزن در نگارشِ `{git_head()}` نوشته شده است.",
        f"> {len(matched)} از {len(S.FORMS)} صفحه‌ی تازه، هم‌نامِ یک صفحه‌ی موجود است؛ "
        "بقیه تازه‌اند.",
        "",
        "---",
        "",
        "## ۰. پیش‌نیازها",
        "",
        "### ۰/۱ بسته‌های NuGet",
        "",
        "| بسته | نسخه | دلیل |",
        "|---|---|---|",
        "| `CommunityToolkit.Mvvm` | ۸.۲ یا بالاتر | `ObservableObject`، `RelayCommand`، `AsyncRelayCommand` |",
        "| `Microsoft.Extensions.Logging.Abstractions` | ۸.۰ | `ILogger` در سازنده‌ی ViewModelها |",
        "| `MaterialDesignThemes` | ۵.۳.۲ (موجود) | فقط برای گذار؛ رابطِ تازه از توکن‌های خودش استفاده می‌کند |",
        "",
        "### ۰/۲ قلمِ وزیرمتن",
        "",
        "قلم در `redesign2/assets/Vazirmatn[wght].woff2` موجود است. برای نصب روی ویندوز:",
        "",
        "```powershell",
        "# اجرا با دسترسیِ مدیر",
        "Copy-Item .\\redesign2\\assets\\'Vazirmatn[wght].woff2' \"$env:WINDIR\\Fonts\\\"",
        "```",
        "",
        "> اگر قلم نصب نباشد، `AppFontFamily` به `Segoe UI`/`Tahoma` سقوط می‌کند و",
        "> رابط همچنان کار می‌کند (فقط کمتر یکدست).",
        "",
        "### ۰/۳ شاخه و پشتیبان",
        "",
        "```bash",
        "git pull --rebase                          # قاعده ۳",
        "git switch -c feat/redesign2-views         # شاخه‌ی کار",
        "```",
        "",
        "---",
        "",
        "## ۱. جایگذاریِ فایل‌ها",
        "",
        "### ۱/۱ پوشه‌های مقصد",
        "",
        "```",
        "src/Sahand.ClinicPro.Desktop/",
        "├── Views/            ← ۳-ویوها                (۸۱ فایل .xaml)",
        "├── ViewModels/       ← ۴-ویومدل‌ها             (۸۱ فایل .cs + _Shared.cs)",
        "├── Controls/         ← ۰۰-پایه/Controls       (۶ کنترل + PasswordHelper)",
        "├── Converters/       ← ۰۰-پایه/Converters     (۷ مبدل)",
        "├── Themes/           ← ۰۰-پایه/Themes         (رنگ، توکن، تایپوگرافی، سبک‌ها)",
        "├── Localization/     ← ۰۰-پایه/Localization   (۱۱۵۱ کلید × ۲ زبان)",
        "├── Assets/Icons.xaml ← ۰۰-پایه/Assets         (۱۷۴ آیکون)",
        "└── Base/             ← ۰۰-پایه/ViewModels/Base (۴ کلاسِ پایه)",
        "```",
        "",
        "### ۱/۲ دستورهای کپی",
        "",
        "```bash",
        "SRC=redesign2/00-foundation",
        "DST=src/Sahand.ClinicPro.Desktop",
        "",
        "# کنترل‌ها، مبدل‌ها، تم‌ها، زبان، پایه‌ها",
        "cp -r $SRC/Controls/*     $DST/Controls/",
        "cp -r $SRC/Converters/*   $DST/Converters/",
        "cp -r $SRC/Themes/*       $DST/Themes/",
        "cp -r $SRC/Localization/* $DST/Localization/",
        "mkdir -p $DST/Assets && cp $SRC/Assets/Icons.xaml $DST/Assets/",
        "mkdir -p $DST/Base && cp $SRC/ViewModels/Base/* $DST/Base/",
        "",
        "# Viewها و ViewModelها",
        "cp redesign2/03-views/*.xaml       $DST/Views/",
        "cp redesign2/04-viewmodels/*.cs    $DST/ViewModels/",
        "```",
        "",
        "> ⚠ پیش از کپی، از پوشه‌های مقصد پشتیبان بگیرید:",
        "> `git stash` یا یک شاخه‌ی `backup/old-ui`.",
        "",
        "---",
        "",
        "## ۲. تغییراتِ `App.xaml`",
        "",
        "واژه‌نامه‌های ادغامی را به ترتیبِ زیر جایگزین کنید (ترتیب مهم است —",
        "هر لایه به لایه‌ی پیش از خود ارجاع می‌دهد):",
        "",
        "```xml",
        "<Application.Resources>",
        "    <ResourceDictionary>",
        "        <ResourceDictionary.MergedDictionaries>",
        "            <!-- ۱. پالتِ رنگ: برای تمِ تاریک این یک خط را عوض کنید -->",
        '            <ResourceDictionary Source="Themes/Colors.Light.xaml" />',
        "",
        "            <!-- ۲. توکن‌های معنایی -->",
        '            <ResourceDictionary Source="Themes/Tokens.xaml" />',
        "",
        "            <!-- ۳. تایپوگرافی -->",
        '            <ResourceDictionary Source="Themes/Typography.xaml" />',
        "",
        "            <!-- ۴. کتابخانه‌ی آیکون -->",
        '            <ResourceDictionary Source="Assets/Icons.xaml" />',
        "",
        "            <!-- ۵. سبک‌های کنترل‌ها -->",
        '            <ResourceDictionary Source="Themes/Controls/Buttons.xaml" />',
        '            <ResourceDictionary Source="Themes/Controls/Inputs.xaml" />',
        '            <ResourceDictionary Source="Themes/Controls/DataDisplay.xaml" />',
        "",
        "            <!-- ۶. مبدل‌ها -->",
        '            <ResourceDictionary Source="Converters/Converters.xaml" />',
        "        </ResourceDictionary.MergedDictionaries>",
        "    </ResourceDictionary>",
        "</Application.Resources>",
        "```",
        "",
        "> `Converters.xaml` را اگر در `Controls/` گذاشتید، مسیر را همان‌جا اصلاح کنید.",
        "",
        "---",
        "",
        "## ۳. نگاشتِ فایل‌ها (۸۱ صفحه)",
        "",
        "| نام | View قدیم | ViewModel قدیم | View جدید | ViewModel جدید |",
        "|---|:--:|:--:|---|---|",
    ]
    L += rows

    if unmatched:
        L += ["", "### ⚠ صفحاتِ قدیمی بدون هم‌نام در طراحیِ تازه", ""]
        for u in unmatched:
            L.append(f"- `{u}`")

    L += [
        "",
        "---",
        "",
        "## ۴. فضای نام و قراردادها",
        "",
        "| موضوع | قدیم | جدید |",
        "|---|---|---|",
        "| فضای نامِ View | `Sahand.ClinicPro.Desktop.Views` | **بدون تغییر** |",
        "| فضای نامِ ViewModel | `Sahand.ClinicPro.Desktop.ViewModels` | **بدون تغییر** |",
        "| فضای نامِ کنترل‌ها | `…Desktop.Controls` | **بدون تغییر** |",
        "| فضای نامِ پایه‌ها | `…Desktop.ViewModels` | `…Desktop.Base` (پوشه‌ی `Base/`) |",
        "| سطرِ `using` در هر ViewModel | — | `using Sahand.ClinicPro.Desktop.Base;` |",
        "",
        "### ۴/۱ چیزهایی که حذف می‌شوند",
        "",
        "| چیز | چرا |",
        "|---|---|",
        "| کلیدهای Material Design در XAML | رابطِ تازه هیچ وابستگی به MD ندارد |",
        "| `MessageBox.Show` | قاعدهٔ ۱۳ — پیام از `ShowError`/`ShowSuccess` می‌آید |",
        "| شکلک در متنِ رابط | قاعدهٔ ۲۰ — جایگزین با `Icon.*` |",
        "| رنگِ هگزِ ثابت | الزامِ ۲۲ — همه از توکن‌های معنایی |",
        "| منطق در code-behind | الزامِ MVVM — همه به ViewModel منتقل می‌شود |",
        "",
        "### ۴/۲ چیزهایی که اضافه می‌شوند",
        "",
        "| چیز | محل |",
        "|---|---|",
        "| `StatusToneConverter` | `Converters/Converters.cs` + ثبت در `Converters.xaml` |",
        "| `RelayCommand` محلی | `Base/RelayCommand.cs` — در پایه‌ها با نامِ مستعار به نسخه‌ی CommunityToolkit ارجاع داده شده |",
        "| ۱۷۴ آیکون | `Assets/Icons.xaml` |",
        "| ۱۱۵۱ کلیدِ زبان | `Localization/Strings.resx` و `Strings.en.resx` |",
        "",
        "---",
        "",
        "## ۵. گام‌های جایگزینی برای هر صفحه",
        "",
        "برای هر یک از ۸۱ صفحه، این شش گام را به ترتیب انجام دهید و **پس از هر صفحه",
        "کامیت و پوش کنید** (قاعدهٔ ۱):",
        "",
        "```mermaid",
        "flowchart TD",
        "    A[\"۱. پشتیبان از فایل قدیمی\"] --> B[\"۲. کپیِ View جدید\"]",
        "    B --> C[\"۳. کپیِ ViewModel جدید\"]",
        "    C --> D[\"۴. انتقالِ منطقِ اختصاصی از code-behind قدیم\"]",
        "    D --> E[\"۵. ثبت در تزریقِ وابستگی\"]",
        "    E --> F[\"۶. ساخت + ممیزی + کامیت و پوش\"]",
        "    F --> G{صفحه‌ی بعدی؟}",
        "    G -->|بله| A",
        "    G -->|خیر| H[\"پایان\"]",
        "```",
        "",
        "### گام ۱ — پشتیبان",
        "",
        "```bash",
        "mkdir -p .migration-backup",
        "cp src/Sahand.ClinicPro.Desktop/Views/PatientsView.xaml .migration-backup/",
        "cp src/Sahand.ClinicPro.Desktop/ViewModels/PatientsViewModel.cs .migration-backup/",
        "```",
        "",
        "### گام ۲ — View",
        "",
        "فایل جدید را کپی کنید و namespaceِ `x:Class` را بررسی کنید (در صورت تغییرِ",
        "ساختارِ پوشه‌ها):",
        "",
        "```xml",
        '<UserControl x:Class="Sahand.ClinicPro.Desktop.Views.PatientsView"',
        "             FlowDirection=\"RightToLeft\"",
        "             FontFamily=\"{DynamicResource AppFontFamily}\">",
        "```",
        "",
        "### گام ۳ — ViewModel",
        "",
        "مدلِ ردیف (`PatientsRow`) در همان فایلِ ViewModel است؛ اگر در لایه‌ی Domain",
        "معادل دارد، آن را حذف و به مدلِ دامنه ارجاع دهید.",
        "",
        "### گام ۴ — انتقالِ منطقِ اختصاصی",
        "",
        "هرچه در code-behind قدیم بود (رویدادها، تبدیل‌ها، فراخوانیِ سرویس) باید به",
        "یک فرمان در ViewModel منتقل شود. نقاطِ `// TODO` در ViewModelهای تازه دقیقاً",
        "همان جاهایی‌اند که باید پر شوند:",
        "",
        "```csharp",
        "// TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:",
        "//   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);",
        "```",
        "",
        "### گام ۵ — تزریقِ وابستگی",
        "",
        "```csharp",
        "// در App.xaml.cs یا CompositionRoot",
        "services.AddTransient<PatientsViewModel>();",
        "services.AddTransient<PatientsView>();",
        "```",
        "",
        "### گام ۶ — ساخت و کامیت",
        "",
        "```bash",
        "dotnet build src/Sahand.ClinicPro.Desktop -warnaserror",
        "python3 redesign2/tools/audit_bindings.py",
        "git add -A && git commit -m '🔄 [Step PatientsView]: جایگزینیِ صفحه‌ی بیماران'",
        "git push",
        "```",
        "",
        "---",
        "",
        "## ۶. نمونه‌ی کامل — صفحه‌ی بیماران",
        "",
        "```diff",
        "- <!-- قدیم: ۸۰۰ خط XAML با کلیدهای Material Design -->",
        "- <DataGrid md:DataGridAssist.CellPadding=\"8\" ItemsSource=\"{Binding Patients}\">",
        "-   <DataGridTextColumn Header=\"نام\" Binding=\"{Binding FullName}\" />",
        "- </DataGrid>",
        "+ <!-- جدید: توکن‌های معنایی + ستونِ نوع‌محور -->",
        "+ <DataGrid Style=\"{DynamicResource Grid.Modern}\"",
        "+           ItemsSource=\"{Binding Items}\"",
        "+           SelectedItem=\"{Binding SelectedItem, Mode=TwoWay}\">",
        "+   <DataGridTemplateColumn Header=\"نام بیمار\" Width=\"*\">",
        "+     <!-- نشانِ تصویری + نام + زیرنویس -->",
        "+   </DataGridTemplateColumn>",
        "+ </DataGrid>",
        "```",
        "",
        "```diff",
        "- private void OnSearchClick(object sender, RoutedEventArgs e)",
        "- {",
        "-     MessageBox.Show(\"...\");",
        "- }",
        "+ // جدید: هیچ منطقی در code-behind",
        "+ public string SearchText { get; set; }   // در پایه، با تأخیرِ ۳۵۰ms",
        "+ protected override Task<PagedResult<PatientsRow>?> FetchAsync(CancellationToken ct)",
        "```",
        "",
        "---",
        "",
        "## ۷. چک‌لیستِ پذیرش",
        "",
        "| # | معیار | ابزار | وضعیتِ هدف |",
        "|:--:|---|---|---|",
        "| ۱ | همه‌ی XAMLها معتبر | تجزیه‌گر XML | ۸۱/۸۱ ✅ |",
        "| ۲ | همه‌ی C#ها متوازن | شمارشِ آکولاد | ۸۲/۸۲ ✅ |",
        "| ۳ | هیچ رنگِ هگزِ ثابت در Viewها | اسکن | ۰ ✅ |",
        "| ۴ | هیچ شکلک در رابط | اسکنِ بازه‌ی ایموجی | ۰ ✅ |",
        "| ۵ | هیچ نویسهٔ CJK | اسکن | ۰ ✅ |",
        "| ۶ | هیچ اتصالِ یتیم | `tools/audit_bindings.py` | ۰ از ۱٬۳۲۳ ✅ |",
        "| ۷ | هیچ کلیدِ منبعِ گم‌شده | ممیزیِ واژه‌نامه | ۰ ✅ |",
        "| ۸ | دو زبان هم‌تراز | `tools/resx_sync.py` | ۱۱۵۱/۱۱۵۱ ✅ |",
        "| ۹ | ساختِ بدون هشدار | `dotnet build -warnaserror` | ۰ هشدار |",
        "| ۱۰ | کامیت و پوش پس از هر واحد | `git log` | ✅ |",
        "",
        "---",
        "",
        "## ۸. بازگشت",
        "",
        "اگر در هر مرحله نیاز به بازگشت بود:",
        "",
        "```bash",
        "cp .migration-backup/* src/Sahand.ClinicPro.Desktop/Views/",
        "git revert HEAD --no-edit",
        "git push",
        "```",
        "",
        "---",
        "",
        "این فایل با `tools/gen_guide.py` ساخته شده است؛ آن را دستی ویرایش نکنید.",
    ]
    return "\n".join(L) + "\n"


def main() -> None:
    out = ROOT / "REPLACEMENT-GUIDE.md"
    out.write_text(build(), encoding="utf-8")
    print(f"✅ {out.relative_to(REPO)}")


if __name__ == "__main__":
    main()

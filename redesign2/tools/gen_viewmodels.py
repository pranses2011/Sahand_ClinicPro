#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
╔══════════════════════════════════════════════════════════════════════════╗
║  gen_viewmodels.py — تولیدِ ۸۱ ViewModel متناظر با ۸۱ View                ║
║                                                                          ║
║  هر ViewModel از همان مشخصاتِ وایرفریم ساخته می‌شود:                      ║
║      • ویژگی‌ها از فیلدهای فرم (با دادهٔ نمونه به‌عنوان مقدارِ آغازین)      ║
║      • فرمان‌ها از دکمه‌های اقدام                                          ║
║      • سطرهای جدول از دادهٔ نمونهٔ وایرفریم                                ║
║      • «ارتباطات» از نقشهٔ پیوندهای وایرفریم                              ║
║                                                                          ║
║  انتخابِ پایه:                                                            ║
║      cols دارد              → PageViewModelBase<TRow>  (لیست + صفحه‌بندی)  ║
║      فرم/دیالوگ/نمایشگر     → FormViewModelBase        (ذخیره/انصراف)     ║
║      در غیر این صورت        → ViewModelBase                               ║
║                                                                          ║
║  خروجی: 04-viewmodels/<Name>ViewModel.cs  +  04-viewmodels/_Shared.cs     ║
╚══════════════════════════════════════════════════════════════════════════╝
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import spec_core as S  # noqa: E402
from gen_views import (  # noqa: E402
    ALIGN, CHIP_STYLE, ikey, model_name, vm_name,
)

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "04-viewmodels"

# ══════════════════════════════════════════════════════════════════════════
# نگاشتِ نوع
# ══════════════════════════════════════════════════════════════════════════

CS_TYPE = {
    "text": "string", "textarea": "string", "password": "string",
    "national": "string", "phone": "string", "combo": "string",
    "lookup": "string", "barcode": "string", "time": "string",
    "color": "string", "email": "string", "file": "string",
    "radio": "string", "chip": "string",
    "money": "decimal", "num": "int", "number": "int",
    "check": "bool", "date": "DateTime?", "slider": "double",
}

CS_DEFAULT = {
    "string": 'string.Empty', "decimal": "0m", "int": "0",
    "bool": "false", "DateTime?": "null", "double": "0d",
}

# کهن‌الگوهایی که پایه‌ی فرم می‌گیرند
FORM_ARCH = {"form-std", "form-narrow", "form-lines", "dialog", "viewer"}

# آیکونِ پیش‌فرض برای مواردِ بدون آیکون
FALLBACK_ICON = "Icon.Circle"


# اعضای واقعیِ پایه‌ها. ساختنِ هم‌نام در کلاسِ فرزند هشدارِ CS0108 می‌دهد و
# چون پروژه با warnaserror ساخته می‌شود، همین هشدار به خطا تبدیل می‌گردد.
# بنابراین هر ویژگی‌ای که با این نام‌ها برخورد کند تغییر نام می‌یابد و هر
# فرمانی که پایه از پیش دارد اصلاً تولید نمی‌شود.
RESERVED_PROPS = {
    "IsBusy", "IsNotBusy", "BusyMessage", "ErrorMessage", "SuccessMessage",
    "HasErrors", "Items", "SelectedItem", "HasSelection", "CanEdit", "CanDelete",
    "IsEmpty", "SearchText", "PageNumber", "PageSize", "TotalCount", "TotalPages",
    "PageSummary", "IsDirty", "IsReadOnly", "IsEditable", "IsEditMode",
    "WindowTitle", "CloseAction",
}

RESERVED_COMMANDS = {
    "LoadCommand", "CreateCommand", "EditCommand", "DeleteCommand",
    "RefreshCommand", "NextPageCommand", "PreviousPageCommand",
    "SaveCommand", "CancelCommand",
}


def safe_prop(name: str) -> str:
    """تغییرِ نامِ ویژگی در صورت برخورد با نام‌های پایه"""
    return name + "Value" if name in RESERVED_PROPS else name


def cs_str(v) -> str:
    """نمایشِ یک مقدار به‌صورت ثابتِ C#"""
    if v is None:
        return "null"
    if isinstance(v, bool):
        return "true" if v else "false"
    if isinstance(v, (int, float)):
        return f"{v}"
    s = str(v).replace("\\", "\\\\").replace('"', '\\"').replace("\n", " ")
    return f'"{s}"'


def ident(s: str) -> str:
    """پاک‌سازیِ نام به شناسه‌ی معتبرِ C#"""
    s = re.sub(r"[^\w]", "_", str(s or ""))
    if not s or s[0].isdigit():
        s = "_" + s
    return s


def prop_name(fd: dict, index: int) -> str:
    """نامِ ویژگی با رعایتِ نام‌های رزروشده‌ی پایه"""
    return safe_prop(ident(fd.get("p") or f"Field{index}"))


# ══════════════════════════════════════════════════════════════════════════
# قطعه‌سازها
# ══════════════════════════════════════════════════════════════════════════

# ارقامِ فارسی و عربی → لاتین (ضروری است: اعدادِ نمونه در وایرفریم‌ها فارسی‌اند
# اما ثابت‌های عددیِ C# باید لاتین باشند)
_DIGITS = str.maketrans("۰۱۲۳۴۵۶۷۸۹٠١٢٣٤٥٦٧٨٩",
                        "01234567890123456789")


def latin(s) -> str:
    """تبدیلِ ارقامِ یک مقدار به لاتین"""
    return str(s or "").translate(_DIGITS)


def numeric_literal(raw) -> str | None:
    """
    اگر مقدار یک عددِ خالص است، ثابتِ عددیِ C# را برمی‌گرداند؛ در غیر این صورت None.

    مقادیری مانند «192.168.1.24»، «۱۴۰۴/۰۷/۱۹» یا «۶:۰۳» عدد نیستند و باید
    رشته باقی بمانند — در غیر این صورت کد کامپایل نمی‌شود یا معنا را خراب می‌کند.
    """
    txt = latin(raw).strip().replace(",", "").replace("٬", "").replace(" ", "")
    txt = txt.replace("تومان", "").strip()
    if not txt or not all(ch.isdigit() or (ch == "-" and i == 0)
                          for i, ch in enumerate(txt)):
        return None
    return txt or None


def col_type(c: dict, values: list) -> str:
    """تعیینِ نوعِ C# یک ستون بر پایه‌ی دادهٔ نمونه، نه فقط برچسبِ آن"""
    kind = c.get("k")
    if kind == "money":
        return "decimal" if all(numeric_literal(v) for v in values) else "string?"
    if kind == "num":
        if not all(numeric_literal(v) for v in values):
            return "string?"
        # اعدادِ بزرگ شناسه‌اند، نه کمیّت: شماره تلفن، کد ملی، شماره حساب.
        # آن‌ها را نباید int گرفت (صفرِ پیشرو و ارزشِ ریاضی بی‌معناست).
        width = max(len(numeric_literal(v) or "") for v in values)
        return "int" if width <= 7 else "string?"
    if kind == "date":
        # تاریخ‌ها در وایرفریم از پیش شمسی‌اند → رشته نگه داشته می‌شوند
        return "string?"
    return "string?"


def cell_value(cell):
    """استخراجِ مقدارِ خام از یک سلول (ساده یا ساختاریافته)"""
    if isinstance(cell, (list, tuple)):
        return cell[1] if len(cell) > 1 else ""
    return cell


def cell_tag(cell) -> str:
    """برچسبِ نوعِ یک سلولِ ساختاریافته (chip / who / date / num / money)"""
    if isinstance(cell, (list, tuple)) and cell:
        return str(cell[0])
    return ""


def row_class(f: dict) -> str:
    """کلاسِ مدلِ ردیف برای صفحاتِ جدولی"""
    if not f.get("cols"):
        return ""
    name = model_name(f)
    rows = f.get("rows") or []
    L = [f"    /// <summary>مدلِ نمایشِ یک ردیف از «{f['title']}»</summary>",
         "    /// <remarks>",
         "    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه",
         "    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.",
         "    /// </remarks>",
         f"    public sealed class {name}",
         "    {"]
    for ci, c in enumerate(f["cols"]):
        base = c["b"]
        kind = c.get("k")
        values = [cell_value(r[ci]) for r in rows if ci < len(r)]
        ctype = col_type(c, values) if values else "string?"
        L.append(f"        /// <summary>{c['h']}"
                 + (f" — {kind}" if kind else "") + "</summary>")
        L.append(f"        public {ctype} {base} {{ get; set; }}")
        if kind == "chip":
            L.append(f"        /// <summary>تنِ رنگیِ نشانِ «{c['h']}»</summary>")
            L.append(f"        public string {base}Tone {{ get; set; }} = \"nu\";")
        if kind == "who":
            L.append(f"        /// <summary>زیرنویسِ «{c['h']}» (مانند کد یا سن)</summary>")
            L.append(f"        public string? {base}Sub {{ get; set; }}")
            L.append(f"        /// <summary>حروفِ اختصاریِ نشان برای «{c['h']}»</summary>")
            L.append(f"        public string? {base}Initials {{ get; set; }}")
            L.append(f"        /// <summary>رنگِ نشانِ «{c['h']}» — از تم خوانده می‌شود</summary>")
            L.append(f"        public Brush? {base}Avatar {{ get; set; }}")
        L.append("")
    if L and L[-1] == "":
        L.pop()
    L.append("    }")
    return "\n".join(L)


def sample_rows(f: dict) -> str:
    """ساختِ دادهٔ نمونه از سطرهای وایرفریم"""
    if not f.get("rows") or not f.get("cols"):
        return ""
    name = model_name(f)
    cols, rows = f["cols"], f["rows"]
    L = ["        /// <summary>",
         "        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.",
         "        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود",
         "        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.",
         "        /// </summary>",
         f"        private static IEnumerable<{name}> SampleData()",
         "        {"]
    for row in rows:
        items = []
        for ci, c in enumerate(cols):
            if ci >= len(row):
                break
            base, kind = c["b"], c.get("k")
            cell = row[ci]
            tag = cell_tag(cell)
            values = [cell_value(r[ci]) for r in rows if ci < len(r)]
            ctype = col_type(c, values)
            if tag == "chip":
                text = cell[1] if len(cell) > 1 else ""
                tone = cell[2] if len(cell) > 2 else "nu"
                items.append(f'{base} = {cs_str(text)}, {base}Tone = {cs_str(tone)}')
                continue
            if tag == "who":
                nm = cell[1] if len(cell) > 1 else ""
                sub = cell[2] if len(cell) > 2 else ""
                ini = cell[3] if len(cell) > 3 else ""
                tone = str(cell[4] if len(cell) > 4 else "") or "b1"
                brush = {"b1": "Brush.Primary", "b2": "Brush.Secondary",
                         "b3": "Brush.Info", "b4": "Brush.Success",
                         "b5": "Brush.Warning", "b6": "Brush.Danger",
                         }.get(tone, "Brush.Primary")
                items.append(f"{base} = {cs_str(nm)}, {base}Sub = {cs_str(sub)}, "
                             f"{base}Initials = {cs_str(ini)}, "
                             f"{base}Avatar = Application.Current."
                             f"TryFindResource(\"{brush}\") as Brush")
                continue
            raw = cell_value(cell)
            if ctype in ("int", "decimal"):
                lit = numeric_literal(raw) or "0"
                if ctype == "decimal":
                    lit += "m"
                items.append(f"{base} = {lit}")
            else:
                items.append(f"{base} = {cs_str(raw)}")
        L.append(f"            yield return new {name} {{ {', '.join(items)} }};")
    L.append("        }")
    return "\n".join(L)


def field_props(f: dict) -> tuple[str, str]:
    """ویژگی‌های فرم + متدِ اعتبارسنجی"""
    is_form = f["arch"] in FORM_ARCH
    decl, valid, seen = [], [], set()
    for si, sec in enumerate(f.get("sections") or []):
        for fi, fd in enumerate(sec.get("fields") or []):
            pn = prop_name(fd, fi)
            if pn in seen:
                continue
            seen.add(pn)
            t = CS_TYPE.get(fd.get("t"), "string")
            dflt = CS_DEFAULT[t]
            val = fd.get("v")
            # مقدارِ آغازین = دادهٔ نمونهٔ وایرفریم (در صورت سازگاری با نوع)
            init = dflt
            if val is not None:
                if t == "bool":
                    init = "true" if val else "false"
                elif t == "string":
                    init = cs_str(val)
                elif t in ("int", "decimal", "double"):
                    lit = numeric_literal(val)
                    if lit:
                        init = lit + ("" if t == "int" else ("m" if t == "decimal" else "d"))
            decl.append(f"        /// <summary>{fd.get('l') or pn}"
                        + (" — الزامی" if fd.get("req") else "") + "</summary>")
            decl.append(f"        public {t} {pn}")
            decl.append("        {")
            decl.append(f"            get => _{pn[0].lower() + pn[1:]};")
            decl.append(f"            set")
            decl.append("            {")
            decl.append(f"                if (SetProperty(ref _{pn[0].lower() + pn[1:]}, value))")
            decl.append("                {")
            if is_form:
                decl.append("                    MarkDirty();")
            if fd.get("req"):
                decl.append("                    Validate();")
            decl.append("                }")
            decl.append("            }")
            decl.append("        }")
            decl.append(f"        private {t} _{pn[0].lower() + pn[1:]} = {init};")
            decl.append("")
            if fd.get("req"):
                label = fd.get("l") or pn
                valid.append(f"            if ({(t == 'string' and 'string.IsNullOrWhiteSpace(' + pn + ')' ) or (t == 'DateTime?' and pn + ' is null') or (t == 'bool' and 'false') or (t in ('int', 'decimal', 'double') and pn + ' <= 0')})")
                valid.append("            {")
                valid.append(f"                AddError(\"{label} الزامی است\", nameof({pn}));")
                valid.append("            }")
                valid.append("            else")
                valid.append("            {")
                valid.append(f"                ClearErrors(nameof({pn}));")
                valid.append("            }")
                valid.append("")
    return "\n".join(decl), "\n".join(valid)


def commands(f: dict) -> str:
    """فرمان‌های اقدامِ صفحه"""
    acts = f.get("actions") or []
    if not acts:
        return ""
    L = []
    seen = set()
    for a in acts:
        cmd = ident(a.get("c") or "Do")
        if cmd in seen:
            continue
        seen.add(cmd)
        if f"{cmd}Command" in RESERVED_COMMANDS:
            # پایه از پیش این فرمان را دارد — نیازی به بازتعریف نیست
            continue
        label = a.get("l") or cmd
        L.append(f"        /// <summary>{label}</summary>")
        L.append(f"        public IAsyncRelayCommand {cmd}Command =>")
        L.append(f"            new AsyncRelayCommand({cmd}Async);")
        L.append("")
        L.append(f"        /// <summary>اجرای «{label}»</summary>")
        L.append(f"        private async Task {cmd}Async()")
        L.append("        {")
        L.append(f"            // TODO: پیوند به سرویسِ واقعی — «{label}»")
        L.append("            await RunAsync(_ => Task.CompletedTask, "
                 f"\"{label}…\");")
        L.append("        }")
        L.append("")
    return "\n".join(L)


def lookup_commands(f: dict) -> str:
    """فرمان‌های جستجو/اسکن برای فیلدهای lookup و barcode"""
    L, seen = [], set()
    for sec in f.get("sections") or []:
        for fi, fd in enumerate(sec.get("fields") or []):
            pn = prop_name(fd, fi)
            if pn in seen:
                continue
            if fd.get("t") == "lookup":
                seen.add(pn)
                L.append(f"        /// <summary>جستجو و انتخابِ «{fd.get('l') or pn}»</summary>")
                L.append(f"        public IAsyncRelayCommand Lookup{pn}Command =>")
                L.append(f"            new AsyncRelayCommand(() => LookupAsync(\"{fd.get('l') or pn}\"));")
                L.append("")
            elif fd.get("t") == "barcode":
                seen.add(pn)
                L.append(f"        /// <summary>خواندنِ بارکد برای «{fd.get('l') or pn}»</summary>")
                L.append(f"        public IRelayCommand Scan{pn}Command =>")
                L.append(f"            new RelayCommand(() => ScanBarcode(\"{fd.get('l') or pn}\"));")
                L.append("")
    return "\n".join(L)


def collections(f: dict) -> str:
    """مجموعه‌ها و ویژگی‌های اختصاصی بر پایهٔ کهن‌الگو"""
    L = []
    is_page = bool(f.get("cols"))
    if f.get("kpis"):
        L += ["        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>",
              "        public ObservableCollection<KpiItem> Kpis { get; } = new();",
              ""]
    if f.get("tabs"):
        L += ["        /// <summary>محتوای تب‌ها</summary>",
              "        public ObservableCollection<TabItemModel> Tabs { get; } = new();",
              ""]
    # شماره‌گذاری با View یکی است: اندیسِ یک‌پایه روی کلِ نوارِ ابزار،
    # فقط فیلترِ جستجو تعریف نمی‌شود چون پایه از پیش SearchText دارد.
    for fi, t in enumerate(f.get("toolbar") or [], start=1):
        if t.get("k") == "search":
            continue
        cstype = "bool" if t.get("k") == "check" else "object?"
        dflt = "false" if t.get("k") == "check" else cs_str(t.get("v"))
        reload = (["                    PageNumber = 1;",
                   "                    _ = LoadAsync();"] if is_page else [])
        L += [f"        /// <summary>فیلترِ «{t.get('l')}»</summary>",
              f"        public {cstype} Filter{fi}",
              "        {",
              f"            get => _filter{fi};",
              "            set",
              "            {",
              f"                if (SetProperty(ref _filter{fi}, value))",
              "                {",
            ] + reload + [
              "                }",
              "            }",
              "        }",
              f"        private {cstype} _filter{fi} = {dflt};",
              ""]
    if f.get("tabs"):
        L += ["        /// <summary>محتوای تب‌ها — هر اندیس متناظر با یک تب در View است</summary>",
              "        public ObservableCollection<object?> TabContents { get; } = new();",
              ""]
    hero = f.get("hero") or {}
    if hero.get("features"):
        L += ["        /// <summary>ویژگی‌های برجسته‌ی برنامه در صفحه‌ی ورود</summary>",
              "        public ObservableCollection<FeatureItem> Features { get; } = new();",
              ""]
    if f["arch"] == "dashboard":
        L += ["        /// <summary>نمودارِ درآمد ۳۰ روزه — در پیاده‌سازی جایگزین می‌شود</summary>",
              "        public object? RevenueChart",
              "{",
              "            get => _revenueChart;",
              "            set => SetProperty(ref _revenueChart, value);",
              "        }",
              "        private object? _revenueChart;",
              ""]
    if f["arch"] == "viewer":
        L += ["        /// <summary>تصویرِ در حال نمایش</summary>",
              "        public ImageSource? CurrentImage",
              "{",
              "            get => _currentImage;",
              "            set => SetProperty(ref _currentImage, value);",
              "        }",
              "        private ImageSource? _currentImage;",
              "",
              "        /// <summary>بزرگ‌نمایی</summary>",
              "        public IRelayCommand ZoomInCommand => new RelayCommand(ZoomIn);",
              "",
              "        /// <summary>کوچک‌نمایی</summary>",
              "        public IRelayCommand ZoomOutCommand => new RelayCommand(ZoomOut);",
              ""]
    if f.get("rel"):
        L += ["        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>",
              "        public ObservableCollection<RelationItem> Relations { get; } = new();",
              ""]
    if f["arch"] == "kanban":
        L += ["        /// <summary>ستون‌های کانبان</summary>",
              "        public ObservableCollection<KanbanColumn> Columns { get; } = new();",
              ""]
    if f["arch"] == "pos":
        L += ["        /// <summary>اقلامِ سبدِ فروش</summary>",
              "        public ObservableCollection<CartLine> CartItems { get; } = new();",
              "",
              "        /// <summary>جمعِ قابلِ پرداختِ سبد</summary>",
              "        public string CartTotal => \"۰ تومان\";",
              ""]
    hero = f.get("hero") or {}
    if hero.get("features"):
        L.append("            // ویژگی‌های صفحه‌ی ورود")
        for icn, text in hero["features"]:
            ic = ikey(icn) or FALLBACK_ICON
            L.append(f'            Features.Add(new FeatureItem('
                     f'Application.Current.TryFindResource("{ic}") as Geometry, "{text}"));')
        L.append("")
    if f["arch"] == "shell":
        L.append("            // منوی کناری — ساخته‌شده از فهرستِ صفحاتِ برنامه")
        groups: dict[str, list[str]] = {}
        for form in S.FORMS:
            if form["arch"] in ("shell", "splash"):
                continue
            groups.setdefault(form["group"], []).append(form["name"])
        for g, names in groups.items():
            L.append(f'            MenuGroups.Add(new MenuGroup("{g}", new[]')
            L.append("            {")
            for nm in names:
                title = next((x["title"] for x in S.FORMS if x["name"] == nm), nm)
                icon = next((x.get("icon") for x in S.FORMS if x["name"] == nm), None)
                ic = ikey(icon) or FALLBACK_ICON
                L.append(f'                new MenuItemModel("{title}", '
                         f'Application.Current.TryFindResource("{ic}") as Geometry, '
                         f'"{nm}"),')
            L.append("            }));")
        L.append("")
    if f["arch"] == "list-cards":
        L += ["        /// <summary>کارت‌های نمایش‌داده‌شده</summary>",
              "        public ObservableCollection<CardItem> Cards { get; } = new();",
              ""]
    if f["arch"] == "detail-timeline":
        L += ["        /// <summary>رخدادهای خطِ زمان</summary>",
              "        public ObservableCollection<TimelineItem> Timeline { get; } = new();",
              ""]
    if f["arch"] == "settings":
        L += ["        /// <summary>بخش‌های تنظیمات</summary>",
              "        public ObservableCollection<string> Sections { get; } = new();",
              "",
              "        /// <summary>بخشِ انتخاب‌شده</summary>",
              "        public string? SelectedSection",
              "        {",
              "            get => _selectedSection;",
              "            set => SetProperty(ref _selectedSection, value);",
              "        }",
              "        private string? _selectedSection;",
              ""]
    if f["arch"] == "shell":
        L += ["        /// <summary>نامِ برنامه</summary>",
              '        public string AppName => "سهند کلینیک‌پرو";',
              "",
              "        /// <summary>تعویضِ تمِ روشن/تاریک</summary>",
              "        public IRelayCommand ToggleThemeCommand => new RelayCommand(ToggleTheme);",
              "",
              "        /// <summary>تعویضِ زبان فارسی/انگلیسی</summary>",
              "        public IRelayCommand ToggleLanguageCommand => new RelayCommand(ToggleLanguage);",
              "",
              "        /// <summary>نمایشِ اعلان‌ها</summary>",
              "        public IRelayCommand NotificationsCommand => new RelayCommand(ShowNotifications);",
              "",
              "        /// <summary>گروه‌های منوی کناری</summary>",
              "        public ObservableCollection<MenuGroup> MenuGroups { get; } = new();",
              "",
              "        /// <summary>صفحه‌ی در حال نمایش</summary>",
              "        public object? CurrentPage",
              "        {",
              "            get => _currentPage;",
              "            set => SetProperty(ref _currentPage, value);",
              "        }",
              "        private object? _currentPage;",
              "",
              "        /// <summary>متنِ نوارِ وضعیت</summary>",
              "        public string StatusText",
              "        {",
              "            get => _statusText;",
              "            set => SetProperty(ref _statusText, value);",
              "        }",
              '        private string _statusText = "آماده";',
              ""]
    if f["arch"] == "splash":
        L += ["        /// <summary>نامِ برنامه</summary>",
              '        public string AppName => "سهند کلینیک‌پرو";',
              "",
              "        /// <summary>شعارِ برنامه</summary>",
              '        public string AppTagline => "مدیریت یکپارچه‌ی درمانگاه";',
              "",
              "        /// <summary>متنِ وضعیتِ بارگذاری</summary>",
              "        public string StatusText",
              "        {",
              "            get => _statusText;",
              "            set => SetProperty(ref _statusText, value);",
              "        }",
              '        private string _statusText = "در حال بارگذاری…";',
              ""]
    if f["arch"] == "detail-timeline" or f.get("hero"):
        L += ["        /// <summary>سربرگِ برجسته</summary>",
              "        public HeroModel? Hero",
              "        {",
              "            get => _hero;",
              "            set => SetProperty(ref _hero, value);",
              "        }",
              "        private HeroModel? _hero;",
              ""]
    if f.get("aside"):
        L += ["        /// <summary>خلاصه‌ی کناریِ صفحه</summary>",
              "        public ObservableCollection<KeyValueItem> Summary { get; } = new();",
              ""]
    return "\n".join(L)


def seed_block(f: dict) -> str:
    """پر کردنِ مجموعه‌ها از دادهٔ نمونه"""
    L = []
    if f.get("kpis"):
        L.append("            // کارت‌های شاخص")
        for k in f["kpis"]:
            trend = "null"
            if k.get("tr"):
                raw = latin(k["tr"]).replace("+", "").replace("−", "-")
                raw = raw.replace("٪", "").replace("%", "").replace("٫", ".").strip()
                digits = "".join(ch for ch in raw if ch.isdigit() or ch in "-.")
                if digits and digits not in ("-", ".", ""):
                    try:
                        trend = f"{float(digits.lstrip('-')):g}".replace(".0", "")
                        if k.get("dir") == "down":
                            trend = "-" + trend.lstrip("-")
                    except ValueError:
                        trend = "null"
            ic = ikey(k.get("ic")) or FALLBACK_ICON
            accent = {"teal": "Brush.Primary", "blue": "Brush.Info",
                      "green": "Brush.Success", "amber": "Brush.Warning",
                      "rose": "Brush.Danger", "violet": "Brush.Secondary",
                      "cyan": "Brush.Info", "slate": "Brush.TextSecondary"
                      }.get(k.get("ac"), "Brush.Primary")
            L.append(f'            Kpis.Add(new KpiItem("{k["l"]}", "{k["v"]}", '
                     f'{trend}, Application.Current.TryFindResource("{ic}") as Geometry, '
                     f'Application.Current.TryFindResource("{accent}") as Brush));')
        L.append("")
    if f.get("tabs"):
        L.append("            // تب‌ها")
        for i, t in enumerate(f["tabs"]):
            label, icon, count = (list(t) + [None, None])[:3]
            ic = ikey(icon) or FALLBACK_ICON
            L.append(f'            Tabs.Add(new TabItemModel("{label}", '
                     f'Application.Current.TryFindResource("{ic}") as Geometry, '
                     f'{(count if isinstance(count, int) else "null")}));')
            L.append(f'            TabContents.Add(null);  // محتوای تب «{label}»')
        L.append("")
    if f.get("rel"):
        L.append("            // ارتباطات")
        for r in f["rel"]:
            src, dst, why = (list(r) + ["", "", ""])[:3]
            L.append(f'            Relations.Add(new RelationItem("{src}", "{dst}", "{why}"));')
        L.append("")
    if f["arch"] == "settings":
        L.append("            // بخش‌های تنظیمات")
        for sec in f.get("sections") or []:
            L.append(f'            Sections.Add("{sec.get("t")}");')
        L.append("")
    if f.get("aside"):
        L.append("            // خلاصه‌ی کناری")
        for k, v in f["aside"].get("fields") or []:
            L.append(f'            Summary.Add(new KeyValueItem("{k}", "{v}"));')
        L.append("")
    if f.get("hero"):
        h = f["hero"]
        L.append("            // سربرگِ برجسته")
        L.append(f'            Hero = new HeroModel("{h.get("name", "")}", '
                 f'"{h.get("sub", "")}", "{h.get("av", "")}");')
        L.append("")
    if f["arch"] == "list-cards":
        L.append("            // کارت‌ها")
        for row in f.get("rows") or []:
            title = row[1] if len(row) > 1 else ""
            sub = row[2] if len(row) > 2 else ""
            status = ""
            if len(row) > 4 and isinstance(row[4], list) and len(row[4]) > 1:
                status = row[4][1]
            L.append(f'            Cards.Add(new CardItem("{title}", "{sub}", "{status}"));')
        L.append("")
    return "\n".join(L)


# ══════════════════════════════════════════════════════════════════════════
# ساختِ فایل
# ══════════════════════════════════════════════════════════════════════════

def build(f: dict) -> str:
    name = vm_name(f)
    is_page = bool(f.get("cols"))
    is_form = f["arch"] in FORM_ARCH
    row = model_name(f) if is_page else None
    base = (f"PageViewModelBase<{row}>" if is_page
            else "FormViewModelBase" if is_form else "ViewModelBase")

    L = []
    L.append("// ═══════════════════════════════════════════════════════════════════════")
    L.append(f"//  {name}")
    L.append(f"//  صفحه‌ی «{f['title']}» · گروه «{f['group']}» · کهن‌الگوی «{f['arch']}»")
    L.append("//")
    L.append(f"//  متناظر با:  03-views/{f['name']}.xaml")
    L.append(f"//  وایرفریم:    01-wireframes-html/{f['name']}.html")
    L.append(f"//  پایه:        {base}")
    L.append("//")
    L.append("//  این کلاس با مولدِ tools/gen_viewmodels.py از همان مشخصاتِ وایرفریم")
    L.append("//  ساخته شده است؛ دادهٔ نمونه درونِ آن همان چیزی است که در وایرفریم دیده")
    L.append("//  می‌شود تا رفتارِ صفحه پیش از اتصالِ سرویسِ واقعی قابلِ بررسی باشد.")
    L.append("// ═══════════════════════════════════════════════════════════════════════")
    L.append("")
    L.append("using System;")
    L.append("using System.Collections.Generic;")
    L.append("using System.Collections.ObjectModel;")
    L.append("using System.Threading;")
    L.append("using System.Threading.Tasks;")
    L.append("using System.Windows;")
    L.append("using System.Windows.Media;")
    L.append("using CommunityToolkit.Mvvm.Input;")
    L.append("using Microsoft.Extensions.Logging;")
    L.append("using Sahand.ClinicPro.Desktop.Base;")
    L.append("")
    L.append("namespace Sahand.ClinicPro.Desktop.ViewModels")
    L.append("{")

    body: list[str] = []

    # مدلِ ردیف
    rc = row_class(f)
    if rc:
        body.append(rc)
        body.append("")

    # کلاسِ اصلی
    body.append("    /// <summary>")
    body.append(f"    /// ViewModelِ صفحه‌ی «{f['title']}»")
    body.append("    /// </summary>")
    body.append("    /// <remarks>")
    if f.get("notes"):
        for i, line in enumerate(_wrap(f["notes"], 68)):
            body.append(f"    /// {line}")
        body.append("    ///")
    body.append(f"    /// کهن‌الگوی طراحی: <c>{f['arch']}</c> — "
                "چیدمان متناسب با همین قالب در View پیاده شده است.")
    body.append(f"    /// گروه: {f['group']}")
    body.append("    /// </remarks>")
    body.append(f"    public sealed class {name} : {base}")
    body.append("    {")

    inner: list[str] = []

    # سازنده
    inner.append("        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>")
    inner.append("        /// <param name=\"logger\">ثبت‌کننده‌ی رویدادها (اختیاری)</param>")
    inner.append(f"        public {name}(ILogger? logger = null) : base(logger)")
    inner.append("        {")
    seed = seed_block(f)
    if seed:
        inner.append(seed.rstrip())
    if is_page:
        inner.append("")
        inner.append("            // بارگذاریِ نخستین صفحه از داده‌ها")
        inner.append("            _ = LoadAsync();")
    inner.append("        }")
    inner.append("")

    # مجموعه‌ها
    coll = collections(f)
    if coll:
        inner.append(coll.rstrip())
        inner.append("")

    # ویژگی‌های فرم
    decl, valid = field_props(f)
    if decl:
        inner.append("        // ══════════════════════════════════════════════════════")
        inner.append("        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است")
        inner.append("        // ══════════════════════════════════════════════════════")
        inner.append("")
        inner.append(decl.rstrip())
        inner.append("")

    # فرمان‌ها
    cmds = commands(f)
    lk = lookup_commands(f)
    if cmds or lk:
        inner.append("        // ══════════════════════════════════════════════════════")
        inner.append("        //  فرمان‌ها")
        inner.append("        // ══════════════════════════════════════════════════════")
        inner.append("")
        inner.append((cmds + lk).rstrip())
        inner.append("")

    # دادهٔ نمونه و واکشی
    if is_page:
        sr = sample_rows(f)
        if sr:
            inner.append(sr)
            inner.append("")
        inner.append("        /// <summary>")
        inner.append("        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه")
        inner.append("        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.")
        inner.append("        /// </summary>")
        inner.append(f"        protected override Task<PagedResult<{row}>?> FetchAsync(CancellationToken ct)")
        inner.append("        {")
        inner.append("            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:")
        inner.append("            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);")
        inner.append(f"            var items = new List<{row}>(SampleData());")
        inner.append(f"            return Task.FromResult<PagedResult<{row}>?>(")
        inner.append(f"                new PagedResult<{row}>(items, items.Count));")
        inner.append("        }")

    if f["arch"] == "viewer":
        inner.append("        /// <summary>بزرگ‌نماییِ تصویر</summary>")
        inner.append("        private void ZoomIn() => ShowSuccess(\"بزرگ‌نمایی\");")
        inner.append("")
        inner.append("        /// <summary>کوچک‌نماییِ تصویر</summary>")
        inner.append("        private void ZoomOut() => ShowSuccess(\"کوچک‌نمایی\");")
        inner.append("")

    if f["arch"] == "shell":
        inner.append("        /// <summary>تعویضِ تم</summary>")
        inner.append("        private void ToggleTheme()")
        inner.append("            => ShowSuccess(\"تم تغییر کرد\");")
        inner.append("")
        inner.append("        /// <summary>تعویضِ زبان</summary>")
        inner.append("        private void ToggleLanguage()")
        inner.append("            => ShowSuccess(\"زبان تغییر کرد\");")
        inner.append("")
        inner.append("        /// <summary>نمایشِ اعلان‌ها</summary>")
        inner.append("        private void ShowNotifications()")
        inner.append("            => ShowSuccess(\"اعلان‌ها\");")
        inner.append("")
    # اعتبارسنجی و ذخیره
    if is_form:
        inner.append("        /// <summary>")
        inner.append("        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره")
        inner.append("        /// </summary>")
        inner.append("        protected override bool Validate()")
        inner.append("        {")
        inner.append("            var ok = true;")
        inner.append("")
        if valid:
            inner.append(valid.rstrip())
        inner.append("            return ok && !HasErrors;")
        inner.append("        }")
        inner.append("")
        inner.append("        /// <summary>")
        inner.append("        /// ذخیره‌ی داده‌ها. در صورت نبودِ سرویس، فقط وضعیت موفقیت نمایش")
        inner.append("        /// داده می‌شود تا رفتارِ فرم قابلِ بررسی باشد.")
        inner.append("        /// </summary>")
        inner.append("        protected override Task PersistAsync(CancellationToken ct)")
        inner.append("        {")
        inner.append("            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی")
        inner.append("            return Task.CompletedTask;")
        inner.append("        }")
        inner.append("")
        inner.append("        /// <summary>جستجو در یک فیلدِ جستجومحور</summary>")
        inner.append("        private Task LookupAsync(string field)")
        inner.append("            => RunAsync(_ => Task.CompletedTask, $\"جستجوی {field}…\");")
        inner.append("")
        inner.append("        /// <summary>خواندنِ بارکد برای یک فیلد</summary>")
        inner.append("        private void ScanBarcode(string field)")
        inner.append("            => ShowSuccess($\"بارکدِ {field} خوانده شد\");")

    while inner and inner[-1] == "":
        inner.pop()
    body += ["        " + l if l and not l.startswith("        ") else l for l in inner]
    body.append("    }")
    body.append("}")

    return "\n".join(L + body)


def _wrap(text: str, width: int) -> list[str]:
    words, lines, line = text.split(), [], ""
    for w in words:
        if len(line) + len(w) + 1 > width:
            lines.append(line)
            line = ""
        line += w + " "
    if line:
        lines.append(line)
    return [l.strip() for l in lines]


SHARED = '''// ═══════════════════════════════════════════════════════════════════════
//  مدل‌های کوچکِ مشترکِ ViewModelها
//
//  این کلاس‌ها بین همه‌ی ViewModelهای تولیدشده به اشتراک گذاشته می‌شوند و
//  تنها «قالبِ نمایش» را نگه می‌دارند — هیچ منطقِ تجاری در آن‌ها نیست.
// ═══════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using System.Windows.Media;

namespace Sahand.ClinicPro.Desktop.ViewModels
{
    /// <summary>یک کارتِ شاخصِ کلیدی در بالای صفحه</summary>
    public sealed record KpiItem(string Title, string Value, double? Trend, Geometry? Icon, Brush? Accent);

    /// <summary>یک پیوندِ ارتباطی بین دو صفحه</summary>
    public sealed record RelationItem(string From, string To, string Why);

    /// <summary>یک تب در نوارِ تب‌ها</summary>
    public sealed record TabItemModel(string Title, Geometry? Icon, int? Count);

    /// <summary>یک زوجِ کلید-مقدار برای پانلِ خلاصه</summary>
    public sealed record KeyValueItem(string Key, string Value);

    /// <summary>یک کارت در چیدمانِ کارتی</summary>
    public sealed record CardItem(string Title, string Sub, string Status);

    /// <summary>یک رخداد در خطِ زمان</summary>
    public sealed record TimelineItem(Geometry? Icon, string Date, string Title, string Detail);

    /// <summary>یک ستون در بوردِ کانبان</summary>
    public sealed class KanbanColumn
    {
        /// <summary>عنوانِ ستون</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>کارت‌های درونِ ستون</summary>
        public IReadOnlyList<CardItem> Cards { get; set; } = System.Array.Empty<CardItem>();
    }

    /// <summary>یک قلم در سبدِ فروش</summary>
    public sealed record CartLine(string Name, int Quantity, decimal UnitPrice);

    /// <summary>سربرگِ برجسته‌ی صفحه‌های جزئیات</summary>
    public sealed record HeroModel(string Name, string Sub, string Initials);

    /// <summary>یک ویژگیِ برجسته در صفحه‌ی ورود</summary>
    public sealed record FeatureItem(Geometry? Icon, string Text);

    /// <summary>یک گروه در منوی کناری</summary>
    public sealed record MenuGroup(string Title, IReadOnlyList<MenuItemModel> Items);

    /// <summary>
    /// یک آیتمِ منو — همراه با فرمانِ بازکردنِ صفحه‌ی مقصد
    /// </summary>
    public sealed class MenuItemModel
    {
        /// <summary>عنوانِ نمایشی</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>آیکونِ آیتم</summary>
        public Geometry? Icon { get; set; }

        /// <summary>نامِ صفحه‌ی مقصد</summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>فرمانِ بازکردنِ صفحه — توسط MainViewModel تنظیم می‌شود</summary>
        public System.Windows.Input.ICommand? OpenCommand { get; set; }
    }
}
'''


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "_Shared.cs").write_text(SHARED, encoding="utf-8")
    n = 0
    for f in S.FORMS:
        (OUT / f"{vm_name(f)}.cs").write_text(build(f), encoding="utf-8")
        n += 1
    print(f"✅ {n} ViewModel در {OUT.relative_to(ROOT)} تولید شد (+ _Shared.cs)")


if __name__ == "__main__":
    main()

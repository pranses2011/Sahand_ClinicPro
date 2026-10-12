#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
╔══════════════════════════════════════════════════════════════════════════╗
║  gen_docs.py — ساختِ فهرست‌های مستندساز برای پوشه‌های خروجی               ║
║                                                                          ║
║  خروجی‌ها:                                                                ║
║    03-views/README.md       فهرستِ ۸۱ View + نمودارِ ارتباطات              ║
║    04-viewmodels/README.md  فهرستِ ۸۱ ViewModel                           ║
║    RELATIONS.md             نمودارِ سراسریِ پیوندِ صفحات                   ║
╚══════════════════════════════════════════════════════════════════════════╝
"""

from __future__ import annotations

import collections
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import spec_core as S  # noqa: E402
from gen_views import ikey, vm_name  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent

ARCH_FA = {
    "dashboard": "پیشخوان", "list-dense": "لیستِ فشرده", "list-cards": "لیستِ کارتی",
    "list-detail": "لیست + جزئیات", "split": "دوپنلی", "detail-tabs": "جزئیاتِ تب‌دار",
    "detail-timeline": "جزئیاتِ خط‌زمان", "form-std": "فرم استاندارد",
    "form-narrow": "فرمِ باریک", "form-lines": "فرمِ خطی", "dialog": "دیالوگ",
    "settings": "تنظیمات", "report": "گزارش", "kanban": "کانبان",
    "pos": "صندوق فروش", "viewer": "نمایشگر", "splash": "آغازین", "shell": "پوسته",
}

GROUP_ICON = {
    "پیشخوان": "🏠", "پذیرش": "🎫", "نوبت‌دهی": "📅", "ویزیت": "🩺",
    "بیماران": "👥", "نسخه": "💊", "داروخانه": "🧪", "آزمایشگاه": "🔬",
    "تصویربرداری": "📷", "مالی": "💰", "منابع انسانی": "🧑‍💼",
    "گزارشات": "📊", "ارتباطات": "📣", "سازمانی": "🏢", "سیستم": "⚙️",
}


_NODE_IDS: dict[str, str] = {}


def safe_id(name: str) -> str:
    """
    شناسه‌ی امن برای گره‌های نمودار.

    mermaid شناسه‌های غیرِ لاتین را نمی‌پذیرد، بنابراین هر عنوان یک شناسه‌ی
    «n۰، n۱، …» می‌گیرد و عنوانِ واقعی در برچسبِ گره نوشته می‌شود.
    """
    if name not in _NODE_IDS:
        _NODE_IDS[name] = f"n{len(_NODE_IDS)}"
    return _NODE_IDS[name]


def relations_graph() -> str:
    """نمودارِ سراسریِ پیوندِ صفحات (mermaid)"""
    edges: list[tuple[str, str, str]] = []
    seen: set[tuple[str, str]] = set()
    for f in S.FORMS:
        for r in f.get("rel") or []:
            src, dst, why = (list(r) + ["", "", ""])[:3]
            if not src or not dst:
                continue
            key = (src, dst)
            if key in seen:
                continue
            seen.add(key)
            edges.append((src, dst, why or ""))

    nodes = {n for e in edges for n in (e[0], e[1])}
    L = ["```mermaid", "graph LR"]

    # رنگ‌بندی بر پایه‌ی گروه
    group_of: dict[str, str] = {}
    for f in S.FORMS:
        group_of[f["title"]] = f["group"]
    for n in sorted(nodes):
        g = group_of.get(n, "")
        icon = GROUP_ICON.get(g, "•")
        L.append(f'    {safe_id(n)}["{icon} {n}"]')

    for src, dst, why in edges:
        label = why[:24].replace('"', "'")
        arrow = f'    {safe_id(src)} -->|"{label}"| {safe_id(dst)}' if label \
            else f'    {safe_id(src)} --> {safe_id(dst)}'
        L.append(arrow)

    L.append("```")
    return "\n".join(L)


def views_index() -> str:
    rows = []
    for f in sorted(S.FORMS, key=lambda x: (x["group"], x["title"])):
        rows.append(
            f"| {GROUP_ICON.get(f['group'], '•')} {f['title']} "
            f"| `{f['name']}.xaml` | {ARCH_FA.get(f['arch'], f['arch'])} "
            f"| {f['group']} | [وایرفریم](../01-wireframes-html/{f['name']}.html) "
            f"| [ViewModel](../04-viewmodels/{vm_name(f)}.cs) |"
        )

    by_arch = collections.Counter(f["arch"] for f in S.FORMS)
    by_group = collections.Counter(f["group"] for f in S.FORMS)

    L = [
        "# 🖼 ۰۳-ویوها — ۸۱ View",
        "",
        "> هر View مستقیماً از وایرفریمِ HTMLِ همان فرم مشتق شده است؛",
        "> برای هر کهن‌الگو یک چیدمانِ مستقل نوشته شده، نه یک قالبِ تکراری.",
        "",
        "## 📊 آمار",
        "",
        "| دسته | تعداد |",
        "|---|:---:|",
        f"| مجموعِ Viewها | **{len(S.FORMS)}** |",
        f"| مجموعِ خطوط | **{sum(len((ROOT / '03-views' / (f['name'] + '.xaml')).read_text(encoding='utf-8').splitlines()) for f in S.FORMS):,}** |",
        "",
        "### پراکندگی بر پایه‌ی کهن‌الگو",
        "",
        "| کهن‌الگو | شرح | تعداد |",
        "|---|---|:---:|",
    ]
    for a, n in by_arch.most_common():
        L.append(f"| `{a}` | {ARCH_FA.get(a, a)} | {n} |")
    L += ["", "### پراکندگی بر پایه‌ی گروه", "", "| گروه | تعداد |", "|---|:---:|"]
    for g, n in by_group.most_common():
        L.append(f"| {GROUP_ICON.get(g, '•')} {g} | {n} |")

    L += [
        "",
        "## 📋 فهرستِ Viewها",
        "",
        "| صفحه | فایل | کهن‌الگو | گروه | وایرفریم | ViewModel |",
        "|---|---|---|---|---|---|",
    ]
    L += rows
    L += [
        "",
        "## 🔗 نمودارِ ارتباطاتِ صفحات",
        "",
        relations_graph(),
        "",
        "## 🧩 کنترل‌های بازاستفاده‌شده",
        "",
        "| کنترل | نقش | محل |",
        "|---|---|---|",
        "| `PageHeader` | سربرگِ یکپارچه‌ی همه‌ی صفحات | `00-foundation/Controls/PageHeader.xaml` |",
        "| `StatCard` | کارتِ شاخصِ کلیدی با روند | `00-foundation/Controls/StatCard.xaml` |",
        "| `SearchBox` | جستجو با تأخیر و دکمه‌ی پاک‌کردن | `00-foundation/Controls/SearchBox.xaml` |",
        "| `EmptyState` | وضعیتِ تهی برای جدول و لیست | `00-foundation/Controls/EmptyState.xaml` |",
        "| `StatusBadge` | نشانِ رنگیِ وضعیت | `00-foundation/Controls/StatusBadge.xaml` |",
        "| `PasswordHelper` | اتصالِ امنِ `PasswordBox` به ViewModel | `00-foundation/Controls/PasswordHelper.cs` |",
        "",
        "---",
        "",
        "این فایل با `tools/gen_docs.py` ساخته شده است؛ آن را دستی ویرایش نکنید.",
    ]
    return "\n".join(L) + "\n"


def viewmodels_index() -> str:
    rows = []
    for f in sorted(S.FORMS, key=lambda x: (x["group"], x["title"])):
        is_page = bool(f.get("cols"))
        is_form = f["arch"] in ("form-std", "form-narrow", "form-lines", "dialog", "viewer")
        base = ("PageViewModelBase&lt;TRow&gt;" if is_page
                else "FormViewModelBase" if is_form else "ViewModelBase")
        rows.append(
            f"| {f['title']} | `{vm_name(f)}.cs` | `{base}` "
            f"| {len(f.get('sections') or [])} "
            f"| {len(f.get('cols') or [])} "
            f"| {len(f.get('rows') or [])} "
            f"| {len(f.get('actions') or [])} "
            f"| [View](../03-views/{f['name']}.xaml) |"
        )

    L = [
        "# 🧠 ۰۴-ویومدل‌ها — ۸۱ ViewModel",
        "",
        "> هر ViewModel از همان مشخصاتی ساخته شده که وایرفریمِ همان فرم را ساخت؛",
        "> بنابراین دادهٔ نمونه‌ی آن دقیقاً همان چیزی است که در وایرفریم دیده می‌شود.",
        "",
        "## 🧬 انتخابِ پایه",
        "",
        "| شرط | پایه | امکاناتِ دریافت‌شده |",
        "|---|---|---|",
        "| فرم ستون دارد | `PageViewModelBase<TRow>` | جستجوی تأخیری، صفحه‌بندی، انتخاب، ایجاد/ویرایش/حذف |",
        "| فرم یا دیالوگ است | `FormViewModelBase` | ردیابیِ تغییر، حالتِ فقط‌خواندنی، ذخیره/انصراف، اعتبارسنجی |",
        "| در غیر این صورت | `ViewModelBase` | وضعیتِ بارگذاری، پیام، اجرای ایمن، `INotifyDataErrorInfo` |",
        "",
        "## 📋 فهرست",
        "",
        "| صفحه | فایل | پایه | بخش‌ها | ستون‌ها | سطرِ نمونه | اقدام‌ها | View |",
        "|---|---|---|:--:|:--:|:--:|:--:|---|",
    ]
    L += rows
    L += [
        "",
        "## 🔁 چرخه‌ی حیاتِ یک صفحه",
        "",
        "```mermaid",
        "sequenceDiagram",
        "    participant V as View (XAML)",
        "    participant VM as ViewModel",
        "    participant B as پایه (PageViewModelBase)",
        "    participant Svc as سرویس (TODO)",
        "    V->>VM: ساخت از طریق DataContext",
        "    VM->>B: سازنده + دادهٔ نمونه",
        "    B->>B: LoadAsync()",
        "    B->>VM: FetchAsync(ct)",
        "    VM->>Svc: واکشی (در حاضر: SampleData)",
        "    Svc-->>VM: PagedResult<TRow>",
        "    VM-->>B: Items + TotalCount",
        "    B-->>V: اعلانِ تغییر → بازترسیمِ جدول",
        "    Note over V,B: جستجو با تأخیرِ ۳۵۰ میلی‌ثانیه",
        "    V->>B: SearchText تغییر کرد",
        "    B->>B: PageNumber = 1 و LoadAsync()",
        "```",
        "",
        "## 🧱 مدل‌های مشترک (`_Shared.cs`)",
        "",
        "| مدل | کاربرد |",
        "|---|---|",
        "| `KpiItem` | کارتِ شاخص (عنوان، مقدار، روند، آیکون، رنگ) |",
        "| `RelationItem` | پیوندِ ارتباطی (مبدأ، مقصد، دلیل) |",
        "| `TabItemModel` | تب (عنوان، آیکون، شمارنده) |",
        "| `KeyValueItem` | زوجِ کلید-مقدار در پانلِ خلاصه |",
        "| `CardItem` | کارت در چیدمانِ کارتی |",
        "| `TimelineItem` | رخدادِ خطِ زمان |",
        "| `KanbanColumn` | ستونِ بوردِ کانبان |",
        "| `CartLine` | قلمِ سبدِ فروش |",
        "| `HeroModel` | سربرگِ برجسته‌ی صفحاتِ جزئیات |",
        "| `FeatureItem` | ویژگیِ برجسته در صفحه‌ی ورود |",
        "| `MenuGroup` / `MenuItemModel` | منوی کناریِ پوسته |",
        "",
        "---",
        "",
        "این فایل با `tools/gen_docs.py` ساخته شده است؛ آن را دستی ویرایش نکنید.",
    ]
    return "\n".join(L) + "\n"


def relations_doc() -> str:
    pairs: dict[tuple[str, str], list[str]] = collections.defaultdict(list)
    for f in S.FORMS:
        for r in f.get("rel") or []:
            src, dst, why = (list(r) + ["", "", ""])[:3]
            if src and dst:
                pairs[(src, dst)].append(why or "—")

    L = [
        "# 🔗 ارتباطاتِ صفحات — نقشه‌ی حرکتِ کاربر",
        "",
        f"> {len(pairs):,} پیوندِ یکتا میانِ {len(S.FORMS)} صفحه، استخراج‌شده از وایرفریم‌ها.",
        "",
        "## 🗺 نمودارِ سراسری",
        "",
        relations_graph(),
        "",
        "## 📋 جدولِ پیوندها",
        "",
        "| مبدأ | مقصد | نوعِ پیوند |",
        "|---|---|---|",
    ]
    for (src, dst), whys in sorted(pairs.items()):
        L.append(f"| {src} | {dst} | {' / '.join(sorted(set(whys)))} |")

    # پرپیوندترین صفحات
    deg = collections.Counter()
    for (src, dst) in pairs:
        deg[src] += 1
        deg[dst] += 1
    L += ["", "## 🌟 پرپیوندترین صفحات", "", "| صفحه | تعدادِ پیوند |", "|---|:---:|"]
    for n, c in deg.most_common(12):
        L.append(f"| {n} | {c} |")

    L += ["", "---", "", "این فایل با `tools/gen_docs.py` ساخته شده است؛ آن را دستی ویرایش نکنید."]
    return "\n".join(L) + "\n"


def main() -> None:
    (ROOT / "03-views" / "README.md").write_text(views_index(), encoding="utf-8")
    (ROOT / "04-viewmodels" / "README.md").write_text(viewmodels_index(), encoding="utf-8")
    (ROOT / "RELATIONS.md").write_text(relations_doc(), encoding="utf-8")
    print("✅ 03-views/README.md")
    print("✅ 04-viewmodels/README.md")
    print("✅ RELATIONS.md")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
╔══════════════════════════════════════════════════════════════════════════╗
║  gen_views.py — تولیدِ ۸۱ View از روی وایرفریم‌ها                          ║
║                                                                          ║
║  هر View مستقیماً از وایرفریمِ HTMLِ همان فرم مشتق می‌شود:                 ║
║      tools/spec_core.FORMS  ←──  همان مشخصاتی که وایرفریم را ساخت         ║
║                                                                          ║
║  تفاوتِ مهم با دورِ پیش (که رد شد):                                        ║
║      خروجی حاصلِ یک «قالبِ واحد» برای همه‌ی فرم‌ها نیست. برای هر کهن‌الگو  ║
║      یک تابعِ چیدمانِ مستقل نوشته شده (۱۹ کهن‌الگو) و محتوای هر فرم        ║
║      (فیلدها، ستون‌ها، دادهٔ نمونه، ارتباطات) اختصاصیِ خودش است.           ║
║                                                                          ║
║  خروجی: 03-views/<Name>.xaml  +  03-views/README.md                       ║
╚══════════════════════════════════════════════════════════════════════════╝
"""

from __future__ import annotations

import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import spec_core as S  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "03-views"

NS = "Sahand.ClinicPro.Desktop"
VM_NS = f"{NS}.ViewModels"

# ══════════════════════════════════════════════════════════════════════════
# نگاشت‌ها
# ══════════════════════════════════════════════════════════════════════════

# نام‌هایی که در Lucide وجود ندارند → معادلِ حرفه‌ای
ICON_SUB = {
    "tooth": "smile", "capsule": "pill", "vaccine": "syringe",
    "wheelchair": "accessibility", "vial": "flask-conical",
    "alert-triangle": "triangle-alert", "alert-circle": "circle-alert",
    "x-circle": "circle-x", "pie-chart": "chart-pie",
    "bar-chart-3": "chart-column", "line-chart": "chart-line",
    "bar-chart": "chart-column-big", "clipboard": "clipboard",
    "columns": "columns-3", "settings-2": "settings-2",
}
ICON_ALIAS = {
    "Building2": "building-2", "Columns3": "columns-3", "Grid3x3": "grid-3x3",
    "Link2": "link-2", "Settings2": "settings-2", "Trash2": "trash-2", "Undo2": "undo-2",
}

# سطحِ نشانِ وضعیت در وایرفریم → سبکِ Badge
CHIP_STYLE = {
    "ok": "Badge.Success", "wn": "Badge.Warning", "dg": "Badge.Danger",
    "in": "Badge.Info", "nu": "Badge.Neutral", "pp": "Badge.Info",
    "cy": "Badge.Neutral", "te": "Badge.Success", "bl": "Badge.Info",
    "bk": "Badge.Neutral", "gr": "Badge.Success", "am": "Badge.Warning",
}

# رنگِ تاکیدیِ کارتِ شاخص
KPI_ACCENT = {
    "teal": "Brush.Primary", "blue": "Brush.Info", "green": "Brush.Success",
    "amber": "Brush.Warning", "rose": "Brush.Danger", "violet": "Brush.Secondary",
    "cyan": "Brush.Info", "slate": "Brush.TextSecondary",
}

# حالتِ دکمه در وایرفریم → سبکِ WPF
BTN_STYLE = {"p": "Btn.Primary", "s": "Btn.Secondary", "g": "Btn.Ghost",
             "d": "Btn.Danger", "k": "Btn.Success", "l": "Btn.Link"}

# ترازِ ستون
ALIGN = {"r": "Right", "l": "Left", "c": "Center"}

# نوعِ فیلد → نامِ کنترلِ WPF (در توضیحات استفاده می‌شود)
FIELD_KIND = {
    "text": "TextBox", "textarea": "TextBox (چندخطی)", "combo": "ComboBox",
    "check": "CheckBox", "date": "DatePicker", "time": "TextBox",
    "national": "TextBox", "phone": "TextBox", "money": "TextBox",
    "num": "TextBox", "password": "PasswordBox", "lookup": "ComboBox",
    "file": "StackPanel", "radio": "RadioButton", "slider": "Slider",
    "chip": "ItemsControl", "color": "TextBox", "email": "TextBox",
    "number": "TextBox (عددی)", "barcode": "TextBox + بارکدخوان",
}


def ikey(name: str | None) -> str | None:
    """heart-pulse → Icon.HeartPulse"""
    if not name:
        return None
    name = ICON_SUB.get(name, name)
    return "Icon." + "".join(p.capitalize() for p in re.split(r"[-_]", name) if p)


def esc(s: str) -> str:
    """گریز از کاراکترهای ویژه‌ی XML"""
    return (str(s).replace("&", "&amp;").replace("<", "&lt;")
            .replace(">", "&gt;").replace('"', "&quot;"))


def vm_name(form: dict) -> str:
    """PatientsView → PatientsViewModel · PatientFormWindow → PatientFormViewModel"""
    n = form["name"]
    for suf in ("View", "Window"):
        if n.endswith(suf):
            return n[: -len(suf)] + "ViewModel"
    return n + "ViewModel"


def model_name(form: dict) -> str:
    """نامِ مدلِ ردیف برای صفحاتِ جدولی"""
    n = form["name"]
    for suf in ("View", "Window"):
        if n.endswith(suf):
            n = n[: -len(suf)]
            break
    return n + "Row"


# ══════════════════════════════════════════════════════════════════════════
# قطعه‌سازها
# ══════════════════════════════════════════════════════════════════════════

def icon_path(key: str | None, size: str = "Small", indent: int = 20,
              brush: str | None = None) -> str:
    """یک <Path> آماده با اندازه و آیکونِ داده‌شده"""
    if not key:
        return ""
    p = " " * indent
    extra = f'\n{p}       Stroke="{{DynamicResource {brush}}}"' if brush else ""
    return (f'{p}<Path Style="{{DynamicResource Icon.{size}}}"\n'
            f'{p}      Data="{{DynamicResource {key}}}"{extra} />')


def kpi_row(kpis: list[dict], ind: int = 12) -> str:
    """ردیفِ کارت‌های شاخص با کنترلِ StatCard"""
    if not kpis:
        return ""
    p = " " * ind
    out = [f'{p}<ItemsControl ItemsSource="{{Binding Kpis}}">',
           f'{p}    <ItemsControl.ItemsPanel>',
           f'{p}        <ItemsPanelTemplate>',
           f'{p}            <UniformGrid Rows="1" />',
           f'{p}        </ItemsPanelTemplate>',
           f'{p}    </ItemsControl.ItemsPanel>',
           f'{p}    <ItemsControl.ItemTemplate>',
           f'{p}        <DataTemplate>',
           f'{p}            <ctrl:StatCard Margin="{{StaticResource S.3}}"',
           f'{p}                          Title="{{Binding Title}}"',
           f'{p}                          Value="{{Binding Value}}"',
           f'{p}                          Hint="{{Binding Hint}}"',
           f'{p}                          Trend="{{Binding Trend}}"',
           f'{p}                          Icon="{{Binding Icon}}"',
           f'{p}                          Accent="{{Binding Accent}}" />',
           f'{p}        </DataTemplate>',
           f'{p}    </ItemsControl.ItemTemplate>',
           f'{p}</ItemsControl>']
    return "\n".join(out)


def stat_cards_static(kpis: list[dict], ind: int = 12) -> str:
    """کارت‌های شاخص با مقادیرِ نمونهٔ ثابت (برای صفحات غیرِ جدولی)"""
    if not kpis:
        return ""
    p = " " * ind
    out = []
    for k in kpis:
        out.append(f'{p}<ctrl:StatCard Margin="{{StaticResource S.3}}"')
        out.append(f'{p}                  Title="{esc(k["l"])}"')
        out.append(f'{p}                  Value="{esc(k["v"])}"')
        trend = (k.get("tr") or "").replace("+", "").replace("−", "-").replace("٪", "")
        trend = trend.replace("−", "-")
        if k.get("tr"):
            try:
                val = float(trend.replace("٫", ".").strip() or 0)
                if k.get("dir") == "down":
                    val = -abs(val)
                out.append(f'{p}                  Trend="{val:g}"')
            except ValueError:
                pass
        ic = ikey(k.get("ic"))
        if ic:
            out.append(f'{p}                  Icon="{{DynamicResource {ic}}}"')
        acc = KPI_ACCENT.get(k.get("ac"))
        if acc:
            out.append(f'{p}                  Accent="{{DynamicResource {acc}}}"')
        out.append(f'{p}                  />')
    return "\n".join(out)


def toolbar_filters(toolbar: list[dict], ind: int = 16) -> str:
    """نوارِ فیلترها — اندازه‌ی هر کنترل متناسب با طولِ داده"""
    if not toolbar:
        return ""
    p = " " * ind
    out = []
    for ti, t in enumerate(toolbar, start=1):
        kind, label = t.get("k"), esc(t.get("l") or "")
        width = {"w-sm": "120", "w-md": "160", "w-lg": "200", "w-xl": "260"}.get(t.get("w"), "160")
        out.append(f'{p}<!-- {label} -->')
        out.append(f'{p}<StackPanel Margin="0,0,{{StaticResource S.3}},0">')
        out.append(f'{p}    <TextBlock Text="{label}"')
        out.append(f'{p}               Style="{{DynamicResource Input.Label}}" />')
        if kind == "search":
            out.append(f'{p}    <ctrl:SearchBox Width="{width}"')
            out.append(f'{p}                    Text="{{Binding SearchText, Mode=TwoWay}}"')
            out.append(f'{p}                    Placeholder="{esc(t.get("v") or "")}" />')
        elif kind == "check":
            out.append(f'{p}    <CheckBox Style="{{DynamicResource Input.CheckBox}}"')
            out.append(f'{p}              IsChecked="{{Binding Filter{ti}, Mode=TwoWay}}" />')
        elif kind == "combo":
            out.append(f'{p}    <ComboBox Width="{width}"')
            out.append(f'{p}              Style="{{DynamicResource Input.ComboBox}}"')
            out.append(f'{p}              SelectedItem="{{Binding Filter{ti}, Mode=TwoWay}}">')
            for o in (t.get("opts") or [t.get("v")]):
                if o:
                    out.append(f'{p}        <ComboBoxItem Content="{esc(o)}"'
                               + (' IsSelected="True" />' if o == t.get("v") else " />"))
            out.append(f'{p}    </ComboBox>')
        elif kind == "date":
            out.append(f'{p}    <DatePicker Width="{width}"')
            out.append(f'{p}                Style="{{DynamicResource Input.Date}}"')
            out.append(f'{p}                SelectedDate="{{Binding Filter{ti}, Mode=TwoWay}}" />')
        else:
            out.append(f'{p}    <TextBox Width="{width}"')
            out.append(f'{p}            Style="{{DynamicResource Input.TextBox}}"')
            out.append(f'{p}            Text="{{Binding Filter{ti}, Mode=TwoWay}}" />')
        out.append(f'{p}</StackPanel>')
    return "\n".join(out)


def action_buttons(actions: list[dict], ind: int = 16, bind: bool = True) -> str:
    """دکمه‌های اقدام — سبک متناسب با اهمیتِ عمل"""
    if not actions:
        return ""
    p = " " * ind
    out = []
    for a in actions:
        style = BTN_STYLE.get(a.get("st"), "Btn.Secondary")
        cmd = a.get("c") or "Do"
        txt = esc(a.get("l") or cmd)
        ic = ikey(a.get("ic"))
        out.append(f'{p}<Button Style="{{DynamicResource {style}}}"')
        if bind:
            out.append(f'{p}        Command="{{Binding {cmd}Command}}"')
        out.append(f'{p}        Margin="0,0,{{StaticResource S.2}},0"')
        out.append(f'{p}        MinHeight="{{StaticResource Size.ButtonHeight}}"')
        out.append(f'{p}        Padding="{{StaticResource P.Button}}">')
        out.append(f'{p}    <StackPanel Orientation="Horizontal">')
        if ic:
            out.append(f'{p}        <Path Style="{{DynamicResource Icon.Small}}"'
                       f'\n{p}              Data="{{DynamicResource {ic}}}"'
                       f'\n{p}              Margin="0,0,{{StaticResource S.2}},0" />')
        out.append(f'{p}        <TextBlock Text="{txt}" VerticalAlignment="Center" />')
        out.append(f'{p}    </StackPanel>')
        out.append(f'{p}</Button>')
    return "\n".join(out)



def col_width(w: str | None) -> str | None:
    """
    تبدیلِ عرضِ ستون از قالبِ وایرفریم (CSS) به قالبِ WPF.

    در CSS عرض‌ها مانند «100px» هستند؛ WPF این واحد را نمی‌پذیرد و مقدار باید
    عددِ خام، «*» یا «Auto» باشد.
    """
    if not w:
        return None
    w = str(w).strip()
    if w.endswith("px"):
        return w[:-2].strip() or None
    if w in ("*", "Auto", "SizeToCells", "SizeToHeader"):
        return w
    return w


def table_block(form: dict, ind: int = 16, bind: bool = True) -> str:
    """جدولِ داده‌ها — ستون‌ها متناسب با نوع و عرضِ داده"""
    cols, rows = form.get("cols") or [], form.get("rows") or []
    if not cols:
        return ""
    p = " " * ind
    out = [f'{p}<DataGrid Style="{{DynamicResource Grid.Modern}}"',
           f'{p}          ItemsSource="{{Binding Items}}"']
    if bind:
        out.append(f'{p}          SelectedItem="{{Binding SelectedItem, Mode=TwoWay}}"')
    out += [f'{p}          AutoGenerateColumns="False"',
            f'{p}          CanUserAddRows="False"',
            f'{p}          IsReadOnly="True"',
            f'{p}          HeadersVisibility="Column"',
            f'{p}          RowHeight="{{StaticResource Size.RowHeight}}"',
            f'{p}          FlowDirection="RightToLeft">',
            f'{p}    <DataGrid.Columns>']
    for c in cols:
        bind_name = c["b"]
        header = esc(c["h"])
        width = col_width(c.get("w"))
        align = ALIGN.get(c.get("al"), "Right")
        kind = c.get("k")
        out.append(f'{p}        <!-- {header} — نوع: {kind} -->')
        if kind == "chip":
            out.append(f'{p}        <DataGridTemplateColumn Header="{header}"'
                       + (f' Width="{width}"' if width else ' Width="SizeToCells"') + '>')
            out.append(f'{p}            <DataGridTemplateColumn.CellTemplate>')
            out.append(f'{p}                <DataTemplate>')
            out.append(f'{p}                    <Border Style="{{DynamicResource Badge}}"')
            out.append(f'{p}                            HorizontalAlignment="Center"')
            out.append(f'{p}                            Background="{{Binding {bind_name}Tone, Converter={{StaticResource StatusTone}}}}">')
            out.append(f'{p}                        <TextBlock Text="{{Binding {bind_name}}}"')
            out.append(f'{p}                                   Style="{{DynamicResource Badge.Text}}" />')
            out.append(f'{p}                    </Border>')
            out.append(f'{p}                </DataTemplate>')
            out.append(f'{p}            </DataGridTemplateColumn.CellTemplate>')
            out.append(f'{p}        </DataGridTemplateColumn>')
        elif kind == "who":
            out.append(f'{p}        <DataGridTemplateColumn Header="{header}" Width="*">')
            out.append(f'{p}            <DataGridTemplateColumn.CellTemplate>')
            out.append(f'{p}                <DataTemplate>')
            out.append(f'{p}                    <StackPanel Orientation="Horizontal">')
            out.append(f'{p}                        <!-- نشانِ تصویریِ بیمار/کاربر -->')
            out.append(f'{p}                        <Border Width="30" Height="30"')
            out.append(f'{p}                                CornerRadius="{{StaticResource R.Pill}}"')
            out.append(f'{p}                                Background="{{Binding {bind_name}Avatar}}"')
            out.append(f'{p}                                Margin="0,0,{{StaticResource S.2}},0">')
            out.append(f'{p}                            <TextBlock Text="{{Binding {bind_name}Initials}}"')
            out.append(f'{p}                                       HorizontalAlignment="Center"')
            out.append(f'{p}                                       VerticalAlignment="Center"')
            out.append(f'{p}                                       Foreground="{{DynamicResource Brush.OnPrimary}}"')
            out.append(f'{p}                                       FontSize="{{StaticResource Fs.Caption}}" />')
            out.append(f'{p}                        </Border>')
            out.append(f'{p}                        <StackPanel VerticalAlignment="Center">')
            out.append(f'{p}                            <TextBlock Text="{{Binding {bind_name}}}"')
            out.append(f'{p}                                       Style="{{DynamicResource Txt.Body}}" />')
            out.append(f'{p}                            <TextBlock Text="{{Binding {bind_name}Sub}}"')
            out.append(f'{p}                                       Style="{{DynamicResource Txt.Caption}}" />')
            out.append(f'{p}                        </StackPanel>')
            out.append(f'{p}                    </StackPanel>')
            out.append(f'{p}                </DataTemplate>')
            out.append(f'{p}            </DataGridTemplateColumn.CellTemplate>')
            out.append(f'{p}        </DataGridTemplateColumn>')
        elif kind == "money":
            out.append(f'{p}        <DataGridTextColumn Header="{header}"'
                       + (f' Width="{width}"' if width else ' Width="*"')
                       + f' Binding="{{Binding {bind_name}}}"')
            out.append(f'{p}                            ElementStyle="{{DynamicResource Txt.Numeric}}">')
            out.append(f'{p}            <DataGridTextColumn.HeaderStyle>')
            out.append(f'{p}                <Style TargetType="DataGridColumnHeader"')
            out.append(f'{p}                       BasedOn="{{StaticResource Grid.Header}}">')
            out.append(f'{p}                    <Setter Property="HorizontalContentAlignment" Value="Left" />')
            out.append(f'{p}                </Style>')
            out.append(f'{p}            </DataGridTextColumn.HeaderStyle>')
            out.append(f'{p}        </DataGridTextColumn>')
        elif kind == "date":
            out.append(f'{p}        <DataGridTextColumn Header="{header}"'
                       + (f' Width="{width}"' if width else ' Width="*"')
                       + f' Binding="{{Binding {bind_name}, Converter={{StaticResource PersianDate}}}}" />')
        else:
            out.append(f'{p}        <DataGridTextColumn Header="{header}"')
            out.append(f'{p}                            Binding="{{Binding {bind_name}}}"')
            if width:
                out.append(f'{p}                            Width="{width}"')
            else:
                out.append(f'{p}                            Width="*"')
            out.append(f'{p}                            ElementStyle="{{DynamicResource Txt.Body}}" />')
    out += [f'{p}    </DataGrid.Columns>', f'{p}</DataGrid>']
    return "\n".join(out)


def empty_state(form: dict, ind: int = 16) -> str:
    """وضعیتِ تهی — هنگامی که جدول داده‌ای ندارد"""
    p = " " * ind
    ic = ikey(form.get("icon")) or "Icon.Inbox"
    return "\n".join([
        f'{p}<ctrl:EmptyState Icon="{{DynamicResource {ic}}}"',
        f'{p}                  Title="{{loc:Loc Common.EmptyTitle}}"',
        f'{p}                  Message="{{loc:Loc Common.EmptyMessage}}"',
        f'{p}                  Visibility="{{Binding IsEmpty,'
        f' Converter={{StaticResource BoolToVisibility}}}}" />',
    ])


def pager(ind: int = 16) -> str:
    """نوارِ صفحه‌بندی"""
    p = " " * ind
    return "\n".join([
        f'{p}<Border Style="{{DynamicResource Card.Flat}}"',
        f'{p}        Padding="{{StaticResource S.3}}"',
        f'{p}        Margin="0,{{StaticResource S.3}},0,0">',
        f'{p}    <Grid>',
        f'{p}        <Grid.ColumnDefinitions>',
        f'{p}            <ColumnDefinition Width="Auto" />',
        f'{p}            <ColumnDefinition Width="*" />',
        f'{p}            <ColumnDefinition Width="Auto" />',
        f'{p}        </Grid.ColumnDefinitions>',
        f'{p}        <TextBlock Grid.Column="0" Text="{{Binding PageSummary}}"',
        f'{p}                   Style="{{DynamicResource Txt.Caption}}"',
        f'{p}                   VerticalAlignment="Center" />',
        f'{p}        <StackPanel Grid.Column="2" Orientation="Horizontal">',
        f'{p}            <Button Style="{{DynamicResource Btn.Icon}}"',
        f'{p}                    Command="{{Binding PreviousPageCommand}}"',
        f'{p}                    ToolTip="{{loc:Loc Common.Previous}}">',
        f'{p}                <Path Style="{{DynamicResource Icon.Small}}"',
        f'{p}                      Data="{{DynamicResource Icon.ChevronRight}}" />',
        f'{p}            </Button>',
        f'{p}            <Button Style="{{DynamicResource Btn.Icon}}"',
        f'{p}                    Command="{{Binding NextPageCommand}}"',
        f'{p}                    ToolTip="{{loc:Loc Common.Next}}">',
        f'{p}                <Path Style="{{DynamicResource Icon.Small}}"',
        f'{p}                      Data="{{DynamicResource Icon.ChevronLeft}}" />',
        f'{p}            </Button>',
        f'{p}        </StackPanel>',
        f'{p}    </Grid>',
        f'{p}</Border>',
    ])


def relations_bar(form: dict, ind: int = 12) -> str:
    """
    نقشهٔ ارتباطات — پیوندِ این صفحه با صفحاتِ دیگر.
    این بخش پاسخِ مستقیم به الزامِ «ارتباطات لحاظ بشه» است.
    """
    rel = form.get("rel") or []
    if not rel:
        return ""
    p = " " * ind
    out = [f'{p}<!-- ══════════════════════════════════════════════════════════',
           f'     ارتباطاتِ این صفحه با دیگر صفحات',
           f'     هر ردیف: «مبدأ» → «مقصد» با توضیحِ نوعِ پیوند',
           f'     این بلوک در همه‌ی صفحات حضور دارد تا مسیرِ حرکتِ کاربر گم نشود.',
           f'     ══════════════════════════════════════════════════════════ -->',
           f'{p}<Border Style="{{DynamicResource Card.Flat}}" Margin="0,{{StaticResource S.3}},0,0">',
           f'{p}    <StackPanel>',
           f'{p}        <StackPanel Orientation="Horizontal" Margin="0,0,0,{{StaticResource S.3}}">',
           icon_path("Icon.GitBranch", "Small", ind + 12, "Brush.Primary"),
           f'{p}            <TextBlock Text="{{loc:Loc Common.Relations}}"',
           f'{p}                       Style="{{DynamicResource Txt.Subtitle}}"',
           f'{p}                       VerticalAlignment="Center"',
           f'{p}                       Margin="{{StaticResource S.2}},0,0,0" />',
           f'{p}        </StackPanel>',
           f'{p}        <ItemsControl ItemsSource="{{Binding Relations}}">',
           f'{p}            <ItemsControl.ItemTemplate>',
           f'{p}                <DataTemplate>',
           f'{p}                    <StackPanel Orientation="Horizontal"',
           f'{p}                                Margin="0,0,0,{{StaticResource S.2}}">',
           f'{p}                        <Border Style="{{DynamicResource Badge.Neutral}}">',
           f'{p}                            <TextBlock Text="{{Binding From}}"',
           f'{p}                                       Style="{{DynamicResource Badge.Text}}" />',
           f'{p}                        </Border>',
           icon_path("Icon.ArrowLeft", "XSmall", ind + 28, "Brush.TextMuted"),
           f'{p}                        <Border Style="{{DynamicResource Badge.Info}}">',
           f'{p}                            <TextBlock Text="{{Binding To}}"',
           f'{p}                                       Style="{{DynamicResource Badge.Text}}" />',
           f'{p}                        </Border>',
           f'{p}                        <TextBlock Text="{{Binding Why}}"',
           f'{p}                                   Style="{{DynamicResource Txt.Caption}}"',
           f'{p}                                   VerticalAlignment="Center"',
           f'{p}                                   Margin="{{StaticResource S.2}},0,0,0" />',
           f'{p}                    </StackPanel>',
           f'{p}                </DataTemplate>',
           f'{p}            </ItemsControl.ItemTemplate>',
           f'{p}        </ItemsControl>',
           f'{p}    </StackPanel>',
           f'{p}</Border>']
    return "\n".join(out)


# ══════════════════════════════════════════════════════════════════════════
# ساختِ شبکه‌ی فیلدها (۱۲ ستون)
# ══════════════════════════════════════════════════════════════════════════

def field_control(fd: dict, ind: int) -> str:
    """کنترلِ متناسب با نوع و طولِ داده — اصلِ «اندازه به نسبتِ کارکرد»"""
    p = " " * ind
    t = fd.get("t")
    val = fd.get("v")
    prop = fd.get("p") or "Value"
    hint = fd.get("hint")
    opts = fd.get("opts") or []

    if t == "textarea":
        return "\n".join([
            f'{p}<TextBox Style="{{DynamicResource Input.TextBox}}"',
            f'{p}         Text="{{Binding {prop}, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}}"',
            f'{p}         AcceptsReturn="True"',
            f'{p}         TextWrapping="Wrap"',
            f'{p}         VerticalScrollBarVisibility="Auto"',
            f'{p}         MinHeight="88"',
            f'{p}         VerticalContentAlignment="Top"',
            (f'{p}         Tag="{esc(val)}"' if isinstance(val, str) else '').rstrip(),
            f'{p}         />',
        ])
    if t == "combo":
        rows = [f'{p}<ComboBox Style="{{DynamicResource Input.ComboBox}}"',
                f'{p}          SelectedItem="{{Binding {prop}, Mode=TwoWay}}">']
        for o in opts:
            sel = ' IsSelected="True"' if o == val else ""
            rows.append(f'{p}    <ComboBoxItem Content="{esc(o)}"{sel} />')
        rows.append(f'{p}</ComboBox>')
        return "\n".join(rows)
    if t == "check":
        return (f'{p}<CheckBox Style="{{DynamicResource Input.CheckBox}}"'
                f'\n{p}          Content="{esc(fd.get("l"))}"'
                f'\n{p}          IsChecked="{{Binding {prop}, Mode=TwoWay}}" />')
    if t == "date":
        return "\n".join([
            f'{p}<DatePicker Style="{{DynamicResource Input.Date}}"',
            f'{p}            SelectedDate="{{Binding {prop}, Mode=TwoWay}}"',
            f'{p}            SelectedDateFormat="Short" />',
        ])
    if t == "password":
        return "\n".join([
            f'{p}<PasswordBox Style="{{DynamicResource Input.Password}}"',
            f'{p}             ctrl:PasswordHelper.Attach="True"',
            f'{p}             ctrl:PasswordHelper.Password="{{Binding {prop}, Mode=TwoWay}}" />',
        ])
    if t == "lookup":
        return "\n".join([
            f'{p}<Grid>',
            f'{p}    <Grid.ColumnDefinitions>',
            f'{p}        <ColumnDefinition Width="*" />',
            f'{p}        <ColumnDefinition Width="Auto" />',
            f'{p}    </Grid.ColumnDefinitions>',
            f'{p}    <ComboBox Grid.Column="0"',
            f'{p}              Style="{{DynamicResource Input.ComboBox}}"',
            f'{p}              IsEditable="True"',
            f'{p}              Text="{{Binding {prop}, Mode=TwoWay}}" />',
            f'{p}    <Button Grid.Column="1"',
            f'{p}            Style="{{DynamicResource Btn.Icon}}"',
            f'{p}            Command="{{Binding Lookup{prop}Command}}"',
            f'{p}            Margin="{{StaticResource S.2}},0,0,0"',
            f'{p}            ToolTip="{{loc:Loc Common.Search}}">',
            f'{p}        <Path Style="{{DynamicResource Icon.Small}}"',
            f'{p}              Data="{{DynamicResource Icon.Search}}" />',
            f'{p}    </Button>',
            f'{p}</Grid>',
        ])
    if t == "money":
        return "\n".join([
            f'{p}<TextBox Style="{{DynamicResource Input.TextBox}}"',
            f'{p}         Text="{{Binding {prop}, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}}"',
            f'{p}         TextAlignment="Left"',
            f'{p}         FlowDirection="LeftToRight" />',
        ])
    if t == "barcode":
        return "\n".join([
            f'{p}<Grid>',
            f'{p}    <Grid.ColumnDefinitions>',
            f'{p}        <ColumnDefinition Width="*" />',
            f'{p}        <ColumnDefinition Width="Auto" />',
            f'{p}    </Grid.ColumnDefinitions>',
            f'{p}    <TextBox Grid.Column="0"',
            f'{p}             Style="{{DynamicResource Input.TextBox}}"',
            f'{p}             Text="{{Binding {prop}, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}}"',
            f'{p}             FlowDirection="LeftToRight"',
            f'{p}             TextAlignment="Left" />',
            f'{p}    <Button Grid.Column="1"',
            f'{p}            Style="{{DynamicResource Btn.Icon}}"',
            f'{p}            Command="{{Binding Scan{prop}Command}}"',
            f'{p}            Margin="{{StaticResource S.2}},0,0,0"',
            f'{p}            ToolTip="بارکدخوان">',
            f'{p}        <Path Style="{{DynamicResource Icon.Small}}"',
            f'{p}              Data="{{DynamicResource Icon.Barcode}}" />',
            f'{p}    </Button>',
            f'{p}</Grid>',
        ])
    if t in ("national", "phone", "num", "number"):
        ml = {"national": 10, "phone": 11, "num": 12, "number": 12}[t]
        return "\n".join([
            f'{p}<TextBox Style="{{DynamicResource Input.TextBox}}"',
            f'{p}         Text="{{Binding {prop}, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}}"',
            f'{p}         MaxLength="{ml}"',
            f'{p}         TextAlignment="Left"',
            f'{p}         FlowDirection="LeftToRight" />',
        ])
    if t == "radio":
        rows = [f'{p}<StackPanel Orientation="Horizontal">']
        for i, o in enumerate(opts or ["بله", "خیر"]):
            rows.append(f'{p}    <RadioButton Style="{{DynamicResource Input.Radio}}"'
                        f' Content="{esc(o)}"'
                        + (' IsChecked="True" />' if i == 0 else ' />'))
        rows.append(f'{p}</StackPanel>')
        return "\n".join(rows)
    # پیش‌فرض: ورودی متنی
    return "\n".join([
        f'{p}<TextBox Style="{{DynamicResource Input.TextBox}}"',
        f'{p}         Text="{{Binding {prop}, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}}"'
        + (f'\n{p}         Tag="{esc(val)}"' if isinstance(val, str) and val else ''),
        f'{p}         />',
    ])


def field_block(fd: dict, row: int, col: int, ind: int) -> str:
    """یک سلولِ فرم: برچسب + کنترل + راهنما + خطا"""
    p = " " * ind
    span = max(1, min(12, int(fd.get("s") or 12)))
    req = fd.get("req")
    label = esc(fd.get("l") or fd.get("p") or "")
    t = fd.get("t")
    out = [f'{p}<!-- {label} — {FIELD_KIND.get(t, "TextBox")} '
           f'· پهنای {span}/۱۲ -->',
           f'{p}<StackPanel Grid.Row="{row}" Grid.Column="{col}" '
           f'Grid.ColumnSpan="{span}"',
           f'{p}            Margin="0,0,{{StaticResource S.3}},{{StaticResource S.3}}">']
    if t != "check":
        out.append(f'{p}    <TextBlock Text="{label}"')
        out.append(f'{p}               Style="{{DynamicResource '
                   + ("Input.LabelRequired" if req else "Input.Label") + '}" />')
    out.append(field_control(fd, ind + 4))
    if fd.get("hint"):
        out.append(f'{p}    <TextBlock Text="{esc(fd["hint"])}"')
        out.append(f'{p}               Style="{{DynamicResource Txt.Caption}}"')
        out.append(f'{p}               Margin="0,{{StaticResource S.1}},0,0" />')
    if req:
        out.append(f'{p}    <!-- خطاهای اعتبارسنجی به‌صورت خودکار '
                   f'از INotifyDataErrorInfo نمایش داده می‌شوند -->')
    out.append(f'{p}</StackPanel>')
    return "\n".join(out)


def sections_grid(sections: list[dict], ind: int = 16) -> str:
    """شبکه‌ی ۱۲ ستونه‌ی فرم با شکستنِ خودکارِ سطر"""
    p = " " * ind
    out = [f'{p}<Grid>',
           f'{p}    <Grid.ColumnDefinitions>']
    out += [f'{p}        <ColumnDefinition Width="*" />' for _ in range(12)]
    out += [f'{p}    </Grid.ColumnDefinitions>',
            f'{p}    <Grid.RowDefinitions>']
    rows_needed = 0
    # محاسبه‌ی تعداد سطرها
    def count_rows(secs):
        r, c = 0, 0
        for sec in secs:
            r += 1  # سربرگِ بخش
            c = 0
            for fd in sec.get("fields") or []:
                s = max(1, min(12, int(fd.get("s") or 12)))
                if c + s > 12:
                    r += 1
                    c = 0
                c += s
            r += 1
        return r + 1

    for _ in range(count_rows(sections) + 1):
        out.append(f'{p}        <RowDefinition Height="Auto" />')
    out.append(f'{p}    </Grid.RowDefinitions>')

    row, col = 0, 0
    for sec in sections:
        # سربرگِ بخش
        out.append(f'{p}    <!-- ── بخش: {esc(sec.get("t") or "")} ── -->')
        out.append(f'{p}    <Border Grid.Row="{row}" Grid.Column="0" Grid.ColumnSpan="12"')
        out.append(f'{p}            Margin="0,{{StaticResource S.2}},0,{{StaticResource S.3}}">')
        out.append(f'{p}        <StackPanel Orientation="Horizontal">')
        ic = ikey(sec.get("ic"))
        if ic:
            out.append(icon_path(ic, "Small", ind + 12, "Brush.Primary"))
        out.append(f'{p}            <TextBlock Text="{esc(sec.get("t") or "")}"')
        out.append(f'{p}                       Style="{{DynamicResource Txt.Title}}"')
        out.append(f'{p}                       VerticalAlignment="Center"')
        out.append(f'{p}                       Margin="{{StaticResource S.2}},0,0,0" />')
        out.append(f'{p}        </StackPanel>')
        out.append(f'{p}    </Border>')
        row += 1
        col = 0
        for fd in sec.get("fields") or []:
            span = max(1, min(12, int(fd.get("s") or 12)))
            if col + span > 12:
                row += 1
                col = 0
            out.append(field_block(fd, row, col, ind + 4))
            col += span
        row += 1
        col = 0
    out.append(f'{p}</Grid>')
    return "\n".join(out)


def tabs_block(form: dict, ind: int = 16) -> str:
    """تب‌های صفحه با نمایشِ تعدادِ آیتم"""
    tabs = form.get("tabs") or []
    if not tabs:
        return ""
    p = " " * ind
    out = [f'{p}<TabControl Style="{{DynamicResource Tab.Modern}}">']
    for i, t in enumerate(tabs):
        label, icon, count = (list(t) + [None, None])[:3]
        out.append(f'{p}    <TabItem Style="{{DynamicResource Tab.Item}}">')
        out.append(f'{p}        <TabItem.Header>')
        out.append(f'{p}            <StackPanel Orientation="Horizontal">')
        ic = ikey(icon)
        if ic:
            out.append(icon_path(ic, "Small", ind + 16))
        out.append(f'{p}                <TextBlock Text="{esc(label)}"')
        out.append(f'{p}                           VerticalAlignment="Center"'
                   + (' Margin="6,0,0,0" />' if ic else ' />'))
        if count is not None:
            out.append(f'{p}                <Border Style="{{DynamicResource Badge.Neutral}}"')
            out.append(f'{p}                        Margin="6,0,0,0">')
            out.append(f'{p}                    <TextBlock Text="{count}"')
            out.append(f'{p}                               Style="{{DynamicResource Badge.Text}}" />')
            out.append(f'{p}                </Border>')
        out.append(f'{p}            </StackPanel>')
        out.append(f'{p}        </TabItem.Header>')
        out.append(f'{p}        <!-- محتوای تب «{esc(label)}» در ViewModel تأمین می‌شود -->')
        out.append(f'{p}        <ContentControl Content="{{Binding TabContents[{i}]}}" />')
        out.append(f'{p}    </TabItem>')
    out.append(f'{p}</TabControl>')
    return "\n".join(out)


def aside_block(aside: dict, ind: int = 16) -> str:
    """پانلِ کناریِ خلاصه"""
    if not aside:
        return ""
    p = " " * ind
    out = [f'{p}<Border Style="{{DynamicResource Card}}"',
           f'{p}        MinWidth="300"',
           f'{p}        Margin="{{StaticResource S.3}},0,0,0">',
           f'{p}    <StackPanel>',
           f'{p}        <StackPanel Orientation="Horizontal"'
           f' Margin="0,0,0,{{StaticResource S.3}}">']
    ic = ikey(aside.get("icon"))
    if ic:
        out.append(icon_path(ic, "Medium", ind + 12, "Brush.Primary"))
    out.append(f'{p}            <TextBlock Text="{esc(aside.get("title") or "")}"')
    out.append(f'{p}                       Style="{{DynamicResource Txt.Subtitle}}"')
    out.append(f'{p}                       VerticalAlignment="Center"'
               + (' Margin="8,0,0,0" />' if ic else ' />'))
    out.append(f'{p}        </StackPanel>')
    for k, v in aside.get("fields") or []:
        out.append(f'{p}        <Grid Margin="0,0,0,{{StaticResource S.2}}">')
        out.append(f'{p}            <Grid.ColumnDefinitions>')
        out.append(f'{p}                <ColumnDefinition Width="Auto" />')
        out.append(f'{p}                <ColumnDefinition Width="*" />')
        out.append(f'{p}            </Grid.ColumnDefinitions>')
        out.append(f'{p}            <TextBlock Grid.Column="0" Text="{esc(k)}"')
        out.append(f'{p}                       Style="{{DynamicResource Txt.Caption}}"')
        out.append(f'{p}                       Margin="0,0,{{StaticResource S.3}},0" />')
        out.append(f'{p}            <TextBlock Grid.Column="1" Text="{esc(v)}"')
        out.append(f'{p}                       Style="{{DynamicResource Txt.Body}}"')
        out.append(f'{p}                       TextAlignment="Right" />')
        out.append(f'{p}        </Grid>')
    out += [f'{p}    </StackPanel>', f'{p}</Border>']
    return "\n".join(out)


def hero_block(hero: dict, ind: int = 16) -> str:
    """سربرگِ برجسته (برای جزئیات و ورود)"""
    if not hero:
        return ""
    p = " " * ind
    out = [f'{p}<Border Style="{{DynamicResource Card}}"',
           f'{p}        Padding="{{StaticResource S.6}}">',
           f'{p}    <Grid>',
           f'{p}        <Grid.ColumnDefinitions>',
           f'{p}            <ColumnDefinition Width="Auto" />',
           f'{p}            <ColumnDefinition Width="*" />',
           f'{p}        </Grid.ColumnDefinitions>']
    if hero.get("av"):
        out.append(f'{p}        <Border Grid.Column="0" Width="72" Height="72"')
        out.append(f'{p}                CornerRadius="{{StaticResource R.Pill}}"')
        out.append(f'{p}                Background="{{DynamicResource Brush.Primary}}"')
        out.append(f'{p}                Margin="0,0,{{StaticResource S.4}},0">')
        out.append(f'{p}            <TextBlock Text="{esc(hero["av"])}"')
        out.append(f'{p}                       HorizontalAlignment="Center"')
        out.append(f'{p}                       VerticalAlignment="Center"')
        out.append(f'{p}                       FontSize="{{StaticResource Fs.Title}}"')
        out.append(f'{p}                       Foreground="{{DynamicResource Brush.OnPrimary}}" />')
        out.append(f'{p}        </Border>')
    out.append(f'{p}        <StackPanel Grid.Column="1">')
    if hero.get("name"):
        out.append(f'{p}            <TextBlock Text="{esc(hero["name"])}"')
        out.append(f'{p}                       Style="{{DynamicResource Txt.Headline}}" />')
    if hero.get("sub"):
        out.append(f'{p}            <TextBlock Text="{esc(hero["sub"])}"')
        out.append(f'{p}                       Style="{{DynamicResource Txt.Caption}}" />')
    if hero.get("chips"):
        out.append(f'{p}            <StackPanel Orientation="Horizontal"'
                   f' Margin="0,{{StaticResource S.3}},0,0">')
        for label, tone in hero["chips"]:
            out.append(f'{p}                <Border Style="{{DynamicResource '
                       + CHIP_STYLE.get(tone, "Badge.Neutral") + '}}"')
            out.append(f'{p}                        Margin="0,0,{{StaticResource S.2}},0">')
            out.append(f'{p}                    <TextBlock Text="{esc(label)}"')
            out.append(f'{p}                               Style="{{DynamicResource Badge.Text}}" />')
            out.append(f'{p}                </Border>')
        out.append(f'{p}            </StackPanel>')
    if hero.get("stats"):
        out.append(f'{p}            <StackPanel Orientation="Horizontal"'
                   f' Margin="0,{{StaticResource S.4}},0,0">')
        for label, value in hero["stats"]:
            out.append(f'{p}                <StackPanel Margin="0,0,{{StaticResource S.6}},0">')
            out.append(f'{p}                    <TextBlock Text="{esc(value)}"')
            out.append(f'{p}                               Style="{{DynamicResource Txt.Title}}" />')
            out.append(f'{p}                    <TextBlock Text="{esc(label)}"')
            out.append(f'{p}                               Style="{{DynamicResource Txt.Caption}}" />')
            out.append(f'{p}                </StackPanel>')
        out.append(f'{p}            </StackPanel>')
    out += [f'{p}        </StackPanel>', f'{p}    </Grid>', f'{p}</Border>']
    return "\n".join(out)


# ══════════════════════════════════════════════════════════════════════════
# چیدمان‌های اختصاصی به ازای هر کهن‌الگو
# ══════════════════════════════════════════════════════════════════════════

def body_dashboard(f: dict) -> str:
    return "\n".join([
        stat_cards_static(f.get("kpis") or []),
        "",
        '        <Grid Margin="0,0,0,12">',
        '            <Grid.ColumnDefinitions>',
        '                <ColumnDefinition Width="2*" />',
        '                <ColumnDefinition Width="Auto" />',
        '            </Grid.ColumnDefinitions>',
        '            <Border Grid.Column="0" Style="{DynamicResource Card}">',
        '                <StackPanel>',
        '                    <TextBlock Text="روند درآمد ۳۰ روزه"',
        '                               Style="{DynamicResource Txt.Subtitle}" />',
        '                    <!-- نمودار — در پیاده‌سازی با LiveCharts جایگزین می‌شود -->',
        '                    <ContentControl Content="{Binding RevenueChart}"',
        '                                    Height="220" />',
        '                </StackPanel>',
        '            </Border>',
        aside_block({"title": "صف انتظار زنده", "icon": "users",
                     "fields": [["در انتظار", "۷"], ["در حال ویزیت", "۳"],
                                ["میانگین انتظار", "۱۴ دقیقه"]]}, 12),
        '        </Grid>',
    ])


def body_list(f: dict) -> str:
    parts = []
    if f.get("kpis"):
        parts += ['        <ItemsControl ItemsSource="{Binding Kpis}">',
                  '            <ItemsControl.ItemsPanel>',
                  '                <ItemsPanelTemplate>',
                  '                    <UniformGrid Rows="1" />',
                  '                </ItemsPanelTemplate>',
                  '            </ItemsControl.ItemsPanel>',
                  '            <ItemsControl.ItemTemplate>',
                  '                <DataTemplate>',
                  '                    <ctrl:StatCard Margin="{StaticResource S.3}"',
                  '                                  Title="{Binding Title}"',
                  '                                  Value="{Binding Value}"',
                  '                                  Trend="{Binding Trend}"',
                  '                                  Icon="{Binding Icon}"',
                  '                                  Accent="{Binding Accent}" />',
                  '                </DataTemplate>',
                  '            </ItemsControl.ItemTemplate>',
                  '        </ItemsControl>', ""]
    filters = toolbar_filters(f.get("toolbar") or [])
    acts = action_buttons(f.get("actions") or [])
    if filters or acts:
        parts += ['        <Border Style="{DynamicResource Card.Flat}"',
                  '                Padding="{StaticResource S.3}"',
                  '                Margin="0,0,0,12">',
                  '            <Grid>',
                  '                <Grid.ColumnDefinitions>',
                  '                    <ColumnDefinition Width="*" />',
                  '                    <ColumnDefinition Width="Auto" />',
                  '                </Grid.ColumnDefinitions>',
                  '                <StackPanel Grid.Column="0" Orientation="Horizontal">',
                  filters,
                  '                </StackPanel>',
                  '                <StackPanel Grid.Column="1" Orientation="Horizontal"',
                  '                            HorizontalAlignment="Left">',
                  acts,
                  '                </StackPanel>',
                  '            </Grid>',
                  '        </Border>', ""]
    parts.append(table_block(f))
    parts.append(empty_state(f))
    parts.append(pager())
    return "\n".join(p for p in parts if p is not None)


def body_list_detail(f: dict) -> str:
    return "\n".join([
        '        <Grid>',
        '            <Grid.ColumnDefinitions>',
        '                <ColumnDefinition Width="*" />',
        '                <ColumnDefinition Width="Auto" />',
        '            </Grid.ColumnDefinitions>',
        '            <StackPanel Grid.Column="0">',
        body_list(f),
        '            </StackPanel>',
        aside_block(f.get("aside") or {}, 12),
        '        </Grid>',
    ])


def body_split(f: dict) -> str:
    return "\n".join([
        '        <Grid>',
        '            <Grid.ColumnDefinitions>',
        '                <ColumnDefinition Width="*" />',
        '                <ColumnDefinition Width="360" />',
        '            </Grid.ColumnDefinitions>',
        '            <StackPanel Grid.Column="0">',
        body_list(f),
        '            </StackPanel>',
        aside_block(f.get("aside") or {}, 12),
        '        </Grid>',
    ])


def body_detail_tabs(f: dict) -> str:
    parts = []
    if f.get("kpis"):
        parts.append(stat_cards_static(f.get("kpis") or []))
        parts.append("")
    if f.get("sections"):
        parts.append(sections_grid(f.get("sections") or [], 8))
        parts.append("")
    if f.get("tabs"):
        parts.append(tabs_block(f))
        parts.append("")
    parts.append(table_block(f))
    parts.append(empty_state(f))
    return "\n".join(p for p in parts if p is not None)


def body_form(f: dict) -> str:
    parts = [sections_grid(f.get("sections") or [], 8), ""]
    if f.get("total"):
        t = f["total"]
        parts += ['        <Border Style="{DynamicResource Card.Flat}"',
                  '                Padding="{StaticResource S.4}">',
                  '            <Grid>',
                  '                <Grid.ColumnDefinitions>',
                  '                    <ColumnDefinition Width="*" />',
                  '                    <ColumnDefinition Width="Auto" />',
                  '                </Grid.ColumnDefinitions>',
                  '                <StackPanel Grid.Column="0" Orientation="Horizontal">']
        for label, value in (("تعداد اقلام", t.get("items")),
                             ("جمع کل", t.get("subtotal")),
                             ("تخفیف", t.get("discount"))):
            if value:
                parts += [f'                    <TextBlock Text="{esc(label)}: {esc(value)}"',
                          '                               Style="{DynamicResource Txt.Caption}"',
                          '                               Margin="0,0,16,0" />']
        parts += ['                </StackPanel>',
                  '                <StackPanel Grid.Column="1" Orientation="Horizontal">',
                  '                    <TextBlock Text="قابل پرداخت"',
                  '                               Style="{DynamicResource Txt.Subtitle}"',
                  '                               VerticalAlignment="Center"',
                  '                               Margin="0,0,8,0" />',
                  f'                    <TextBlock Text="{esc(t.get("total"))}"',
                  '                               Style="{DynamicResource Txt.Title}" />',
                  '                </StackPanel>',
                  '            </Grid>',
                  '        </Border>', ""]
    return "\n".join(parts)


def body_settings(f: dict) -> str:
    parts = ['        <Grid>',
             '            <Grid.ColumnDefinitions>',
             '                <ColumnDefinition Width="220" />',
             '                <ColumnDefinition Width="*" />',
             '            </Grid.ColumnDefinitions>',
             '            <!-- فهرستِ بخش‌های تنظیمات -->',
             '            <ListBox Grid.Column="0"',
             '                     ItemsSource="{Binding Sections}"',
             '                     SelectedItem="{Binding SelectedSection, Mode=TwoWay}"',
             '                     Margin="0,0,12,0" />',
             '            <ScrollViewer Grid.Column="1" VerticalScrollBarVisibility="Auto">',
             '                <StackPanel>']
    parts.append(sections_grid(f.get("sections") or [], 16))
    parts += ['                </StackPanel>',
              '            </ScrollViewer>',
              '        </Grid>', ""]
    return "\n".join(parts)


def body_report(f: dict) -> str:
    return body_list(f)


def body_kanban(f: dict) -> str:
    cols = f.get("cols") or []
    out = ['        <!-- ستون‌های کانبان: هر ستون یک وضعیت -->',
           '        <ItemsControl ItemsSource="{Binding Columns}">',
           '            <ItemsControl.ItemsPanel>',
           '                <ItemsPanelTemplate>',
           '                    <UniformGrid Rows="1" />',
           '                </ItemsPanelTemplate>',
           '            </ItemsControl.ItemsPanel>',
           '            <ItemsControl.ItemTemplate>',
           '                <DataTemplate>',
           '                    <Border Style="{DynamicResource Card}"',
           '                            Margin="{StaticResource S.2}"',
           '                            MinWidth="260">',
           '                        <StackPanel>',
           '                            <TextBlock Text="{Binding Title}"',
           '                                       Style="{DynamicResource Txt.Subtitle}" />',
           '                            <ItemsControl ItemsSource="{Binding Cards}"',
           '                                          Margin="0,8,0,0">',
           '                                <ItemsControl.ItemTemplate>',
           '                                    <DataTemplate>',
           '                                        <Border Style="{DynamicResource Card.Flat}"',
           '                                                Padding="{StaticResource S.3}"',
           '                                                Margin="0,0,0,8">',
           '                                            <StackPanel>',
           '                                                <TextBlock Text="{Binding Title}"',
           '                                                           Style="{DynamicResource Txt.Body}" />',
           '                                                <TextBlock Text="{Binding Sub}"',
           '                                                           Style="{DynamicResource Txt.Caption}" />',
           '                                            </StackPanel>',
           '                                        </Border>',
           '                                    </DataTemplate>',
           '                                </ItemsControl.ItemTemplate>',
           '                            </ItemsControl>',
           '                        </StackPanel>',
           '                    </Border>',
           '                </DataTemplate>',
           '            </ItemsControl.ItemTemplate>',
           '        </ItemsControl>', ""]
    out.append(table_block(f))
    return "\n".join(out)


def body_pos(f: dict) -> str:
    return "\n".join([
        '        <Grid>',
        '            <Grid.ColumnDefinitions>',
        '                <ColumnDefinition Width="*" />',
        '                <ColumnDefinition Width="340" />',
        '            </Grid.ColumnDefinitions>',
        '            <StackPanel Grid.Column="0">',
        body_list(f),
        '            </StackPanel>',
        '            <!-- سبدِ فروش -->',
        '            <Border Grid.Column="1" Style="{DynamicResource Card}"',
        '                    Margin="{StaticResource S.3},0,0,0">',
        '                <StackPanel>',
        '                    <StackPanel Orientation="Horizontal"',
        '                                Margin="0,0,0,12">',
        icon_path("Icon.ShoppingCart", "Medium", 24, "Brush.Primary"),
        '                        <TextBlock Text="سبد فروش"',
        '                                   Style="{DynamicResource Txt.Subtitle}"',
        '                                   VerticalAlignment="Center" Margin="8,0,0,0" />',
        '                    </StackPanel>',
        '                    <ItemsControl ItemsSource="{Binding CartItems}" />',
        '                    <Separator Style="{DynamicResource Separator.H}" />',
        '                    <TextBlock Text="{Binding CartTotal}"',
        '                               Style="{DynamicResource Txt.Title}" />',
        '                </StackPanel>',
        '            </Border>',
        '        </Grid>',
    ])


def body_dialog(f: dict) -> str:
    return sections_grid(f.get("sections") or [], 8)


def body_form_narrow(f: dict) -> str:
    parts = []
    hero = f.get("hero")
    if hero and hero.get("features"):
        parts += ['        <ItemsControl ItemsSource="{Binding Features}"',
                  '                      Margin="0,0,0,20">',
                  '            <ItemsControl.ItemTemplate>',
                  '                <DataTemplate>',
                  '                    <StackPanel Orientation="Horizontal" Margin="0,0,0,8">',
                  '                        <Path Style="{DynamicResource Icon.Small}"',
                  '                              Data="{Binding Icon}"',
                  '                              Stroke="{DynamicResource Brush.Success}"',
                  '                              Margin="0,0,8,0" />',
                  '                        <TextBlock Text="{Binding Text}"',
                  '                                   Style="{DynamicResource Txt.Caption}" />',
                  '                    </StackPanel>',
                  '                </DataTemplate>',
                  '            </ItemsControl.ItemTemplate>',
                  '        </ItemsControl>', ""]
    parts.append(sections_grid(f.get("sections") or [], 8))
    return "\n".join(parts)


def body_viewer(f: dict) -> str:
    return "\n".join([
        '        <Border Style="{DynamicResource Card}" Padding="0">',
        '            <Grid>',
        '                <Image Source="{Binding CurrentImage}"',
        '                       Stretch="Uniform" />',
        '                <StackPanel Orientation="Horizontal"',
        '                            HorizontalAlignment="Center"',
        '                            VerticalAlignment="Bottom"',
        '                            Margin="0,0,0,16">',
        '                    <Button Style="{DynamicResource Btn.Icon}"',
        '                            Command="{Binding ZoomOutCommand}">',
        '                        <Path Style="{DynamicResource Icon.Small}"',
        '                              Data="{DynamicResource Icon.ZoomOut}" />',
        '                    </Button>',
        '                    <Button Style="{DynamicResource Btn.Icon}"',
        '                            Command="{Binding ZoomInCommand}">',
        '                        <Path Style="{DynamicResource Icon.Small}"',
        '                              Data="{DynamicResource Icon.ZoomIn}" />',
        '                    </Button>',
        '                </StackPanel>',
        '            </Grid>',
        '        </Border>',
    ])


def body_splash(f: dict) -> str:
    return "\n".join([
        '        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">',
        '            <Path Style="{DynamicResource Icon.Display}"',
        '                  Data="{DynamicResource Icon.HeartPulse}"',
        '                  Width="72" Height="72"',
        '                  Stroke="{DynamicResource Brush.Primary}"',
        '                  StrokeThickness="1.5"',
        '                  HorizontalAlignment="Center" />',
        '            <TextBlock Text="{Binding AppName}"',
        '                       Style="{DynamicResource Txt.Display}"',
        '                       HorizontalAlignment="Center"',
        '                       Margin="0,16,0,4" />',
        '            <TextBlock Text="{Binding AppTagline}"',
        '                       Style="{DynamicResource Txt.Caption}"',
        '                       HorizontalAlignment="Center" />',
        '            <ProgressBar IsIndeterminate="True"',
        '                         Width="220" Height="4"',
        '                         Margin="0,24,0,0" />',
        '            <TextBlock Text="{Binding StatusText}"',
        '                       Style="{DynamicResource Txt.Caption}"',
        '                       HorizontalAlignment="Center"',
        '                       Margin="0,8,0,0" />',
        '        </StackPanel>',
    ])


def body_shell(f: dict) -> str:
    return "\n".join([
        '        <Grid>',
        '            <Grid.ColumnDefinitions>',
        '                <ColumnDefinition Width="Auto" />',
        '                <ColumnDefinition Width="*" />',
        '            </Grid.ColumnDefinitions>',
        '            <Grid.RowDefinitions>',
        '                <RowDefinition Height="Auto" />',
        '                <RowDefinition Height="*" />',
        '                <RowDefinition Height="Auto" />',
        '            </Grid.RowDefinitions>',
        '',
        '            <!-- نوارِ بالا -->',
        '            <Border Grid.Row="0" Grid.ColumnSpan="2"',
        '                    Height="{StaticResource Size.HeaderHeight}"',
        '                    Background="{DynamicResource Brush.Surface}"',
        '                    BorderBrush="{DynamicResource Brush.Border}"',
        '                    BorderThickness="0,0,0,1">',
        '                <Grid Margin="{StaticResource S.4}">',
        '                    <StackPanel Orientation="Horizontal"',
        '                                VerticalAlignment="Center">',
        '                        <Path Style="{DynamicResource Icon.Medium}"',
        '                              Data="{DynamicResource Icon.HeartPulse}"',
        '                              Stroke="{DynamicResource Brush.Primary}" />',
        '                        <TextBlock Text="{Binding AppName}"',
        '                                   Style="{DynamicResource Txt.Title}"',
        '                                   VerticalAlignment="Center"',
        '                                   Margin="8,0,0,0" />',
        '                    </StackPanel>',
        '                    <StackPanel Orientation="Horizontal"',
        '                                HorizontalAlignment="Right"',
        '                                VerticalAlignment="Center">',
        '                        <Button Style="{DynamicResource Btn.Icon}"',
        '                                Command="{Binding ToggleThemeCommand}">',
        '                            <Path Style="{DynamicResource Icon.Small}"',
        '                                  Data="{DynamicResource Icon.SunMoon}" />',
        '                        </Button>',
        '                        <Button Style="{DynamicResource Btn.Icon}"',
        '                                Command="{Binding ToggleLanguageCommand}">',
        '                            <Path Style="{DynamicResource Icon.Small}"',
        '                                  Data="{DynamicResource Icon.Languages}" />',
        '                        </Button>',
        '                        <Button Style="{DynamicResource Btn.Icon}"',
        '                                Command="{Binding NotificationsCommand}">',
        '                            <Path Style="{DynamicResource Icon.Small}"',
        '                                  Data="{DynamicResource Icon.Bell}" />',
        '                        </Button>',
        '                    </StackPanel>',
        '                </Grid>',
        '            </Border>',
        '',
        '            <!-- منوی کناری -->',
        '            <Border Grid.Row="1" Grid.Column="0"',
        '                    Width="{StaticResource Size.SidebarWidth}"',
        '                    Background="{DynamicResource Brush.Sidebar}">',
        '                <ItemsControl ItemsSource="{Binding MenuGroups}"',
        '                              Margin="{StaticResource S.3}">',
        '                    <ItemsControl.ItemTemplate>',
        '                        <DataTemplate>',
        '                            <StackPanel Margin="0,0,0,16">',
        '                                <TextBlock Text="{Binding Title}"',
        '                                           Style="{DynamicResource Txt.Overline}"',
        '                                           Margin="0,0,0,8" />',
        '                                <ItemsControl ItemsSource="{Binding Items}">',
        '                                    <ItemsControl.ItemTemplate>',
        '                                        <DataTemplate>',
        '                                            <Button Style="{DynamicResource Btn.Ghost}"',
        '                                                    Command="{Binding OpenCommand}"',
        '                                                    HorizontalContentAlignment="Right"',
        '                                                    Margin="0,0,0,2">',
        '                                                <StackPanel Orientation="Horizontal">',
        '                                                    <Path Style="{DynamicResource Icon.Small}"',
        '                                                          Data="{Binding Icon}"',
        '                                                          Margin="0,0,8,0" />',
        '                                                    <TextBlock Text="{Binding Title}" />',
        '                                                </StackPanel>',
        '                                            </Button>',
        '                                        </DataTemplate>',
        '                                    </ItemsControl.ItemTemplate>',
        '                                </ItemsControl>',
        '                            </StackPanel>',
        '                        </DataTemplate>',
        '                    </ItemsControl.ItemTemplate>',
        '                </ItemsControl>',
        '            </Border>',
        '',
        '            <!-- ناحیه‌ی محتوا -->',
        '            <ContentControl Grid.Row="1" Grid.Column="1"',
        '                            Content="{Binding CurrentPage}"',
        '                            Margin="{StaticResource P.Page}" />',
        '',
        '            <!-- نوارِ وضعیت -->',
        '            <Border Grid.Row="2" Grid.ColumnSpan="2"',
        '                    Padding="{StaticResource S.3}"',
        '                    Background="{DynamicResource Brush.SurfaceAlt}">',
        '                <TextBlock Text="{Binding StatusText}"',
        '                           Style="{DynamicResource Txt.Caption}" />',
        '            </Border>',
        '        </Grid>',
    ])


def body_detail_timeline(f: dict) -> str:
    parts = [hero_block(f.get("hero") or {}, 8), ""]
    if f.get("tabs"):
        parts.append(tabs_block(f, 8))
        parts.append("")
    parts += ['        <!-- خط زمانِ درمان -->',
              '        <ItemsControl ItemsSource="{Binding Timeline}"',
              '                      Margin="0,16,0,0">',
              '            <ItemsControl.ItemTemplate>',
              '                <DataTemplate>',
              '                    <Grid Margin="0,0,0,12">',
              '                        <Grid.ColumnDefinitions>',
              '                            <ColumnDefinition Width="24" />',
              '                            <ColumnDefinition Width="Auto" />',
              '                            <ColumnDefinition Width="*" />',
              '                        </Grid.ColumnDefinitions>',
              '                        <Path Grid.Column="0"',
              '                              Style="{DynamicResource Icon.Small}"',
              '                              Data="{Binding Icon}"',
              '                              Stroke="{DynamicResource Brush.Primary}" />',
              '                        <TextBlock Grid.Column="1"',
              '                                   Text="{Binding Date}"',
              '                                   Style="{DynamicResource Txt.Caption}"',
              '                                   Margin="8,0,12,0" />',
              '                        <StackPanel Grid.Column="2">',
              '                            <TextBlock Text="{Binding Title}"',
              '                                       Style="{DynamicResource Txt.Body}" />',
              '                            <TextBlock Text="{Binding Detail}"',
              '                                       Style="{DynamicResource Txt.Caption}" />',
              '                        </StackPanel>',
              '                    </Grid>',
              '                </DataTemplate>',
              '            </ItemsControl.ItemTemplate>',
              '        </ItemsControl>']
    return "\n".join(parts)


def body_cards(f: dict) -> str:
    return "\n".join([
        '        <ItemsControl ItemsSource="{Binding Cards}">',
        '            <ItemsControl.ItemsPanel>',
        '                <ItemsPanelTemplate>',
        '                    <WrapPanel />',
        '                </ItemsPanelTemplate>',
        '            </ItemsControl.ItemsPanel>',
        '            <ItemsControl.ItemTemplate>',
        '                <DataTemplate>',
        '                    <Border Style="{DynamicResource Card}"',
        '                            Width="300" Margin="{StaticResource S.3}">',
        '                        <StackPanel>',
        '                            <TextBlock Text="{Binding Title}"',
        '                                       Style="{DynamicResource Txt.Subtitle}" />',
        '                            <TextBlock Text="{Binding Sub}"',
        '                                       Style="{DynamicResource Txt.Caption}"',
        '                                       Margin="0,4,0,8" />',
        '                            <Border Style="{DynamicResource Badge}"',
        '                                    HorizontalAlignment="Right">',
        '                                <TextBlock Text="{Binding Status}"',
        '                                           Style="{DynamicResource Badge.Text}" />',
        '                            </Border>',
        '                        </StackPanel>',
        '                    </Border>',
        '                </DataTemplate>',
        '            </ItemsControl.ItemTemplate>',
        '        </ItemsControl>',
    ])


ARCH = {
    "dashboard": body_dashboard,
    "list-dense": body_list,
    "list-cards": body_cards,
    "list-detail": body_list_detail,
    "split": body_split,
    "detail-tabs": body_detail_tabs,
    "detail-timeline": body_detail_timeline,
    "form-std": body_form,
    "form-narrow": body_form_narrow,
    "form-lines": body_form,
    "dialog": body_dialog,
    "settings": body_settings,
    "report": body_report,
    "kanban": body_kanban,
    "pos": body_pos,
    "viewer": body_viewer,
    "splash": body_splash,
    "shell": body_shell,
}


# ══════════════════════════════════════════════════════════════════════════
# ساختِ فایل
# ══════════════════════════════════════════════════════════════════════════

def header_comment(f: dict, is_window: bool) -> str:
    """سربرگِ مستندساز: مشخصات، فهرستِ فیلدها/ستون‌ها و جدولِ ارتباطات"""
    L = []
    L.append("<!--")
    L.append("╔══════════════════════════════════════════════════════════════════════╗")
    L.append(f"║  {f['title']} — {f['name']}")
    L.append("╠══════════════════════════════════════════════════════════════════════╣")
    L.append(f"║  گروه        : {f['group']}")
    L.append(f"║  کهن‌الگو     : {f['arch']}")
    L.append(f"║  ViewModel   : {vm_name(f)}")
    L.append(f"║  وایرفریم    : 01-wireframes-html/{f['name']}.html")
    if f.get("notes"):
        L.append("╠══════════════════════════════════════════════════════════════════════╣")
        words, line = f["notes"].split(), "║  توضیح       : "
        for w in words:
            if len(line) + len(w) + 1 > 72:
                L.append(line)
                line = "║                "
            line += w + " "
        L.append(line.rstrip())
    L.append("╠══════════════════════════════════════════════════════════════════════╣")
    L.append("║  ساختار (اندازه‌ی هر فیلد/ستون بر پایه‌ی ۱۲ ستون است تا")
    L.append("║  «بزرگیِ عنصر متناسب با کارکردش» باشد — کد ملی ۳ ستون، نشانی ۱۲ ستون)")
    L.append("╠══════════════════════════════════════════════════════════════════════╣")
    L.append("║  نام              نوع              پهنا   الزامی")
    L.append("║  ─────────────────────────────────────────────────")
    for sec in f.get("sections") or []:
        L.append(f"║  ▸ {sec.get('t')}")
        for fd in sec.get("fields") or []:
            nm = str(fd.get("p") or "")[:15]
            ty = str(FIELD_KIND.get(fd.get("t"), fd.get("t")))[:15]
            L.append(f"║    {nm:<16}{ty:<17}{int(fd.get('s') or 12):<7}{'بله' if fd.get('req') else 'خیر'}")
    if f.get("cols"):
        L.append("║  ▸ ستون‌های جدول")
        for c in f["cols"]:
            L.append(f"║    {c['b']:<16}{str(c.get('k')):<17}{str(c.get('w') or '*'):<7}{ALIGN.get(c.get('al'), 'Right')}")
    L.append("╠══════════════════════════════════════════════════════════════════════╣")
    L.append("║  ارتباطات این صفحه")
    L.append("║  ─────────────────────────────────────────────────")
    for r in f.get("rel") or []:
        src, dst, why = (list(r) + ["", "", ""])[:3]
        L.append(f"║    {src}  ──▶  {dst}")
        L.append(f"║         {why}")
    L.append("╚══════════════════════════════════════════════════════════════════════╝")
    L.append("")
    L.append("  قواعدِ رعایت‌شده در این فایل:")
    L.append("    • راست‌چین (RTL) و قلمِ وزیرمتن در سراسرِ صفحه")
    L.append("    • هیچ رنگِ ثابت (هگز) — همه از نشانه‌های معناییِ DynamicResource")
    L.append("    • الگوی MVVM: هیچ منطقی در code-behind، همه چیز از ViewModel")
    L.append("    • متن‌ها از فایلِ زبان (loc:Loc) برای پشتیبانیِ دو زبانهٔ آینده")
    L.append("    • تاریخ‌ها با تبدیل‌گرِ PersianDate به شمسی نمایش داده می‌شوند")
    L.append("    • نقشهٔ ارتباطات در پایینِ صفحه مسیرِ حرکت را نشان می‌دهد")
    L.append("-->")
    return "\n".join(L)


def reindent(text: str, delta: int) -> str:
    """جابه‌جاییِ یکنواختِ تورفتگیِ همه‌ی خطوطِ غیرتهی"""
    pad = " " * delta
    if delta >= 0:
        return "\n".join(pad + l if l.strip() else l for l in text.split("\n"))
    cut = -delta
    return "\n".join(l[cut:] if l.strip() and l.startswith(" " * cut) else l
                      for l in text.split("\n"))


# اندازه‌ی پیش‌فرضِ پنجره به نسبتِ کارکردِ آن
WIN_SIZE = {
    "shell":      (1440, 900, "SizeToContent=\"Manual\""),
    "form-std":   (960, 820, None),
    "form-lines": (1080, 860, None),
    "form-narrow": (520, 720, None),
    "dialog":     (620, 520, 'SizeToContent="Height"'),
    "viewer":     (1180, 800, None),
    "splash":     (560, 420, 'ResizeMode="NoResize" WindowStyle="None"'),
    "detail-tabs": (1280, 880, None),
    "detail-timeline": (1280, 880, None),
}


def build(f: dict) -> str:
    """سرهم‌بندیِ نهاییِ یک View"""
    is_window = f["name"].endswith("Window")
    arch = f["arch"]
    vm = vm_name(f)
    ic = ikey(f.get("icon"))
    root = "Window" if is_window else "UserControl"

    L = [header_comment(f, is_window), ""]
    L.append(f'<{root} x:Class="{NS}.Views.{f["name"]}"')
    L += [
        '        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"',
        '        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"',
        '        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"',
        '        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"',
        '        xmlns:ctrl="clr-namespace:Sahand.ClinicPro.Desktop.Controls"',
        '        xmlns:loc="clr-namespace:Sahand.ClinicPro.Desktop.Localization"',
        '        xmlns:vm="clr-namespace:Sahand.ClinicPro.Desktop.ViewModels"',
        '        mc:Ignorable="d"',
    ]
    if is_window:
        w, h, extra = WIN_SIZE.get(arch, (960, 760, None))
        L += [
            f'        Title="{esc(f["title"])}"',
            '        Style="{DynamicResource Window.Modern}"',
            '        WindowStartupLocation="CenterScreen"',
            f'        Height="{h}" Width="{w}"',
        ]
        if extra:
            L.append(f'        {extra}')
    else:
        L += ['        d:DesignHeight="820" d:DesignWidth="1280"']
    L += [
        f'        d:DataContext="{{d:DesignInstance vm:{vm}}}"',
        '        FlowDirection="RightToLeft"',
        '        FontFamily="{DynamicResource AppFontFamily}"',
        '        Background="{DynamicResource Brush.Canvas}">',
        '',
    ]

    body = ARCH.get(arch, body_list)(f)
    shell = arch == "shell"

    if shell:
        L += [reindent(body, -4), ""]
    else:
        L += [
            '    <Grid>',
            '        <Grid.RowDefinitions>',
            '            <RowDefinition Height="Auto" />',
            '            <RowDefinition Height="*" />',
        ]
        if is_window or arch.startswith("form"):
            L.append('            <RowDefinition Height="Auto" />')
        L += [
            '        </Grid.RowDefinitions>',
            '',
            '        <!-- ═══ سربرگِ یکپارچه‌ی صفحه ═══ -->',
            '        <ctrl:PageHeader Grid.Row="0"',
            f'                        Title="{esc(f["title"])}"',
            f'                        Subtitle="{esc(f.get("subtitle") or "")}"',
        ]
        if ic:
            L.append(f'                        Icon="{{DynamicResource {ic}}}"')
        L += [
            '                        Margin="{StaticResource P.Page}" />',
            '',
            '        <ScrollViewer Grid.Row="1"',
            '                      VerticalScrollBarVisibility="Auto"',
            '                      Padding="{StaticResource P.Page}">',
            '            <StackPanel>',
            reindent(body, 4),
            relations_bar(f, 12),
            '            </StackPanel>',
            '        </ScrollViewer>',
        ]
        acts = f.get("actions") or []
        if is_window or arch.startswith("form"):
            L += [
                '',
                '        <!-- ═══ نوارِ اقدام‌های ثابت ═══ -->',
                '        <Border Grid.Row="2"',
                '                Background="{DynamicResource Brush.Surface}"',
                '                BorderBrush="{DynamicResource Brush.Border}"',
                '                BorderThickness="0,1,0,0"',
                '                Padding="{StaticResource S.4}">',
                '            <StackPanel Orientation="Horizontal"',
                '                        HorizontalAlignment="Left">',
                action_buttons(acts, 16),
                '            </StackPanel>',
                '        </Border>',
            ]
        L += ['    </Grid>', '']

    L.append(f'</{root}>')
    return "\n".join(L)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    written = []
    for f in S.FORMS:
        is_window = f["name"].endswith("Window")
        text = build(f)
        path = OUT / f"{f['name']}.xaml"
        path.write_text(text, encoding="utf-8")
        try:
            ET.parse(path)
        except ET.ParseError as e:
            print(f"  ❌ XML نامعتبر {f['name']}: {e}")
            raise
        written.append(f)

    print(f"✅ {len(written)} View در {OUT.relative_to(ROOT)} تولید شد")
    unknown = sorted({f["arch"] for f in S.FORMS} - set(ARCH))
    if unknown:
        print(f"   ⚠ کهن‌الگوی بدون چیدمان: {unknown}")


if __name__ == "__main__":
    main()

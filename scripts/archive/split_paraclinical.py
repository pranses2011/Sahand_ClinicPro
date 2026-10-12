#!/usr/bin/env python3
# ═══════════════════════════════════════════════════════════════════
# چرخهٔ ۴۳ — تفکیک صفحات پاراکلینیک: استخراج محتوای چهار تب از
# ParaclinicalView.xaml و ساخت چهار UserControl مستقل
# (دندانپزشکی / چشم‌پزشکی / فیزیوتراپی / تصویربرداری)
# ═══════════════════════════════════════════════════════════════════
import re

SRC = 'src/Sahand.ClinicPro.Desktop/Views/ParaclinicalView.xaml'
s = open(SRC, encoding='utf-8').read()

def extract_tab(header_marker):
    """محتوای درونی یک TabItem را با ترازوبلوک برمی‌گرداند."""
    start = s.index(f'<TabItem Header="{header_marker}">')
    i = s.index('>', start) + 1
    depth = 1
    j = i
    while depth > 0:
        if s[j] == '<':
            if s.startswith('</TabItem>', j):
                depth -= 1
                if depth == 0:
                    end = j
                    break
            elif s.startswith('<TabItem', j) and not s.startswith('<TabItems', j):
                depth += 1
        j += 1
    return s[i:end].strip()

tabs = {
    'dentistry': extract_tab('🦷 دندانپزشکی'),
    'eye': extract_tab('👁️ چشم‌پزشکی'),
    'physio': extract_tab('🏃 فیزیوتراپی'),
    'imaging': extract_tab('📷 تصویربرداری'),
}

TEMPLATE = '''<UserControl x:Class="Sahand.ClinicPro.Desktop.Views.{cls}"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             mc:Ignorable="d"
             d:DesignHeight="640" d:DesignWidth="1080"
             FlowDirection="RightToLeft">
    <!-- ═══ {title} — چرخهٔ چهل‌وسوم (گزارش مالک): «دندانپزشکی و چشم پزشکی و
         تصویربرداری و فزیوتراپی در یک صفحه و تب‌های جداگانه هستند. برای هر
         کدوم یک صفحه مستقل طراحی بکن تا با همدیگه تداخلی نداشته باشن» —
         محتوای این تب از پاراکلینیکِ سابقِ تب‌دار جدا شد و اکنون صفحهٔ
         اختصاصی مستقل خودش است؛ تمام منطق و اتصال‌ها بدون تغییر منتقل شدند. ═══ -->
    <Grid Margin="16,12">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,10">
            <materialDesign:PackIcon Kind="{icon}" Width="26" Height="26"
                                     Foreground="{{DynamicResource MaterialDesign.Brush.Primary}}" VerticalAlignment="Center" />
            <TextBlock Text="{title}" FontSize="20" FontWeight="Bold" Margin="10,0,0,0" VerticalAlignment="Center" />
        </StackPanel>

        <!-- محتوای صفحه — عیناً از تب پاراکلینیک سابق (اتصال‌ها بدون تغییر) -->
{content}
    </Grid>
</UserControl>
'''

CODE_BEHIND = '''using System.Windows.Controls;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت صفحهٔ مستقل «{title}» — چرخهٔ چهل‌وسوم: تفکیک از پاراکلینیکِ تب‌دار
/// به صفحهٔ اختصاصی (گزارش مالک: «برای هر کدوم یک صفحه مستقل طراحی بکن»).
/// </summary>
public partial class {cls} : UserControl
{
    /// <summary>سازنده — مقداردهی اولیهٔ اجزا</summary>
    public {cls}()
    {
        InitializeComponent();
    }
}
'''

# نام کلاس/عنوان/آیکون هر صفحه
pages = {
    'dentistry': ('DentistryView', 'دندانپزشکی', 'ToothOutline'),
    'eye': ('OphthalmologyView', 'چشم‌پزشکی', 'Eye'),
    'physio': ('PhysiotherapyView', 'فیزیوتراپی', 'Run'),
    'imaging': ('ImagingView', 'تصویربرداری', 'CameraBurst'),
}

for key, (cls, title, icon) in pages.items():
    content = tabs[key]
    # تورفتگی محتوای استخراج‌شده (۴+۸ فاصله برای جای‌گیری درون ContentControl)
    indented = '\n'.join(('        ' + line) if line.strip() else line for line in content.splitlines())
    xaml = TEMPLATE.format(cls=cls, title=title, icon=icon, content=indented)
    open(f'src/Sahand.ClinicPro.Desktop/Views/{cls}.xaml', 'w', encoding='utf-8').write(xaml)
    open(f'src/Sahand.ClinicPro.Desktop/Views/{cls}.xaml.cs', 'w', encoding='utf-8').write(
        CODE_BEHIND.replace('{cls}', cls).replace('{title}', title))
    print(f"OK — {cls}.xaml({len(content.splitlines())} خط محتوا) + code-behind")

print("تفکیک کامل شد")

using System.Windows;
using System.Windows.Controls;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop;

/// <summary>
/// انتخابگر قالب بخش‌های قابل‌چیدمان داشبورد — چرخهٔ ۷۴ (گزارش مالک:
/// «مدیریت چیدمان داشبورد… بصورت Drag &amp; Drop… کنترل کامل»).
/// هر بخش (DashboardSectionItem) بر اساس کلیدش قالب خودش را می‌گیرد؛
/// قالب‌ها در منابع DashboardView تعریف شده‌اند (SectionTemplate_*).
/// کلید ناشناخته → قالب خالی (بخش هرگز کرش نمی‌کند).
/// </summary>
public class DashboardSectionTemplateSelector : DataTemplateSelector
{
    /// <summary>انتخاب قالب بر اساس کلید بخش</summary>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not DashboardSectionItem section)
            return null;

        var key = $"SectionTemplate_{section.Key}";
        return (container as FrameworkElement)?.TryFindResource(key) as DataTemplate;
    }
}

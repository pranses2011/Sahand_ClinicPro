using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>
/// رفتار چسبانِ جابه‌جایی آیتم‌ها با کشیدن و رها کردن — چرخهٔ ۷۴.
/// گزارش مالک: «مدیریت چیدمان داشبورد خیلی سطحی و ناقص هست… میخوام طوری
/// پیاده سازیش بکنی که بصورت Drag &amp; Drop باشه و بتونم کنترل کامل و
/// حرفه‌ای روی داشبورد داشته باشم».
/// طرز کار: روی هر ItemsControl (با منبع ObservableCollection) فعال می‌شود؛
/// نگه‌داشتن دکمهٔ ماوس روی یک آیتم و کشیدنش روی آیتم دیگر، جای‌به‌جایی
/// می‌کند. جابه‌جایی از طریق خودِ ObservableCollection انجام می‌شود تا
/// رویداد CollectionChanged ViewModel را برای ذخیرهٔ خودکار بیدار کند.
/// کلیک سادهٔ بدون حرکت، به دکمه‌ها/عناصر درونی کار نمی‌زند (درگ فقط با
/// جابه‌جایی واقعی ماوس شروع می‌شود — آستانهٔ ۴ پیکسل).
/// </summary>
public static class DragDropReorder
{
    /// <summary>فعال‌سازی رفتار روی ItemsControl</summary>
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached(
            "Enabled", typeof(bool), typeof(DragDropReorder),
            new PropertyMetadata(false, OnEnabledChanged));

    /// <summary>خواندن پرچم فعال</summary>
    public static bool GetEnabled(DependencyObject obj)
        => (bool)obj.GetValue(EnabledProperty);

    /// <summary>نوشتن پرچم فعال</summary>
    public static void SetEnabled(DependencyObject obj, bool value)
        => obj.SetValue(EnabledProperty, value);

    /// <summary>نقطهٔ شروع فشردن ماوس — برای آستانهٔ حرکت پیش از درگ</summary>
    private static Point? _pressOrigin;

    /// <summary>نصب هندلرها هنگام فعال‌سازی</summary>
    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ItemsControl items)
            return;
        if (Equals(e.NewValue, true))
        {
            items.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            items.PreviewMouseMove += OnPreviewMouseMove;
            items.DragOver += OnDragOver;
            items.Drop += OnDrop;
            items.AllowDrop = true;
        }
        else
        {
            items.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            items.PreviewMouseMove -= OnPreviewMouseMove;
            items.DragOver -= OnDragOver;
            items.Drop -= OnDrop;
        }
    }

    /// <summary>ثبت نقطهٔ فشردن — برای تشخیص «کلیک» از «درگ»</summary>
    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => _pressOrigin = e.GetPosition(null);

    /// <summary>شروع درگ پس از عبور از آستانهٔ حرکت</summary>
    private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _pressOrigin is not { } origin)
            return;
        var position = e.GetPosition(null);
        if (Math.Abs(position.X - origin.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - origin.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        if (sender is not ItemsControl items)
            return;
        var container = ContainerUnderMouse(items, position);
        if (container is null)
            return;

        try
        {
            DragDrop.DoDragDrop(container, new DataObject(ContainerDataFormat, container), DragDropEffects.Move);
        }
        finally
        {
            _pressOrigin = null;
        }
        e.Handled = true;
    }

    /// <summary>قالب دادهٔ درگ — ظرف آیتم مبدأ</summary>
    private const string ContainerDataFormat = "Sahand.ClinicPro.DragDropReorder.Container";

    /// <summary>نشانگر جابه‌جایی هنگام عبور روی خود فهرست</summary>
    private static void OnDragOver(object sender, DragEventArgs e)
        => e.Effects = e.Data.GetDataPresent(ContainerDataFormat)
            ? DragDropEffects.Move
            : DragDropEffects.None;

    /// <summary>رها کردن — جابه‌جایی آیتم مبدأ به جای آیتم مقصد در کلکشن</summary>
    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(ContainerDataFormat))
            return;
        if (e.Data.GetData(ContainerDataFormat) is not FrameworkElement source)
            return;
        if (sender is not ItemsControl items)
            return;

        var target = ContainerUnderMouse(items, e.GetPosition(items));
        if (target is null || ReferenceEquals(target, source))
            return;

        var sourceItem = source.DataContext;
        var targetItem = target.DataContext;
        var list = items.ItemsSource as IList;
        if (list is null || sourceItem is null || targetItem is null)
            return;
        var sourceIndex = list.IndexOf(sourceItem);
        var targetIndex = list.IndexOf(targetItem);
        if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
            return;

        // جابه‌جایی در خود کلکشن — ObservableCollection رویداد تغییر می‌دهد
        // (Move در رابطهٔ عمومی IList نیست؛ Remove+Insert همان اثر را دارد)
        list.RemoveAt(sourceIndex);
        list.Insert(targetIndex, sourceItem);
        e.Handled = true;
    }

    /// <summary>
    /// یافتن ظرف آیتم زیر نقطه — پیمایش درخت بصری به بالا تا ظرفِ تولیدشده
    /// توسط همین ItemsControl (ContentPresenter در ItemsControl ساده،
    /// ListBoxItem در ListBox و…)
    /// </summary>
    private static FrameworkElement? ContainerUnderMouse(ItemsControl items, Point position)
    {
        var hit = items.InputHitTest(position) as DependencyObject;
        while (hit is not null && hit != items)
        {
            if (hit is FrameworkElement element)
            {
                var item = element.DataContext;
                if (item is not null && ReferenceEquals(
                        items.ItemContainerGenerator.ContainerFromItem(item), element))
                    return element;
            }
            hit = System.Windows.Media.VisualTreeHelper.GetParent(hit);
        }
        return null;
    }
}

using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت نوبت‌دهی دو-تبی — گام U4
/// ═══ چرخهٔ ۷۴ — مرتب‌سازی چندستونهٔ فهرست نوبت‌ها (گزارش مالک) ═══
/// «بتونم مرتب‌سازی چندگانه هم انجام بدم (مثلاً وقتی روی عنوان ستون تاریخ
/// میزنم، لیست بر اساس تاریخ مرتب می‌شود ولی نمی‌توانم همزمان بر اساس ساعت
/// هم مرتب کنم و یا بر اساس وضعیت)». الگوی پیاده‌سازی: رویداد Sorting جدول
/// به‌طور کامل مدیریت می‌شود (e.Handled) و وضعیت مرتب‌سازی به‌صورت فهرستِ
/// اولویت‌دارِ (مسیر، جهت) در ViewModel نگهداری و روی نمای مجموعه اعمال
/// می‌شود؛ به این ترتیب مرتب‌سازی «ترکیبی» است، پس از هر جستجو/صفحه‌بندیِ
/// جدید هم بازاعمال می‌شود و پیکان‌های هدر ستون‌ها هم به‌طور خودکار با
/// SortDescriptions نما هم‌گام می‌مانند.
/// </summary>
public partial class AppointmentsView : UserControl
{
    /// <summary>سازنده</summary>
    public AppointmentsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => HookGridSorting();
    }

    /// <summary>اتصال رویداد انتخاب تاریخ تقویم به فرمان ViewModel</summary>
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is AppointmentsViewModel vm)
        {
            vm.SelectDateCommand.Execute(null);
        }
    }

    /// <summary>رویداد انتخاب تاریخ از تقویم شمسی</summary>
    // ═══ طرح جدید اسلات‌ها: هاور = نمایش موقت جزئیات، خروج موس = بازگشت به
    // سنجاق‌شده، کلیک روی رزروشده = سنجاق/برداشتن، کلیک روی آزاد = انتخاب.
    // فقط همان کاشی دست می‌خورد؛ کل شبکه دوباره رندر نمی‌شود. ═══
    private void OnSlotMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SlotVM slot } && DataContext is AppointmentsViewModel vm)
            vm.HoverSlot(slot);
    }

    private void OnSlotMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SlotVM slot } && DataContext is AppointmentsViewModel vm)
            vm.UnhoverSlot(slot);
    }

    private void OnSlotMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: SlotVM slot } || DataContext is not AppointmentsViewModel vm)
            return;
        if (slot.IsBooked) vm.TogglePinSlot(slot);
        else if (vm.SelectSlotCommand.CanExecute(slot)) vm.SelectSlotCommand.Execute(slot);
    }

    private void OnCalendarDateSelected(object? sender, DateTime? e)
    {
        if (DataContext is AppointmentsViewModel vm)
            vm.SelectDateCommand.Execute(e);
    }

    /// <summary>
    /// اتصال یک‌بارهٔ هندلر مرتب‌سازی چندستونه — پس از آماده‌شدن درخت بصری
    /// (ستون‌های DataGrid در Loaded در دسترس‌اند)
    /// </summary>
    private void HookGridSorting()
    {
        if (AppointmentsGrid is null) return;
        // حذف هندلر تکراری در بارگذاری‌های بعدی (تب‌ها/ناوبری)
        AppointmentsGrid.Sorting -= OnGridSorting;
        AppointmentsGrid.Sorting += OnGridSorting;
    }

    /// <summary>
    /// منطق مرتب‌سازی چندستونه — چرخهٔ ۷۴:
    ///   کلیک ساده روی هدر = مرتب‌سازی تک‌ستونه (صعودی ← نزولی ← حذف)؛
    ///   Shift + کلیک = افزودن/معکوس‌کردن همان ستون در مرتب‌سازی ترکیبیِ موجود؛
    ///   ستون‌های بدون SortMemberPath (چک‌باکس/عملیات) به Default واگذار می‌شوند.
    /// </summary>
    private void OnGridSorting(object sender, DataGridSortingEventArgs e)
    {
        if (sender is not DataGrid grid || DataContext is not AppointmentsViewModel vm)
            return;

        var path = e.Column.SortMemberPath;
        if (string.IsNullOrEmpty(path))
            return; // ستون بدون مسیر مرتب‌سازی — رفتار پیش‌فرض

        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        var current = vm.GetSortState();

        ListSortDirection? nextDirection;
        List<(string Path, ListSortDirection Direction)> newState;

        if (shift)
        {
            // ── ترکیبی: اگر ستون موجود است جهتش معکوس، وگرنه «صعودی» اضافه می‌شود ──
            var existing = current.FindIndex(s => s.Path == path);
            if (existing >= 0)
            {
                var dir = current[existing].Direction == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
                current[existing] = (path, dir);
                newState = current;
                nextDirection = dir;
            }
            else
            {
                current.Add((path, ListSortDirection.Ascending));
                newState = current;
                nextDirection = ListSortDirection.Ascending;
            }
        }
        else
        {
            // ── تک‌ستونه: صعودی ← نزولی ← حذف ──
            if (current.Count == 1 && current[0].Path == path)
            {
                if (current[0].Direction == ListSortDirection.Ascending)
                {
                    newState = new List<(string, ListSortDirection)> { (path, ListSortDirection.Descending) };
                    nextDirection = ListSortDirection.Descending;
                }
                else
                {
                    // کلیک سوم = حذف مرتب‌سازی (ترتیب سرور)
                    newState = new List<(string, ListSortDirection)>();
                    nextDirection = null;
                }
            }
            else
            {
                newState = new List<(string, ListSortDirection)> { (path, ListSortDirection.Ascending) };
                nextDirection = ListSortDirection.Ascending;
            }
        }

        // اعمال روی ViewModel و نما؛ جهت هدر برای نمایش پیکان هم مقداردهی می‌شود
        vm.SetSortState(newState);
        e.Column.SortDirection = nextDirection;
        e.Handled = true; // جلوگیری از مرتب‌سازی پیش‌فرضِ تک‌ستونهٔ DataGrid
    }
}

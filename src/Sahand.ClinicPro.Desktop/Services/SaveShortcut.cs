using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>میانبر صریح دکمه؛ در وضعیت مبهم هیچ عملیاتی اجرا نمی‌شود.</summary>
public static class SaveShortcut
{
    public static readonly DependencyProperty NumberProperty = DependencyProperty.RegisterAttached(
        "Number", typeof(int), typeof(SaveShortcut), new PropertyMetadata(0));
    public static void SetNumber(DependencyObject element, int value) => element.SetValue(NumberProperty, value);
    public static int GetNumber(DependencyObject element) => (int)element.GetValue(NumberProperty);

    public static void Register() => EventManager.RegisterClassHandler(typeof(Window), Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnKeyDown));

    private static void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != (ModifierKeys.Control | ModifierKeys.Shift) || e.Key < Key.D1 || e.Key > Key.D9 || e.IsRepeat) return;
        var number = (int)e.Key - (int)Key.D1 + 1;
        var candidates = new List<Button>();
        Collect((DependencyObject)sender, number, candidates);
        // دو دکمهٔ فعال هم‌زمان یا چند ردیف جدول: هرگز بین عملیات انتخاب حدسی نکن.
        if (candidates.Count != 1) return;
        var button = candidates[0];
        if (UIElementAutomationPeer.CreatePeerForElement(button) is ButtonAutomationPeer peer && peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoke)
        { e.Handled = true; invoke.Invoke(); }
    }

    private static void Collect(DependencyObject root, int number, List<Button> candidates)
    {
        if (root is UIElement ui && (!ui.IsVisible || !ui.IsEnabled)) return;
        if (root is Button button && GetNumber(button) == number) candidates.Add(button);
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) Collect(VisualTreeHelper.GetChild(root, i), number, candidates);
    }
}

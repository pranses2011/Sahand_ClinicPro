using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;

namespace Sahand.ClinicPro.Desktop.Controls;

/// <summary>
/// ورودی تگی — افزودن با Enter و حذف با دکمه ✕
/// مصرف‌کننده: فرم بیمار (آلرژی/بیماری مزمن/داروهای فعلی)، تشخیص‌های ثانویه و...
/// </summary>
public partial class TagInputControl : UserControl
{
    /// <summary>سازنده — ساخت فرمان حذف</summary>
    public TagInputControl()
    {
        InitializeComponent();
        RemoveTagCommand = new RelayCommand<object>(RemoveTag);
    }

    /* ─────────────  خصوصیت‌های وابستگی  ───────────── */

    /// <summary>مجموعه تگ‌ها — ObservableCollection رشته‌ای از ViewModel متصل می‌شود</summary>
    public static readonly DependencyProperty TagsProperty =
        DependencyProperty.Register(nameof(Tags), typeof(IList),
            typeof(TagInputControl), new PropertyMetadata(null));

    /// <summary>عنوان بالای کنترل</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string),
            typeof(TagInputControl), new PropertyMetadata(string.Empty));

    /// <summary>راهنمای جعبه ورودی</summary>
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string),
            typeof(TagInputControl), new PropertyMetadata("مورد جدید بنویسید و Enter بزنید"));

    /// <summary>رنگ پس‌زمینه تگ‌ها — قرمز آلرژی، نارنجی مزمن، آبی دارو</summary>
    public static readonly DependencyProperty TagBackgroundProperty =
        DependencyProperty.Register(nameof(TagBackground), typeof(Brush),
            typeof(TagInputControl),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35))));

    /* ─────────────  فرمان‌ها  ───────────── */

    /// <summary>فرمان حذف تگ — از الگوی دکمه داخل قالب داده</summary>
    public static readonly DependencyProperty RemoveTagCommandProperty =
        DependencyProperty.Register(nameof(RemoveTagCommand), typeof(ICommand),
            typeof(TagInputControl), new PropertyMetadata(null));

    /* ─────────────  پوشش  ───────────── */

    /// <summary>مجموعه تگ‌ها</summary>
    public IList? Tags
    {
        get => (IList?)GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    /// <summary>عنوان</summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>راهنما</summary>
    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>رنگ تگ</summary>
    public Brush TagBackground
    {
        get => (Brush)GetValue(TagBackgroundProperty);
        set => SetValue(TagBackgroundProperty, value);
    }

    /// <summary>فرمان حذف</summary>
    public ICommand RemoveTagCommand
    {
        get => (ICommand)GetValue(RemoveTagCommandProperty);
        private set => SetValue(RemoveTagCommandProperty, value);
    }

    /* ─────────────  منطق  ───────────── */

    /// <summary>Enter → افزودن تگ؛ Escape → خالی کردن جعبه</summary>
    private void OnTagBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Return))
            return;

        var text = NewTagBox.Text?.Trim();
        if (string.IsNullOrEmpty(text) || Tags is null)
            return;

        // جلوگیری از تکرار (بدون حساسیت بزرگی حروف)
        var exists = Tags.Cast<object?>()
            .Any(t => string.Equals(Convert.ToString(t), text, StringComparison.OrdinalIgnoreCase));
        if (!exists)
        {
            if (Tags.IsReadOnly)
            {
                // اگر فقط-خواندنی است (مثل آرایه) مجموعه قابل تغییر جایگزین نمی‌کنیم — بی‌اثر
                return;
            }
            Tags.Add(text);
        }

        NewTagBox.Clear();
        e.Handled = true;
    }

    /// <summary>حذف تگ از مجموعه</summary>
    private void RemoveTag(object? tag)
    {
        if (tag is null || Tags is null || Tags.IsReadOnly)
            return;
        Tags.Remove(tag);
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پنجرهٔ جستجوی یکپارچه — چرخهٔ ۶۹ (فاز ۳ / F-06)
/// فوکوس خودکار جعبهٔ جستجو + کلیدهای ↑↓/Enter/Esc + دابل‌کلیک روی نتیجه.
/// گروه‌بندی فهرست با CollectionView بر اساس GroupTitle انجام می‌شود.
/// </summary>
public partial class QuickSearchWindow : Window
{
    private readonly QuickSearchViewModel _viewModel;

    /// <summary>سازنده با ViewModel</summary>
    public QuickSearchWindow(QuickSearchViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

        // گروه‌بندی نتایج بر اساس سرگروه فارسی حوزه (بیماران/نوبت‌ها/نسخه‌ها)
        var view = System.Windows.Data.CollectionViewSource.GetDefaultView(viewModel.Results);
        view.GroupDescriptions.Add(new System.Windows.Data.PropertyGroupDescription(nameof(QuickSearchRow.GroupTitle)));

        // فوکوس خودکار — جستجو بدون کلیک اضافه
        Loaded += (_, _) => SearchBox.Focus();
    }

    /// <summary>کلیدهای داخل جعبهٔ جستجو: ↓ می‌رود به فهرست · Esc می‌بندد</summary>
    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && _viewModel.Results.Count > 0)
        {
            ResultsList.Focus();
            ResultsList.SelectedIndex = 0;
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            _viewModel.OpenResultCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    /// <summary>کلیدهای فهرست: Enter اجرا · Esc بستن</summary>
    private void ResultsList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.OpenResultCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    /// <summary>دابل‌کلیک روی نتیجه = اجرای همان نتیجه</summary>
    private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        _viewModel.OpenResultCommand.Execute(null);
    }
}

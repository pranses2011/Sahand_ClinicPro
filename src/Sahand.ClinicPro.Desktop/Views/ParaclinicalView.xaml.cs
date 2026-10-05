using System.Windows;
using System.Windows.Controls;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پاراکلینیک — گام U12: چک‌باکس تکنیک‌ها
/// </summary>
public partial class ParaclinicalView : UserControl
{
    /// <summary>سازنده</summary>
    public ParaclinicalView() => InitializeComponent();

    /// <summary>انتخاب تکنیک</summary>
    private void OnTechniqueChecked(object sender, RoutedEventArgs e)
        => AddTechnique(sender);

    /// <summary>لغو انتخاب تکنیک</summary>
    private void OnTechniqueUnchecked(object sender, RoutedEventArgs e)
        => AddTechnique(sender, remove: true);

    private void AddTechnique(object sender, bool remove = false)
    {
        if (DataContext is not ParaclinicalViewModel vm || sender is not CheckBox cb) return;
        var technique = cb.Content?.ToString();
        if (string.IsNullOrEmpty(technique)) return;
        if (remove) vm.SelectedTechniques.Remove(technique);
        else if (!vm.SelectedTechniques.Contains(technique)) vm.SelectedTechniques.Add(technique);
    }
}

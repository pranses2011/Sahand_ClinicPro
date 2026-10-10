using System.Windows;
using System.Windows.Controls;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت صفحهٔ مستقل «فیزیوتراپی» — چرخهٔ چهل‌وسوم: تفکیک از پاراکلینیکِ تب‌دار
/// به صفحهٔ اختصاصی (گزارش مالک: «برای هر کدوم یک صفحه مستقل طراحی بکن»).
/// </summary>
public partial class PhysiotherapyView : UserControl
{
    /// <summary>سازنده — مقداردهی اولیهٔ اجزا</summary>
    public PhysiotherapyView()
    {
        InitializeComponent();
    }

    // ═══ هندلرهای چک‌باکس تکنیک‌ها — از کد پشت پاراکلینیک سابق منتقل شد
    // (چرخهٔ ۴۳: تفکیک صفحات؛ اتصال رویدادهای Checked/Unchecked در XAML به
    // همین متدها می‌رسد و فهرست SelectedTechniques ویومدل را همگام می‌کند)
    /// <summary>انتخاب تکنیک درمانی</summary>
    private void OnTechniqueChecked(object sender, RoutedEventArgs e)
        => AddTechnique(sender);

    /// <summary>لغو انتخاب تکنیک</summary>
    private void OnTechniqueUnchecked(object sender, RoutedEventArgs e)
        => AddTechnique(sender, remove: true);

    /// <summary>همگام‌سازی فهرست تکنیک‌های منتخب ویومدل با چک‌باکس</summary>
    private void AddTechnique(object sender, bool remove = false)
    {
        if (DataContext is not PhysiotherapyViewModel vm || sender is not CheckBox cb) return;
        var technique = cb.Content?.ToString();
        if (string.IsNullOrEmpty(technique)) return;
        if (remove) vm.SelectedTechniques.Remove(technique);
        else if (!vm.SelectedTechniques.Contains(technique)) vm.SelectedTechniques.Add(technique);
    }
}

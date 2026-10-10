using System.Windows.Controls;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت صفحهٔ لوازم مصرفی درمانگاه — چرخهٔ چهل‌ویکم (گزارش مالک)
/// </summary>
/// <remarks>
/// «احتمال داره یک درمانگاه ماژول داروخانه رو نداشته باشه و اونموقع نمیتونه
/// لوازم مصرفی رو تعریف و استفاده بکنه» — همهٔ منطق در ConsumablesViewModel
/// است (MVVM خالص)؛ این کلاس فقط به‌خاطر قرارداد UserControls پروژه حاضر است
/// و هیچ رفتاری نمی‌افزاید.
/// </remarks>
public partial class ConsumablesView : UserControl
{
    /// <summary>سازندهٔ پیش‌فرض — برای طراح XAML</summary>
    public ConsumablesView() => InitializeComponent();
}

using System.Windows.Controls;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// میزکار پزشک — چرخهٔ بیست‌وهفتم (گزارش هجدهم مالک):
/// «صفحهٔ ویزیت پزشک وجود ندارد». نمای اختصاصی پزشکِ واردشده — همهٔ منطق در
/// DoctorVisitViewModel است؛ این کلاس فقط سازندهٔ استاندارد WPF است.
/// </summary>
public partial class DoctorVisitView : UserControl
{
    /// <summary>سازنده — مقداردهی اولیهٔ اجزای XAML</summary>
    public DoctorVisitView()
    {
        InitializeComponent();
    }
}

using System.Windows;
using System.Windows.Input;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت ثبت نتیجه آزمایش — چرخهٔ ۶۹: دابل‌کلیک روی سطر = فرم اختصاصی
/// همان آزمایش (گزارش مالک)؛ فرم مادر پس از ذخیرهٔ موفق تازه می‌شود.
/// </summary>
public partial class LabResultWindow : Window
{
    private readonly LabResultViewModel _viewModel;

    /// <summary>سازنده با ViewModel</summary>
    public LabResultWindow(LabResultViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        viewModel.CloseRequested += (_, _) => Close();

        // چرخهٔ ۶۹: درخواست باز کردن فرم اختصاصی از دکمه
        viewModel.ItemEntryRequested += (_, item) => OpenEntryWindow(item);
    }

    /// <summary>
    /// دابل‌کلیک روی سطر جدول — مسیر اصلی پیشنهادی مالک (چرخهٔ ۶۹):
    /// «روی آزمایش دابل کلیک کنیم تا در یک فرم دیگه که مختص اون آزمایش
    /// هست، اطلاعات آزمایش رو نشون بده و نتیجه رو اونجا ثبت بکنیم».
    /// </summary>
    private void ItemsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ItemsGrid.SelectedItem is LabItemRow row)
            OpenEntryWindow(row);
    }

    /// <summary>
    /// باز کردن فرم اختصاصی ثبت نتیجه برای یک قلم — با زمینهٔ بیمار
    /// (نام/سن/شماره درخواست)؛ پس از هر ذخیرهٔ موفق، لیست مادر تازه می‌شود
    /// تا رنگ ردیف و بج تفسیر از موتور واحد سرور به‌روز شود.
    /// </summary>
    private void OpenEntryWindow(LabItemRow item)
    {
        var entryVm = new LabTestResultEntryViewModel(
            _viewModel.Api, item, _viewModel.OrderId,
            _viewModel.PatientContext, _viewModel.PatientAgeText,
            _viewModel.OrderNumberText,
            onSaved: _viewModel.LoadAsync,
            patientId: _viewModel.PatientId, orderStatus: _viewModel.OrderStatus);

        var window = new LabTestResultEntryWindow(entryVm)
        {
            Owner = this
        };
        window.ShowDialog();
    }
}

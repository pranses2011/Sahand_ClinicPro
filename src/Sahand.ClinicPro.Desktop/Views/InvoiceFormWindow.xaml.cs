using System.Windows;
using System.Windows.Controls;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// پنجره صدور فاکتور — گام U8
/// </summary>
public partial class InvoiceFormWindow : Window
{
    private readonly HashSet<ValidationError> _numericErrors = new();
    /// <summary>سازنده</summary>
    public InvoiceFormWindow(InvoiceFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Validation.AddErrorHandler(this, (_, e) =>
        {
            if (e.Action == ValidationErrorEventAction.Added) _numericErrors.Add(e.Error);
            else _numericErrors.Remove(e.Error);
            viewModel.HasInvalidInvoiceInput = _numericErrors.Count != 0;
        });
        var closingAfterSuccess = false;
        viewModel.CloseRequested += (_, _) => { closingAfterSuccess = true; Close(); };
        Closing += (_, e) => { if (viewModel.IsBusy && !closingAfterSuccess) e.Cancel = true; };
    }
}

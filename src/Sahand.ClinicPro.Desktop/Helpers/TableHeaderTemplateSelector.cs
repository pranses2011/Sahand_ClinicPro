using System.Windows;
using System.Windows.Controls;

namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>
/// Wraps textual DataGrid headers while leaving custom visual headers untouched.
/// </summary>
public sealed class TableHeaderTemplateSelector : DataTemplateSelector
{
    /// <summary>Wrapped template used for string headers.</summary>
    public DataTemplate? TextTemplate { get; set; }

    /// <inheritdoc />
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is string ? TextTemplate : null;
}

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>ویرایش ردیفی بدون تغییر قالب اصلی تا تأیید کاربر.</summary>
public partial class VisualTemplateEditorWindow : Window
{
    public ObservableCollection<VisualTemplateBlock> Rows { get; }
    public IReadOnlyList<ComplementaryViewModel.TemplateVariable> Variables { get; }
    public string? ResultHtml { get; private set; }

    public VisualTemplateEditorWindow(IEnumerable<VisualTemplateBlock> rows, IEnumerable<ComplementaryViewModel.TemplateVariable> variables)
    {
        Rows = new ObservableCollection<VisualTemplateBlock>(rows.Select(r => new VisualTemplateBlock { Kind = r.Kind, Text = r.Text, Key = r.Key }));
        Variables = variables.GroupBy(v => v.Key).Select(g => g.First()).ToArray();
        InitializeComponent();
        DataContext = this;
    }
    private void OnAddText(object sender, RoutedEventArgs e)
    { var row = new VisualTemplateBlock { Kind = "text", Text = "متن جدید" }; Rows.Add(row); RowsList.SelectedItem = row; }
    private void OnAddVariable(object sender, RoutedEventArgs e)
    {
        if (Variables.Count == 0) { AppDialog.Error("طراحی قالب", "برای این سند متغیری در دسترس نیست."); return; }
        var row = new VisualTemplateBlock { Kind = "variable", Text = Variables[0].Label + ":", Key = Variables[0].Key };
        Rows.Add(row); RowsList.SelectedItem = row;
    }
    private void OnRemove(object sender, RoutedEventArgs e)
    { if (RowsList.SelectedItem is VisualTemplateBlock row) Rows.Remove(row); }
    private void Move(int offset)
    {
        if (RowsList.SelectedItem is not VisualTemplateBlock row) return;
        var index = Rows.IndexOf(row); var target = index + offset;
        if (target < 0 || target >= Rows.Count) return;
        Rows.Move(index, target); RowsList.SelectedItem = row;
    }
    private void OnRowGripMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || sender is not FrameworkElement { DataContext: VisualTemplateBlock row } grip) return;
        DragDrop.DoDragDrop(grip, row, DragDropEffects.Move);
    }

    private void OnRowDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(VisualTemplateBlock)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnRowDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(VisualTemplateBlock)) is not VisualTemplateBlock row || !Rows.Contains(row)) return;
        DependencyObject? current = e.OriginalSource as DependencyObject;
        while (current is not null && current is not ListBoxItem)
            current = current is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
        var destination = current is ListBoxItem item && item.DataContext is VisualTemplateBlock target ? Rows.IndexOf(target) : Rows.Count - 1;
        var source = Rows.IndexOf(row);
        if (destination >= 0 && source != destination) Rows.Move(source, destination);
        RowsList.SelectedItem = row;
        e.Handled = true;
    }

    private void OnMoveUp(object sender, RoutedEventArgs e) => Move(-1);
    private void OnMoveDown(object sender, RoutedEventArgs e) => Move(1);
    private void OnApply(object sender, RoutedEventArgs e)
    {
        try { ResultHtml = VisualTemplateDesign.Build(Rows.ToList(), Variables.Select(v => v.Key).ToArray()); DialogResult = true; }
        catch (ArgumentException ex) { AppDialog.Error("طراحی قالب", ex.Message); }
    }
    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}

using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پنجره گزارش‌ساز رایگانه — چرخه سیزدهم (درخواست ۴-۳):
/// ساخت ستون‌های داینامیک جدول نتایج بر اساس ستون‌های گزارش اجراشده
/// </summary>
public partial class AdhocReportWindow : Window
{
    /// <summary>ViewModel نگاشت‌شده</summary>
    private AdhocReportViewModel? _vm;

    /// <summary>سازنده با تزریق ViewModel</summary>
    public AdhocReportWindow(AdhocReportViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        DataContextChanged += (_, _) => BindViewModel(vm);
        Loaded += (_, _) => BindViewModel(vm);
    }

    /// <summary>اتصال VM و ساخت ستون‌ها هنگام تغییر داده‌ها</summary>
    private void BindViewModel(AdhocReportViewModel vm)
    {
        if (_vm == vm) return;
        _vm = vm;

        if (vm.Columns is INotifyCollectionChanged cols)
            cols.CollectionChanged += (_, _) => RebuildColumns(vm);

        if (vm.Rows is INotifyCollectionChanged rows)
            rows.CollectionChanged += (_, _) => RebuildColumns(vm);
    }

    /// <summary>ساخت مجدد ستون‌های جدول بر اساس فهرست ستون‌های VM</summary>
    private void RebuildColumns(AdhocReportViewModel vm)
    {
        if (vm.Columns.Count == 0) return;

        ResultsGrid.Columns.Clear();
        for (var i = 0; i < vm.Columns.Count; i++)
        {
            var index = i;
            var column = new DataGridTextColumn
            {
                Header = vm.Columns[i],
                Width = DataGridLength.Auto,
                MaxWidth = 420,
                ElementStyle = (Style)FindResource("App.TableCellTextBlock"),
                // سلول‌ها با نمای‌ساز سفارشی از ردیف استخراج می‌شوند
                Binding = new Binding
                {
                    Mode = BindingMode.OneWay,
                    Converter = new Converters.CellValueConverter(),
                    ConverterParameter = index.ToString()
                }
            };

            // ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) — رفع کرش گزارش‌ساز ═══
            // «'' is not a valid value for property 'FontFamily'»: ویژگی
            // FontFamily در WPF یک فراخوان اعتبارسنجی دارد که مقدار تهی را
            // رد می‌کند (null.ToString() در پیام خطا به‌صورت '' دیده می‌شود)؛
            // پیش‌تر ستونِ اول «FontFamily = null» می‌گرفت و هر بار اجرای
            // گزارش در همان ستون اول با ArgumentException می‌شکست. راه‌حل:
            // مقداردهی فقط برای ستون‌های داده (Consolas) و بی‌اعتنایی به
            // ستون اول تا فونت را از پنجره ارث ببرد — هرگز null ست نمی‌شود.
            // ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) ═══ فونت هاردکد Consolas
            // برای ستون‌های داده حذف شد — ارقام فارسی آن fallback می‌گرفت و با
            // فونتی متفاوت رندر می‌شد؛ اکنون فونت وزیرمتن جاسازی‌شدهٔ برنامه
            // اعمال می‌شود (قانون «فونت روی ارقام فارسی اعمال شود»)
            if (i > 0)
                column.FontFamily = Sahand.ClinicPro.Desktop.Helpers.FontAssets.Vazirmatn;

            ResultsGrid.Columns.Add(column);
        }
    }
}

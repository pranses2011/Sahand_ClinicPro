using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Data;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت گزارش‌ساز — گام U16:
/// ساخت ستون‌های داینامیک جدول نتایج بر اساس کاتالوگ هر گزارش
/// </summary>
public partial class ReportBrowserView : UserControl
{
    /// <summary>ViewModel نگاشت‌شده</summary>
    private ReportBrowserViewModel? _vm;

    /// <summary>سازنده</summary>
    public ReportBrowserView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BindViewModel();
        Loaded += (_, _) => BindViewModel();
    }

    /// <summary>اتصال VM و ساخت ستون‌ها هنگام تغییر داده‌ها</summary>
    private void BindViewModel()
    {
        if (_vm == DataContext || DataContext is not ReportBrowserViewModel vm) return;
        _vm = vm;

        if (vm.Columns is INotifyCollectionChanged cols)
            cols.CollectionChanged += (_, _) => RebuildColumns(vm);

        if (vm.Rows is INotifyCollectionChanged rows)
            rows.CollectionChanged += (_, _) => RebuildColumns(vm);

        RebuildColumns(vm);
    }

    /// <summary>ساخت مجدد ستون‌های جدول بر اساس فهرست ستون‌های VM</summary>
    private void RebuildColumns(ReportBrowserViewModel vm)
    {
        if (vm.Columns.Count == 0) return;

        ResultsGrid.Columns.Clear();
        for (var i = 0; i < vm.Columns.Count; i++)
        {
            var index = i;

            // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — رفع کرش گزارش‌ها ═══
            // «ArgumentException: '' is not a valid value for property 'FontFamily'»
            // در همین متد رخ می‌داد. دو ریشه داشت:
            //   ۱) مقداردهی شرطی «i == 0 ? null : …» روی DependencyProperty
            //      فونت — گذاشتن null صریح در مقداردهندهٔ شیء، زنجیرهٔ اعتبارسنجی
            //      WPF را با مقدار رشته‌ای خالی («») روبرو می‌کرد و کرش می‌کرد؛
            //      فونت حالت فقط در صورت نیاز و پس از ساخت ستون تنظیم می‌شود.
            //   ۲) فونت هاردکد «Consolas» تضاد با خط فارسی/ارقام فارسی و تم
            //      برنامه داشت؛ ستون‌ها اکنون فونت را از منبع پویای تم
            //      (App.FontFamily — همان وزیرمتن یا انتخاب کاربر) به ارث
            //      می‌برند تا تغییر فونت تنظیمات روی گزارش‌ها هم بنشیند.
            var column = new DataGridTextColumn
            {
                Header = vm.Columns[i],
                Width = i == 0 ? DataGridLength.Auto : DataGridLength.SizeToCells,
                // سلول‌ها با نمای‌ساز سفارشی از ردیف استخراج می‌شوند
                Binding = new Binding
                {
                    Mode = BindingMode.OneWay,
                    Converter = new Converters.CellValueConverter(),
                    ConverterParameter = index.ToString()
                }
            };

            ResultsGrid.Columns.Add(column);
        }
    }
}

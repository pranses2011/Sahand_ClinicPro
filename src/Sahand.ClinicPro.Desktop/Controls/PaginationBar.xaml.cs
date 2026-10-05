using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;

namespace Sahand.ClinicPro.Desktop.Controls;

/// <summary>
/// نوار صفحه‌بندی سروری — دکمه قبلی/بعدی + شماره صفحه + تعداد در صفحه
/// مصرف‌کننده: همه لیست‌های صفحه‌بندی‌شده (بیماران، نوبت‌ها، نسخه‌ها و...)
/// </summary>
public partial class PaginationBar : UserControl
{
    /// <summary>سازنده — ساخت فرمان‌های پیمایش</summary>
    public PaginationBar()
    {
        InitializeComponent();
        PrevPageCommand = new RelayCommand(() => Move(-1),
            () => PageNumber > 1);
        NextPageCommand = new RelayCommand(() => Move(+1),
            () => PageNumber < TotalPages);
        UpdateRange();
    }

    /* ─────────────  خصوصیت‌های وابستگی  ───────────── */

    /// <summary>شماره صفحه فعلی (از ۱)</summary>
    public static readonly DependencyProperty PageNumberProperty =
        DependencyProperty.Register(nameof(PageNumber), typeof(int),
            typeof(PaginationBar),
            new FrameworkPropertyMetadata(1,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (d, _) => ((PaginationBar)d).OnPageChanged()));

    /// <summary>جمع صفحات</summary>
    public static readonly DependencyProperty TotalPagesProperty =
        DependencyProperty.Register(nameof(TotalPages), typeof(int),
            typeof(PaginationBar), new PropertyMetadata(1, OnMetaChanged));

    /// <summary>جمع رکوردها</summary>
    public static readonly DependencyProperty TotalCountProperty =
        DependencyProperty.Register(nameof(TotalCount), typeof(long),
            typeof(PaginationBar), new PropertyMetadata(0L, OnMetaChanged));

    /// <summary>تعداد رکورد در صفحه</summary>
    public static readonly DependencyProperty PageSizeProperty =
        DependencyProperty.Register(nameof(PageSize), typeof(int),
            typeof(PaginationBar),
            new FrameworkPropertyMetadata(20,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnMetaChanged));

    /// <summary>فرمان اطلاع از تغییر صفحه — پارامتر: شماره صفحه جدید</summary>
    public static readonly DependencyProperty PageChangedCommandProperty =
        DependencyProperty.Register(nameof(PageChangedCommand), typeof(System.Windows.Input.ICommand),
            typeof(PaginationBar), new PropertyMetadata(null));

    /// <summary>متن بازه — «نمایش ۱ تا ۲۰ از ۱٬۲۵۴»</summary>
    public static readonly DependencyProperty RangeTextProperty =
        DependencyProperty.Register(nameof(RangeText), typeof(string),
            typeof(PaginationBar), new PropertyMetadata(string.Empty));

    /// <summary>فرمان قبلی</summary>
    public static readonly DependencyProperty PrevPageCommandProperty =
        DependencyProperty.Register(nameof(PrevPageCommand), typeof(System.Windows.Input.ICommand),
            typeof(PaginationBar), new PropertyMetadata(null));

    /// <summary>فرمان بعدی</summary>
    public static readonly DependencyProperty NextPageCommandProperty =
        DependencyProperty.Register(nameof(NextPageCommand), typeof(System.Windows.Input.ICommand),
            typeof(PaginationBar), new PropertyMetadata(null));

    private static void OnMetaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PaginationBar)d).UpdateRange();

    /* ─────────────  پوشش  ───────────── */

    /// <summary>شماره صفحه</summary>
    public int PageNumber
    {
        get => (int)GetValue(PageNumberProperty);
        set => SetValue(PageNumberProperty, value);
    }

    /// <summary>جمع صفحات</summary>
    public int TotalPages
    {
        get => (int)GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }

    /// <summary>جمع رکوردها</summary>
    public long TotalCount
    {
        get => (long)GetValue(TotalCountProperty);
        set => SetValue(TotalCountProperty, value);
    }

    /// <summary>تعداد در صفحه</summary>
    public int PageSize
    {
        get => (int)GetValue(PageSizeProperty);
        set => SetValue(PageSizeProperty, value);
    }

    /// <summary>فرمان تغییر صفحه</summary>
    public System.Windows.Input.ICommand PageChangedCommand
    {
        get => (System.Windows.Input.ICommand)GetValue(PageChangedCommandProperty);
        set => SetValue(PageChangedCommandProperty, value);
    }

    /// <summary>متن بازه</summary>
    public string RangeText
    {
        get => (string)GetValue(RangeTextProperty);
        private set => SetValue(RangeTextProperty, value);
    }

    /// <summary>فرمان قبلی</summary>
    public System.Windows.Input.ICommand PrevPageCommand
    {
        get => (System.Windows.Input.ICommand)GetValue(PrevPageCommandProperty);
        private set => SetValue(PrevPageCommandProperty, value);
    }

    /// <summary>فرمان بعدی</summary>
    public System.Windows.Input.ICommand NextPageCommand
    {
        get => (System.Windows.Input.ICommand)GetValue(NextPageCommandProperty);
        private set => SetValue(NextPageCommandProperty, value);
    }

    /* ─────────────  منطق  ───────────── */

    /// <summary>پیمایش نسبی صفحه</summary>
    private void Move(int delta)
    {
        var target = Math.Clamp(PageNumber + delta, 1, Math.Max(1, TotalPages));
        if (target == PageNumber)
            return;
        PageNumber = target;
    }

    /// <summary>بازخورد تغییر صفحه — به‌روزرسانی بازه و اطلاع به فرمان بیرونی</summary>
    private void OnPageChanged()
    {
        UpdateRange();
        if (PageChangedCommand is { } cmd && cmd.CanExecute(PageNumber))
            cmd.Execute(PageNumber);
    }

    /// <summary>محاسبه متن بازه نمایش</summary>
    private void UpdateRange()
    {
        if (TotalCount <= 0)
        {
            RangeText = "موردی برای نمایش نیست";
            return;
        }

        var from = (PageNumber - 1) * (long)PageSize + 1;
        var to = Math.Min(PageNumber * (long)PageSize, TotalCount);
        RangeText = $"نمایش {from:N0} تا {to:N0} از {TotalCount:N0}";
    }

    /// <summary>تغییر تعداد در صفحه — بازگشت به صفحه ۱</summary>
    private void OnPageSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
            return;
        PageNumber = 1;
        if (PageChangedCommand is { } cmd && cmd.CanExecute(1))
            cmd.Execute(1);
    }
}

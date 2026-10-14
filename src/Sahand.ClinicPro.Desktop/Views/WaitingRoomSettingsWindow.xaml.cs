using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Desktop.Services;
using MaterialDesignThemes.Wpf;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// ردیف ویرایش یک بخش در فهرست تنظیمات — نگاشت یک‌به‌یک به WaitingRoomSection
/// </summary>
public class SectionEditRow
{
    public required Services.WaitingRoomSection Model { get; init; }
    public string Title => DisplayTitle(Model);
    public string Detail => Model.Type switch
    {
        WaitingRoomSectionType.DoctorQueue => Model.DoctorId is null ? "همهٔ پزشکان" : Model.DoctorName ?? "",
        WaitingRoomSectionType.ServiceQueue => Model.ServiceName ?? "",
        _ => "نوبت‌های امروز درمانگاه",
    };
    public string Icon => Model.Type switch
    {
        WaitingRoomSectionType.DoctorQueue => "Doctor",
        WaitingRoomSectionType.ServiceQueue => "Syringe",
        _ => "CalendarClock",
    };

    /// <summary>عنوان نمایشی — همان منطق نمایشگر</summary>
    private static string DisplayTitle(Services.WaitingRoomSection s) =>
        string.IsNullOrWhiteSpace(s.Title)
            ? s.Type switch
            {
                WaitingRoomSectionType.DoctorQueue => s.DoctorId is null ? "صف انتظار ویزیت" : $"صف {s.DoctorName}",
                WaitingRoomSectionType.ServiceQueue => $"صف {s.ServiceName}",
                _ => "نوبت‌های امروز",
            }
            : s.Title;
}

/// <summary>
/// گزینهٔ انتخابگر هدف بخش — پزشک یا خدمت
/// </summary>
public record TargetOption(long Id, string Name);

/// <summary>
/// کد پشت تنظیمات نمایشگر سالن انتظار — چرخهٔ ۷۴.
/// پیاده‌سازی Drag &amp; Drop: درگ از یک ردیف فهرست بخش‌ها و رها کردن روی ردیف
/// دیگر → جابه‌جایی؛ هم‌زمان دکمه‌های پیکان بالا/پایین و ویرایشگر جزئیات
/// (عنوان/پزشک/خدمت) و پیش‌نمایش زندهٔ چیدمان (نام درمانگاه + ساعت‌ها + بخش‌ها
/// + تبلیغ ثابت) بروز می‌شوند.
/// </summary>
public partial class WaitingRoomSettingsWindow : Window
{
    /// <summary>پیکربندی در حال ویرایش — از انبار محلی خوانده شده</summary>
    private readonly WaitingRoomConfig _config;

    /// <summary>ردیف‌های فهرست بخش‌ها — قابل جابه‌جایی با کشیدن و رها کردن</summary>
    private readonly ObservableCollection<SectionEditRow> _rows = new();

    /// <summary>پزشکان فعال — برای بخش «صف پزشک»</summary>
    private readonly List<TargetOption> _doctors = new();

    /// <summary>خدمات فعال — برای بخش «صف خدمت»</summary>
    private readonly List<TargetOption> _services = new();

    /// <summary>گارد بازخورد ویرایشگر → جلوگیری از حلقهٔ رویداد هنگام بارگذاری برنامه‌ای</summary>
    private bool _loadingEditor;

    // ═══ چرخهٔ ۷۶ — گارد آماده‌سازی نما ═══
    // رویدادهای انتخاب (SelectionChanged) می‌توانند در میانهٔ InitializeComponent
    // شعله‌ور شوند؛ پیش از پایان سازنده هیچ کنترلی تضمین‌شده آماده نیست و
    // RefreshPreview باید بی‌صدا صرف‌نظر کند (رفع NullReferenceException
    // گزارش‌شده روی دکمهٔ تنظیمات نمایشگر سالن).
    /// <summary>نما تا پایان سازنده آماده نیست — رویدادهای زودهنگام نادیده گرفته می‌شوند</summary>
    private bool _viewReady;

    /// <summary>ردیف در حال درگ — برای جابه‌جایی با درگ</summary>
    private SectionEditRow? _dragRow;

    /// <summary>سازنده — بارگذاری پیکربندی + کاتالوگ‌ها + تازه‌سازی پیش‌نمایش</summary>
    public WaitingRoomSettingsWindow()
    {
        InitializeComponent();
        _config = WaitingRoomConfig.Load();

        // سوییچ‌ها
        AnalogClockCheck.IsChecked = _config.ShowAnalogClock;
        DigitalClockCheck.IsChecked = _config.ShowDigitalClock;
        LongDateCheck.IsChecked = _config.ShowLongDate;
        ClinicNameBox.Text = _config.ClinicName;
        // ═══ چرخهٔ ۷۵ — اعلان، تعداد ستون و اقلام اختیاری تبلیغ ═══
        AnnouncementBox.Text = _config.Announcement;
        ColumnCountCombo.SelectedIndex = Math.Clamp(_config.ColumnCount, 1, 4) - 1;
        AdSubtitleCheck.IsChecked = _config.ShowAdSubtitle;
        AdFeaturesCheck.IsChecked = _config.ShowAdFeatures;
        AdVersionCheck.IsChecked = _config.ShowAdVersion;

        AnalogClockCheck.Checked += (_, _) => RefreshPreview();
        AnalogClockCheck.Unchecked += (_, _) => RefreshPreview();
        DigitalClockCheck.Checked += (_, _) => RefreshPreview();
        DigitalClockCheck.Unchecked += (_, _) => RefreshPreview();
        LongDateCheck.Checked += (_, _) => RefreshPreview();
        LongDateCheck.Unchecked += (_, _) => RefreshPreview();
        ClinicNameBox.TextChanged += (_, _) => RefreshPreview();
        AnnouncementBox.TextChanged += (_, _) => RefreshPreview();
        AdSubtitleCheck.Checked += (_, _) => RefreshPreview();
        AdSubtitleCheck.Unchecked += (_, _) => RefreshPreview();
        AdFeaturesCheck.Checked += (_, _) => RefreshPreview();
        AdFeaturesCheck.Unchecked += (_, _) => RefreshPreview();
        AdVersionCheck.Checked += (_, _) => RefreshPreview();
        AdVersionCheck.Unchecked += (_, _) => RefreshPreview();

        // فهرست بخش‌ها
        foreach (var section in _config.Sections)
            _rows.Add(new SectionEditRow { Model = section });
        SectionsList.ItemsSource = _rows;
        _rows.CollectionChanged += (_, _) => RefreshPreview();
        if (_rows.Count > 0)
            SectionsList.SelectedIndex = 0;

        // مانیتورها
        LoadScreens();

        // کاتالوگ پزشکان/خدمات — برای ویرایشگر بخش‌ها
        _ = LoadCatalogsAsync();

        _viewReady = true; // از این پس رویدادها مجازند پیش‌نمایش را تازه کنند
        RefreshPreview();
    }

    /// <summary>فهرست مانیتورهای متصل — انتخاب ذخیره‌شده یا مانیتور اصلی</summary>
    private void LoadScreens()
    {
        try
        {
            // شمارش P/Invoke سبک — Helpers/Monitors (چرخهٔ ۷۴)
            var screens = Helpers.Monitors.All();
            var options = screens.Select(s => new ScreenOption(s.DeviceName, s.Title)).ToList();
            if (options.Count == 0)
                options.Add(new ScreenOption("", "مانیتور اصلی"));
            ScreenCombo.ItemsSource = options;
            var primary = screens.FirstOrDefault(s => s.Primary);
            var saved = options.FirstOrDefault(o => o.DeviceName == _config.ScreenDeviceName)
                        ?? options.FirstOrDefault(o => o.DeviceName == (primary?.DeviceName ?? ""));
            ScreenCombo.SelectedItem = saved ?? options[0];
        }
        catch
        {
            ScreenCombo.ItemsSource = new[] { new ScreenOption("", "مانیتور اصلی") };
            ScreenCombo.SelectedIndex = 0;
        }
    }

    /// <summary>بارگذاری پزشکان و خدمات از سرور — برای انتخابگرهای ویرایشگر بخش</summary>
    private async Task LoadCatalogsAsync()
    {
        try
        {
            var api = App.Services.GetRequiredService<IApiClient>();
            var doctors = await api.GetAsync<JsonElement>(doctorsUrl);
            if (Unwrap(doctors) is { ValueKind: JsonValueKind.Array } dList)
                foreach (var d in dList.EnumerateArray())
                {
                    var id = d.TryGetProperty("id", out var di) && di.ValueKind == JsonValueKind.Number ? di.GetInt64() : 0;
                    var fn = d.TryGetProperty("fullName", out var fnj) && fnj.ValueKind == JsonValueKind.String ? fnj.GetString() ?? "" : "";
                    if (id > 0) _doctors.Add(new TargetOption(id, fn));
                }
            var services = await api.GetAsync<JsonElement>(servicesUrl);
            if (Unwrap(services) is { ValueKind: JsonValueKind.Array } sList)
                foreach (var s in sList.EnumerateArray())
                {
                    var id = s.TryGetProperty("serviceId", out var si) && si.ValueKind == JsonValueKind.Number ? si.GetInt64() : 0;
                    var nm = s.TryGetProperty("serviceName", out var snj) && snj.ValueKind == JsonValueKind.String ? snj.GetString() ?? "" : "";
                    if (id > 0) _services.Add(new TargetOption(id, nm));
                }
        }
        catch
        {
            // بدون کاتالوگ هم بخش‌های عمومی (همهٔ پزشکان/نوبت‌ها) کار می‌کنند
        }
        _ = Dispatcher.BeginInvoke(RebindEditor);
    }

    private const string doctorsUrl = "doctors?page=1&pageSize=200";
    private const string servicesUrl = "lookups/services?search=&insuranceCompanyId=";

    /// <summary>بازکردن پاکت ApiResponse — data یا ریشه</summary>
    private static JsonElement? Unwrap(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d))
            return d.ValueKind == JsonValueKind.Null ? null : d;
        return root.ValueKind == JsonValueKind.Array ? root : null;
    }

    /// <summary>تازه‌سازی پیش‌نمایش زنده — نام/اعلان/ساعت‌ها/بخش‌های ستونی/تبلیغ</summary>
    private void RefreshPreview()
    {
        // چرخهٔ ۷۶ — رویداد زودهنگام (قبل از پایان سازنده) → نادیده
        if (!_viewReady)
            return;
        var clinicName = string.IsNullOrWhiteSpace(ClinicNameBox.Text)
            ? _config.EffectiveClinicName()
            : ClinicNameBox.Text.Trim();
        PreviewClinicName.Text = clinicName;
        // چرخهٔ ۷۵ — اعلان زیر نام درمانگاه در پیش‌نمایش هم دیده شود
        PreviewAnnouncement.Text = string.IsNullOrWhiteSpace(AnnouncementBox.Text)
            ? "لطفاً در سالن انتظار حضور داشته باشید…"
            : AnnouncementBox.Text.Trim();
        PreviewAnalogClock.Visibility = ToVis(AnalogClockCheck.IsChecked == true);
        PreviewDigital.Visibility = ToVis(DigitalClockCheck.IsChecked == true);
        PreviewDate.Visibility = ToVis(LongDateCheck.IsChecked == true);

        // ═══ چرخهٔ ۷۵ — پیش‌نمایش چیدمان ستونی صف‌ها ═══
        // همان الگوریتم نمایشگر: بخش‌ها ستون‌پرشونده، هر ستون حداکثر دو بخش
        PreviewColumns.ColumnDefinitions.Clear();
        PreviewColumns.Children.Clear();
        var columnCount = Math.Clamp(ColumnCountCombo.SelectedIndex + 1, 1, 4);
        for (var i = 0; i < columnCount; i++)
            PreviewColumns.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        const int maxPerColumn = 2;
        var titles = _rows.Select(r => r.Title).ToList();
        var idx = 0;
        for (var col = 0; col < columnCount && idx < titles.Count; col++)
        {
            var columnStack = new StackPanel { Margin = new Thickness(col == 0 ? 0 : 6, 0, 0, 0) };
            var placed = 0;
            while (placed < maxPerColumn && idx < titles.Count)
            {
                columnStack.Children.Add(BuildPreviewCard(titles[idx]));
                placed++;
                idx++;
            }
            Grid.SetColumn(columnStack, col);
            PreviewColumns.Children.Add(columnStack);
        }

        // ═══ چرخهٔ ۷۵ — اقلام اختیاری تبلیغ در پیش‌نمایش ═══
        PreviewAdSubtitle.Visibility = ToVis(AdSubtitleCheck.IsChecked == true);
        PreviewAdFeatures.Visibility = ToVis(AdFeaturesCheck.IsChecked == true);
        PreviewAdVersion.Visibility = ToVis(AdVersionCheck.IsChecked == true);
    }

    /// <summary>ساخت کارت پیش‌نمایش یک بخش — هم‌زبانِ بصری نمایشگر واقعی</summary>
    private static Border BuildPreviewCard(string title)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x7F, 0xC4, 0xFF)),
        });
        stack.Children.Add(new TextBlock
        {
            Text = "نمونهٔ ردیف بیمار — شمارهٔ صف",
            FontSize = 7.5,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x9D, 0xB8, 0xCF)),
            Margin = new Thickness(0, 3, 0, 0),
        });
        return new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x10, 0x23, 0x3A)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x1E, 0x3A, 0x5C)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 6),
            Child = stack,
        };
    }

    /// <summary>تغییر هر تنظیم سراسری (مثل تعداد ستون) — فقط تازه‌سازی پیش‌نمایش</summary>
    private void OnAnySettingChanged(object sender, SelectionChangedEventArgs e)
        => RefreshPreview();

    /// <summary>تبدیل bool به Visibility</summary>
    private static Visibility ToVis(bool v)
        => v ? Visibility.Visible : Visibility.Collapsed;

    /* ═══════════ ویرایشگر بخش انتخابی ═══════════ */

    /// <summary>بارگذاری بخش انتخابی در ویرایشگر</summary>
    private void OnSectionsListSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RebindEditor();
    }

    /// <summary>نمایش/پنهان‌سازی ویرایشگر + پر کردن انتخابگر هدف بر اساس نوع</summary>
    private void RebindEditor()
    {
        var row = SectionsList.SelectedItem as SectionEditRow;
        var visible = row is not null;
        EditorHeader.Visibility = ToVis(visible);
        EditorPanel.Visibility = ToVis(visible);
        if (row is null)
            return;

        _loadingEditor = true;
        try
        {
            SectionTitleBox.Text = row.Model.Title;
            switch (row.Model.Type)
            {
                case WaitingRoomSectionType.DoctorQueue:
                    EditorTargetLabel.Text = "پزشک (خالی = همهٔ پزشکان)";
                    EditorTargetCombo.ItemsSource =
                        new List<TargetOption> { new(0, "همهٔ پزشکان") }.Concat(_doctors).ToList();
                    EditorTargetCombo.SelectedItem =
                        _doctors.FirstOrDefault(d => d.Id == row.Model.DoctorId) ??
                        (TargetOption?)EditorTargetCombo.ItemsSource.Cast<TargetOption>().First();
                    break;
                case WaitingRoomSectionType.ServiceQueue:
                    EditorTargetLabel.Text = "خدمت درمانی";
                    EditorTargetCombo.ItemsSource = _services;
                    EditorTargetCombo.SelectedItem = _services.FirstOrDefault(s => s.Id == row.Model.ServiceId);
                    break;
                default:
                    EditorTargetLabel.Text = "—";
                    EditorTargetCombo.ItemsSource = null;
                    break;
            }
        }
        finally
        {
            _loadingEditor = false;
        }
    }

    /// <summary>تغییر عنوان/هدف بخش — روی مدل همان ردیف اعمال و پیش‌نمایش تازه می‌شود</summary>
    private void OnEditorChanged(object sender, RoutedEventArgs e)
    {
        if (_loadingEditor || SectionsList.SelectedItem is not SectionEditRow row)
            return;
        row.Model.Title = SectionTitleBox.Text.Trim();
        if (EditorTargetCombo.SelectedItem is TargetOption target)
        {
            switch (row.Model.Type)
            {
                case WaitingRoomSectionType.DoctorQueue:
                    row.Model.DoctorId = target.Id == 0 ? null : target.Id;
                    row.Model.DoctorName = target.Id == 0 ? null : target.Name;
                    break;
                case WaitingRoomSectionType.ServiceQueue:
                    row.Model.ServiceId = target.Id;
                    row.Model.ServiceName = target.Name;
                    break;
            }
        }
        // تازه‌سازی عنوان ردیف فهرست (پیام تغییر مشخصات — رکورد immutable نیست ولی
        // پرچم‌ها بایند OneWayاند؛ بازسازی سطر جایگزین ساده و مطمئن است)
        var index = _rows.IndexOf(row);
        if (index >= 0)
        {
            var selected = SectionsList.SelectedItem;
            _rows[index] = new SectionEditRow { Model = row.Model };
            SectionsList.SelectedItem = selected;
        }
        RefreshPreview();
    }

    /* ═══════════ افزودن/حذف/جابه‌جایی ═══════════ */

    /// <summary>افزودن بخش جدید — منوی کوچک نوع‌محور (سه گزینهٔ ثابت)</summary>
    private void OnAddSection(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu
        {
            FlowDirection = FlowDirection.RightToLeft,
            FontSize = 12,
        };
        void AddItem(string title, PackIconKind icon, WaitingRoomSectionType type)
        {
            var item = new MenuItem
            {
                Header = title,
                Icon = new PackIcon { Kind = icon, Width = 16, Height = 16 },
            };
            item.Click += (_, _) =>
            {
                var section = new Services.WaitingRoomSection
                {
                    Type = type,
                    Title = title,
                };
                _rows.Add(new SectionEditRow { Model = section });
                SectionsList.SelectedItem = _rows[^1];
            };
            menu.Items.Add(item);
        }
        AddItem("صف انتظار پزشک", PackIconKind.Doctor, WaitingRoomSectionType.DoctorQueue);
        AddItem("صف خدمت درمانی (دندانپزشکی/چشم/…)", PackIconKind.Syringe, WaitingRoomSectionType.ServiceQueue);
        AddItem("نوبت‌های امروز", PackIconKind.CalendarClock, WaitingRoomSectionType.TodayAppointments);
        menu.PlacementTarget = AddSectionButton;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    /// <summary>حذف بخش انتخابی</summary>
    private void OnRemoveSection(object sender, RoutedEventArgs e)
    {
        if (SectionsList.SelectedItem is SectionEditRow row)
            _rows.Remove(row);
    }

    /// <summary>جابه‌جایی به بالا — هم‌ارز درگ</summary>
    private void OnMoveUp(object sender, RoutedEventArgs e)
    {
        if (SectionsList.SelectedItem is not SectionEditRow row)
            return;
        var index = _rows.IndexOf(row);
        if (index <= 0) return;
        _rows.Move(index, index - 1);
        SectionsList.SelectedItem = row;
    }

    /// <summary>جابه‌جایی به پایین — هم‌ارز درگ</summary>
    private void OnMoveDown(object sender, RoutedEventArgs e)
    {
        if (SectionsList.SelectedItem is not SectionEditRow row)
            return;
        var index = _rows.IndexOf(row);
        if (index < 0 || index >= _rows.Count - 1) return;
        _rows.Move(index, index + 1);
        SectionsList.SelectedItem = row;
    }

    /* ═══════════ Drag & Drop — جابه‌جایی ردیف‌ها با ماوس ═══════════ */

    /// <summary>شروع درگ — وقتی ماوس با دکمهٔ چپ روی یک ردیف حرکت می‌کند</summary>
    private void OnSectionsListPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;
        var row = SectionsList.SelectedItem as SectionEditRow;
        if (row is null || _dragRow == row)
            return;
        _dragRow = row;
        try
        {
            DragDrop.DoDragDrop(SectionsList, new DataObject(typeof(SectionEditRow), row), DragDropEffects.Move);
        }
        finally
        {
            _dragRow = null;
        }
    }

    /// <summary>نشانگر رها کردن هنگام عبور از فهرست</summary>
    private void OnSectionsListDragOver(object sender, DragEventArgs e)
        => e.Effects = e.Data.GetDataPresent(typeof(SectionEditRow))
            ? DragDropEffects.Move
            : DragDropEffects.None;

    /// <summary>رها کردن — جابه‌جایی ردیف مبدأ به جای ردیف مقصد</summary>
    private void OnSectionsListDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(SectionEditRow)) is false)
            return;
        if (e.Data.GetData(typeof(SectionEditRow)) is not SectionEditRow source)
            return;

        // یافتن ردیف مقصد از روی موقعیت ماوس
        var target = GetRowUnderMouse(e);
        if (target is null || ReferenceEquals(source, target))
            return;

        var from = _rows.IndexOf(source);
        var to = _rows.IndexOf(target);
        if (from < 0 || to < 0)
            return;
        _rows.Move(from, to);
        SectionsList.SelectedItem = source;
    }

    /// <summary>یافتن ردیف زیر ماوس — پیمایش ظرف‌های آیتم</summary>
    private SectionEditRow? GetRowUnderMouse(DragEventArgs e)
    {
        var element = SectionsList.InputHitTest(e.GetPosition(SectionsList)) as DependencyObject;
        while (element is not null)
        {
            if (element is ListBoxItem { Content: SectionEditRow row })
                return row;
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }
        return SectionsList.SelectedItem as SectionEditRow;
    }

    /* ═══════════ پایانی: ذخیره / پیش‌فرض / انصراف ═══════════ */

    /// <summary>ذخیرهٔ پیکربندی + باز کردن نمایشگر با تنظیمات تازه</summary>
    private void OnSaveAndOpen(object sender, RoutedEventArgs e)
    {
        ApplyToConfig();
        _config.Save();

        // بستن نمایشگر باز قبلی (در صورت باز بودن) و گشودن نمونهٔ تازه
        foreach (var window in System.Windows.Application.Current.Windows.OfType<WaitingRoomDisplayWindow>().ToList())
            window.Close();
        var display = new WaitingRoomDisplayWindow();
        display.Show();
        Close();
    }

    /// <summary>بازگشت به پیکربندی پیش‌فرض — با تأیید فارسی</summary>
    private void OnReset(object sender, RoutedEventArgs e)
    {
        var confirmed = AppDialog.Confirm("بازگشت به پیش‌فرض",
            "همهٔ بخش‌ها و تنظیمات نمایشگر به حالت پیش‌فرض بازمی‌گردد. ادامه می‌کنید؟");
        if (!confirmed)
            return;
        var fresh = WaitingRoomConfig.Default();
        _config.ClinicName = fresh.ClinicName;
        _config.ScreenDeviceName = fresh.ScreenDeviceName;
        _config.ShowAnalogClock = fresh.ShowAnalogClock;
        _config.ShowDigitalClock = fresh.ShowDigitalClock;
        _config.ShowLongDate = fresh.ShowLongDate;
        _config.Sections = fresh.Sections;
        // چرخهٔ ۷۵ — تنظیمات جدید هم به پیش‌فرض برمی‌گردند
        _config.Announcement = fresh.Announcement;
        _config.ColumnCount = fresh.ColumnCount;
        _config.ShowAdSubtitle = fresh.ShowAdSubtitle;
        _config.ShowAdFeatures = fresh.ShowAdFeatures;
        _config.ShowAdVersion = fresh.ShowAdVersion;

        ClinicNameBox.Text = string.Empty;
        AnnouncementBox.Text = string.Empty;
        ColumnCountCombo.SelectedIndex = Math.Clamp(_config.ColumnCount, 1, 4) - 1;
        AnalogClockCheck.IsChecked = true;
        DigitalClockCheck.IsChecked = true;
        LongDateCheck.IsChecked = true;
        AdSubtitleCheck.IsChecked = false;
        AdFeaturesCheck.IsChecked = false;
        AdVersionCheck.IsChecked = false;
        _rows.Clear();
        foreach (var section in _config.Sections)
            _rows.Add(new SectionEditRow { Model = section });
        LoadScreens();
        RefreshPreview();
    }

    /// <summary>انصراف — بدون ذخیره</summary>
    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    /// <summary>نوشتن وضعیت رابط روی پیکربندی</summary>
    private void ApplyToConfig()
    {
        _config.ClinicName = ClinicNameBox.Text.Trim();
        _config.ShowAnalogClock = AnalogClockCheck.IsChecked == true;
        _config.ShowDigitalClock = DigitalClockCheck.IsChecked == true;
        _config.ShowLongDate = LongDateCheck.IsChecked == true;
        // چرخهٔ ۷۵ — اعلان/ستون/تبلیغ
        _config.Announcement = AnnouncementBox.Text.Trim();
        _config.ColumnCount = Math.Clamp(ColumnCountCombo.SelectedIndex + 1, 1, 4);
        _config.ShowAdSubtitle = AdSubtitleCheck.IsChecked == true;
        _config.ShowAdFeatures = AdFeaturesCheck.IsChecked == true;
        _config.ShowAdVersion = AdVersionCheck.IsChecked == true;
        _config.Sections = _rows.Select(r => r.Model).ToList();
        if (ScreenCombo.SelectedItem is ScreenOption screen)
            _config.ScreenDeviceName = screen.DeviceName;
    }
}

/// <summary>گزینهٔ انتخابگر مانیتور</summary>
public record ScreenOption(string DeviceName, string Title);

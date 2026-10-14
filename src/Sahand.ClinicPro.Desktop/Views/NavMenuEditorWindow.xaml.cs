using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// یک ردیف نمایشی در ساختار منو — گروه یا صفحه (تورفته زیر گروه خودش)
/// </summary>
public class NavRowVm
{
    /// <summary>کلید یکتای ردیف (برای نگاشت فرزند به گروه)</summary>
    public required string Id { get; init; }

    /// <summary>گروه است؟ (وگرنه صفحه)</summary>
    public required bool IsGroup { get; init; }

    /// <summary>کلید گروه والد — تهی یعنی سطح بالا</summary>
    public string? ParentId { get; init; }

    /// <summary>عنوان نمایشی (گروه: دلخواه؛ صفحه: از کاتالوگ)</summary>
    public required string Title { get; init; }

    /// <summary>کلید صفحه (فقط ردیف‌های صفحه)</summary>
    public string? PageKey { get; init; }

    /// <summary>آیکون</summary>
    public required string Icon { get; init; }

    /// <summary>تورفتگی فرزندان گروه</summary>
    public System.Windows.Thickness Indent { get; init; } = new(2, 3, 2, 3);

    /// <summary>رنگ آیکون — طلایی برای گروه، آبی برای صفحه</summary>
    public Brush IconBrush => IsGroup
        ? new SolidColorBrush(Color.FromRgb(0xEF, 0x6C, 0x00))
        : new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0));

    /// <summary>وزن عنوان</summary>
    public FontWeight TitleWeight => IsGroup ? FontWeights.Bold : FontWeights.Normal;

    /// <summary>برچسب نوع ردیف</summary>
    public string KindLabel => IsGroup ? "گروه منو" : "صفحه";
}

/// <summary>
/// کد پشت ادیتور منوی کناری — چرخهٔ ۷۴ (گزارش مالک):
/// «بتونم منوهای کناری رو به سلیقه خودم مرتب بکنم و حتی بتونم منوی اصلی
/// بسازم که صفه های مورد نظرم رو در زیر منوی اون قرار بدم و همچنین دکمه
/// برگشت به تنظیمات پیشفرض هم داشته باشه و این تنظیمات به ازای هر کاربر
/// باشه و در دیتابیس ذخیره بشه».
/// سازوکار:
///   • ستون راست: صفحات مجازِ در دسترس (PageCatalog با پالایش مجوز فعلی)؛
///   • ستون چپ: ساختار فعلی منو به‌صورت ردیف‌های مسطح (گروه + فرزندان
///     تورفته) — جابه‌جایی با درگ (DragDropReorder) یا پیکان‌ها؛
///   • رهاکردنِ صفحهٔ در دسترس روی ردیف گروه → افزودن به زیرمنوی همان گروه؛
///     روی فضای خالی یا ردیف سطح‌بالا → آیتم تکی؛
///   • «گروه جدید» با عنوان دلخواه و آیکون انتخابی؛
///   • ذخیره → PUT users/me/nav-layout (per-user در دیتابیس).
/// </summary>
public partial class NavMenuEditorWindow : Window
{
    /// <summary>ردیف‌های ساختار منو — قابل جابه‌جایی</summary>
    private readonly ObservableCollection<NavRowVm> _rows = new();

    /// <summary>صفحات در دسترس (مجاز به کاربر جاری)</summary>
    private readonly ObservableCollection<NavRowVm> _available = new();

    /// <summary>سرویس API از DI</summary>
    private readonly IApiClient _api;

    /// <summary>آیکون‌های پیشنهادی برای گروه دلخواه</summary>
    private static readonly string[] GroupIcons =
        { "Folder", "Hospital", "Prescription", "TestTube", "AccountTie", "CashMultiple",
          "MessageTextOutline", "Cog", "Star", "HeartPulse", "School", "ViewDashboard" };

    /// <summary>قالب دادهٔ درگِ صفحهٔ در دسترس</summary>
    private const string PageDragFormat = "Sahand.ClinicPro.NavPageDrag";

    /// <summary>قالب دادهٔ درگِ جابه‌جایی ردیف ساختار</summary>
    private const string RowDragFormat = "Sahand.ClinicPro.NavRowDrag";

    /// <summary>سازنده — بارگذاری ساختار فعلی (سرور یا پیش‌فرض) + صفحات موجود</summary>
    public NavMenuEditorWindow()
    {
        InitializeComponent();
        _api = App.Services.GetRequiredService<IApiClient>();
        StructureList.ItemsSource = _rows;
        AvailableList.ItemsSource = _available;
        _ = LoadAsync();
    }

    /// <summary>
    /// بارگذاری ساختار منو — اولویت: چیدمان ذخیره‌شدهٔ کاربر (GET me/nav-layout)؛
    /// در نبود آن، ساختار پیش‌فرض فعلی برنامه به‌صورت مسطح از منوی فعلی.
    /// </summary>
    private async Task LoadAsync()
    {
        try
        {
            var response = await _api.GetAsync<NavLayoutEnvelope>("users/me/nav-layout");
            var json = response?.Data?.Layout;
            if (!string.IsNullOrWhiteSpace(json))
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    _rows.Clear();
                    foreach (var element in doc.RootElement.EnumerateArray())
                        AppendJsonEntry(element);
                    if (_rows.Count > 0)
                    {
                        LoadAvailablePages();
                        return;
                    }
                }
            }
        }
        catch
        {
            // بدون چیدمان ذخیره‌شده — ساختار پیش‌فرض
        }
        LoadDefaultStructure();
        LoadAvailablePages();
    }

    /// <summary>بارگذاری ساختار پیش‌فرض — همان منوی فعلی برنامه (مسطح)</summary>
    private void LoadDefaultStructure()
    {
        _rows.Clear();
        var main = App.Services.GetRequiredService<ViewModels.MainWindowViewModel>();
        foreach (var nav in main.NavItems)
        {
            var id = Guid.NewGuid().ToString("N");
            if (nav.HasChildren)
            {
                _rows.Add(new NavRowVm
                {
                    Id = id, IsGroup = true, Title = nav.Title, Icon = nav.Icon,
                });
                foreach (var child in nav.Children)
                    _rows.Add(new NavRowVm
                    {
                        Id = Guid.NewGuid().ToString("N"), IsGroup = false, ParentId = id,
                        Title = child.Title, PageKey = child.PageKey, Icon = child.Icon,
                        Indent = new System.Windows.Thickness(24, 3, 2, 3),
                    });
            }
            else if (!string.IsNullOrEmpty(nav.PageKey))
            {
                _rows.Add(new NavRowVm
                {
                    Id = id, IsGroup = false, Title = nav.Title, PageKey = nav.PageKey, Icon = nav.Icon,
                });
            }
        }
    }

    /// <summary>افزودن یک مدخل JSON به ردیف‌های ساختار (آیتم تکی یا گروه)</summary>
    private void AppendJsonEntry(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;
        var id = Guid.NewGuid().ToString("N");
        if (element.TryGetProperty("pages", out var pages) && pages.ValueKind == JsonValueKind.Array)
        {
            _rows.Add(new NavRowVm
            {
                Id = id, IsGroup = true,
                Title = element.TryGetProperty("title", out var t) ? t.GetString() ?? "گروه من" : "گروه من",
                Icon = element.TryGetProperty("icon", out var ic) ? ic.GetString() ?? "Folder" : "Folder",
            });
            foreach (var page in pages.EnumerateArray())
            {
                var key = page.ValueKind == JsonValueKind.String ? page.GetString() : null;
                var entry = key is null ? null : PageCatalog.All.FirstOrDefault(p => p.Key == key);
                if (entry is null) continue;
                _rows.Add(new NavRowVm
                {
                    Id = Guid.NewGuid().ToString("N"), IsGroup = false, ParentId = id,
                    Title = entry.Title, PageKey = entry.Key, Icon = entry.Icon,
                    Indent = new System.Windows.Thickness(24, 3, 2, 3),
                });
            }
            return;
        }
        if (element.TryGetProperty("pageKey", out var pk) && pk.ValueKind == JsonValueKind.String)
        {
            var key = pk.GetString();
            var entry = key is null ? null : PageCatalog.All.FirstOrDefault(p => p.Key == key);
            if (entry is null) return;
            _rows.Add(new NavRowVm
            {
                Id = id, IsGroup = false, Title = entry.Title, PageKey = entry.Key, Icon = entry.Icon,
            });
        }
    }

    /// <summary>صفحات در دسترس — کاتالوگ با پالایش مجوزِ همان صفحهٔ اصلی</summary>
    private void LoadAvailablePages()
    {
        _available.Clear();
        var main = App.Services.GetRequiredService<ViewModels.MainWindowViewModel>();
        foreach (var page in PageCatalog.All.Where(p => p.ShowInPicker))
        {
            if (!main.HasPageAccess(page.Key))
                continue;
            _available.Add(new NavRowVm
            {
                Id = page.Key, IsGroup = false, Title = page.Title,
                PageKey = page.Key, Icon = page.Icon,
            });
        }
    }

    /* ═══════════ درگ از «صفحات در دسترس» ═══════════ */

    /// <summary>شروع درگ صفحهٔ انتخاب‌شده از فهرست راست</summary>
    private void OnAvailableDrag(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || AvailableList.SelectedItem is not NavRowVm page)
            return;
        DragDrop.DoDragDrop(AvailableList, new DataObject(PageDragFormat, page), DragDropEffects.Copy);
    }

    /// <summary>شروع درگ جابه‌جایی ردیف ساختار</summary>
    private void OnStructureDrag(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || StructureList.SelectedItem is not NavRowVm row)
            return;
        DragDrop.DoDragDrop(StructureList, new DataObject(RowDragFormat, row), DragDropEffects.Move);
    }

    /// <summary>نشانگر درگ روی ساختار</summary>
    private void OnStructureDragOver(object sender, DragEventArgs e)
        => e.Effects = e.Data.GetDataPresent(PageDragFormat) || e.Data.GetDataPresent(RowDragFormat)
            ? (e.Data.GetDataPresent(PageDragFormat) ? DragDropEffects.Copy : DragDropEffects.Move)
            : DragDropEffects.None;

    /// <summary>
    /// رها کردن روی ساختار — دو حالت:
    ///   ۱) صفحهٔ در دسترس (Copy): افزودن به گروه زیر ماوس، یا سطحِ بالا؛
    ///   ۲) ردیف ساختار (Move): جابه‌جایی به جای ردیف مقصد (حفظ والد گروه).
    /// </summary>
    private void OnStructureDrop(object sender, DragEventArgs e)
    {
        // ── ۱) افزودن صفحهٔ جدید ──
        if (e.Data.GetDataPresent(PageDragFormat) &&
            e.Data.GetData(PageDragFormat) is NavRowVm page)
        {
            var target = RowUnderMouse(e);
            if (target is { IsGroup: true })
                InsertPageUnderGroup(page, target); // داخل گروه مقصد
            else
                _rows.Add(new NavRowVm // سطح بالا (یا انتهای فهرست)
                {
                    Id = Guid.NewGuid().ToString("N"), IsGroup = false,
                    Title = page.Title, PageKey = page.PageKey, Icon = page.Icon,
                });
            e.Handled = true;
            return;
        }

        // ── ۲) جابه‌جایی ردیف ──
        if (e.Data.GetDataPresent(RowDragFormat) &&
            e.Data.GetData(RowDragFormat) is NavRowVm source)
        {
            var target = RowUnderMouse(e);
            if (target is null || ReferenceEquals(source, target))
                return;

            // گروه‌ها با همهٔ فرزندانشان جابه‌جا می‌شوند (بلوک پیوسته)
            var block = CollectBlock(source);
            var targetIndex = _rows.IndexOf(target);
            if (targetIndex < 0)
                return;
            // اگر مقصد داخل بلوک خود مبدأ است — کاری نکن
            if (block.Contains(target))
                return;

            foreach (var r in block)
                _rows.Remove(r);
            // اگر ردیف مبدأ فرزند است و مقصد سطح‌بالا است، فرزندِ درگ‌شده به والدِ
            // مقصد می‌چسبد (یا سطح بالا می‌ماند) — رفتاری ساده و پیش‌بین
            var targetIndex2 = Math.Clamp(targetIndex, 0, _rows.Count);
            var moved = block[0];
            if (!source.IsGroup && target.IsGroup)
            {
                // فرزندِ رهاشده روی گروه → زیر گروه مقصد
                moved = new NavRowVm
                {
                    Id = source.Id, IsGroup = false, ParentId = target.Id,
                    Title = source.Title, PageKey = source.PageKey, Icon = source.Icon,
                    Indent = new System.Windows.Thickness(24, 3, 2, 3),
                };
                block[0] = moved;
            }
            for (var i = 0; i < block.Count; i++)
                _rows.Insert(Math.Clamp(targetIndex2 + i, 0, _rows.Count), block[i]);
            StructureList.SelectedItem = moved;
            e.Handled = true;
        }
    }

    /// <summary>درج یک صفحه زیر گروه مقصد (پیش از نخستین ردیفِ بعد از بلوک گروه)</summary>
    private void InsertPageUnderGroup(NavRowVm page, NavRowVm group)
    {
        var block = CollectBlock(group);
        var insertAt = _rows.IndexOf(block[^1]) + 1;
        _rows.Insert(Math.Clamp(insertAt, 0, _rows.Count), new NavRowVm
        {
            Id = Guid.NewGuid().ToString("N"), IsGroup = false, ParentId = group.Id,
            Title = page.Title, PageKey = page.PageKey, Icon = page.Icon,
            Indent = new System.Windows.Thickness(24, 3, 2, 3),
        });
    }

    /// <summary>بلوک پیوستهٔ یک ردیف — خودش + فرزندانش (برای گروه‌ها)</summary>
    private List<NavRowVm> CollectBlock(NavRowVm row)
    {
        var block = new List<NavRowVm> { row };
        if (!row.IsGroup)
            return block;
        var index = _rows.IndexOf(row);
        for (var i = index + 1; i < _rows.Count; i++)
        {
            if (_rows[i].ParentId == row.Id)
                block.Add(_rows[i]);
            else
                break;
        }
        return block;
    }

    /// <summary>ردیف ساختار زیر ماوس</summary>
    private NavRowVm? RowUnderMouse(DragEventArgs e)
    {
        var hit = StructureList.InputHitTest(e.GetPosition(StructureList)) as DependencyObject;
        while (hit is not null && !ReferenceEquals(hit, StructureList))
        {
            if (hit is ListBoxItem { Content: NavRowVm row })
                return row;
            hit = VisualTreeHelper.GetParent(hit);
        }
        return null;
    }

    /* ═══════════ دکمه‌ها ═══════════ */

    /// <summary>افزودن گروه دلخواه — عنوان + انتخاب آیکون</summary>
    private void OnAddGroup(object sender, RoutedEventArgs e)
    {
        var dialog = new NavGroupPromptWindow { Owner = this };
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.GroupTitle))
        {
            _rows.Add(new NavRowVm
            {
                Id = Guid.NewGuid().ToString("N"), IsGroup = true,
                Title = dialog.GroupTitle.Trim(), Icon = dialog.SelectedIcon,
            });
            StructureList.SelectedItem = _rows[^1];
        }
    }

    /// <summary>حذف ردیف انتخابی — گروه با همهٔ فرزندانش</summary>
    private void OnRemoveRow(object sender, RoutedEventArgs e)
    {
        if (StructureList.SelectedItem is not NavRowVm row)
            return;
        foreach (var r in CollectBlock(row))
            _rows.Remove(r);
    }

    /// <summary>جابه‌جایی به بالا — بلوک کامل (گروه + فرزندان)</summary>
    private void OnMoveRowUp(object sender, RoutedEventArgs e)
    {
        if (StructureList.SelectedItem is not NavRowVm row)
            return;
        var index = _rows.IndexOf(row);
        if (index <= 0)
            return;
        // ردیف بالایی: اگر فرزندِ گروهی است، جابه‌جایی باید قبل از سرگروه باشد
        var above = _rows[index - 1];
        var target = above.ParentId is { } pid && index >= 2
            ? _rows[index - 2].ParentId == pid ? index - 2 : index - 1
            : index - 1;
        MoveBlockTo(row, target);
    }

    /// <summary>جابه‌جایی به پایین — بلوک کامل</summary>
    private void OnMoveRowDown(object sender, RoutedEventArgs e)
    {
        if (StructureList.SelectedItem is not NavRowVm row)
            return;
        var block = CollectBlock(row);
        var afterEnd = _rows.IndexOf(block[^1]) + 1;
        if (afterEnd >= _rows.Count)
            return;
        // مقصد: پس از بلوکِ ردیف بعدی
        var nextBlock = CollectBlock(_rows[afterEnd]);
        var target = _rows.IndexOf(nextBlock[0]);
        MoveBlockTo(row, target);
    }

    /// <summary>جابه‌جایی بلوک به اندیس سرگروه مقصد</summary>
    private void MoveBlockTo(NavRowVm row, int targetIndex)
    {
        var block = CollectBlock(row);
        foreach (var r in block)
            _rows.Remove(r);
        foreach (var r in block)
            _rows.Insert(Math.Clamp(targetIndex, 0, _rows.Count), r);
        StructureList.SelectedItem = row;
    }

    /// <summary>بازسازی فهرست ساختار پس از تغییر انتخاب (بدون عمل — رزرو برای ویرایش عنوان)</summary>
    private void OnStructureSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    /* ═══════════ ذخیره / پیش‌فرض / انصراف ═══════════ */

    /// <summary>ذخیره — ساختار مسطح به JSON سلسله‌مراتبی و PUT به سرور</summary>
    private async void OnSave(object sender, RoutedEventArgs e)
    {
        try
        {
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartArray();
                for (var i = 0; i < _rows.Count; i++)
                {
                    var row = _rows[i];
                    if (row.ParentId is not null)
                        continue; // فرزند با گروه خودش نوشته می‌شود
                    if (row.IsGroup)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("title", row.Title);
                        writer.WriteString("icon", row.Icon);
                        writer.WriteStartArray("pages");
                        foreach (var child in _rows.Where(r => r.ParentId == row.Id))
                            writer.WriteStringValue(child.PageKey);
                        writer.WriteEndArray();
                        writer.WriteEndObject();
                    }
                    else
                    {
                        writer.WriteStartObject();
                        writer.WriteString("pageKey", row.PageKey);
                        writer.WriteEndObject();
                    }
                }
                writer.WriteEndArray();
            }
            var json = System.Text.Encoding.UTF8.GetString(buffer.ToArray());
            if (json.Length > 7800)
            {
                AppDialog.Error("ساختار منو بزرگ است", "تعداد صفحات/گروه‌ها بیش از حد مجاز است — چند مورد را حذف کنید.");
                return;
            }

            var ok = await _api.PutAsync("users/me/nav-layout", new { layout = json });
            if (!ok)
            {
                AppDialog.Error("ذخیره ناموفق", _api.LastError ?? "سرور چیدمان منو را نپذیرفت.");
                return;
            }
            AppDialog.Success("ذخیره شد", "چیدمان منوی کناری شما ذخیره شد و از همین لحظه اعمال می‌شود.");
            Close();
        }
        catch (Exception ex)
        {
            AppDialog.Error("خطای غیرمنتظره", ex.Message);
        }
    }

    /// <summary>بازگشت به منوی پیش‌فرض — رکورد ذخیره‌شده پاک می‌شود</summary>
    private async void OnReset(object sender, RoutedEventArgs e)
    {
        if (!AppDialog.Confirm("بازگشت به پیش‌فرض",
                "منوی شخصی شما حذف و منوی پیش‌فرض برنامه بازمی‌گردد. ادامه می‌کنید؟"))
            return;
        // ساختار خالی = حذف تنظیم — اندپوینت مقدار «[]» را چیدمان خالی ذخیره می‌کند؛
        // کلاینت مادر «[]» را چیدمان خالی می‌بیند و به پیش‌فرض برمی‌گردد
        var ok = await _api.PutAsync("users/me/nav-layout", new { layout = "[]" });
        if (!ok)
        {
            AppDialog.Error("عملیات ناموفق", _api.LastError ?? "بازگشت به پیش‌فرض انجام نشد.");
            return;
        }
        LoadDefaultStructure();
        AppDialog.Success("پیش‌فرض بازگشت", "پس از «ذخیره»، منوی پیش‌فرض اعمال می‌شود.");
    }

    /// <summary>انصراف — بدون ذخیره</summary>
    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    /* ═══════════ پاسخ API ═══════════ */

    /// <summary>پاکت GET me/nav-layout</summary>
    public sealed class NavLayoutEnvelope
    {
        public bool IsSuccess { get; set; }
        public NavLayoutData? Data { get; set; }
    }

    /// <summary>دادهٔ چیدمان منو</summary>
    public sealed class NavLayoutData
    {
        public string? Layout { get; set; }
    }
}

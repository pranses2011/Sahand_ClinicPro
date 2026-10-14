using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// یک ردیف نمایشی در ساختار منو — گروه یا صفحه (تورفته زیر گروه خودش)
/// </summary>
public partial class NavRowVm : ObservableObject
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

    /// <summary>برچسب شمار فرزندان گروه («۳ صفحه») — پس از هر تغییر ساختار تازه می‌شود</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChildren))]
    private string _childrenLabel = string.Empty;

    /// <summary>آیا گروه فرزندی دارد؟ — نمایش چیپ شمارنده</summary>
    public bool HasChildren => ChildrenLabel.Length > 0;
}

/// <summary>
/// کد پشت ادیتور منوی کناری — چرخهٔ ۷۶ (بازطراحی رویکرد).
/// گزارش مالک چرخهٔ ۷۵: «شخصی‌سازی منوهای کناری — نوع فعلی خوب نیست و سخت
/// است؛ رویکرد را عوض کن». ویرایشگر درگ‌ومحورِ چرخهٔ ۷۴ با الگوی «همه‌چیز با
/// یک کلیک» جایگزین شد:
///   • نوار بالا: کشوی صفحات در دسترس + «افزودن به انتها» + «گروه جدید»؛
///   • هر ردیف ساختار دکمه‌های خودش را دارد:
///       ➕ (فقط گروه‌ها) — افزودن صفحهٔ منتخب نوار بالا به همان گروه؛
///       ▲/▼ — جابه‌جایی بلوک کامل (گروه + فرزندان)؛
///       ✕ — حذف بلوک کامل؛
///   • ساختار JSON و قرارداد سرور (PUT users/me/nav-layout، per-user) دست‌نخورده.
/// </summary>
public partial class NavMenuEditorWindow : Window
{
    /// <summary>ردیف‌های ساختار منو</summary>
    private readonly ObservableCollection<NavRowVm> _rows = new();

    /// <summary>صفحات در دسترس (مجاز به کاربر جاری) — منبع کشوی نوار بالا</summary>
    private readonly ObservableCollection<NavRowVm> _available = new();

    /// <summary>سرویس API از DI</summary>
    private readonly IApiClient _api;

    /// <summary>سازنده — بارگذاری ساختار فعلی (سرور یا پیش‌فرض) + صفحات موجود</summary>
    public NavMenuEditorWindow()
    {
        InitializeComponent();
        _api = App.Services.GetRequiredService<IApiClient>();
        StructureList.ItemsSource = _rows;
        PageCombo.ItemsSource = _available;
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
                        RefreshGroupCounts();
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
        RefreshGroupCounts();
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
        if (_available.Count > 0)
            PageCombo.SelectedIndex = 0;
    }

    /// <summary>تازه‌سازی چیپ شمار فرزندان همهٔ گروه‌ها — پس از هر تغییر ساختار</summary>
    private void RefreshGroupCounts()
    {
        foreach (var group in _rows.Where(r => r.IsGroup))
        {
            var count = _rows.Count(r => r.ParentId == group.Id);
            group.ChildrenLabel = count > 0 ? $"{count} صفحه" : string.Empty;
        }
    }

    /* ═══════════ نوار بالا: افزودن صفحه / گروه جدید ═══════════ */

    /// <summary>صفحهٔ منتخب کشوی نوار بالا — تهی یعنی هنوز چیزی انتخاب نشده</summary>
    private NavRowVm? SelectedPage => PageCombo.SelectedItem as NavRowVm;

    /// <summary>
    /// افزودن صفحهٔ منتخب به «انتهای» ساختار (سطح بالا) — چرخهٔ ۷۶: جایگزین
    /// درگ‌انداختنِ صفحه روی فضای خالی در ویرایشگر قدیمی
    /// </summary>
    private void OnAddPageToEnd(object sender, RoutedEventArgs e)
    {
        if (SelectedPage is not { } page)
        {
            AppDialog.Info("صفحه‌ای انتخاب نشده", "ابتدا صفحهٔ مورد نظر را از کشوی «صفحهٔ مورد نظر» نوار بالا انتخاب کنید.");
            return;
        }
        AppendPage(page, parentId: null);
    }

    /// <summary>
    /// دکمهٔ ➕ روی ردیف گروه — افزودن صفحهٔ منتخبِ نوار بالا به همین گروه
    /// (چرخهٔ ۷۶: جایگزین درگ‌انداختنِ صفحه روی ردیف گروه در ویرایشگر قدیمی)
    /// </summary>
    private void OnRowAddToGroup(object sender, RoutedEventArgs e)
    {
        if (((System.Windows.Controls.Primitives.ButtonBase)sender).CommandParameter is not NavRowVm group || !group.IsGroup)
            return;
        if (SelectedPage is not { } page)
        {
            AppDialog.Info("صفحه‌ای انتخاب نشده", "ابتدا صفحهٔ مورد نظر را از کشوی «صفحهٔ مورد نظر» نوار بالا انتخاب کنید، سپس دکمهٔ ➕ گروه دلخواه را بزنید.");
            return;
        }
        AppendPage(page, group.Id);
        StructureList.SelectedItem = _rows.FirstOrDefault(r => r.ParentId == group.Id);
    }

    /// <summary>افزودن صفحه به ساختار — سطح بالا یا زیر گروه مشخص</summary>
    private void AppendPage(NavRowVm page, string? parentId)
    {
        var row = new NavRowVm
        {
            Id = Guid.NewGuid().ToString("N"), IsGroup = false, ParentId = parentId,
            Title = page.Title, PageKey = page.PageKey, Icon = page.Icon,
            Indent = parentId is null ? new System.Windows.Thickness(2, 3, 2, 3) : new System.Windows.Thickness(24, 3, 2, 3),
        };
        if (parentId is null)
        {
            _rows.Add(row);
        }
        else
        {
            // درج پس از آخرین فرزند گروه (بلوک پیوستهٔ گروه)
            var block = CollectBlock(_rows.First(r => r.Id == parentId));
            _rows.Insert(_rows.IndexOf(block[^1]) + 1, row);
        }
        RefreshGroupCounts();
    }

    /// <summary>افزودن گروه دلخواه — عنوان + انتخاب آیکون</summary>
    private void OnAddGroup(object sender, RoutedEventArgs e)
    {
        var dialog = new NavGroupPromptWindow { Owner = this };
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.GroupTitle))
        {
            var group = new NavRowVm
            {
                Id = Guid.NewGuid().ToString("N"), IsGroup = true,
                Title = dialog.GroupTitle.Trim(), Icon = dialog.SelectedIcon,
            };
            _rows.Add(group);
            RefreshGroupCounts();
            StructureList.SelectedItem = group;
            StructureList.ScrollIntoView(group);
        }
    }

    /* ═══════════ دکمه‌های ردیفی: جابه‌جایی / حذف ═══════════ */

    /// <summary>دکمهٔ ✕ ردیف — حذف بلوک کامل (گروه با همهٔ فرزندانش)</summary>
    private void OnRowRemove(object sender, RoutedEventArgs e)
    {
        if (((System.Windows.Controls.Primitives.ButtonBase)sender).CommandParameter is not NavRowVm row)
            return;
        foreach (var r in CollectBlock(row))
            _rows.Remove(r);
        RefreshGroupCounts();
    }

    /// <summary>دکمهٔ ▲ ردیف — جابه‌جایی بلوک کامل به بالا (پیش از بلوک قبلی)</summary>
    private void OnRowMoveUp(object sender, RoutedEventArgs e)
    {
        if (((System.Windows.Controls.Primitives.ButtonBase)sender).CommandParameter is not NavRowVm row)
            return;
        var index = _rows.IndexOf(row);
        if (index <= 0)
            return;
        // اگر ردیف بالایی فرزندِ گروهی است، باید جای بلوکِ سرگروه آن جابه‌جا شد
        var above = _rows[index - 1];
        var target = above.ParentId is { } pid && index >= 2 && _rows[index - 2].ParentId == pid
            ? index - 2
            : index - 1;
        MoveBlockTo(row, target);
        RefreshGroupCounts();
    }

    /// <summary>دکمهٔ ▼ ردیف — جابه‌جایی بلوک کامل به پایین (پس از بلوک بعدی)</summary>
    private void OnRowMoveDown(object sender, RoutedEventArgs e)
    {
        if (((System.Windows.Controls.Primitives.ButtonBase)sender).CommandParameter is not NavRowVm row)
            return;
        var block = CollectBlock(row);
        var afterEnd = _rows.IndexOf(block[^1]) + 1;
        if (afterEnd >= _rows.Count)
            return;
        var nextBlock = CollectBlock(_rows[afterEnd]);
        MoveBlockTo(row, _rows.IndexOf(nextBlock[0]));
        RefreshGroupCounts();
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

    /// <summary>جابه‌جایی بلوک کامل به اندیس سرگروه مقصد</summary>
    private void MoveBlockTo(NavRowVm row, int targetIndex)
    {
        var block = CollectBlock(row);
        foreach (var r in block)
            _rows.Remove(r);
        foreach (var r in block)
            _rows.Insert(Math.Clamp(targetIndex, 0, _rows.Count), r);
        StructureList.SelectedItem = row;
        StructureList.ScrollIntoView(row);
    }

    /* ═══════════ ذخیره / پیش‌فرض / انصراف ═══════════ */

    /// <summary>ذخیره — ساختار مسطح به JSON سلسله‌مراتبی و PUT به سرور (الگوی رویدادِ همگام + متد async مجزا — قانون پروژه)</summary>
    private void OnSave(object sender, RoutedEventArgs e) => _ = SaveAsync();

    /// <summary>منطق ذخیره — تمام خطاها درون همین متد مهار می‌شود</summary>
    private async Task SaveAsync()
    {
        try
        {
            // گروه بی‌فرزند معنا ندارد — هشدار شفاف پیش از ذخیره
            var emptyGroups = _rows.Where(r => r.IsGroup && _rows.All(x => x.ParentId != r.Id)).ToList();
            if (emptyGroups.Count > 0)
            {
                var proceed = AppDialog.Confirm("گروه خالی",
                    $"«{emptyGroups[0].Title}»" + (emptyGroups.Count > 1 ? $" و {emptyGroups.Count - 1} گروه دیگر" : "") +
                    " هیچ صفحه‌ای ندارد و در منو نمایش داده نمی‌شود. همین‌طور ذخیره می‌کنید؟");
                if (!proceed) return;
            }

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

    /// <summary>بازگشت به منوی پیش‌فرض — رکورد ذخیره‌شده پاک می‌شود (الگوی رویدادِ همگام + متد async مجزا — قانون پروژه)</summary>
    private void OnReset(object sender, RoutedEventArgs e) => _ = ResetAsync();

    /// <summary>منطق بازگشت به پیش‌فرض</summary>
    private async Task ResetAsync()
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
        RefreshGroupCounts();
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

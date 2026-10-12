// ═══════════════════════════════════════════════════════════════════════
//  مدل‌های کوچکِ مشترکِ ViewModelها
//
//  این کلاس‌ها بین همه‌ی ViewModelهای تولیدشده به اشتراک گذاشته می‌شوند و
//  تنها «قالبِ نمایش» را نگه می‌دارند — هیچ منطقِ تجاری در آن‌ها نیست.
// ═══════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using System.Windows.Media;

namespace Sahand.ClinicPro.Desktop.ViewModels
{
    /// <summary>یک کارتِ شاخصِ کلیدی در بالای صفحه</summary>
    public sealed record KpiItem(string Title, string Value, double? Trend, Geometry? Icon, Brush? Accent);

    /// <summary>یک پیوندِ ارتباطی بین دو صفحه</summary>
    public sealed record RelationItem(string From, string To, string Why);

    /// <summary>یک تب در نوارِ تب‌ها</summary>
    public sealed record TabItemModel(string Title, Geometry? Icon, int? Count);

    /// <summary>یک زوجِ کلید-مقدار برای پانلِ خلاصه</summary>
    public sealed record KeyValueItem(string Key, string Value);

    /// <summary>یک کارت در چیدمانِ کارتی</summary>
    public sealed record CardItem(string Title, string Sub, string Status);

    /// <summary>یک رخداد در خطِ زمان</summary>
    public sealed record TimelineItem(Geometry? Icon, string Date, string Title, string Detail);

    /// <summary>یک ستون در بوردِ کانبان</summary>
    public sealed class KanbanColumn
    {
        /// <summary>عنوانِ ستون</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>کارت‌های درونِ ستون</summary>
        public IReadOnlyList<CardItem> Cards { get; set; } = System.Array.Empty<CardItem>();
    }

    /// <summary>یک قلم در سبدِ فروش</summary>
    public sealed record CartLine(string Name, int Quantity, decimal UnitPrice);

    /// <summary>سربرگِ برجسته‌ی صفحه‌های جزئیات</summary>
    public sealed record HeroModel(string Name, string Sub, string Initials);

    /// <summary>یک ویژگیِ برجسته در صفحه‌ی ورود</summary>
    public sealed record FeatureItem(Geometry? Icon, string Text);

    /// <summary>یک گروه در منوی کناری</summary>
    public sealed record MenuGroup(string Title, IReadOnlyList<MenuItemModel> Items);

    /// <summary>
    /// یک آیتمِ منو — همراه با فرمانِ بازکردنِ صفحه‌ی مقصد
    /// </summary>
    public sealed class MenuItemModel
    {
        /// <summary>عنوانِ نمایشی</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>آیکونِ آیتم</summary>
        public Geometry? Icon { get; set; }

        /// <summary>نامِ صفحه‌ی مقصد</summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>فرمانِ بازکردنِ صفحه — توسط MainViewModel تنظیم می‌شود</summary>
        public System.Windows.Input.ICommand? OpenCommand { get; set; }
    }
}

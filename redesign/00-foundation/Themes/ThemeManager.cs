using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace Sahand.ClinicPro.Desktop.Themes
{
    /// <summary>
    /// نام تم‌های پشتیبانی‌شده
    /// </summary>
    public enum AppTheme
    {
        /// <summary>تم روشن</summary>
        Light,
        /// <summary>تم تاریک</summary>
        Dark
    }

    /// <summary>
    /// مدیر تعویض تم در لحظه — بدون نیاز به راه‌اندازی مجدد برنامه
    /// </summary>
    /// <remarks>
    /// روش کار: این کلاس کلیدهای فایل <c>Themes/Colors.{Name}.xaml</c> را
    /// در مجموعه‌ی منابعِ برنامه کپی می‌کند. چون تمام Viewها از
    /// <c>DynamicResource</c> استفاده می‌کنند، تغییرِ مقدارِ یک کلید
    /// بلافاصله در سراسر برنامه منعکس می‌شود.
    /// </remarks>
    public static class ThemeManager
    {
        private const string ColorsUriFormat =
            "pack://application:,,,/Sahand.ClinicPro.Desktop;component/Themes/Colors.{0}.xaml";

        private const string SettingsKey = "App.Theme";

        /// <summary>تم فعلی برنامه</summary>
        public static AppTheme Current { get; private set; } = AppTheme.Light;

        /// <summary>
        /// رویدادی که پس از هر تغییر تم رخ می‌دهد
        /// </summary>
        public static event EventHandler<AppTheme>? ThemeChanged;

        /// <summary>
        /// اعمال تم مشخص‌شده روی کل برنامه
        /// </summary>
        /// <param name="theme">تم مقصد (روشن یا تاریک)</param>
        /// <param name="persist">آیا انتخاب در تنظیمات کاربر ذخیره شود</param>
        public static void Apply(AppTheme theme, bool persist = true)
        {
            if (Application.Current is null) return;

            var appResources = Application.Current.Resources;

            // ۱) یافتن فرهنگ‌نامه‌ی رنگ‌ها در منابع برنامه
            var target = FindColorsDictionary(appResources);
            if (target is null)
            {
                // فرهنگ‌نامه وجود ندارد → یک نمونه‌ی خالی ایجاد و ادغام می‌کنیم
                target = new ResourceDictionary();
                appResources.MergedDictionaries.Insert(0, target);
            }

            // ۲) بارگذاری پالت مقصد
            var incoming = LoadColors(theme);
            if (incoming is null) return;

            // ۳) کپی کلیدها (جایگزینی مقدار باعث به‌روزرسانی DynamicResource می‌شود)
            foreach (var key in incoming.Keys.OfType<object>().ToList())
            {
                target[key] = incoming[key];
            }

            Current = theme;

            // ۴) ذخیره در تنظیمات کاربر
            if (persist)
                Persist(theme);

            ThemeChanged?.Invoke(null, theme);
        }

        /// <summary>
        /// بارگذاری تم ذخیره‌شده‌ی کاربر در زمان راه‌اندازی
        /// </summary>
        public static void ApplySaved()
        {
            var saved = ReadPersisted();
            Apply(saved, persist: false);
        }

        /// <summary>
        /// جابه‌جایی بین روشن و تاریک
        /// </summary>
        public static void Toggle()
            => Apply(Current == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);

        /// <summary>
        /// یافتن فرهنگ‌نامه‌ای که کلیدهای رنگ (<c>C.Primary</c>) را نگه می‌دارد
        /// </summary>
        private static ResourceDictionary? FindColorsDictionary(ResourceDictionary root)
        {
            if (root.Contains("C.Primary"))
                return root;

            foreach (var merged in root.MergedDictionaries)
            {
                if (merged.Contains("C.Primary"))
                    return merged;

                var nested = FindColorsDictionary(merged);
                if (nested is not null)
                    return nested;
            }

            return null;
        }

        /// <summary>
        /// بارگذاری فایل پالت رنگ متناظر با تم
        /// </summary>
        private static ResourceDictionary? LoadColors(AppTheme theme)
        {
            var uri = new Uri(string.Format(ColorsUriFormat, theme), UriKind.Absolute);
            try
            {
                return Application.LoadComponent(uri) as ResourceDictionary;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UriFormatException)
            {
                return null;
            }
        }

        #region ذخیره‌سازی

        private static void Persist(AppTheme theme)
        {
            try
            {
                Properties.Settings.Default[SettingsKey] = theme.ToString();
                Properties.Settings.Default.Save();
            }
            catch
            {
                // ذخیره‌سازی نباید باعث شکستِ تعویض تم شود
            }
        }

        private static AppTheme ReadPersisted()
        {
            try
            {
                var raw = Properties.Settings.Default[SettingsKey] as string;
                return string.Equals(raw, nameof(AppTheme.Dark), StringComparison.OrdinalIgnoreCase)
                    ? AppTheme.Dark
                    : AppTheme.Light;
            }
            catch
            {
                return AppTheme.Light;
            }
        }

        #endregion
    }
}

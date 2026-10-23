using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Sahand.ClinicPro.Desktop.Localization
{
    /// <summary>
    /// زبان‌های پشتیبانی‌شده توسط رابط کاربر
    /// </summary>
    public sealed class LanguageInfo
    {
        /// <summary>شناسه‌ی فرهنگ (مانند fa-IR)</summary>
        public string Culture { get; init; } = "fa-IR";

        /// <summary>نام نمایشی به زبان خودش</summary>
        public string DisplayName { get; init; } = "فارسی";

        /// <summary>شکلک پرچم</summary>
        public string Flag { get; init; } = "🇮🇷";

        /// <summary>آیا راست‌چین است</summary>
        public bool IsRtl { get; init; } = true;

        /// <summary>نام تقویم پیش‌فرض</summary>
        public string Calendar { get; init; } = "Persian";
    }

    /// <summary>
    /// مدیر زبان و بومی‌سازی — پشتیبانی از تعویض زبان در لحظه
    /// </summary>
    /// <remarks>
    /// تمام رشته‌های رابط کاربر در فایل‌های <c>Strings.resx</c> و
    /// <c>Strings.en.resx</c> نگهداری می‌شوند. برای افزودن زبان جدید،
    /// کافی است فایل <c>Strings.{code}.resx</c> را اضافه کرده و
    /// عضوی از <see cref="SupportedLanguages"/> را به‌روزرسانی کنید.
    /// </remarks>
    public static class LocalizationManager
    {
        /// <summary>زبان پیش‌فرض برنامه</summary>
        public const string DefaultCulture = "fa-IR";

        private const string SettingsKey = "App.Language";

        /// <summary>زبان‌های پشتیبانی‌شده</summary>
        public static IReadOnlyList<LanguageInfo> SupportedLanguages { get; } = new[]
        {
            new LanguageInfo
            {
                Culture = "fa-IR", DisplayName = "فارسی", Flag = "🇮🇷",
                IsRtl = true, Calendar = "Persian"
            },
            new LanguageInfo
            {
                Culture = "en-US", DisplayName = "English", Flag = "🇬🇧",
                IsRtl = false, Calendar = "Gregorian"
            },
        };

        /// <summary>زبان فعلی</summary>
        public static LanguageInfo Current { get; private set; } = SupportedLanguages[0];

        /// <summary>رویداد تغییر زبان</summary>
        public static event EventHandler<LanguageInfo>? LanguageChanged;

        /// <summary>
        /// دریافت رشته‌ی ترجمه‌شده بر اساس کلید
        /// </summary>
        /// <param name="key">کلید رشته (مانند Common.Save)</param>
        /// <returns>متن ترجمه‌شده؛ در صورت نبود کلید، همان کلید بازگردانده می‌شود</returns>
        public static string Get(string key)
            => Strings.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

        /// <summary>
        /// دریافت رشته با قالب‌بندی آرگومان‌ها
        /// </summary>
        /// <param name="key">کلید رشته</param>
        /// <param name="args">آرگومان‌های قالب</param>
        public static string Get(string key, params object[] args)
        {
            var template = Get(key);
            try
            {
                return args.Length == 0 ? template : string.Format(template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }

        /// <summary>
        /// تغییر زبان برنامه در لحظه
        /// </summary>
        /// <param name="culture">شناسه‌ی فرهنگ مقصد</param>
        /// <param name="persist">ذخیره در تنظیمات کاربر</param>
        public static void SetCulture(string culture, bool persist = true)
        {
            var info = SupportedLanguages.FirstOrDefault(
                           l => string.Equals(l.Culture, culture, StringComparison.OrdinalIgnoreCase))
                       ?? SupportedLanguages.FirstOrDefault(
                           l => l.Culture.StartsWith(culture[..2], StringComparison.OrdinalIgnoreCase))
                       ?? SupportedLanguages[0];

            var ci = new CultureInfo(info.Culture)
            {
                DateTimeFormat = { Calendar = CreateCalendar(info.Calendar) },
                NumberFormat =
                {
                    // استفاده از ارقام و جداکننده‌های بومی
                    DigitSubstitution = DigitShapes.NativeNational
                }
            };

            CultureInfo.CurrentCulture = ci;
            CultureInfo.CurrentUICulture = ci;
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;

            Current = info;

            if (persist)
                Persist(info.Culture);

            LanguageChanged?.Invoke(null, info);
        }

        /// <summary>
        /// بارگذاری زبان ذخیره‌شده در زمان راه‌اندازی
        /// </summary>
        public static void ApplySaved()
        {
            string saved;
            try
            {
                saved = Properties.Settings.Default[SettingsKey] as string ?? DefaultCulture;
            }
            catch
            {
                saved = DefaultCulture;
            }

            SetCulture(saved, persist: false);
        }

        /// <summary>
        /// آیا زبان فعلی راست‌چین است
        /// </summary>
        public static bool IsRightToLeft => Current.IsRtl;

        private static Calendar CreateCalendar(string name) => name switch
        {
            "Persian" => new PersianCalendar(),
            "Hijri" => new HijriCalendar(),
            _ => new GregorianCalendar()
        };

        private static void Persist(string culture)
        {
            try
            {
                Properties.Settings.Default[SettingsKey] = culture;
                Properties.Settings.Default.Save();
            }
            catch
            {
                // نادیده گرفتن خطای ذخیره‌سازی
            }
        }
    }
}

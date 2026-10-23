using System.Windows;
using Sahand.ClinicPro.Desktop.Localization;
using Sahand.ClinicPro.Desktop.Themes;

namespace Sahand.ClinicPro.Desktop
{
    /// <summary>
    /// نقطه‌ی ورود برنامه
    /// </summary>
    public partial class App : Application
    {
        /// <inheritdoc />
        protected override void OnStartup(StartupEventArgs e)
        {
            // ── ۱. اعمالِ تمِ ذخیره‌شده (یا پیش‌فرض) ──────────────────
            ThemeManager.ApplySaved();

            // ── ۲. اعمالِ زبانِ ذخیره‌شده (یا پیش‌فرض) ────────────────
            LocalizationManager.ApplySaved();

            // ── ۳. هم‌گام‌سازیِ جهتِ رابط با زبانِ جاری ────────────────
            Resources["AppFlowDirection"] =
                LocalizationManager.IsRightToLeft ? FlowDirection.RightToLeft
                                                  : FlowDirection.LeftToRight;

            // ── ۴. به‌روزرسانیِ خودکارِ جهت هنگام تغییر زبان ───────────
            LocalizationManager.LanguageChanged += (_, _) =>
                Resources["AppFlowDirection"] =
                    LocalizationManager.IsRightToLeft ? FlowDirection.RightToLeft
                                                      : FlowDirection.LeftToRight;

            base.OnStartup(e);
        }
    }
}

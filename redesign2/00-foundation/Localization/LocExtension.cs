using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace Sahand.ClinicPro.Desktop.Localization
{
    /// <summary>
    /// افزونه‌ی نشانه‌گذاری برای اتصال مستقیمِ کلیدهای ترجمه در XAML
    /// </summary>
    /// <remarks>
    /// نمونه استفاده:
    /// <code>
    /// &lt;TextBlock Text="{loc:Loc Common.Save}" /&gt;
    /// &lt;Button Content="{loc:Loc Patients.New}" /&gt;
    /// </code>
    ///
    /// این افزونه تغییر زبان در لحظه را پشتیبانی می‌کند: با رخ‌دادنِ
    /// <see cref="LocalizationManager.LanguageChanged"/>، مقدارِ همه‌ی
    /// هدف‌های ثبت‌شده دوباره تنظیم می‌شود.
    /// </remarks>
    [MarkupExtensionReturnType(typeof(object))]
    public class LocExtension : MarkupExtension
    {
        private static readonly List<WeakReference<LocTarget>> Targets = new();

        static LocExtension()
        {
            LocalizationManager.LanguageChanged += (_, _) => RefreshAllTargets();
        }

        /// <summary>کلید رشته در فایل منابع</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// مقدار بازگردانده‌شده در صورت نبودِ کلید (پیش‌فرض: خودِ کلید)
        /// </summary>
        public string Fallback { get; set; } = string.Empty;

        /// <summary>ایجاد افزونه با کلید مشخص</summary>
        public LocExtension() { }

        /// <summary>ایجاد افزونه با کلید مشخص</summary>
        public LocExtension(string key) => Key = key;

        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
                return string.Empty;

            var target = ResolveTarget(serviceProvider);
            if (target is not null)
            {
                lock (Targets)
                {
                    PurgeDeadTargets();
                    Targets.Add(new WeakReference<LocTarget>(target));
                }
            }

            return GetValue();
        }

        private object GetValue()
        {
            var value = Strings.ResourceManager.GetString(Key, System.Globalization.CultureInfo.CurrentUICulture);
            return value ?? (string.IsNullOrEmpty(Fallback) ? Key : Fallback);
        }

        private LocTarget? ResolveTarget(IServiceProvider serviceProvider)
        {
            if (serviceProvider.GetService(typeof(IProvideValueTarget)) is not IProvideValueTarget pvt)
                return null;

            if (pvt.TargetObject is not DependencyObject dependencyObject) return null;
            if (pvt.TargetProperty is not DependencyProperty dependencyProperty) return null;

            return new LocTarget(dependencyObject, dependencyProperty, this);
        }

        private static void RefreshAllTargets()
        {
            List<LocTarget> alive;
            lock (Targets)
            {
                PurgeDeadTargets();
                alive = Targets.Select(t => t.TryGetTarget(out var x) ? x : null)
                               .Where(x => x is not null).Select(x => x!).ToList();
            }

            foreach (var target in alive)
                target.Apply();
        }

        private static void PurgeDeadTargets()
            => Targets.RemoveAll(r => !r.TryGetTarget(out _));

        private sealed class LocTarget
        {
            private readonly WeakReference<DependencyObject> _object;
            private readonly DependencyProperty _property;
            private readonly LocExtension _extension;

            public LocTarget(DependencyObject target, DependencyProperty property, LocExtension extension)
            {
                _object = new WeakReference<DependencyObject>(target);
                _property = property;
                _extension = extension;
            }

            public void Apply()
            {
                if (!_object.TryGetTarget(out var target)) return;

                var value = _extension.GetValue();

                // تبدیل به نوعِ مقصد در صورت نیاز
                if (_property.PropertyType == typeof(Brush))
                {
                    target.SetValue(_property,
                        new BrushConverter().ConvertFromString(value as string ?? string.Empty));
                    return;
                }

                target.SetValue(_property, value);
            }
        }
    }
}

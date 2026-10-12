using System.Windows;
using System.Windows.Controls;

namespace Sahand.ClinicPro.Desktop.Controls
{
    /// <summary>
    /// نشان رنگی برای نمایش وضعیت یک رکورد
    /// </summary>
    public partial class StatusBadge : UserControl
    {
        public StatusBadge()
        {
            InitializeComponent();
            Loaded += (_, _) => ApplyLevel();
        }

        /// <summary>متن نشان</summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(StatusBadge),
                new PropertyMetadata(string.Empty));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>سطح رنگی: Success | Warning | Danger | Info | Neutral</summary>
        public static readonly DependencyProperty LevelProperty =
            DependencyProperty.Register(nameof(Level), typeof(string), typeof(StatusBadge),
                new PropertyMetadata("Neutral", OnLevelChanged));

        public string Level
        {
            get => (string)GetValue(LevelProperty);
            set => SetValue(LevelProperty, value);
        }

        private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((StatusBadge)d).ApplyLevel();

        private void ApplyLevel()
        {
            if (BadgeBorder is null || BadgeText is null) return;

            var level = Level?.ToLowerInvariant() switch
            {
                "success" => "Success",
                "warning" => "Warning",
                "danger" => "Danger",
                "critical" => "Danger",
                "error" => "Danger",
                "info" => "Info",
                _ => "Neutral"
            };

            var backgroundKey = $"Brush.{level}Light";
            var foregroundKey = $"Brush.{level}";

            if (TryFindResource($"Badge.{level}") is System.Windows.Style badgeStyle)
                BadgeBorder.Style = badgeStyle;

            if (TryFindResource(foregroundKey) is System.Windows.Media.Brush foreground)
                BadgeText.Foreground = foreground;

            if (TryFindResource(backgroundKey) is System.Windows.Media.Brush background)
                BadgeBorder.Background = background;
        }
    }
}

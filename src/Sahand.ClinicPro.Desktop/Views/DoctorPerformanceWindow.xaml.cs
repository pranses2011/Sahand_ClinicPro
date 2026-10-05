using System.Windows;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// پنجره آمار عملکرد پزشک — گام U3
/// </summary>
public partial class DoctorPerformanceWindow : Window
{
    /// <summary>سازنده — بارگذاری آمار از API</summary>
    public DoctorPerformanceWindow(IApiClient api, long doctorId, string doctorName)
    {
        InitializeComponent();
        DataContext = new PerformanceContext(api, doctorId, doctorName);
    }

    /// <summary>مدل داده سبک برای نمایش آمار</summary>
    private sealed class PerformanceContext : System.ComponentModel.INotifyPropertyChanged
    {
        private readonly System.Collections.Generic.List<(string, string)> _props = new();

        /// <summary>نام پزشک</summary>
        public string DoctorName { get; }

        /// <summary>ویزیت این ماه</summary>
        public string VisitsThisMonth { get; private set; } = "—";

        /// <summary>ویزیت امروز</summary>
        public string VisitsToday { get; private set; } = "—";

        /// <summary>امتیاز</summary>
        public string RatingDisplay { get; private set; } = "—";

        /// <summary>درآمد</summary>
        public string RevenueDisplay { get; private set; } = "—";

        /// <summary>کارانه</summary>
        public string CommissionDisplay { get; private set; } = "—";

        /// <summary>میانگین مدت</summary>
        public string AverageDisplay { get; private set; } = "—";

        /// <summary>نرخ عدم مراجعه عددی</summary>
        public double NoShowRate { get; private set; }

        /// <summary>نرخ عدم مراجعه نمایشی</summary>
        public string NoShowDisplay { get; private set; } = "—";

#pragma warning disable CS0067
        /// <summary>رویداد تغییر ویژگی — استفاده نمی‌شود ولی لازم است</summary>
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

        /// <summary>سازنده — بارگذاری</summary>
        public PerformanceContext(IApiClient api, long doctorId, string doctorName)
        {
            DoctorName = $"آمار عملکرد: {doctorName}";
            _ = LoadAsync(api, doctorId);
        }

        private async System.Threading.Tasks.Task LoadAsync(IApiClient api, long doctorId)
        {
            var response = await api.GetAsync<PerformanceResponse>($"doctors/{doctorId}/performance");
            if (response?.Data is null) return;
            var d = response.Data;
            VisitsThisMonth = d.VisitsThisMonth.ToString("N0");
            VisitsToday = d.VisitsToday.ToString("N0");
            RatingDisplay = $"{d.Rating:0.0} از ۵";
            RevenueDisplay = d.RevenueThisMonth.ToString("N0");
            CommissionDisplay = d.CommissionThisMonth.ToString("N0");
            AverageDisplay = $"{d.AverageVisitMinutes:0.#} دقیقه";
            NoShowRate = d.NoShowRate;
            NoShowDisplay = $"{d.NoShowRate:0.#}٪";
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(string.Empty));
        }

        private sealed class PerformanceResponse
        {
            public bool IsSuccess { get; set; }
            public PerformanceData? Data { get; set; }
        }

        private sealed class PerformanceData
        {
            public int VisitsThisMonth { get; set; }
            public int VisitsToday { get; set; }
            public decimal RevenueThisMonth { get; set; }
            public decimal CommissionThisMonth { get; set; }
            public double AverageVisitMinutes { get; set; }
            public decimal Rating { get; set; }
            public double NoShowRate { get; set; }
        }
    }
}

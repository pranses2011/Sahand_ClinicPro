using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Desktop.Base;
using Sahand.ClinicPro.Desktop.Localization;
using Sahand.ClinicPro.Desktop.Themes;
namespace Sahand.ClinicPro.Desktop.ViewModels.Redesign
{
    /// <summary>
    /// ViewModel صفحه‌ی «اعلان‌ها» — مرکز اعلان‌های سیستم
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/NotificationsView.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/NotificationsView.html</c>
    /// 🎨 View متناظر:     <c>03-views/NotificationsView.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class NotificationsViewModel : PageViewModelBase<NotificationsItem>
    {
        public NotificationsViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════



        private string _filterType = null;

        /// <summary>فیلتر: نوع</summary>
        public string FilterType
        {
            get => _filterType;
            set
            {
                if (SetProperty(ref _filterType, value))
                    _ = LoadAsync();
            }
        }

        private string _filterRead = null;

        /// <summary>فیلتر: وضعیت</summary>
        public string FilterRead
        {
            get => _filterRead;
            set
            {
                if (SetProperty(ref _filterRead, value))
                    _ = LoadAsync();
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _markAllReadCommand;

        /// <summary>خواندن همه</summary>
        public IAsyncRelayCommand MarkAllReadCommand =>
            _markAllReadCommand ??= new AsyncRelayCommand(MarkAllReadAsync);

        private IAsyncRelayCommand? _refreshCommand;

        /// <summary>تازه‌سازی</summary>
        public IAsyncRelayCommand RefreshCommand =>
            _refreshCommand ??= new AsyncRelayCommand(RefreshAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override async Task<PagedResult<NotificationsItem>?> FetchAsync(CancellationToken ct)
        {
            // TODO: فراخوانی واقعی سرویس؛ نمونه:
            //   var response = await _apiClient.GetAsync<PagedResponse<NotificationsItem>>(
            //       "api/v1/<built-in method lower of str object at 0x7f5384a94cf0>s?page={PageNumber}&size={PageSize}", ct);
            //   return new PagedResult<NotificationsItem>(response.Items, response.TotalCount);

            await Task.Delay(200, ct);
            var items = new List<NotificationsItem>();
            return new PagedResult<NotificationsItem>(items, 0);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// خواندن همه
        /// </summary>
        private async Task markAllReadAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "خواندن همه");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

        /// <summary>
        /// تازه‌سازی
        /// </summary>
        private async Task refreshAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "تازه‌سازی");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }


    /// <summary>
    /// مدل نمایش یک ردیف در فهرست «اعلان‌ها»
    /// </summary>
    public sealed class NotificationsItem
    {
        /// <summary></summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>عنوان</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>متن</summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>زمان</summary>
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>اولویت</summary>
        public string Priority { get; set; } = string.Empty;

        /// <summary>سطح رنگیِ نشان وضعیت</summary>
        public string StatusLevel { get; set; } = "Neutral";
    }
}

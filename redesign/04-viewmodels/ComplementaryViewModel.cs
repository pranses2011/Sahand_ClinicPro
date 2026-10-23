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
    /// ViewModel صفحه‌ی «خدمات تکمیلی» — خدمات پاراکلینیکی و ارجاعات
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/ComplementaryView.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/ComplementaryView.html</c>
    /// 🎨 View متناظر:     <c>03-views/ComplementaryView.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class ComplementaryViewModel : PageViewModelBase<ComplementaryItem>
    {
        public ComplementaryViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════



        private string _filterType = null;

        /// <summary>فیلتر: نوع خدمت</summary>
        public string FilterType
        {
            get => _filterType;
            set
            {
                if (SetProperty(ref _filterType, value))
                    _ = LoadAsync();
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _newCommand;

        /// <summary>درخواست جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            _newCommand ??= new AsyncRelayCommand(NewAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override async Task<PagedResult<ComplementaryItem>?> FetchAsync(CancellationToken ct)
        {
            // TODO: فراخوانی واقعی سرویس؛ نمونه:
            //   var response = await _apiClient.GetAsync<PagedResponse<ComplementaryItem>>(
            //       "api/v1/<built-in method lower of str object at 0x7f5384a951b0>s?page={PageNumber}&size={PageSize}", ct);
            //   return new PagedResult<ComplementaryItem>(response.Items, response.TotalCount);

            await Task.Delay(200, ct);
            var items = new List<ComplementaryItem>();
            return new PagedResult<ComplementaryItem>(items, 0);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// درخواست جدید
        /// </summary>
        private async Task newAsync()
        {
            // TODO: باز کردن فرم ایجاد از طریق IDialogService
            await Task.CompletedTask;
        }

    }


    /// <summary>
    /// مدل نمایش یک ردیف در فهرست «خدمات تکمیلی»
    /// </summary>
    public sealed class ComplementaryItem
    {
        /// <summary>خدمت</summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>مرکز ارائه‌دهنده</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>تاریخ درخواست</summary>
        public string RequestDate { get; set; } = string.Empty;

        /// <summary>وضعیت</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>سطح رنگیِ نشان وضعیت</summary>
        public string StatusLevel { get; set; } = "Neutral";
    }
}

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
    /// ViewModel صفحه‌ی «مرکز آموزش» — محتوای آموزشی و دوره‌ها
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/TrainingCenterView.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/TrainingCenterView.html</c>
    /// 🎨 View متناظر:     <c>03-views/TrainingCenterView.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class TrainingCenterViewModel : PageViewModelBase<TrainingCenterItem>
    {
        public TrainingCenterViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════



        private string _filterCategory = null;

        /// <summary>فیلتر: دسته</summary>
        public string FilterCategory
        {
            get => _filterCategory;
            set
            {
                if (SetProperty(ref _filterCategory, value))
                    _ = LoadAsync();
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _openCommand;

        /// <summary>مشاهده</summary>
        public IAsyncRelayCommand OpenCommand =>
            _openCommand ??= new AsyncRelayCommand(OpenAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override async Task<PagedResult<TrainingCenterItem>?> FetchAsync(CancellationToken ct)
        {
            // TODO: فراخوانی واقعی سرویس؛ نمونه:
            //   var response = await _apiClient.GetAsync<PagedResponse<TrainingCenterItem>>(
            //       "api/v1/<built-in method lower of str object at 0x7f5384a950b0>s?page={PageNumber}&size={PageSize}", ct);
            //   return new PagedResult<TrainingCenterItem>(response.Items, response.TotalCount);

            await Task.Delay(200, ct);
            var items = new List<TrainingCenterItem>();
            return new PagedResult<TrainingCenterItem>(items, 0);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// مشاهده
        /// </summary>
        private async Task openAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "مشاهده");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }


    /// <summary>
    /// مدل نمایش یک ردیف در فهرست «مرکز آموزش»
    /// </summary>
    public sealed class TrainingCenterItem
    {
        /// <summary>عنوان</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>دسته</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>مدت</summary>
        public string Duration { get; set; } = string.Empty;

        /// <summary>پیشرفت</summary>
        public string Progress { get; set; } = string.Empty;

        /// <summary>سطح رنگیِ نشان وضعیت</summary>
        public string StatusLevel { get; set; } = "Neutral";
    }
}

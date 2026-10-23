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
    /// ViewModel صفحه‌ی «شعب» — مدیریت شعب و مراکز درمانی
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/BranchesView.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/BranchesView.html</c>
    /// 🎨 View متناظر:     <c>03-views/BranchesView.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class BranchesViewModel : PageViewModelBase<BranchesItem>
    {
        public BranchesViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════



        private string _filterStatus = null;

        /// <summary>فیلتر: وضعیت</summary>
        public string FilterStatus
        {
            get => _filterStatus;
            set
            {
                if (SetProperty(ref _filterStatus, value))
                    _ = LoadAsync();
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _newCommand;

        /// <summary>+ شعبه جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            _newCommand ??= new AsyncRelayCommand(NewAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override async Task<PagedResult<BranchesItem>?> FetchAsync(CancellationToken ct)
        {
            // TODO: فراخوانی واقعی سرویس؛ نمونه:
            //   var response = await _apiClient.GetAsync<PagedResponse<BranchesItem>>(
            //       "api/v1/<built-in method lower of str object at 0x7f5384a95330>s?page={PageNumber}&size={PageSize}", ct);
            //   return new PagedResult<BranchesItem>(response.Items, response.TotalCount);

            await Task.Delay(200, ct);
            var items = new List<BranchesItem>();
            return new PagedResult<BranchesItem>(items, 0);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// + شعبه جدید
        /// </summary>
        private async Task newAsync()
        {
            // TODO: باز کردن فرم ایجاد از طریق IDialogService
            await Task.CompletedTask;
        }

    }


    /// <summary>
    /// مدل نمایش یک ردیف در فهرست «شعب»
    /// </summary>
    public sealed class BranchesItem
    {
        /// <summary>کد</summary>
        public string BranchCode { get; set; } = string.Empty;

        /// <summary>نام شعبه</summary>
        public string BranchName { get; set; } = string.Empty;

        /// <summary>شهر</summary>
        public string City { get; set; } = string.Empty;

        /// <summary>تلفن</summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>مدیر</summary>
        public string ManagerName { get; set; } = string.Empty;

        /// <summary>فعال</summary>
        public string IsActive { get; set; } = string.Empty;

        /// <summary>سطح رنگیِ نشان وضعیت</summary>
        public string StatusLevel { get; set; } = "Neutral";
    }
}

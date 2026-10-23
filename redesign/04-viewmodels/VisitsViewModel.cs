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
    /// ViewModel صفحه‌ی «فهرست ویزیت‌ها» — تاریخچه‌ی ویزیت‌های انجام‌شده
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/VisitsView.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/VisitsView.html</c>
    /// 🎨 View متناظر:     <c>03-views/VisitsView.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class VisitsViewModel : PageViewModelBase<VisitsItem>
    {
        public VisitsViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════



        private string _filterDoctor = null;

        /// <summary>فیلتر: پزشک</summary>
        public string FilterDoctor
        {
            get => _filterDoctor;
            set
            {
                if (SetProperty(ref _filterDoctor, value))
                    _ = LoadAsync();
            }
        }

        private DateTime? _filterFromdate = null;

        /// <summary>فیلتر: از تاریخ</summary>
        public DateTime? FilterFromdate
        {
            get => _filterFromdate;
            set
            {
                if (SetProperty(ref _filterFromdate, value))
                    _ = LoadAsync();
            }
        }

        private DateTime? _filterTodate = null;

        /// <summary>فیلتر: تا تاریخ</summary>
        public DateTime? FilterTodate
        {
            get => _filterTodate;
            set
            {
                if (SetProperty(ref _filterTodate, value))
                    _ = LoadAsync();
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _newCommand;

        /// <summary>ویزیت جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            _newCommand ??= new AsyncRelayCommand(NewAsync);

        private IAsyncRelayCommand? _openCommand;

        /// <summary>مشاهده</summary>
        public IAsyncRelayCommand OpenCommand =>
            _openCommand ??= new AsyncRelayCommand(OpenAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override async Task<PagedResult<VisitsItem>?> FetchAsync(CancellationToken ct)
        {
            // TODO: فراخوانی واقعی سرویس؛ نمونه:
            //   var response = await _apiClient.GetAsync<PagedResponse<VisitsItem>>(
            //       "api/v1/<built-in method lower of str object at 0x7f5384a95470>s?page={PageNumber}&size={PageSize}", ct);
            //   return new PagedResult<VisitsItem>(response.Items, response.TotalCount);

            await Task.Delay(200, ct);
            var items = new List<VisitsItem>();
            return new PagedResult<VisitsItem>(items, 0);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// ویزیت جدید
        /// </summary>
        private async Task newAsync()
        {
            // TODO: باز کردن فرم ایجاد از طریق IDialogService
            await Task.CompletedTask;
        }

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
    /// مدل نمایش یک ردیف در فهرست «فهرست ویزیت‌ها»
    /// </summary>
    public sealed class VisitsItem
    {
        /// <summary>تاریخ</summary>
        public string VisitDate { get; set; } = string.Empty;

        /// <summary>بیمار</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>پزشک</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>تشخیص</summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>وضعیت</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>سطح رنگیِ نشان وضعیت</summary>
        public string StatusLevel { get; set; } = "Neutral";
    }
}

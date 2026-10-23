using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Sahand.ClinicPro.Desktop.Base
{
    /// <summary>
    /// پایه‌ی ViewModel برای صفحاتِ «لیست + فیلتر + صفحه‌بندی»
    /// </summary>
    /// <typeparam name="TItem">نوع ردیفِ نمایش‌داده‌شده در جدول</typeparam>
    public abstract class PageViewModelBase<TItem> : ViewModelBase
    {
        private ObservableCollection<TItem> _items = new();
        private TItem? _selectedItem;
        private string _searchText = string.Empty;
        private int _pageNumber = 1;
        private int _pageSize = 25;
        private int _totalCount;

        protected PageViewModelBase(ILogger? logger = null) : base(logger) { }

        // ══════════ داده ══════════

        /// <summary>ردیف‌های نمایش‌داده‌شده در جدول</summary>
        public ObservableCollection<TItem> Items
        {
            get => _items;
            protected set => SetProperty(ref _items, value);
        }

        /// <summary>ردیف انتخاب‌شده</summary>
        public TItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(CanEdit));
                    OnPropertyChanged(nameof(CanDelete));
                    EditCommand.NotifyCanExecuteChanged();
                    DeleteCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>آیا ردیفی انتخاب شده است</summary>
        public bool HasSelection => SelectedItem is not null;

        /// <summary>آیا دکمه‌ی ویرایش فعال است</summary>
        public virtual bool CanEdit => HasSelection;

        /// <summary>آیا دکمه‌ی حذف فعال است</summary>
        public virtual bool CanDelete => HasSelection;

        /// <summary>آیا جدول خالی است (برای نمایش وضعیت تهی)</summary>
        public bool IsEmpty => !IsBusy && Items.Count == 0;

        // ══════════ جستجو و صفحه‌بندی ══════════

        /// <summary>متن جستجو</summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    _ = SearchDebouncedAsync();
            }
        }

        /// <summary>شماره‌ی صفحه‌ی فعلی (شروع از ۱)</summary>
        public int PageNumber
        {
            get => _pageNumber;
            set
            {
                if (SetProperty(ref _pageNumber, value < 1 ? 1 : value))
                    _ = LoadAsync();
            }
        }

        /// <summary>تعداد ردیف در هر صفحه</summary>
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value < 1 ? 25 : value))
                    _ = LoadAsync();
            }
        }

        /// <summary>تعداد کل ردیف‌ها (بدون احتساب صفحه‌بندی)</summary>
        public int TotalCount
        {
            get => _totalCount;
            protected set
            {
                if (SetProperty(ref _totalCount, value))
                    OnPropertiesChanged(nameof(TotalPages), nameof(PageSummary));
            }
        }

        /// <summary>تعداد کل صفحات</summary>
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        /// <summary>متن خلاصه‌ی صفحه‌بندی (مانند «نمایش ۱ تا ۲۵ از ۱۲۴۵»)</summary>
        public string PageSummary
        {
            get
            {
                if (TotalCount == 0) return "موردی یافت نشد";
                var from = (PageNumber - 1) * PageSize + 1;
                var to = Math.Min(PageNumber * PageSize, TotalCount);
                return $"نمایش {from} تا {to} از {TotalCount}";
            }
        }

        // ══════════ فرمان‌ها ══════════

        /// <summary>بارگذاری مجدد داده‌ها</summary>
        public IAsyncRelayCommand LoadCommand => new AsyncRelayCommand(LoadAsync);

        /// <summary>ایجاد مورد جدید</summary>
        public virtual IAsyncRelayCommand CreateCommand => new AsyncRelayCommand(CreateAsync);

        /// <summary>ویرایش مورد انتخاب‌شده</summary>
        public IAsyncRelayCommand EditCommand => new AsyncRelayCommand(EditAsync, () => CanEdit);

        /// <summary>حذف مورد انتخاب‌شده</summary>
        public IAsyncRelayCommand DeleteCommand => new AsyncRelayCommand(DeleteAsync, () => CanDelete);

        /// <summary>تازه‌سازی</summary>
        public IAsyncRelayCommand RefreshCommand => new AsyncRelayCommand(LoadAsync);

        /// <summary>رفتن به صفحه‌ی بعد</summary>
        public IRelayCommand NextPageCommand =>
            new RelayCommand(() => PageNumber++, () => PageNumber < TotalPages);

        /// <summary>رفتن به صفحه‌ی قبل</summary>
        public IRelayCommand PreviousPageCommand =>
            new RelayCommand(() => PageNumber--, () => PageNumber > 1);

        // ══════════ متدهای قابل‌بازنویسی ══════════

        /// <summary>بارگذاری داده‌ها از سرویس</summary>
        public override async Task LoadAsync()
            => await RunAsync(async ct =>
            {
                var result = await FetchAsync(ct);
                if (result is not null)
                {
                    Items = new ObservableCollection<TItem>(result.Items);
                    TotalCount = result.TotalCount;
                    OnPropertyChanged(nameof(IsEmpty));
                    NextPageCommand.NotifyCanExecuteChanged();
                    PreviousPageCommand.NotifyCanExecuteChanged();
                }
            }, "در حال بارگذاری…");

        /// <summary>
        /// واکشی واقعی داده‌ها — در کلاس فرزند پیاده‌سازی می‌شود
        /// </summary>
        protected abstract Task<PagedResult<TItem>?> FetchAsync(System.Threading.CancellationToken ct);

        /// <summary>ایجاد مورد جدید</summary>
        protected virtual Task CreateAsync() => Task.CompletedTask;

        /// <summary>ویرایش مورد انتخاب‌شده</summary>
        protected virtual Task EditAsync() => Task.CompletedTask;

        /// <summary>حذف مورد انتخاب‌شده</summary>
        protected virtual Task DeleteAsync() => Task.CompletedTask;

        /// <summary>جستجو با تأخیر (Debounce) برای جلوگیری از فراخوانی‌های مکرر</summary>
        private async Task SearchDebouncedAsync()
        {
            await Task.Delay(350);
            if (SearchText == _searchText)
            {
                PageNumber = 1;
                await LoadAsync();
            }
        }
    }

    /// <summary>
    /// نتیجه‌ی صفحه‌بندی‌شده برای صفحات لیستی
    /// </summary>
    public sealed record PagedResult<T>(System.Collections.Generic.IReadOnlyList<T> Items, int TotalCount);
}

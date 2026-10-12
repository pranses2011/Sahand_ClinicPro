using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Sahand.ClinicPro.Desktop.Base
{
    /// <summary>
    /// پایه‌ی مشترک برای همه‌ی ViewModelهای طراحیِ جدید
    /// </summary>
    /// <remarks>
    /// این کلاس از <see cref="ObservableObject"/> ارث می‌برد، بنابراین
    /// در کلاس‌های فرزند می‌توان از ویژگی‌های مولدِ کدِ
    /// <c>[ObservableProperty]</c> و <c>[RelayCommand]</c> استفاده کرد.
    ///
    /// امکانات فراهم‌شده:
    /// <list type="bullet">
    /// <item>اعتبارسنجی از طریق <see cref="INotifyDataErrorInfo"/></item>
    /// <item>مدیریت یکپارچه‌ی خطا، پیام و وضعیت بارگذاری</item>
    /// <item>پشتیبانی از <see cref="CancellationToken"/> برای لغو عملیات</item>
    /// <item>قفل همزمانی برای جلوگیری از اجرای تکراری</item>
    /// </list>
    /// </remarks>
    public abstract class ViewModelBase : ObservableObject, INotifyDataErrorInfo, IDisposable
    {
        private readonly Dictionary<string, List<string>> _errors = new();
        private readonly SemaphoreSlim _busyLock = new(1, 1);
        private CancellationTokenSource? _cts;
        private bool _disposed;

        private bool _isBusy;
        private string _busyMessage = string.Empty;
        private string? _errorMessage;
        private string? _successMessage;

        protected ViewModelBase(ILogger? logger = null) => Logger = logger;

        /// <summary>ثبت‌کننده‌ی رویدادها (اختیاری)</summary>
        protected ILogger? Logger { get; }

        // ══════════════════════════════════════════════════════════
        //  وضعیت بارگذاری
        // ══════════════════════════════════════════════════════════

        /// <summary>آیا عملیاتی در حال اجراست</summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    OnPropertyChanged(nameof(IsNotBusy));
            }
        }

        /// <summary>معکوسِ <see cref="IsBusy"/> — برای اتصال راحت‌تر در XAML</summary>
        public bool IsNotBusy => !IsBusy;

        /// <summary>پیام نمایش‌داده‌شده در حین بارگذاری</summary>
        public string BusyMessage
        {
            get => _busyMessage;
            private set => SetProperty(ref _busyMessage, value);
        }

        // ══════════════════════════════════════════════════════════
        //  پیام‌ها
        // ══════════════════════════════════════════════════════════

        /// <summary>پیام خطای سطح صفحه</summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>پیام موفقیتِ سطح صفحه</summary>
        public string? SuccessMessage
        {
            get => _successMessage;
            private set => SetProperty(ref _successMessage, value);
        }

        /// <summary>نمایش پیام خطا</summary>
        protected void ShowError(string message)
        {
            ErrorMessage = message;
            SuccessMessage = null;
        }

        /// <summary>نمایش پیام موفقیت</summary>
        protected void ShowSuccess(string message)
        {
            SuccessMessage = message;
            ErrorMessage = null;
        }

        /// <summary>پاک کردن پیام‌ها</summary>
        protected void ClearMessages()
        {
            ErrorMessage = null;
            SuccessMessage = null;
        }

        /// <summary>اطلاع‌رسانی تغییر چند ویژگی</summary>
        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var name in propertyNames)
                OnPropertyChanged(name);
        }

        // ══════════════════════════════════════════════════════════
        //  اجرای ایمن
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// اجرای عملیات ناهمزمان با مدیریت خودکارِ وضعیت بارگذاری و خطا
        /// </summary>
        protected async Task<bool> RunAsync(
            Func<CancellationToken, Task> operation,
            string busyMessage = "در حال پردازش…",
            bool suppressConcurrent = true)
        {
            if (suppressConcurrent && IsBusy) return false;
            if (suppressConcurrent) await _busyLock.WaitAsync();

            _cts = new CancellationTokenSource();
            IsBusy = true;
            BusyMessage = busyMessage;
            ClearMessages();

            try
            {
                await operation(_cts.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                ShowError("عملیات لغو شد");
                return false;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "خطا در عملیات: {Operation}", busyMessage);
                ShowError(DescribeException(ex));
                return false;
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
                if (suppressConcurrent) _busyLock.Release();
                _cts?.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// اجرای عملیات ناهمزمانِ دارای نتیجه با مدیریت خطا
        /// </summary>
        protected async Task<T?> RunAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            string busyMessage = "در حال بارگذاری…",
            bool suppressConcurrent = true)
        {
            if (suppressConcurrent && IsBusy) return default;
            if (suppressConcurrent) await _busyLock.WaitAsync();

            _cts = new CancellationTokenSource();
            IsBusy = true;
            BusyMessage = busyMessage;
            ClearMessages();

            try
            {
                return await operation(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                ShowError("عملیات لغو شد");
                return default;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "خطا در عملیات: {Operation}", busyMessage);
                ShowError(DescribeException(ex));
                return default;
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
                if (suppressConcurrent) _busyLock.Release();
                _cts?.Dispose();
                _cts = null;
            }
        }

        /// <summary>لغو عملیات در حال اجرا</summary>
        /// <summary>
        /// بارگذاریِ اولیهٔ داده‌ها.
        /// </summary>
        /// <remarks>
        /// پیاده‌سازیِ پیش‌فرض کاری انجام نمی‌دهد؛ کلاس‌های فرزند آن را
        /// بازنویسی می‌کنند تا داده‌های مورد نیازِ نخستین نمایش را بیاورند.
        /// فراخوانیِ این متد در سازنده امن است.
        /// </remarks>
        public virtual Task LoadAsync() => Task.CompletedTask;

        protected void CancelOperation() => _cts?.Cancel();

        /// <summary>تبدیل استثنا به پیام قابل‌فهم برای کاربر</summary>
        protected virtual string DescribeException(Exception ex) => ex switch
        {
            TimeoutException => "زمان ارتباط با سرور به پایان رسید. لطفاً دوباره تلاش کنید.",
            System.Net.Http.HttpRequestException => "ارتباط با سرور برقرار نشد. اتصال شبکه را بررسی کنید.",
            UnauthorizedAccessException => "دسترسی به این بخش مجاز نیست.",
            InvalidOperationException => ex.Message,
            _ => "خطای غیرمنتظره رخ داد. در صورت تکرار با پشتیبانی تماس بگیرید."
        };

        // ══════════════════════════════════════════════════════════
        //  اعتبارسنجی
        // ══════════════════════════════════════════════════════════

        /// <inheritdoc />
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        /// <inheritdoc />
        public bool HasErrors => _errors.Count > 0;

        /// <inheritdoc />
        public System.Collections.IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return _errors.Values.SelectMany(v => v).ToList();

            return _errors.TryGetValue(propertyName!, out var list)
                ? list
                : Array.Empty<string>();
        }

        /// <summary>افزودن خطای اعتبارسنجی برای یک ویژگی</summary>
        protected void AddError(string message, [CallerMemberName] string? propertyName = null)
        {
            if (propertyName is null) return;

            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(message))
            {
                _errors[propertyName].Add(message);
                OnErrorsChanged(propertyName);
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        /// <summary>پاک کردن خطاهای یک ویژگی</summary>
        protected void ClearErrors([CallerMemberName] string? propertyName = null)
        {
            if (propertyName is null) return;
            if (_errors.Remove(propertyName))
            {
                OnErrorsChanged(propertyName);
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        /// <summary>پاک کردن همه‌ی خطاها</summary>
        protected void ClearAllErrors()
        {
            var keys = _errors.Keys.ToList();
            _errors.Clear();
            foreach (var key in keys)
                OnErrorsChanged(key);
            OnPropertyChanged(nameof(HasErrors));
        }

        private void OnErrorsChanged(string propertyName)
            => ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

        // ══════════════════════════════════════════════════════════
        //  IDisposable
        // ══════════════════════════════════════════════════════════

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>آزادسازی منابع</summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _cts?.Dispose();
                _busyLock.Dispose();
            }
            _disposed = true;
        }
    }
}

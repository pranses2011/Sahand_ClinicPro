using System.Net.Http;
using System.Windows.Threading;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// پیاده‌سازی اتصال بلادرنگ — دو HubConnection هم‌زمان به QueueHub و NotificationHub (گام R1)
/// اتصال خودکار با تلاش مجدد تصادفی؛ توکن JWT از query string خوانده می‌شود (الگوی رسمی SignalR)
/// </summary>
public sealed class RealtimeClientService : IRealtimeClient, IAsyncDisposable
{
    /// <summary>مدیر توکن — خواندن توکن دسترسی کاربر جاری</summary>
    private readonly ITokenManager _tokens;

    /// <summary>پیکربندی — نشانی پایه API از ApiSettings:BaseUrl</summary>
    private readonly IConfiguration _configuration;

    /// <summary>لاگر</summary>
    private readonly ILogger<RealtimeClientService>? _logger;

    /// <summary>قفل هم‌زمانی برای جلوگیری از اتصال موازی</summary>
    private readonly object _gate = new();

    /// <summary>اتصال به Hub صف</summary>
    private HubConnection? _queueConnection;

    /// <summary>اتصال به Hub اعلان</summary>
    private HubConnection? _notificationConnection;

    /// <summary>آیا اتصال برقرار است</summary>
    public bool IsConnected =>
        _queueConnection?.State == HubConnectionState.Connected &&
        _notificationConnection?.State == HubConnectionState.Connected;

    /// <summary>وضعیت اتصال برای StatusBar</summary>
    public string StatusText =>
        IsConnected ? "بلادرنگ فعال" :
        _queueConnection?.State is HubConnectionState.Reconnecting or HubConnectionState.Connecting
            ? "در حال اتصال بلادرنگ…" : "بلادرنگ قطع";

    /// <summary>رویداد تغییر صف</summary>
    public event EventHandler<(string Reason, long? BranchId)>? QueueChanged;

    /// <summary>رویداد دریافت اعلان</summary>
    public event EventHandler<(string Title, string Message, string Type)>? NotificationReceived;

    /// <summary>رویداد هشدار سراسری</summary>
    public event EventHandler<(string Title, string Message)>? BroadcastAlert;

    /// <summary>رویداد تغییر وضعیت اتصال</summary>
    public event EventHandler? ConnectionStateChanged;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public RealtimeClientService(ITokenManager tokens, IConfiguration configuration,
        ILogger<RealtimeClientService>? logger = null)
    {
        _tokens = tokens;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// برقراری اتصال به هر دو Hub — ساخت اتصال تازه در هر بار ورود
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (IsConnected) return;
        }

        var token = _tokens.AccessToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger?.LogDebug("اتصال بلادرنگ آغاز نشد — توکنی در دسترس نیست");
            return;
        }

        // نشانی Hub از نشانی REST ساخته می‌شود:
        // http://host:port/api/v1 → http://host:port (ریشه سایت) + /hubs/queue
        var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/api/v1";
        var serverRoot = TrimToServerRoot(baseUrl);

        _queueConnection = BuildConnection(serverRoot + "/hubs/queue", token);
        _notificationConnection = BuildConnection(serverRoot + "/hubs/notifications", token);

        // ── رویدادهای Hub صف ──
        _queueConnection.On<QueuePayload>("queue-changed", payload =>
            RaiseOnUi(() => QueueChanged?.Invoke(this, (payload?.Reason ?? "update", payload?.BranchId))));
        _queueConnection.On<AlertPayload>("broadcast-alert", payload =>
            RaiseOnUi(() => BroadcastAlert?.Invoke(this, (payload?.Title ?? string.Empty,
                payload?.Message ?? string.Empty))));

        // ── رویدادهای Hub اعلان ──
        _notificationConnection.On<NotificationPayload>("notification-received", payload =>
            RaiseOnUi(() => NotificationReceived?.Invoke(this, (payload?.Title ?? string.Empty,
                payload?.Message ?? string.Empty, payload?.Type ?? "Info"))));
        _notificationConnection.On<AlertPayload>("broadcast-alert", payload =>
            RaiseOnUi(() => BroadcastAlert?.Invoke(this, (payload?.Title ?? string.Empty,
                payload?.Message ?? string.Empty))));

        // گزارش وضعیت به StatusBar پس از هر تغییر
        HookStateChanges(_queueConnection);
        HookStateChanges(_notificationConnection);

        try
        {
            await _queueConnection.StartAsync(cancellationToken);
            await _notificationConnection.StartAsync(cancellationToken);
            _logger?.LogInformation("اتصال بلادرنگ به هر دو Hub برقرار شد");
        }
        catch (Exception ex)
        {
            // قطعی سرور غیرمهلک است — اتصال خودکار بعداً دوباره تلاش می‌کند
            _logger?.LogWarning(ex, "برقراری اتصال بلادرنگ ناموفق بود — تلاش مجدد خودکار");
        }

        ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// قطع اتصال هر دو Hub — هنگام خروج
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_queueConnection is not null)
                await _queueConnection.DisposeAsync().ConfigureAwait(false);
            if (_notificationConnection is not null)
                await _notificationConnection.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "قطع اتصال بلادرنگ با خطا همراه بود");
        }
        finally
        {
            _queueConnection = null;
            _notificationConnection = null;
            ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>آزادسازی منابع</summary>
    public async ValueTask DisposeAsync() => await StopAsync();

    /// <summary>
    /// ساخت اتصال با توکن query string + تلاش مجدد خودکار + پینگ
    /// </summary>
    private static HubConnection BuildConnection(string url, string token)
        => new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                // توکن JWT از query string — مسیر رسمی احراز هویت Hub در ASP.NET Core
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                // گواهی خودامضای محیط توسعه
                options.HttpMessageHandlerFactory = handler =>
                {
                    if (handler is HttpClientHandler clientHandler)
                        clientHandler.ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    return handler;
                };
            })
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30) })
            .Build();

    /// <summary>قلاب تغییر وضعیت — گزارش به StatusBar</summary>
    private void HookStateChanges(HubConnection connection)
    {
        connection.Reconnecting += _ =>
        {
            ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        };
        connection.Reconnected += _ =>
        {
            ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        };
        connection.Closed += _ =>
        {
            ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// اجرای رویداد روی رشته UI — همه مشترکان ViewModel روی رشته اصلی هستند
    /// </summary>
    private static void RaiseOnUi(Action raise)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
            raise();
        else
            dispatcher.BeginInvoke(raise, DispatcherPriority.Background);
    }

    /// <summary>
    /// تبدیل نشانی REST به ریشه سرور — «http://h:5000/api/v1/» → «http://h:5000»
    /// </summary>
    private static string TrimToServerRoot(string baseUrl)
    {
        try
        {
            var uri = new Uri(baseUrl.TrimEnd('/'));
            return $"{uri.Scheme}://{uri.Authority}";
        }
        catch (UriFormatException)
        {
            return baseUrl.TrimEnd('/');
        }
    }

    // ── قرارداد پیام‌های سیم — هم‌نام با QueueChangedMessage/… در سمت سرور ──

    /// <summary>بار پیام تغییر صف</summary>
    private sealed class QueuePayload
    {
        /// <summary>علت تغییر</summary>
        public string? Reason { get; set; }

        /// <summary>شعبه</summary>
        public long? BranchId { get; set; }
    }

    /// <summary>بار پیام اعلان کاربر</summary>
    private sealed class NotificationPayload
    {
        /// <summary>عنوان</summary>
        public string? Title { get; set; }

        /// <summary>متن</summary>
        public string? Message { get; set; }

        /// <summary>نوع</summary>
        public string? Type { get; set; }
    }

    /// <summary>بار پیام هشدار سراسری</summary>
    private sealed class AlertPayload
    {
        /// <summary>عنوان</summary>
        public string? Title { get; set; }

        /// <summary>متن</summary>
        public string? Message { get; set; }
    }
}

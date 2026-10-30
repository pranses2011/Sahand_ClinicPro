using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class ApiClientBranchSafetyTests
{
    private sealed class Tokens : ITokenManager
    {
        public string? AccessToken { get; private set; } = "old-token";
        public string? RefreshToken { get; private set; } = "refresh";
        public DateTime? ExpiresAt => null;
        public string? Username => "user";
        public string? DisplayName => null;
        public IReadOnlyList<string> Roles => Array.Empty<string>();
        public IReadOnlyList<string> Permissions => Array.Empty<string>();
        public bool IsAdmin => false;
        public bool HasValidToken => true;
        public event EventHandler? AuthStateChanged { add { } remove { } }
        public void SaveTokens(string a, string r, int e, string u) { AccessToken = a; RefreshToken = r; }
        public void SaveTokens(string a, string r, int e, string u, string? d, IReadOnlyList<string>? roles, IReadOnlyList<string>? permissions) => SaveTokens(a, r, e, u);
        public void UpdateProfile(string? d, IReadOnlyList<string>? r, IReadOnlyList<string>? p) { }
        public void Clear() { AccessToken = null; RefreshToken = null; }
    }
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> action) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(action(request));
    }
    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body) };
    private static ApiClient Create(Handler handler) => new(new HttpClient(handler), new Tokens(), new ConfigurationBuilder().Build());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Refresh_retry_keeps_original_branch_even_if_selected_branch_changes(bool noOriginalBranch)
    {
        ApiClient? api = null;
        var branches = new List<string?>();
        var handler = new Handler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("auth/refresh"))
            {
                api!.BranchId = 22;
                return Json(HttpStatusCode.OK, "{\"isSuccess\":true,\"data\":{\"accessToken\":\"new-token\",\"refreshToken\":\"new-refresh\",\"expiresIn\":100,\"username\":\"user\"}}");
            }
            branches.Add(request.Headers.TryGetValues("X-Branch-Id", out var values) ? values.Single() : null);
            return branches.Count == 1 ? Json(HttpStatusCode.Unauthorized, "{}") : Json(HttpStatusCode.OK, "{\"data\":123}");
        });
        api = Create(handler);
        api.BranchId = noOriginalBranch ? null : 11;
        var result = await api.PostAsync<Envelope>("receptions/idempotent", new { });
        Assert.Equal(123, result!.Data);
        Assert.Equal(new[] { noOriginalBranch ? null : "11", noOriginalBranch ? null : "11" }, branches);
        Assert.Equal(22, api.BranchId);
    }

    [Fact]
    public async Task Anonymous_requests_do_not_send_existing_bearer_token()
    {
        var auth = new List<string?>();
        var handler = new Handler(request => { auth.Add(request.Headers.Authorization?.ToString()); return Json(HttpStatusCode.OK, "{\"isSuccess\":true,\"data\":123}"); });
        var api = Create(handler);
        await api.GetAnonymousAsync<Envelope>("public/test");
        await api.PostAnonymousAsync("public/test", new { });
        await api.GetAsync<Envelope>("private/test");
        Assert.Null(auth[0]); Assert.Null(auth[1]); Assert.Equal("Bearer old-token", auth[2]);
    }
    private sealed class EmptyInvoiceStore : IInvoiceRecoveryStore
    {
        public PendingInvoice? Load(string server, long patientId) => null;
        public void Reserve(PendingInvoice pending) { }
        public void Clear(PendingInvoice pending) { }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Invoice_quote_cannot_be_used_after_branch_changes(bool changeDuringQuote)
    {
        ApiClient? api = null;
        var invoicePosts = 0;
        var handler = new Handler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("invoices/preview"))
            {
                if (changeDuringQuote) api!.BranchId = 22;
                return Json(HttpStatusCode.OK, "{\"data\":{\"subTotal\":100,\"discountAmount\":0,\"insuranceShare\":0,\"tax\":0,\"patientPayable\":100,\"quoteVersion\":\"test\"}}");
            }
            if (path.EndsWith("invoices/idempotent")) invoicePosts++;
            return Json(HttpStatusCode.OK, "{\"data\":[]}");
        });
        api = Create(handler);
        api.BranchId = 11;
        var vm = new InvoiceFormViewModel(api, new FakeSnackbar(), new EmptyInvoiceStore())
        { Patient = new QuickPatient(7, "بیمار", "", "", null, null, null) };
        vm.Items.Add(new InvoiceItemVM { ServiceId = 1, Name = "خدمت", UnitPrice = 100, Quantity = 1 });
        await vm.CalculateQuoteCommand.ExecuteAsync(null);
        if (!changeDuringQuote) api.BranchId = 22;
        Assert.False(vm.TryCreateDraftPreview(out _));
        await vm.SaveAndPrintCommand.ExecuteAsync(null);
        Assert.Equal(0, invoicePosts);
    }

    public sealed class Envelope { public int Data { get; set; } }
}

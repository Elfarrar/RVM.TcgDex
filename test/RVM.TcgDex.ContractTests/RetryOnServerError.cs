using System.Net;

namespace RVM.TcgDex.ContractTests;

/// <summary>
/// The TCGdex load balancer sometimes answers 503 "No available server" for a few minutes. That is
/// an outage, not a contract change: retry before letting it open an issue.
/// </summary>
internal sealed class RetryOnServerError() : DelegatingHandler(new HttpClientHandler())
{
    private const int Attempts = 5;

    public static HttpClient Client() => new(new RetryOnServerError()) { Timeout = TimeSpan.FromMinutes(2) };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if ((int)response.StatusCode < 500 || attempt == Attempts)
                return response;

            response.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(10 * attempt), cancellationToken);
        }
    }
}

using System.Net;
using System.Text;

namespace RVM.TcgDex.Tests;

/// <summary>
/// Stands in for the TCGdex API: answers known paths with recorded JSON and fails the test on any
/// request nobody expected.
/// </summary>
internal sealed class FakeApi : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _routes = new(StringComparer.Ordinal);

    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>Answers <paramref name="pathAndQuery"/> (e.g. <c>/v2/en/cards/swsh3-136</c>) with a fixture file.</summary>
    public FakeApi Returns(string pathAndQuery, string fixture, HttpStatusCode status = HttpStatusCode.OK) =>
        ReturnsBody(pathAndQuery, Fixture(fixture), status);

    public FakeApi ReturnsBody(string pathAndQuery, string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _routes[pathAndQuery] = (status, body);
        return this;
    }

    public TCGdex Client(Language language = Language.En) =>
        new(new HttpClient(this), new TcgDexOptions { Language = language });

    public static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name), Encoding.UTF8);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request);

        var key = request.RequestUri!.PathAndQuery;
        if (!_routes.TryGetValue(key, out var route))
            throw new InvalidOperationException($"Unexpected request: {key}");

        return Task.FromResult(new HttpResponseMessage(route.Status)
        {
            Content = new StringContent(route.Body, Encoding.UTF8, "application/json"),
        });
    }
}

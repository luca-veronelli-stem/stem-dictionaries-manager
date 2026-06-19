using System.Net;
using API.RateLimiting;

namespace Tests.Integration.API;

/// <summary>
/// Verifies the #14 fix: a single API key that exceeds its per-key fixed-window
/// budget is throttled with <c>429 Too Many Requests</c>, while requests within
/// the budget succeed. Before the fix the API had no rate limiting, so a key
/// could issue unbounded requests.
/// </summary>
public class RateLimitingTests
{
    [Fact]
    public async Task BusinessEndpoint_ExceedingPerKeyBudget_Returns429()
    {
        using var factory = new DevHostApiFactory();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", DevHostApiFactory.ApiKey);

        var statuses = new List<HttpStatusCode>();
        for (int i = 0; i < ApiRateLimiting.PermitLimit + 5; i++)
        {
            HttpResponseMessage response = await client.GetAsync("/api/devices");
            statuses.Add(response.StatusCode);
        }

        Assert.Equal(HttpStatusCode.OK, statuses[0]);
        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
    }
}

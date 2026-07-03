using PoeMarketWatcher.Infrastructure.Ggg;

namespace PoeMarketWatcher.Infrastructure.Tests.Ggg;

public class RateLimitHeadersTests
{
    [Fact]
    public void ParseExtractsNamedLimitStatePolicyRulesAndRetryAfter()
    {
        var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Rate-Limit-Policy"] = ["ladder"],
            ["X-Rate-Limit-Rules"] = ["Client"],
            ["X-Rate-Limit-Client"] = ["20:5:60"],
            ["X-Rate-Limit-Client-State"] = ["7:5:0"],
            ["Retry-After"] = ["12"]
        };

        var parsed = RateLimitHeaders.Parse(headers);

        Assert.Equal("ladder", parsed.Policy);
        Assert.Equal(["Client"], parsed.Rules);
        Assert.Equal(TimeSpan.FromSeconds(12), parsed.RetryAfter);

        var client = Assert.Single(parsed.Limits);
        Assert.Equal("Client", client.Key);
        Assert.Equal([new RateLimitLimit(20, 5, 60)], client.Value.Limits);
        Assert.Equal([new RateLimitState(7, 5, 0)], client.Value.States);
    }

    [Fact]
    public void ParsePreservesMultipleLimitAndStateTriplesForNamedRule()
    {
        var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Rate-Limit-Rules"] = ["Client"],
            ["X-Rate-Limit-Client"] = ["20:5:60,100:60:300"],
            ["X-Rate-Limit-Client-State"] = ["1:5:0,10:60:0"]
        };

        var parsed = RateLimitHeaders.Parse(headers);

        var client = Assert.Single(parsed.Limits);
        Assert.Equal("Client", client.Key);
        Assert.Equal(
            [new RateLimitLimit(20, 5, 60), new RateLimitLimit(100, 60, 300)],
            client.Value.Limits);
        Assert.Equal(
            [new RateLimitState(1, 5, 0), new RateLimitState(10, 60, 0)],
            client.Value.States);
    }
}

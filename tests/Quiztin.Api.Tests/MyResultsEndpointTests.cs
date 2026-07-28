using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Quiztin.Api.Tests;

/// <summary>
/// The student's own results endpoint (spec 0011). GET /api/results/mine is [Authorize]d and takes
/// no id — the caller comes from the token — so an anonymous request is 401 and never reaches the
/// data (AC-1). Runs through the real host pipeline. The data shape and the aggregation are proven
/// by the service level MyResultsTests against a real Postgres; those need a database, which this
/// routing factory deliberately does not provide.
/// </summary>
public sealed class MyResultsEndpointTests : IClassFixture<QuiztinApiFactory>
{
    private readonly HttpClient _client;

    public MyResultsEndpointTests(QuiztinApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Without_a_token_my_results_is_401_and_never_reaches_the_data()
    {
        var response = await _client.GetAsync("/api/results/mine");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

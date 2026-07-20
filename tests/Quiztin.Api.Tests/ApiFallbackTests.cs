using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Quiztin.Api.Tests;

/// <summary>
/// Pins how unmatched paths resolve at the host's tail, where the SPA fallback lives.
/// The property under test: an /api path that no controller registered must surface as
/// a 404, not as the 200 HTML shell. That distinction is what makes a broken deployment
/// visible — a module that loses its AddApplicationPart yields unregistered routes on an
/// otherwise green build (spec 0008 flagged exactly this risk), and a status-code smoke
/// test can only catch it if the miss is not masked as a success.
/// </summary>
public sealed class ApiFallbackTests : IClassFixture<QuiztinApiFactory>
{
    private readonly HttpClient _client;

    public ApiFallbackTests(QuiztinApiFactory factory) => _client = factory.CreateClient();

    [Theory]
    [InlineData("/api/does-not-exist-at-all")]
    [InlineData("/api/classrooms/nonsense-route")]
    [InlineData("/api/")]
    public async Task Unknown_api_path_returns_a_json_404_never_the_spa_shell(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        // The house error envelope, so apiFetch surfaces a clean ApiError rather than
        // failing Zod validation on an HTML body (a confusing ContractError).
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Not found.", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Unknown_api_path_returns_404_for_non_get_verbs_too()
    {
        var response = await _client.PostAsync("/api/does-not-exist-at-all", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// The guard must not shadow real routes: a registered endpoint still reaches
    /// authentication, so an anonymous call is a 401 and not the catch-all's 404.
    /// This is the assertion that would fail if the catch-all ever outranked a
    /// controller route.
    /// </summary>
    [Theory]
    [InlineData("/api/classrooms/owned")]
    [InlineData("/api/classrooms/enrolled")]
    public async Task Registered_endpoint_still_challenges_unauthenticated_callers(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Health_endpoint_is_unaffected()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// SPA deep links are client-side routes with no server endpoint, so they must keep
    /// falling through to the bootstrap shell — the behaviour the /api guard is carved
    /// out of, and the thing a too-broad fix would break.
    /// </summary>
    [Theory]
    [InlineData("/dashboard")]
    [InlineData("/join/ABC123")]
    [InlineData("/classrooms/0b6b2a5e-6d3f-4a1a-9b1e-2f9b0c7f1d55")]
    public async Task Spa_deep_links_still_serve_the_shell(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(QuiztinApiFactory.SpaShell, await response.Content.ReadAsStringAsync());
    }

    /// <summary>The explicit "/" endpoint still outranks the fallback (spec 0003, AC-14).</summary>
    [Fact]
    public async Task Root_still_serves_html()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }
}

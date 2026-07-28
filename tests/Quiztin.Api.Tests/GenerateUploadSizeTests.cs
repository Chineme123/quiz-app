using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Quiztin.Api.Tests;

/// <summary>
/// The source-material upload cap on the quiz generation endpoint (spec 0009, AC-7). An upload over
/// the 5 MB cap must be rejected with 413 Payload Too Large, not the 400 that multipart form binding
/// produces on its own. The RejectOversizedUpload resource filter enforces this before binding, so
/// the verdict is reached without a quiz or a database. Runs through the real host pipeline.
/// </summary>
public sealed class GenerateUploadSizeTests : IClassFixture<QuiztinApiFactory>
{
    private const long Cap = 5 * 1024 * 1024;
    private readonly HttpClient _client;

    public GenerateUploadSizeTests(QuiztinApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task An_upload_over_the_cap_is_rejected_with_413_not_400()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MintToken());

        using var content = OversizedUpload();
        var response = await _client.PostAsync($"/api/quizzes/{Guid.NewGuid()}/generate", content);

        // 413, not the 400 the multipart binder would give (the defect), and not 401 (the token
        // authorizes). The filter short-circuits before binding, so no quiz or DB is needed.
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Without_a_token_an_oversized_body_is_401_and_never_read()
    {
        // Authorization runs before the size filter, so an anonymous oversized post is 401. This
        // pins that reaching the 413 requires an authenticated caller.
        using var content = OversizedUpload();
        var response = await _client.PostAsync($"/api/quizzes/{Guid.NewGuid()}/generate", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static MultipartFormDataContent OversizedUpload()
    {
        var content = new MultipartFormDataContent { { new StringContent("Networking"), "Topic" } };
        var file = new ByteArrayContent(new byte[Cap + 1024]);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", "too-big.pdf");
        return content;
    }

    // A valid HS256 JWT for the test host (secret + issuer + audience match QuiztinApiFactory /
    // appsettings). [Authorize] only needs an authenticated identity, so no particular claim beyond
    // a valid signature, issuer, audience, and lifetime is required to reach the size filter.
    private static string MintToken()
    {
        static string B64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var header = B64Url(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = B64Url(Encoding.UTF8.GetBytes(
            $"{{\"nameid\":\"{Guid.NewGuid()}\",\"iss\":\"quiztin\",\"aud\":\"quiztin\",\"iat\":{now},\"nbf\":{now},\"exp\":{now + 3600}}}"));
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("quiztin-tests-signing-key-not-a-real-secret"));
        var signature = B64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{header}.{payload}")));
        return $"{header}.{payload}.{signature}";
    }
}

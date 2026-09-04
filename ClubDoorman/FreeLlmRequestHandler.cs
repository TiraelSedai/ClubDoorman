using System.Text;
using System.Text.Json.Nodes;

namespace ClubDoorman;

internal sealed class FreeLlmRequestHandler() : DelegatingHandler(new HttpClientHandler())
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // The SDK's structured-output helper does not expose chat template options.
        if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.EndsWith("/chat/completions", StringComparison.Ordinal))
        {
            using var original = request.Content!;
            var body = JsonNode.Parse(await original.ReadAsStringAsync(cancellationToken))!.AsObject();
            body["chat_template_kwargs"] = new JsonObject { ["enable_thinking"] = false };
            request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

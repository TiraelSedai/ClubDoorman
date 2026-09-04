using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using tryAGI.OpenAI;

namespace ClubDoorman.Test;

/// <summary>
/// The free endpoint gets one attempt and no more. Polly is not enough for that: the SDK client retries three times
/// on its own unless it is told not to, so this pins the behaviour against an SDK upgrade quietly restoring it.
/// </summary>
public class FreeLlmClientRetryTests
{
    [Test]
    public async Task FreeClient_DisablesThinkingAndPreservesStructuredOutput()
    {
        var port = FreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var settings = new Config.FreeLlmSettings(new Uri($"http://127.0.0.1:{port}/v1"), "sk-test", "some/model");
        using var client = AiChecks.BuildFreeClient(settings);

        var completion = client.Chat.CreateChatCompletionAsAsync<AiChecks.SpamProbability>(
            messages: ["проверка".AsUserMessage()],
            model: settings.Model,
            strict: true,
            cancellationToken: timeout.Token
        );
        var context = await listener.GetContextAsync().WaitAsync(timeout.Token);
        using var body = await JsonDocument.ParseAsync(context.Request.InputStream, cancellationToken: timeout.Token);
        var response = Encoding.UTF8.GetBytes(
            """
            {"id":"chatcmpl-test","object":"chat.completion","created":0,"model":"some/model","choices":[{"index":0,"message":{"role":"assistant","content":"{\"Probability\":0.9,\"Reason\":\"spam\"}"},"finish_reason":"stop"}]}
            """
        );
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = response.Length;
        await context.Response.OutputStream.WriteAsync(response, timeout.Token);
        context.Response.Close();
        var result = await completion;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Request.Url!.AbsolutePath, Is.EqualTo("/v1/chat/completions"));
            Assert.That(context.Request.Headers["Authorization"], Is.EqualTo("Bearer sk-test"));
            Assert.That(body.RootElement.GetProperty("chat_template_kwargs").GetProperty("enable_thinking").GetBoolean(), Is.False);
            Assert.That(body.RootElement.GetProperty("model").GetString(), Is.EqualTo(settings.Model));
            Assert.That(body.RootElement.GetProperty("messages")[0].GetProperty("content").GetString(), Is.EqualTo("проверка"));
            Assert.That(body.RootElement.GetProperty("response_format").GetProperty("type").GetString(), Is.EqualTo("json_schema"));
            Assert.That(
                body.RootElement.GetProperty("response_format").GetProperty("json_schema").GetProperty("strict").GetBoolean(),
                Is.True
            );
            Assert.That(result.Value1, Is.Not.Null);
            Assert.That(result.Value1!.Probability, Is.EqualTo(0.9));
            Assert.That(result.Value1.Reason, Is.EqualTo("spam"));
        }
    }

    [Test]
    public async Task FreeClient_AsksAFailingEndpointExactlyOnce()
    {
        var port = FreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var requests = 0;
        var serving = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync();
                }
                catch (Exception)
                {
                    return;
                }
                Interlocked.Increment(ref requests);
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
        });

        var settings = new Config.FreeLlmSettings(new Uri($"http://127.0.0.1:{port}/v1"), "sk-test", "some/model");
        using var client = AiChecks.BuildFreeClient(settings);

        Assert.That(
            async () =>
                await client.Chat.CreateChatCompletionAsAsync<AiChecks.SpamProbability>(
                    messages: ["проверка".AsUserMessage()],
                    model: settings.Model,
                    strict: true
                ),
            Throws.InstanceOf<Exception>(),
            "a 500 has to surface, not be swallowed"
        );

        listener.Stop();
        await serving;
        Assert.That(requests, Is.EqualTo(1));
    }

    private static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }
}

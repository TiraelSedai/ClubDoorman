using System.Net;
using System.Net.Sockets;
using tryAGI.OpenAI;

namespace ClubDoorman.Test;

/// <summary>
/// The free endpoint gets one attempt and no more. Polly is not enough for that: the SDK client retries three times
/// on its own unless it is told not to, so this pins the behaviour against an SDK upgrade quietly restoring it.
/// </summary>
public class FreeLlmClientRetryTests
{
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

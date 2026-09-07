using Telegram.Bot.Types;

namespace ClubDoorman;

internal sealed class BioInviteTracker(TimeProvider clock)
{
    private static readonly TimeSpan Window = TimeSpan.FromDays(1);
    private readonly Dictionary<string, Dictionary<(long UserId, long ChatId), Observation>> _invites = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();
    private DateTimeOffset _nextSweep;

    internal sealed record Warning(Message Message, string Reason)
    {
        internal List<Delivery> Deliveries { get; } = [];
    }

    internal sealed record Result(bool IsShared, IReadOnlyList<Warning> Warnings);

    internal enum DeliveryStatus
    {
        Ready,
        Reserved,
        Sent,
    }

    internal sealed class Delivery
    {
        public DeliveryStatus Status { get; set; }
    }

    private sealed class Observation(Message message, DateTimeOffset seenAt, TelegramInvitePreview invite)
    {
        public Message Message { get; set; } = message;
        public DateTimeOffset SeenAt { get; set; } = seenAt;
        public Delivery Delivery { get; set; } = new();
        public string InviteHash { get; } = invite.Hash;
        public string? Fingerprint { get; } = invite.Content?.Fingerprint;
    }

    public Result Observe(Message message, string? bio, IReadOnlyList<TelegramInvitePreview>? previews = null)
    {
        ArgumentNullException.ThrowIfNull(message.From);
        previews ??= TelegramInvitePreviews.ExtractHashes(bio).Select(hash => new TelegramInvitePreview(hash, null)).ToArray();
        var now = clock.GetUtcNow();
        var cutoff = now - Window;
        var warnings = new Dictionary<(long ChatId, int MessageId), Warning>();
        var shared = false;

        lock (_lock)
        {
            if (now >= _nextSweep)
            {
                foreach (var (hash, observations) in _invites.ToArray())
                {
                    RemoveExpired(observations, cutoff);
                    if (observations.Count == 0)
                        _invites.Remove(hash);
                }
                _nextSweep = now + TimeSpan.FromMinutes(15);
            }

            foreach (var invite in previews)
            {
                var exactKey = $"invite:{invite.Hash}";
                var exact = GetObservations(exactKey, cutoff);
                var userKey = (message.From.Id, message.Chat.Id);
                exact.TryGetValue(userKey, out var current);
                var contentGroup = invite.Content == null ? null : GetObservations($"content:{invite.Content.Fingerprint}", cutoff);
                Observation? previousContentMatch = null;
                contentGroup?.TryGetValue(userKey, out previousContentMatch);
                if (current == null || current.Fingerprint != invite.Content?.Fingerprint)
                    current = new Observation(Snapshot(message), now, invite) { Delivery = current?.Delivery ?? new Delivery() };
                else
                {
                    current.Message = Snapshot(message);
                    current.SeenAt = now;
                }

                if (current.Delivery.Status == DeliveryStatus.Ready && previousContentMatch != null)
                    current.Delivery = previousContentMatch.Delivery;

                // Share warning state across exact-token and content matches, so the same evidence is not reported twice.
                var groups = new List<(Dictionary<(long UserId, long ChatId), Observation> Entries, bool ByContent)> { (exact, false) };
                if (contentGroup != null)
                    groups.Add((contentGroup, true));
                foreach (var (observations, byContent) in groups)
                {
                    observations[userKey] = current;
                    var other = observations.Values.FirstOrDefault(entry => entry.Message.From!.Id != message.From.Id);
                    if (other == null)
                        continue;

                    shared = true;
                    foreach (var entry in observations.Values.Where(entry => entry.Delivery.Status == DeliveryStatus.Ready))
                    {
                        var counterpart = entry.Message.From!.Id == message.From.Id ? other : current;
                        var reason = byContent
                            ? $"Вероятно, приглашения ведут в одну группу: совпадают название, описание и фото: {invite.Content!.Title}"
                            : "Одинаковая ссылка-приглашение в описаниях разных пользователей за последние 24 часа";
                        reason +=
                            $"{Environment.NewLine}Приглашение пользователя: https://t.me/+{entry.InviteHash}"
                            + $"{Environment.NewLine}Приглашение другого пользователя: https://t.me/+{counterpart.InviteHash}"
                            + $"{Environment.NewLine}Другой пользователь: {Utils.FullName(counterpart.Message.From!)}"
                            + $"{Environment.NewLine}{Utils.LinkToMessage(counterpart.Message.Chat, counterpart.Message.Id)}";
                        var warningKey = (entry.Message.Chat.Id, entry.Message.Id);
                        if (!warnings.TryGetValue(warningKey, out var warning))
                        {
                            warning = new Warning(entry.Message, reason);
                            warnings.Add(warningKey, warning);
                        }
                        warning.Deliveries.Add(entry.Delivery);
                        entry.Delivery.Status = DeliveryStatus.Reserved;
                    }
                }
            }
        }

        return new Result(shared, warnings.Values.ToArray());
    }

    public async Task Report(Result result, Func<Warning, Task> send)
    {
        var next = 0;
        try
        {
            for (; next < result.Warnings.Count; next++)
            {
                var warning = result.Warnings[next];
                await send(warning);
                lock (_lock)
                {
                    foreach (var delivery in warning.Deliveries)
                        delivery.Status = DeliveryStatus.Sent;
                }
            }
        }
        finally
        {
            // Release the failed warning and the unattempted tail, including when the caller was cancelled.
            lock (_lock)
            {
                foreach (var warning in result.Warnings.Skip(next))
                {
                    foreach (var delivery in warning.Deliveries)
                        delivery.Status = DeliveryStatus.Ready;
                }
            }
        }
    }

    private Dictionary<(long UserId, long ChatId), Observation> GetObservations(string key, DateTimeOffset cutoff)
    {
        if (!_invites.TryGetValue(key, out var observations))
            _invites[key] = observations = [];
        RemoveExpired(observations, cutoff);
        return observations;
    }

    private static void RemoveExpired(Dictionary<(long UserId, long ChatId), Observation> observations, DateTimeOffset cutoff)
    {
        foreach (var (key, entry) in observations.ToArray())
        {
            if (entry.SeenAt <= cutoff)
                observations.Remove(key);
        }
    }

    // Do not retain the update's reply chain, media or other nested Telegram objects for a day.
    private static Message Snapshot(Message message) =>
        new()
        {
            Id = message.Id,
            Chat = new Chat
            {
                Id = message.Chat.Id,
                Title = message.Chat.Title,
                Username = message.Chat.Username,
                Type = message.Chat.Type,
            },
            From = new User
            {
                Id = message.From!.Id,
                FirstName = message.From.FirstName,
                LastName = message.From.LastName,
                Username = message.From.Username,
            },
            Text = Utils.TextWithLinks(message),
            EditDate = message.EditDate,
        };
}

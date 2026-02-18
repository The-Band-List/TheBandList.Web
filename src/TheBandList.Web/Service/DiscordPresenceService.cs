using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;

namespace TheBandList.Web.Service
{
    public class DiscordPresenceService : BackgroundService
    {
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _cfg;
        private readonly ConcurrentDictionary<ulong, UserStatus> _statuses = new();

        public bool IsReady { get; private set; }
        public event Action<ulong, string>? StatusChanged;

        public DiscordPresenceService(IConfiguration cfg)
        {
            _cfg = cfg;

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds |
                                 GatewayIntents.GuildMembers |
                                 GatewayIntents.GuildPresences,
                AlwaysDownloadUsers = true
            });

            _client.Ready += async () =>
            {
                IsReady = true;
                foreach (var g in _client.Guilds)
                {
                    try { await g.DownloadUsersAsync(); } catch { }
                    foreach (var u in g.Users) SetStatus(u.Id, u.Status);
                }
            };

            _client.GuildAvailable += g =>
            {
                foreach (var u in g.Users) SetStatus(u.Id, u.Status);
                return Task.CompletedTask;
            };

            _client.PresenceUpdated += (user, before, after) =>
            {
                SetStatus(user.Id, after.Status);
                return Task.CompletedTask;
            };
        }

        public async Task<string> GetStatusAsync(ulong userId, CancellationToken ct = default)
        {
            if (!IsReady) return "offline";

            foreach (var g in _client.Guilds)
            {
                var u = g.GetUser(userId);
                if (u != null) return Map(u.Status);
            }

            foreach (var g in _client.Guilds)
            {
                try
                {
                    await g.DownloadUsersAsync();
                    var u = g.GetUser(userId);
                    if (u != null) return Map(u.Status);
                }
                catch { }
            }

            if (_statuses.TryGetValue(userId, out var s)) return Map(s);
            return "offline";
        }

        public bool TryGetCachedStatus(ulong userId, out string status)
        {
            if (_statuses.TryGetValue(userId, out var s)) { status = Map(s); return true; }
            status = "offline"; return false;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var token = FirstNonEmpty(
                "DISCORD_BOT_TOKEN",
                "Authentication:Discord:BotToken",
                "Discord:BotToken"
            ) ?? throw new InvalidOperationException("Token bot manquant.");

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            try { await Task.Delay(Timeout.Infinite, ct); }
            catch (TaskCanceledException) { }
        }

        private void SetStatus(ulong userId, UserStatus newStatus)
        {
            if (_statuses.TryGetValue(userId, out var old) && old == newStatus) return;
            _statuses[userId] = newStatus;
            StatusChanged?.Invoke(userId, Map(newStatus));
        }

        private static string Map(UserStatus s) => s switch
        {
            UserStatus.Online => "online",
            UserStatus.Idle => "idle",
            UserStatus.DoNotDisturb => "dnd",
            _ => "offline"
        };

        private string? FirstNonEmpty(params string[] keys)
        {
            foreach (var k in keys)
            {
                var v = _cfg[k];
                if (!string.IsNullOrWhiteSpace(v)) return v;
                v = Environment.GetEnvironmentVariable(k);
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
            return null;
        }
    }
}

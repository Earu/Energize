using Discord.Rest;
using Discord.WebSocket;
using Energize.Interfaces.Services;
using Energize.Toolkit;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
{
    [Service("EventLogs")]
    public class LogEvent : IServiceImplementation
    {
        public LogEvent(EnergizeClient eclient)
        {
            this.Client     = eclient.DiscordClient;
            this.RestClient = eclient.DiscordRestClient;
            this.Prefix     = eclient.Prefix;
            this.Log        = eclient.Logger;
        }

        public DiscordRestClient    RestClient { get; }
        public DiscordShardedClient Client     { get; }
        public string               Prefix     { get; }
        public Logger               Log        { get; }

        public bool AreLogsEnabled(SocketGuild guild)
            => guild.Roles.Any(x => x.Name == "EnergizeLogs");

        [Event("ShardReady")]
        public async Task OnShardReady(DiscordSocketClient clientshard)
            => this.Log.Nice("Shard", ConsoleColor.Magenta, $"Shard {clientshard.ShardId} ready || Online on {clientshard.Guilds.Count} guilds");

        [Event("ShardDisconnected")]
        public async Task OnShardDisconnected(Exception e, DiscordSocketClient clientshard)
        {
            if (e is WebSocketException wsex && wsex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                return;
            this.Log.Nice("Shard", ConsoleColor.Red, $"Shard {clientshard.ShardId} disconnected || Offline for {clientshard.Guilds.Count} guilds\n{e}");
        }

        [Event("JoinedGuild")]
        public async Task OnJoinedGuild(SocketGuild guild)
            => this.Log.Nice("Guild", ConsoleColor.Magenta, $"Joined {guild.Name} || ID => [ {guild.Id} ]");

        [Event("LeftGuild")]
        public async Task OnLeftGuild(SocketGuild guild)
            => this.Log.Nice("Guild", ConsoleColor.Red, $"Left {guild.Name} || ID => [ {guild.Id} ]");

        public void Initialize() { }

        public Task InitializeAsync()
            => Task.CompletedTask;
    }
}

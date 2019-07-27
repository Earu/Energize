using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Energize.Essentials;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
{
    [Service("EventLogging")]
    public class EventLoggingService : ServiceImplementationBase
    {
        public EventLoggingService(EnergizeClient client)
        {
            this.Client     = client.DiscordClient;
            this.RestClient = client.DiscordRestClient;
            this.Prefix     = client.Prefix;
            this.Log        = client.Logger;
        }

        public DiscordRestClient    RestClient { get; }
        public DiscordShardedClient Client     { get; }
        public string               Prefix     { get; }
        public Logger               Log        { get; }

        /*public bool AreLogsEnabled(SocketGuild guild)
            => guild.Roles.Any(x => x.Name == "EnergizeLogs");*/

        [DiscordEvent("ShardReady")]
        public async Task OnShardReady(DiscordSocketClient clientShard)
            => this.Log.Nice("Shard", ConsoleColor.Magenta, $"Shard {clientShard.ShardId} ready || Online on {clientShard.Guilds.Count} guilds");

        [DiscordEvent("ShardDisconnected")]
        public async Task OnShardDisconnected(Exception e, DiscordSocketClient clientShard)
        {
            this.Log.LogTo("dnet_ws_closes.log", e.ToString());
            if (e is WebSocketException wsex && wsex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely) return;
            if (e is WebSocketClosedException wscex && wscex.CloseCode == 1001) return;
            this.Log.Nice("Shard", ConsoleColor.Red, $"Shard {clientShard.ShardId} disconnected || Offline for {clientShard.Guilds.Count} guilds");
        }

        [DiscordEvent("JoinedGuild")]
        public async Task OnJoinedGuild(SocketGuild guild)
            => this.Log.Nice("Guild", ConsoleColor.Magenta, $"Joined {guild.Name} || ID => [ {guild.Id} ]");

        [DiscordEvent("LeftGuild")]
        public async Task OnLeftGuild(SocketGuild guild)
            => this.Log.Nice("Guild", ConsoleColor.Red, $"Left {guild.Name} || ID => [ {guild.Id} ]");
    }
}

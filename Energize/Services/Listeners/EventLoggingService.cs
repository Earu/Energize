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

        [Event("ShardReady")]
        public async Task OnShardReady(DiscordSocketClient clientshard)
            => this.Log.Nice("Shard", ConsoleColor.Magenta, $"Shard {clientshard.ShardId} ready || Online on {clientshard.Guilds.Count} guilds");

        [Event("ShardDisconnected")]
        public async Task OnShardDisconnected(Exception e, DiscordSocketClient clientshard)
        {
            if (e is WebSocketException wsex && wsex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely) return;
            if (e is WebSocketClosedException wscex && wscex.CloseCode == 1001) return;
            this.Log.Nice("Shard", ConsoleColor.Red, $"Shard {clientshard.ShardId} disconnected || Offline for {clientshard.Guilds.Count} guilds");
        }

        [Event("JoinedGuild")]
        public async Task OnJoinedGuild(SocketGuild guild)
            => this.Log.Nice("Guild", ConsoleColor.Magenta, $"Joined {guild.Name} || ID => [ {guild.Id} ]");

        [Event("LeftGuild")]
        public async Task OnLeftGuild(SocketGuild guild)
            => this.Log.Nice("Guild", ConsoleColor.Red, $"Left {guild.Name} || ID => [ {guild.Id} ]");
    }
}

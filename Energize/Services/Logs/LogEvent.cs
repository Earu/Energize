using Discord.Rest;
using Discord.WebSocket;
using Energize.Toolkit;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Energize.Services.Logs
{
    [Service("EventLogs")]
    public class LogEvent
    {
        public LogEvent(EnergizeClient eclient)
        {
            this.Client     = eclient.Discord;
            this.RESTClient = eclient.DiscordREST;
            this.Prefix     = eclient.Prefix;
            this.Log        = eclient.Logger;
        }

        public DiscordRestClient    RESTClient { get; }
        public DiscordShardedClient Client     { get; }
        public string               Prefix     { get; }
        public Logger               Log        { get; }

        public bool AreLogsEnabled(SocketGuild guild)
            => guild.Roles.Any(x => x.Name == "EnergizeLogs");

        [Event("ShardReady")]
        public async Task OnShardReady(DiscordSocketClient clientshard)
            => this.Log.Nice("Shard", ConsoleColor.Magenta, $"Shard {clientshard.ShardId} ready || Online on {clientshard.Guilds.Count} guilds");

        [Event("ShardDisconnected")]
        public async Task OnShardDisconnected(Exception e,DiscordSocketClient clientshard)
            => this.Log.Nice("Shard", ConsoleColor.Red, $"Shard {clientshard.ShardId} disconnected || Offline for {clientshard.Guilds.Count} guilds\n{e}");

        [Event("JoinedGuild")]
        public async Task OnJoinedGuild(SocketGuild guild)
            => this.Log.Nice("Guild", ConsoleColor.Magenta, $"Joined {guild.Name} || ID => [ {guild.Id} ]");

        [Event("LeftGuild")]
        public async Task OnLeftGuild(SocketGuild guild)
            => this.Log.Nice("Guild", ConsoleColor.Red, $"Left {guild.Name} || ID => [ {guild.Id} ]");
    }
}

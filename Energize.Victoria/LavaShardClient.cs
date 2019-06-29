using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Victoria
{
    /// <summary>
    /// Represents a <see cref="DiscordShardedClient"/> with Lavalink server.
    /// </summary>
    public sealed class LavaShardClient : LavaBaseClient
    {
        /// <summary>
        /// Starts websocket connection with Lavalink server once <see cref="DiscordSocketClient"/> hits ready event.
        /// </summary>
        /// <param name="shardedClient"><see cref="DiscordSocketClient"/></param>
        /// <param name="configuration"><see cref="Configuration"/></param>
        public Task StartAsync(DiscordShardedClient shardedClient, Configuration configuration = null)
        {
            shardedClient.ShardDisconnected += this.OnShardDisconnected;
            return this.InitializeAsync(shardedClient, configuration);
        }

        private async Task OnShardDisconnected(Exception exception, DiscordSocketClient socketClient)
        {
            if (this.Configuration.PreservePlayers)
                return;

            foreach (var guild in socketClient.Guilds)
            {
                if (!this.Players.TryRemove(guild.Id, out var player))
                    continue;

                await player.DisposeAsync().ConfigureAwait(false);
            }

            this.Players.Clear();
            this.ShadowLog?.WriteLog(LogSeverity.Error, "Shards disconnecting. Disposing all connected players.", exception);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Energize.Interfaces.Services;
using Energize.Toolkit;
using Victoria;

namespace Energize.Services.Listeners
{
    public class MusicPlayer : IServiceImplementation
    {
        private readonly DiscordShardedClient _Client;
        private readonly LavaShardClient _LavaClient;
        private readonly Dictionary<ulong, LavaPlayer> _Players;

        private LavaRestClient _LavaRestClient;

        public MusicPlayer(EnergizeClient client)
        {
            this._Client = client.DiscordClient;
            this._LavaClient = new LavaShardClient();
            this._Players = new Dictionary<ulong, LavaPlayer>();
        }

        public async Task ConnectAsync(IVoiceChannel vc)
        {
            LavaPlayer ply = await this._LavaClient.ConnectAsync(vc);
            this._Players.Add(vc.GuildId, ply);
        }

        public async Task<LavaPlayer> GetOrCreatePlayer(IVoiceChannel vc)
        {
            if (!this._Players.ContainsKey(vc.GuildId))
                await this.ConnectAsync(vc);

            return this._Players[vc.GuildId];
        }

        public void Initialize() { }

        public async Task InitializeAsync()
        {
            Configuration lavalinkconfig = new Configuration
            {
                ReconnectInterval = TimeSpan.FromSeconds(15.0),
                ReconnectAttempts = 3,
                Host = Config.LVK_HOST,
                Port = Config.LVK_PORT,
                Password = Config.LVK_PASSWORD,
            };

            this._LavaRestClient = new LavaRestClient(lavalinkconfig);
            await this._LavaClient.StartAsync(this._Client, lavalinkconfig);
        }
    }
}
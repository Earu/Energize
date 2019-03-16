using Discord.WebSocket;
using Energize.Interfaces.Services.Listeners;
using Energize.Toolkit;
using System;
using System.Threading.Tasks;
using Victoria;

namespace Energize.Services.Listeners
{
    [Service("Music")]
    public class MusicPlayer : IMusicPlayerService
    {
        private readonly DiscordShardedClient _Client;

        private bool _Initialized;

        public MusicPlayer(EnergizeClient client)
        {
            this._Initialized = false;
            this._Client = client.DiscordClient;
            this.LavaClient = new LavaShardClient();
        }

        public LavaShardClient LavaClient { get; }
        public LavaRestClient LavaRestClient { get; private set; }

        public void Initialize() { }

        public Task InitializeAsync()
            => Task.CompletedTask;

        [Event("ShardReady")]
        public async Task OnShardReady(DiscordSocketClient clientshard)
        {
            if (this._Initialized) return;
            Configuration config = new Configuration
            {
                ReconnectInterval = TimeSpan.FromSeconds(15.0),
                ReconnectAttempts = 3,
                Host = Config.LVK_HOST,
                Port = Config.LVK_PORT,
                Password = Config.LVK_PASSWORD,
                SelfDeaf = false,
            };

            this.LavaRestClient = new LavaRestClient(config);
            await this.LavaClient.StartAsync(this._Client, config);
            this._Initialized = true;
        }
    }
}
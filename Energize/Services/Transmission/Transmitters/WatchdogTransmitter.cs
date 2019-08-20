using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.Services.Listeners;
using Octovisor.Client;

namespace Energize.Services.Transmission.Transmitters
{
    internal class WatchdogTransmitter : BaseTransmitter
    {
        private readonly ServiceManager ServiceManager;
        private readonly MessageSender MessageSender;
        private readonly DiscordShardedClient DiscordClient;

        internal WatchdogTransmitter(EnergizeClient client, OctoClient octoClient) : base(client, octoClient)
        {
            this.ServiceManager = client.ServiceManager;
            this.MessageSender = client.MessageSender;
            this.DiscordClient = client.DiscordClient;
        }

        internal override void Initialize()
            => this.Client.OnTransmission<string>("reconnect", this.OnReconnectRequested);

        private async void OnReconnectRequested(RemoteProcess proc, string publicIp)
        {
            if (string.IsNullOrWhiteSpace(publicIp) || proc.Name != "Watchdog") return;
            this.Log($"Remote nodes reconnected with IP: {publicIp}");

            IMusicPlayerService music = this.ServiceManager.GetService<IMusicPlayerService>("Music");
            await music.DisconnectAllPlayersAsync("Network issues detected, disconnecting to prevent further bugs");
            await music.StartAsync(publicIp);

            SocketChannel bugChan = this.DiscordClient.GetChannel(Config.Instance.Discord.BugReportChannelID);
            if (bugChan != null)
                await this.MessageSender.SendWarningAsync(bugChan, "network errors", "Reconnected to lavalink remote server");
        }
    }
}

using Energize.Interfaces.Services.Listeners;
using Octovisor.Client;
using System;

namespace Energize.Services.Transmission.Transmitters
{
    internal class WatchdogTransmitter : BaseTransmitter
    {
        private readonly ServiceManager ServiceManager;

        internal WatchdogTransmitter(EnergizeClient client, OctoClient octoClient) : base(client, octoClient)
        {
            this.ServiceManager = client.ServiceManager;
        }

        internal override void Initialize()
            => this.Client.OnTransmission<string>("reconnect", this.OnReconnectRequested);

        private async void OnReconnectRequested(RemoteProcess proc, string publicIp)
        {
            if (string.IsNullOrWhiteSpace(publicIp) || proc.Name != "Watchdog") return;
            this.Logger.Nice("IPC", ConsoleColor.Yellow, $"Remote nodes re-connected with IP: {publicIp}");
            IMusicPlayerService music = this.ServiceManager.GetService<IMusicPlayerService>("Music");
            await music.DisconnectAllPlayersAsync("Network issues detected, disconnecting to prevent further bugs");
            await music.StartAsync(publicIp);
        }
    }
}

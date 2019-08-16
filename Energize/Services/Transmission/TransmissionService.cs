using Energize.Essentials;
using Energize.Services.Transmission.Transmitters;
using Octovisor.Client;
using Octovisor.Messages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energize.Services.Transmission
{
    [Service("Transmission")]
    public class TransmissionService : ServiceImplementationBase
    {
        private readonly OctoClient OctoClient;
        private readonly Logger Logger;
        private readonly List<BaseTransmitter> Transmitters;

        public TransmissionService(EnergizeClient client)
        {
            OctovisorConfig config = Config.Instance.Octovisor;
            OctoConfig octoConfig = new OctoConfig
            {
                Address = config.Address,
                Port = config.Port,
                ProcessName = config.ProcessName,
                Token = config.Token,
            };

            this.Logger = client.Logger;
            this.OctoClient = new OctoClient(octoConfig);
            this.OctoClient.Log += log =>
            {
                if (log.Severity == LogSeverity.Info)
                    this.Logger.Nice("Octo", ConsoleColor.Magenta, log.Content);
            };

            this.Transmitters = new List<BaseTransmitter>
            {
                new WebsiteTransmitter(client, this.OctoClient),
                new WatchdogTransmitter(client, this.OctoClient)
            };
        }

        public override async Task InitializeAsync()
        {
            try
            {
                await this.OctoClient.ConnectAsync();
                this.Transmitters.ForEach(transmitter => transmitter.Initialize());
            }
            catch (Exception ex)
            {
                this.Logger.Nice("Octo", ConsoleColor.Red, ex.Message);
            }
        }
    }
}

using Energize.Essentials;
using Energize.Interfaces.Services;
using Octovisor.Client;
using System;
using System.Threading.Tasks;

namespace Energize.Services.Transmission
{
    [Service("Transmission")]
    public class TransmissionService : ServiceImplementationBase, IServiceImplementation
    {
        private readonly OctoClient _OctoClient;
        private readonly Logger _Logger;

        public TransmissionService(EnergizeClient client)
        {
            OctovisorConfig config = Essentials.Config.Instance.Octovisor;

            Octovisor.Client.Config octoconfig = new Octovisor.Client.Config
            {
                Address = config.Address,
                Port = config.Port,
                ProcessName = config.ProcessName,
                Token = config.Token,
            };

            this._Logger = client.Logger;
            this._OctoClient = new OctoClient(octoconfig);
            this._OctoClient.Log += log => this._Logger.Nice("Octovisor", ConsoleColor.Magenta, log);
        }

        public override async Task InitializeAsync()
        {
            try
            {
                await this._OctoClient.ConnectAsync();
            }
            catch(Exception ex)
            {
                this._Logger.Nice("Octovisor", ConsoleColor.Red, ex.Message);
            }
        }
    }
}

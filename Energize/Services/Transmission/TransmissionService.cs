using Energize.Interfaces.Services;
using Octovisor.Client;
using System.Threading.Tasks;

namespace Energize.Services.Transmission
{
    [Service("Transmission")]
    public class TransmissionService : ServiceImplementationBase, IServiceImplementation
    {
        private readonly OctoClient _OctoClient;
        public TransmissionService()
        {
            Essentials.OctovisorConfig config = Essentials.Config.Instance.Octovisor;
            Config octoconfig = new Config
            {
                Address = config.Address,
                Port = config.Port,
                ProcessName = config.ProcessName,
                Token = config.Token,
            };

            this._OctoClient = new OctoClient(octoconfig);
        }

        public override async Task InitializeAsync()
            => await this._OctoClient.ConnectAsync();
    }
}

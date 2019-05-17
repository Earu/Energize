using Energize.Essentials;
using Energize.Interfaces.Services;
using Energize.Services.Listeners;
using Energize.Services.Transmission.TransmissionModels;
using Octovisor.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Energize.Services.Transmission
{
    [Service("Transmission")]
    public class TransmissionService : ServiceImplementationBase, IServiceImplementation
    {
        private readonly OctoClient OctoClient;
        private readonly Logger Logger;
        private readonly ServiceManager ServiceManager;

        public TransmissionService(EnergizeClient client)
        {
            this.ServiceManager = client.ServiceManager;
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
            this.OctoClient.Log += log => this.Logger.Nice("Octovisor", ConsoleColor.Magenta, log);
            this.OctoClient.OnTransmission<object, IEnumerable<Command>>("commands", (proc, _) =>
            {
                var commandService = this.ServiceManager.GetService<CommandHandlingService>("Commands");
                var cmds = commandService.RegisteredCommands.ToList().Select(kv => Command.ToModel(kv.Value));
                return cmds;
            });
        }

        public override async Task InitializeAsync()
        {
            try
            {
                await this.OctoClient.ConnectAsync();
            }
            catch(Exception ex)
            {
                this.Logger.Nice("Octovisor", ConsoleColor.Red, ex.Message);
            }
        }
    }
}

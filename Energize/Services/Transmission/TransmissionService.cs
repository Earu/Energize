using Energize.Essentials;
using Energize.Interfaces.Services;
using Energize.Services.Listeners;
using Energize.Services.Transmission.TransmissionModels;
using Octovisor.Client;
using Octovisor.Messages;
using System;
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
            this.OctoClient.Log += log =>
            {
                if (log.Severity == LogSeverity.Info)
                    this.Logger.Nice("Octovisor", ConsoleColor.Magenta, log.Content);
            };

            this.OctoClient.OnTransmission<object, CommandInformation>("commands", (proc, _) =>
            {
                var commandService = this.ServiceManager.GetService<CommandHandlingService>("Commands");
                var cmdInfo = new CommandInformation
                {
                    BotMention = client.DiscordClient.CurrentUser.ToString(),
                    Prefix = client.Prefix,
                    Commands = commandService.RegisteredCommands.ToList().Select(kv => Command.ToModel(kv.Value)).ToList()
                };

                return cmdInfo;
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

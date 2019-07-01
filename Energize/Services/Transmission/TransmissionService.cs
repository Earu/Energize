using Discord.WebSocket;
using Energize.Essentials;
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
    public class TransmissionService : ServiceImplementationBase
    {
        private readonly OctoClient OctoClient;
        private readonly string Prefix;
        private readonly DiscordShardedClient DiscordClient;
        private readonly Logger Logger;
        private readonly ServiceManager ServiceManager;

        public TransmissionService(EnergizeClient client)
        {
            this.Prefix = client.Prefix;
            this.DiscordClient = client.DiscordClient;
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
        }

        public override async Task InitializeAsync()
        {
            try
            {
                await this.OctoClient.ConnectAsync();

                this.OctoClient.OnTransmission<object, CommandInformation>("commands", (proc, _) =>
                {
                    CommandHandlingService commandService = this.ServiceManager.GetService<CommandHandlingService>("Commands");
                    this.Logger.Nice("IPC", ConsoleColor.Magenta, $"Sent command information to process \'{proc}\'");
                    return new CommandInformation
                    {
                        BotMention = this.DiscordClient.CurrentUser.ToString(),
                        Prefix = this.Prefix,
                        Commands = commandService.RegisteredCommands.ToList().Select(kv => Command.ToModel(kv.Value)).ToList()
                    };
                });

                this.OctoClient.OnTransmission<object, BotInformation>("info", (proc, _) =>
                {
                    this.Logger.Nice("IPC", ConsoleColor.Magenta, $"Sent bot information to process \'{proc}\'");
                    return new BotInformation
                    {
                        ServerCount = this.DiscordClient.Guilds.Count,
                        UserCount = this.DiscordClient.Guilds.Sum(guild => guild.Users.Count),
                    };
                });
            }
            catch (Exception ex)
            {
                this.Logger.Nice("Octovisor", ConsoleColor.Red, ex.Message);
            }
        }
    }
}

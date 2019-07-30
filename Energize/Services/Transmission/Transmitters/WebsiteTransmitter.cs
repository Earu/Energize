using Discord.WebSocket;
using Energize.Services.Listeners;
using Energize.Services.Transmission.TransmissionModels;
using Octovisor.Client;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Energize.Services.Transmission.Transmitters
{
    internal class WebsiteTransmitter : BaseTransmitter
    {
        private readonly ServiceManager ServiceManager;
        private readonly DiscordShardedClient DiscordClient;
        private readonly string Prefix;

        internal WebsiteTransmitter(EnergizeClient client, OctoClient octoClient) : base(client, octoClient)
        {
            this.ServiceManager = client.ServiceManager;
            this.DiscordClient = client.DiscordClient;
        }

        internal override void Initialize()
        {
            this.Client.OnTransmission<object, CommandInformation>("commands", this.OnCommandsRequested);
            this.Client.OnTransmission<object, BotInformation>("info", this.OnInformationRequested);
            this.Client.OnTransmission("update", this.OnUpdateRequested);
        }

        private CommandInformation OnCommandsRequested(RemoteProcess proc, object _)
        {
            this.Logger.Nice("IPC", ConsoleColor.Magenta, $"Sent command information to process \'{proc}\'");

            CommandHandlingService commandService = this.ServiceManager.GetService<CommandHandlingService>("Commands");
            return new CommandInformation
            {
                BotMention = this.DiscordClient.CurrentUser.ToString(),
                Prefix = this.Prefix,
                Commands = commandService.RegisteredCommands.ToList().Select(kv => Command.ToModel(kv.Value)).ToList()
            };
        }

        private BotInformation OnInformationRequested(RemoteProcess proc, object _)
        {
            this.Logger.Nice("IPC", ConsoleColor.Magenta, $"Sent bot information to process \'{proc}\'");

            return new BotInformation
            {
                ServerCount = this.DiscordClient.Guilds.Count,
                UserCount = this.DiscordClient.Guilds.Sum(guild => guild.Users.Count),
            };
        }

        private void OnUpdateRequested(RemoteProcess proc)
        {
            this.Logger.Nice("IPC", ConsoleColor.Magenta, $"Update requested from \'{proc}\'");

            string path = Directory.GetCurrentDirectory();
            Process.Start(new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "pull https://github.com/Energizers/Energize.git",
                WorkingDirectory = path,
            });
        }
    }
}

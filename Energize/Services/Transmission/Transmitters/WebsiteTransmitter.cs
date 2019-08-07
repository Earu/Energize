using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Energize.Essentials;
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
        private readonly DiscordRestClient DiscordRestClient;
        private readonly string Prefix;
        private readonly MessageSender MessageSender;

        internal WebsiteTransmitter(EnergizeClient client, OctoClient octoClient) : base(client, octoClient)
        {
            this.ServiceManager = client.ServiceManager;
            this.DiscordClient = client.DiscordClient;
            this.DiscordRestClient = client.DiscordRestClient;
            this.Prefix = client.Prefix;
            this.MessageSender = client.MessageSender;
        }

        internal override void Initialize()
        {
            this.Client.OnTransmission<object, CommandInformation>("commands", this.OnCommandsRequested);
            this.Client.OnTransmission<object, BotInformation>("info", this.OnInformationRequested);
            this.Client.OnTransmission("update", this.OnUpdateRequested);
            this.Client.OnTransmission<DiscordBotsVote>("upvote", this.OnDiscordBotsUpvote);
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

        private async void OnUpdateRequested(RemoteProcess proc)
        {
            this.Logger.Nice("IPC", ConsoleColor.Magenta, $"Update requested from \'{proc}\'");
            SocketChannel updateChan = this.DiscordClient.GetChannel(Config.Instance.Discord.UpdateChannelID);
            if (updateChan != null)
                await this.MessageSender.Normal(updateChan, "update", "Fetched latest changes");

            string path = Directory.GetCurrentDirectory();
            string gitUrl = "https://github.com/Energizers/Energize.git";

            if (!Directory.Exists(path + "/.git"))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"clone {gitUrl}",
                    WorkingDirectory = path,
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"pull {gitUrl}",
                    WorkingDirectory = path,
                });
            }
        }

        private async void OnDiscordBotsUpvote(RemoteProcess proc, DiscordBotsVote vote)
        {
            if (vote.BotId != Config.Instance.Discord.BotID) return;

            string multiplier = vote.IsWeekend ? "(x2)" : string.Empty;
            this.Logger.Nice("IPC", ConsoleColor.Magenta, $"New upvote {multiplier} by {vote.UserId}");

            SocketChannel chan = this.DiscordClient.GetChannel(Config.Instance.Discord.FeedbackChannelID);
            if (chan != null)
            {
                RestUser user = await this.DiscordRestClient.GetUserAsync(vote.UserId);
                EmbedBuilder builder = new EmbedBuilder();
                builder
                    .WithColor(new Color(165, 28, 21))
                    .WithDescription($"💎 New upvote {multiplier}")
                    .WithField("User", user == null ? $"Unknown ({vote.UserId})" : user.ToString());

                await this.MessageSender.Send(chan, builder.Build());
            }
        }
    }
}

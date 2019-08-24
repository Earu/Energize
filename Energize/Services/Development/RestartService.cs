using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.Services.Development;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Energize.Services.Development
{
    [Service("Restart")]
    public class RestartService : ServiceImplementationBase, IRestartService
    {
        private readonly ConcurrentBag<ulong> ChannelIds;
        private readonly Logger Logger;
        private readonly MessageSender MessageSender;
        private readonly DiscordShardedClient DiscordClient;
        private readonly string LogPath;

        public RestartService(EnergizeClient client)
        {
            this.ChannelIds = new ConcurrentBag<ulong>();
            this.Logger = client.Logger;
            this.MessageSender = client.MessageSender;
            this.DiscordClient = client.DiscordClient;
            this.LogPath = "logs/restart.log";
        }

        private bool IsIdListed(ulong chanId)
        {
            foreach (ulong id in this.ChannelIds)
            {
                if (id == chanId)
                    return true;
            }

            return false;
        }

        public async Task WarnChannelAsync(IChannel chan, string message)
        {
            if (!this.IsIdListed(chan.Id))
                this.ChannelIds.Add(chan.Id);

            await this.MessageSender.SendWarningAsync(chan, "restart", message);
        }

        public async Task RestartAsync()
        {
            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");

            foreach (ulong id in this.ChannelIds)
                await File.AppendAllTextAsync(this.LogPath, $"{id}\n");

            await Task.Delay(1000);
            Process.GetCurrentProcess().Kill();
        }

        public override async Task OnReadyAsync()
        {
            if (!File.Exists(this.LogPath)) return;

            string[] lines = await File.ReadAllLinesAsync(this.LogPath);
            File.Delete(this.LogPath);

            foreach(string line in lines)
            {
                if (!ulong.TryParse(line, out ulong id))
                {
                    this.Logger.Nice("Restart", ConsoleColor.Red, $"Could not warn channel \'{line}\' that restart is done");
                    continue;
                }

                this.Logger.Nice("Restart", ConsoleColor.Yellow, $"Warning channel \'{id}\' that restart is finished");
                SocketChannel chan = this.DiscordClient.GetChannel(id);
                if (chan != null)
                    await this.MessageSender.SendGoodAsync(chan, "restart", "Done restarting");
            }
        }
    }
}

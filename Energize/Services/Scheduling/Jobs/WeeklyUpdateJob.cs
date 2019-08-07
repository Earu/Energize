using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.Services;
using Energize.Interfaces.Services.Development;
using Energize.Interfaces.Services.Listeners;
using Quartz;
using System.Threading.Tasks;

namespace Energize.Services.Scheduling.Jobs
{
    public class WeeklyUpdateJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            IServiceManager serviceManager = (IServiceManager)dataMap["ServiceManager"];
            DiscordShardedClient discordClient = (DiscordShardedClient)dataMap["DiscordClient"];

            IMusicPlayerService music = serviceManager.GetService<IMusicPlayerService>("Music");
            IRestartService restart = serviceManager.GetService<IRestartService>("Restart");

            await music.DisconnectAllPlayersAsync("Weekly update on-going, disconnecting", true);
            SocketChannel updateChan = discordClient.GetChannel(Config.Instance.Discord.UpdateChannelID);
            if (updateChan != null)
                await restart.WarnChannelAsync(updateChan, "Updating to the latest changes...");

            await restart.RestartAsync();
        }
    }
}

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
            IMusicPlayerService music = serviceManager.GetService<IMusicPlayerService>("Music");
            IRestartService restart = serviceManager.GetService<IRestartService>("Restart");
            await music.DisconnectAllPlayersAsync("Weekly update on-going, disconnecting", true);
            await restart.RestartAsync();
        }
    }
}

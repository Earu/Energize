using System.Diagnostics;
using System.Threading.Tasks;
using Energize.Interfaces.Services;
using Energize.Interfaces.Services.Listeners;
using Quartz;

namespace Energize.Services.Scheduling.Jobs
{
    public class WeeklyUpdateJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            IServiceManager serviceManager = (IServiceManager)dataMap["ServiceManager"];
            IMusicPlayerService music = serviceManager.GetService<IMusicPlayerService>("Music");
            await music.DisconnectAllPlayersAsync("Weekly update on-going, disconnecting");
            Process.GetCurrentProcess().Kill();
        }
    }
}

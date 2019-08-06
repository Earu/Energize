using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Energize.Services.Scheduling
{
    [Service("Scheduling")]
    public class SchedulingService : ServiceImplementationBase
    {
        private readonly Essentials.Logger Logger;
        private readonly JobScheduler JobSceduler;

        public SchedulingService(EnergizeClient client)
        {
            this.Logger = client.Logger;
            this.JobSceduler = new JobScheduler(client);
        }

        public override async Task InitializeAsync()
        {
            LogProvider.SetCurrentLogProvider(new SchedulerLogProvider(this.Logger));
            NameValueCollection props = new NameValueCollection
            {
                { "quartz.serializer.type", "binary" }
            };

            ISchedulerFactory factory = new StdSchedulerFactory(props);
            IScheduler scheduler = await factory.GetScheduler();
            await scheduler.Start();

            await this.JobSceduler.ScheduleWeeklyUpdateAsync(scheduler);
        }
    }
}

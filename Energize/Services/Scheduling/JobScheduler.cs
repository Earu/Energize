using Energize.Services.Scheduling.Jobs;
using Quartz;
using System;
using System.Threading.Tasks;

namespace Energize.Services.Scheduling
{
    public class JobScheduler
    {
        private readonly Essentials.Logger Logger;
        private readonly ServiceManager ServiceManager;

        public JobScheduler(EnergizeClient client)
        {
            this.Logger = client.Logger;
            this.ServiceManager = client.ServiceManager;
        }

        public async Task ScheduleWeeklyUpdateAsync(IScheduler scheduler)
        {
            JobDataMap dataMap = new JobDataMap
            {
                { "ServiceManager", this.ServiceManager },
                { "Logger", this.Logger }
            };

            IJobDetail job = JobBuilder.Create<WeeklyUpdateJob>()
                .WithIdentity("Weekly Update", "Energize")
                .SetJobData(dataMap)
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("Weekly Trigger", "Energize")
                .WithSchedule(CronScheduleBuilder.WeeklyOnDayAndHourAndMinute(DayOfWeek.Monday, 5, 0))
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }
    }
}

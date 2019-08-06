using Quartz.Logging;
using System;

namespace Energize.Services.Scheduling
{
    public class SchedulerLogProvider : ILogProvider
    {
        private readonly Essentials.Logger Logger;

        public SchedulerLogProvider(Essentials.Logger logger)
        {
            this.Logger = logger;
        }

        public Logger GetLogger(string name)
        {
            return (level, func, ex, parameters) =>
            {
                if (ex != null)
                {
                    this.Logger.Danger(ex);
                    this.Logger.LogTo("quartz.log", ex.ToString());
                }
                else if (level >= LogLevel.Info && func != null)
                {
                    string output = string.Format(func(), parameters);
                    this.Logger.LogTo("quartz.log", output);
                }

                return true;
            };
        }

        public IDisposable OpenMappedContext(string key, string value) 
            => throw new NotImplementedException();

        public IDisposable OpenNestedContext(string message) 
            => throw new NotImplementedException();
    }
}

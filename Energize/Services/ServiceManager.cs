using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Energize.Services
{
    public class EventHandlerException
    {
        public EventHandlerException(Exception ex, string fileName, string methodName, int line)
        {
            this.Error = ex;
            this.FileName = fileName;
            this.MethodName = methodName;
            this.Line = line;
        }

        public Exception Error { get; private set; }
        public string FileName { get; private set; }
        public string MethodName { get; private set; }
        public int Line { get; private set; }
    }

    public class ServiceManager : IServiceManager
    {
        private static readonly string Namespace = typeof(ServiceManager).Namespace;
        private static readonly Type DiscordShardedClientType = typeof(DiscordShardedClient);
        private static readonly Type ServiceAttributeType = typeof(ServiceAttribute);
        private static readonly Type EventAttributeType = typeof(EventAttribute);
        private static readonly EventInfo[] DiscordClientEvents = DiscordShardedClientType.GetEvents();

        private readonly Dictionary<string, IService> Services;
        private readonly IEnumerable<Type> ServiceTypes;
        private readonly EnergizeClient Client;
        private readonly Logger Logger;
        private readonly List<EventHandlerException> CaughtExceptions;

        public ServiceManager(EnergizeClient client)
        {
            this.Services = new Dictionary<string, IService>();
            this.ServiceTypes = Assembly.GetExecutingAssembly().GetTypes().Where(this.IsService);
            this.Client = client;
            this.Logger = client.Logger;
            this.CaughtExceptions = new List<EventHandlerException>();
        }

        public IEnumerable<IGrouping<Exception, EventHandlerException>> TakeCaughtExceptions()
        {
            IEnumerable<IGrouping<Exception, EventHandlerException>> exs = this.CaughtExceptions.GroupBy(x => x.Error);
            this.CaughtExceptions.Clear();

            return exs;
        }

        private bool IsService(Type type)
            => type.FullName.StartsWith(Namespace) && Attribute.IsDefined(type, ServiceAttributeType);

        private bool IsEventHandler(MethodInfo methodInfo, EventInfo eventInfo)
        {
            if (Attribute.IsDefined(methodInfo, EventAttributeType))
            {
                EventAttribute atr = methodInfo.GetCustomAttribute<EventAttribute>();
                return atr.Name.Equals(eventInfo.Name);
            }

            return false;
        }

        private static IServiceImplementation Instanciate(EnergizeClient client, Type type)
        {
            if (type.GetConstructor(new [] { typeof(EnergizeClient) }) != null)
                return (IServiceImplementation)Activator.CreateInstance(type, client);
            else
                return (IServiceImplementation)Activator.CreateInstance(type);
        }

        private void RegisterService(Type type, IServiceImplementation instance)
        {
            ServiceAttribute serverAtr = type.GetCustomAttribute<ServiceAttribute>();
            this.Services[serverAtr.Name] = new Service(serverAtr.Name, instance);
        }

        private void ContinueWithHandler(Task task)
        {
            if (!task.IsFaulted || task.Exception == null) return;

            Exception ex = task.Exception.InnerException;
            this.Logger.Danger(ex);

            StackFrame frame = new StackTrace(ex, true).GetFrame(0);
            EventHandlerException eventEx = new EventHandlerException(ex, frame.GetFileName(), frame.GetMethod().Name, frame.GetFileLineNumber());
            this.CaughtExceptions.Add(eventEx);
        }

        private void RegisterDiscordHandler(DiscordShardedClient client, EventInfo eventInfo, Type type, IServiceImplementation instance)
        {
            MethodInfo eventHandler = type.GetMethods().FirstOrDefault(methodInfo => this.IsEventHandler(methodInfo, eventInfo));
            if (eventHandler == null) return;
            
            ParameterExpression[] parameters = Array.ConvertAll(eventHandler.GetParameters(),
                param => Expression.Parameter(param.ParameterType));
            Delegate dlg = Expression.Lambda(
                eventInfo.EventHandlerType,
                Expression.Call(
                    Expression.Call(
                        Expression.Constant(instance),
                        eventHandler,
                        parameters
                    ),
                    typeof(Task).GetMethod("ContinueWith", new[] { typeof(Action<Task>) }),
                    Expression.Constant((Action<Task>)this.ContinueWithHandler)
                ),
                parameters
            ).Compile();

            eventInfo.AddEventHandler(client, dlg);
        }

        internal void InitializeServices()
        {
            foreach (Type type in this.ServiceTypes)
            {
                IServiceImplementation instance = Instanciate(this.Client, type);
                this.RegisterService(type, instance);

                foreach (EventInfo eventInfo in DiscordClientEvents)
                    this.RegisterDiscordHandler(this.Client.DiscordClient, eventInfo, type, instance);
            }

            try
            {
                foreach ((string _, IService service) in this.Services)
                    service.Instance?.Initialize();
            }
            catch(Exception ex)
            {
                this.Logger.Danger(ex);
            }
        }

        internal async Task InitializeServicesAsync()
        {
            try
            {
                foreach ((string _, IService service) in this.Services)
                {
                    if (service.Instance != null)
                        await service.Instance.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                this.Logger.Danger(ex);
            }
        }

        public T GetService<T>(string name) where T : IServiceImplementation
        {
            if(this.Services.ContainsKey(name))
                return (T)this.Services[name].Instance;

            return default;
        }
    }
}

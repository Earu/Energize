using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Energize.Services
{
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

        public ServiceManager(EnergizeClient client)
        {
            this.Services = new Dictionary<string, IService>();
            this.ServiceTypes = Assembly.GetExecutingAssembly().GetTypes().Where(this.IsService);
            this.Client = client;
            this.Logger = client.Logger;
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

        private IServiceImplementation Instanciate(EnergizeClient client, Type type)
        {
            if (type.GetConstructor(new Type[] { typeof(EnergizeClient) }) != null)
                return (IServiceImplementation)Activator.CreateInstance(type, client);
            else
                return (IServiceImplementation)Activator.CreateInstance(type);
        }

        private void RegisterService(Type type, IServiceImplementation instance)
        {
            ServiceAttribute serverAtr = type.GetCustomAttribute<ServiceAttribute>();
            this.Services[serverAtr.Name] = new Service(serverAtr.Name, instance);
        }

        private void ContinueWithHandler(Task t)
        {
            if (t.IsFaulted)
                this.Logger.Danger(t.Exception.InnerException);
        }

        private void RegisterDiscordHandler(DiscordShardedClient client, EventInfo eventInfo, Type type, IServiceImplementation instance)
        {
            MethodInfo eventHandler = type.GetMethods().FirstOrDefault(methodInfo => this.IsEventHandler(methodInfo, eventInfo));
            if (eventHandler != null)
            {
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
        }

        internal void InitializeServices()
        {
            foreach (Type type in this.ServiceTypes)
            {
                IServiceImplementation instance = this.Instanciate(this.Client, type);
                this.RegisterService(type, instance);

                foreach (EventInfo eventInfo in DiscordClientEvents)
                    this.RegisterDiscordHandler(this.Client.DiscordClient, eventInfo, type, instance);
            }

            try
            {
                foreach (KeyValuePair<string, IService> service in this.Services)
                    if (service.Value.Instance != null)
                        service.Value.Instance.Initialize();
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
                foreach (KeyValuePair<string, IService> service in this.Services)
                    if (service.Value.Instance != null)
                        await service.Value.Instance.InitializeAsync();
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
            else
                return default;
        }
    }
}

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
        private static readonly string _Namespace = typeof(ServiceManager).Namespace;
        private static readonly Type _DiscordShardedClientType = typeof(DiscordShardedClient);
        private static readonly Type _ServiceAttributeType = typeof(ServiceAttribute);
        private static readonly Type _EventAttributeType = typeof(EventAttribute);
        private static readonly EventInfo[] _DiscordClientEvents = _DiscordShardedClientType.GetEvents();

        private readonly Dictionary<string, IService> _Services;
        private readonly IEnumerable<Type> _ServiceTypes;
        private readonly EnergizeClient _Client;
        private readonly Logger _Logger;

        public ServiceManager(EnergizeClient client)
        {
            this._Services = new Dictionary<string, IService>();
            this._ServiceTypes = Assembly.GetExecutingAssembly().GetTypes().Where(this.IsService);
            this._Client = client;
            this._Logger = client.Logger;
        }

        private bool IsService(Type type)
            => type.FullName.StartsWith(_Namespace) && Attribute.IsDefined(type, _ServiceAttributeType);

        private bool IsEventHandler(MethodInfo methodinfo, EventInfo eventinfo)
        {
            if (Attribute.IsDefined(methodinfo, _EventAttributeType))
            {
                EventAttribute atr = methodinfo.GetCustomAttribute<EventAttribute>();
                return atr.Name.Equals(eventinfo.Name);
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
            ServiceAttribute servatr = type.GetCustomAttribute<ServiceAttribute>();
            this._Services[servatr.Name] = new Service(servatr.Name, instance);
        }

        private void ContinueWithHandler(Task t)
        {
            if (t.IsFaulted)
                this._Logger.Danger(t.Exception.InnerException);
        }

        private void RegisterDiscordHandler(DiscordShardedClient client, EventInfo eventinfo, Type type, IServiceImplementation instance)
        {
            MethodInfo eventhandler = type.GetMethods().FirstOrDefault(methodinfo => this.IsEventHandler(methodinfo, eventinfo));
            if (eventhandler != null)
            {
                ParameterExpression[] parameters = Array.ConvertAll(eventhandler.GetParameters(),
                    param => Expression.Parameter(param.ParameterType));
                Delegate dlg = Expression.Lambda(
                    eventinfo.EventHandlerType,
                    Expression.Call(
                        Expression.Call(
                            Expression.Constant(instance),
                            eventhandler,
                            parameters
                        ),
                        typeof(Task).GetMethod("ContinueWith", new[] { typeof(Action<Task>) }),
                        Expression.Constant((Action<Task>)this.ContinueWithHandler)
                    ),
                    parameters
                ).Compile();

                eventinfo.AddEventHandler(client, dlg);
            }
        }

        internal void InitializeServices()
        {
            foreach (Type type in this._ServiceTypes)
            {
                IServiceImplementation instance = this.Instanciate(this._Client, type);
                this.RegisterService(type, instance);

                foreach (EventInfo eventinfo in _DiscordClientEvents)
                    this.RegisterDiscordHandler(this._Client.DiscordClient, eventinfo, type, instance);
            }

            try
            {
                foreach (KeyValuePair<string, IService> service in this._Services)
                    if (service.Value.Instance != null)
                        service.Value.Instance.Initialize();
            }
            catch(Exception ex)
            {
                this._Logger.Danger(ex);
            }
        }

        internal async Task InitializeServicesAsync()
        {
            try
            {
                foreach (KeyValuePair<string, IService> service in this._Services)
                    if (service.Value.Instance != null)
                        await service.Value.Instance.InitializeAsync();
            }
            catch (Exception ex)
            {
                this._Logger.Danger(ex);
            }
        }

        public T GetService<T>(string name) where T : IServiceImplementation
        {
            if(this._Services.ContainsKey(name))
                return (T)this._Services[name].Instance;
            else
                return default;
        }
    }
}

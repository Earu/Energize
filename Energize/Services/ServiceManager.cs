using Discord.WebSocket;
using Energize.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Energize.Services
{
    public class ServiceManager : IServiceManager
    {
        private const string _Namespace = "Energize.Services";

        private static readonly Type _DiscordShardedClientType = typeof(DiscordShardedClient);
        private static readonly EventInfo[] _DiscordClientEvents = _DiscordShardedClientType.GetEvents();

        private static List<string> _MethodBlacklist = new List<string>
        {
            "ToString",
            "Equals",
            "GetHashCode",
            "GetType"
        };

        private readonly Dictionary<string, IService> _Services = new Dictionary<string, IService>();

        internal void LoadServices(EnergizeClient eclient)
        {
            Type satype = typeof(ServiceAttribute);
            Type eatype = typeof(EventAttribute);
            
            IEnumerable<Type> services = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => type.FullName.StartsWith(_Namespace) && Attribute.IsDefined(type,satype));

            foreach(Type service in services)
            {
                IServiceImplementation inst = null;
                try
                {
                    if (service.GetConstructor(new Type[] { typeof(EnergizeClient) }) != null)
                        inst = (IServiceImplementation)Activator.CreateInstance(service, eclient);
                    else
                        //Use default constructor
                        inst = (IServiceImplementation)Activator.CreateInstance(service);
                }
                catch(Exception e)
                {
                    eclient.Logger.Nice("Init", ConsoleColor.Red, $"Failed to instanciate a service: {e.Message}");
                }

                try
                {
                    inst.Initialize();
                }
                catch(Exception e)
                {
                    eclient.Logger.Nice("Init", ConsoleColor.Red, $"Couldn't initialize a service: {e.Message}");
                }

                ServiceAttribute att = service.GetCustomAttributes(satype,false).First() as ServiceAttribute;
                this._Services[att.Name] = new Service(att.Name, inst as IServiceImplementation);

                IEnumerable<MethodInfo> servmethods = service.GetMethods()
                    .Where(x => !_MethodBlacklist.Contains(x.Name));
                
                foreach(EventInfo minfo in _DiscordClientEvents)
                {
                    IEnumerable<MethodInfo> methods = servmethods
                        .Where(x => Attribute.IsDefined(x,eatype) && 
                        (x.GetCustomAttributes(eatype,false).First() as EventAttribute).Name == minfo.Name);

                    if(methods.Count() > 0)
                    {
                        MethodInfo method = methods.First();
                        try
                        {
                            EventInfo _event = _DiscordShardedClientType.GetEvent(minfo.Name);
                            _event.AddEventHandler(eclient.DiscordClient, method.CreateDelegate(_event.EventHandlerType, inst));
                        }
                        catch
                        {
                            eclient.Logger.Nice("Init", ConsoleColor.Red, att.Name 
                            + $" tried to sub to an event with wrong signature <{method.Name}>");
                        }
                    }
                }
            }
        }

        internal async Task LoadServicesAsync(EnergizeClient eclient)
        {
            foreach(KeyValuePair<string, IService> service in this._Services)
            {
                if(service.Value.Instance != null)
                {
                    try
                    {
                        await service.Value.Instance.InitializeAsync();
                    }
                    catch (Exception e)
                    {
                        eclient.Logger.Nice("Init", ConsoleColor.Red, $"<{service.Key}> something went wrong when "
                        + $"invoking InitializeAsync: {e.Message}");
                    }
                }
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

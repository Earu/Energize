using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Energize.Services
{
    internal class ServiceManager
    {
        private const String _Namespace = "Energize.Services";
        private static readonly Type _BaseServiceType = typeof(BaseService);
        private static readonly Type _DiscordSocketClientType = typeof(DiscordSocketClient);
        private static readonly MethodInfo[] _BaseServiceMethods = _BaseServiceType.GetMethods();
        private static Dictionary<string, Service> _Services = new Dictionary<string, Service>();
        private static List<string> _MethodBlacklist = new List<string>
        {
            "ToString",
            "Equals",
            "GetHashCode",
            "GetType"
        };

        public static void LoadServices(EnergizeClient eclient)
        {
            IEnumerable<Type> services = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => type.FullName.StartsWith(_Namespace) && Attribute.IsDefined(type,typeof(ServiceAttribute)));

            foreach(Type service in services)
            {
                object inst = Activator.CreateInstance(service,eclient);
                Service serv = new Service(inst);

                MethodInfo initinfo = service.GetMethod("Initialize");
                if(initinfo != null)
                {
                    initinfo.Invoke(inst, null);
                    serv.Initialized = true;
                }

                ServiceAttribute att = service.GetCustomAttributes(typeof(ServiceAttribute), false).First() as ServiceAttribute;
                serv.Name = att.Name;
                _Services[att.Name] = serv;
                
                foreach(MethodInfo minfo in _BaseServiceMethods)
                {
                    IEnumerable<MethodInfo> methods = service.GetMethods().Where(x => x.Name == minfo.Name && !_MethodBlacklist.Contains(x.Name));
                    if(methods.Count() > 0)
                    {
                        try
                        {
                            EventInfo _event = _DiscordSocketClientType.GetEvent(minfo.Name);
                            _event.AddEventHandler(eclient.Discord, methods.First().CreateDelegate(_event.EventHandlerType, inst));
                        }
                        catch
                        {
                            eclient.Log.Nice("Init", ConsoleColor.Red, att.Name + " tried to sub to an event with wrong signature <" + methods.First().Name + ">");
                        }
                    }
                }
            }
        }

        public static async Task LoadServicesAsync(EnergizeClient eclient)
        {
            foreach(KeyValuePair<string,Service> service in _Services)
            {
                MethodInfo minfo = service.Value.Instance.GetType().GetMethod("InitializeAsync");
                if(minfo != null)
                {
                    Task tinit = (Task)minfo.Invoke(service.Value.Instance, new object[] { eclient });
                    await tinit;
                }
            }
        }

        public static Service GetService(string name)
        {
            if(_Services.ContainsKey(name))
            {
                return _Services[name];
            }
            else
            {
                return null;
            }
        }
    }
}

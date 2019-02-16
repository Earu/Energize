﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Energize.Services
{
    public class ServiceManager
    {
        private const string _Namespace = "Energize.Services";

        private static readonly Type        _DiscordShardedClientType = typeof(DiscordShardedClient);
        private static readonly EventInfo[] _DiscordClientEvents      = _DiscordShardedClientType.GetEvents();

        private static Dictionary<string, Service> _Services        = new Dictionary<string, Service>();
        private static List<string>                _MethodBlacklist = new List<string>
        {
            "ToString",
            "Equals",
            "GetHashCode",
            "GetType"
        };

        public static void LoadServices(EnergizeClient eclient)
        {
            Type satype = typeof(ServiceAttribute);
            Type eatype = typeof(EventAttribute);
            
            IEnumerable<Type> services = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => type.FullName.StartsWith(_Namespace) && Attribute.IsDefined(type,satype));

            foreach(Type service in services)
            {
                object inst = null;
                bool hasconstructor = false;
                try
                {
                    if (service.GetConstructor(new Type[] { typeof(EnergizeClient) }) != null)
                    {
                        inst = Activator.CreateInstance(service, eclient);
                        hasconstructor = true;
                    }
                    else
                    {
                        //Use default constructor
                        inst = Activator.CreateInstance(service);
                    }
                }
                catch(Exception e)
                {
                    eclient.Log.Nice("Init", ConsoleColor.Red, $"Failed to instanciate a service: {e}");
                }

                Service serv = new Service(inst)
                {
                    HasConstructor = hasconstructor
                };

                MethodInfo initinfo = service.GetMethod("Initialize");
                if(initinfo != null)
                {
                    try
                    {
                        initinfo.Invoke(inst, null);
                    }
                    catch(Exception e)
                    {
                        eclient.Log.Nice("Init", ConsoleColor.Red, $"Couldn't initialize a service: {e.Message}");
                    }
                    serv.Initialized = true;
                }

                ServiceAttribute att = service.GetCustomAttributes(satype,false).First() as ServiceAttribute;
                serv.Name = att.Name;
                _Services[att.Name] = serv;
                
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
                            _event.AddEventHandler(eclient.Discord,method.CreateDelegate(_event.EventHandlerType, inst));
                        }
                        catch
                        {
                            eclient.Log.Nice("Init", ConsoleColor.Red, att.Name 
                            + " tried to sub to an event with wrong signature <" + method.Name + ">");
                        }
                    }
                }
            }
        }

        public static async Task LoadServicesAsync(EnergizeClient eclient)
        {
            foreach(KeyValuePair<string,Service> service in _Services)
            {
                if(service.Value.Instance != null)
                {
                    MethodInfo minfo = service.Value.Instance.GetType().GetMethod("InitializeAsync");
                    if (minfo != null)
                    {
                        try
                        {
                            Task tinit = (Task)minfo.Invoke(service.Value.Instance, null);
                            await tinit;
                        }
                        catch (Exception e)
                        {
                            eclient.Log.Nice("Init", ConsoleColor.Red, $"<{service.Key}> something went wrong when "
                            + $"invoking InitializeAsync: {e.Message}");
                        }
                    }
                }
            }
        }

        public static Service GetServiceHolder(string name)
            => _Services.ContainsKey(name) ? _Services[name] : null;

        public static T GetService<T>(string name)
        {
            if(_Services.ContainsKey(name))
            {
                object inst = _Services[name].Instance;
                if (inst is T) return (T)inst;
                else return default;
            }
            else
            {
                return default;
            }
        }
    }
}
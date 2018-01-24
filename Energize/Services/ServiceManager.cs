using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Discord.WebSocket;

namespace Energize.Services
{
    internal class ServiceManager
    {
        private const String Namespace = nameof ( Energize.Services );
        private static readonly Type BaseServiceType = typeof ( BaseService );
        private static readonly Type DiscordSocketClientType = typeof ( DiscordSocketClient );
        private static readonly MethodInfo[] BaseServiceMethods = BaseServiceType.GetMethods ( );

        public static void LoadServices ( DiscordSocketClient client )
        {
            IEnumerable<Type> services = typeof ( ServiceManager )
                .Assembly
                .GetTypes ( )
                .Where ( type => type.FullName.StartsWith ( Namespace ) && type.IsSubclassOf ( BaseServiceType ) && type != BaseServiceType );

            foreach ( Type service in services )
            {
                foreach ( MethodInfo method in BaseServiceMethods )
                {
                    if ( method.IsDefined ( service ) )
                    {
                        EventInfo @event = DiscordSocketClientType.GetEvent ( method.Name );
                        @event.AddEventHandler ( client, method.CreateDelegate ( @event.EventHandlerType ) );
                    }
                }
            }
        }
    }
}

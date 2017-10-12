using System;
using System.Threading.Tasks;
using DSharpPlus;
using EBotDiscord;

namespace EBot
{
    class Program
    {
        
        static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            EBotClient client = new EBotClient(EBotCredentials.TOKEN_DEV, "$^");
            client.TryConnect();

            await Task.Delay(-1);
        }
    }
}

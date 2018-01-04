using System.Threading.Tasks;
using System;

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
            await EBotConfig.Load();
            await EBotData.Load();

            EBotClient client = new EBotClient(EBotConfig.TOKEN_MAIN, "x");

            await client.InitializeAsync();
            await Task.Delay(-1);
        }
    }
}

using System.Threading.Tasks;

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

            EBotClient client = new EBotClient(EBotConfig.TOKEN_DEV, "_");

            await client.InitializeAsync();
            await Task.Delay(-1);
        }
    }
}

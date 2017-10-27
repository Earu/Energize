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
            await EBotCredentials.Load();

            EBotClient client = new EBotClient(EBotCredentials.TOKEN_DEV, "$^");

            await client.TryConnect();
            await Task.Delay(-1);
        }
    }
}

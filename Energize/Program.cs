using Energize.Toolkit;
using System.Threading.Tasks;

namespace Energize
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            await Config.Load();
            await StaticData.Load();

            EnergizeClient client = new EnergizeClient(Config.TOKEN_DEV, "]");

            await client.InitializeAsync();
            await Task.Delay(-1);
        }
    }
}

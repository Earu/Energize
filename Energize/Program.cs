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
            await EnergizeConfig.Load();
            await EnergizeData.Load();

            EnergizeClient client = new EnergizeClient(EnergizeConfig.TOKEN_MAIN, "x");

            await client.InitializeAsync();
            await Task.Delay(-1);
        }
    }
}

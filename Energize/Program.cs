using Energize.Toolkit;
using System.Threading.Tasks;

namespace Energize
{
    class Program
    {
        static void Main(string[] args)
            => MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            EnergizeClient client = new EnergizeClient(Config.Instance.Discord.Token, "xx");
            await client.InitializeAsync();
            await Task.Delay(-1);
        }
    }
}

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
            DiscordConfig discord = Config.Instance.Discord;
            EnergizeClient client = new EnergizeClient(discord.Token, discord.Prefix);
            await client.InitializeAsync();
            await Task.Delay(-1);
        }
    }
}

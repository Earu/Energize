using Discord.WebSocket;
using Energize.Interfaces.Services.Generation;
using System.Threading.Tasks;

namespace Energize.Services.Generation
{
    [Service("Markov")]
    public class MarkovHandler : IMarkovService
    {
        public MarkovHandler(EnergizeClient client)
            => Markov.MarkovChain.Initialize(client.Logger);

        public string Generate(string input)
            => Markov.MarkovChain.Generate(input);

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
            => Markov.MarkovChain.HandleMessageReceived(msg);

        public void Initialize() { }

        public Task InitializeAsync()
            => Task.CompletedTask;
    }
}

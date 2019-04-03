using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.Services.Generation;
using System.Threading.Tasks;

namespace Energize.Services.Generation
{
    [Service("Markov")]
    public class MarkovHandler : IMarkovService
    {
        private readonly string _Prefix;
        private readonly Logger _Logger;
        private readonly char[] _Separators = { ' ', '.', ',', '!', '?', ';', '_', '\n' };
        private readonly int _MaxDepth = 2;

        public MarkovHandler(EnergizeClient client)
        {
            this._Prefix = client.Prefix;
            this._Logger = client.Logger;
        }

        public void Learn(string content,ulong id, Logger logger)
        {
            MarkovChain chain = new MarkovChain();
            chain.Learn(content, logger);
        }

        public string Generate(string data)
        {
            MarkovChain chain = new MarkovChain();

            data = data.ToLower();
            string firstpart = string.Empty;
            string[] parts = data.Split(_Separators);
            if(parts.Length > _MaxDepth)
            {
                firstpart = string.Join(' ', parts, parts.Length - _MaxDepth, _MaxDepth);
                return $"{data } {chain.Generate(firstpart,40).TrimStart()}";
            }
            else
            {
                firstpart = string.Join(' ', parts);
                return $"{firstpart} {chain.Generate(firstpart,40).TrimStart()}";
            }
        }

        private bool IsChannelNSFW(IChannel chan)
        {
            bool isnsfw = false;
            if (chan is IDMChannel)
                isnsfw = true;
            else
            {
                ITextChannel textchan = chan as ITextChannel;
                isnsfw = textchan.IsNsfw;
            }

            return isnsfw;
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (this.IsChannelNSFW(msg.Channel) || msg.Author.IsBot || msg.Content.StartsWith(this._Prefix)) return;

            ulong id = 0;
            if (msg.Channel is IGuildChannel)
            {
                IGuildChannel guildchan = msg.Channel as IGuildChannel;
                id = guildchan.Guild.Id;
            }
            this.Learn(msg.Content, id, this._Logger);
        }

        public void Initialize() { }

        public Task InitializeAsync()
            => Task.CompletedTask;
    }
}

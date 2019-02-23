using Discord;
using Discord.WebSocket;
using Energize.Interfaces.Services;
using Energize.Toolkit;
using System;
using System.Threading.Tasks;

namespace Energize.Services.Markov
{
    [Service("Markov")]
    public class MarkovHandler : IMarkovService
    {
        private readonly string _Prefix;
        private readonly Logger _Logger;
        private readonly char[] _Separators = { ' ', '.', ',', '!', '?', ';', '_' };
        private readonly int _MaxDepth = 2;

        public MarkovHandler(EnergizeClient client)
        {
            this._Prefix = client.Prefix;
            this._Logger = client.Logger;
        }

        public void Learn(string content,ulong id, Logger log)
        {
            MarkovChain chain = new MarkovChain();
            try
            {   
                chain.Learn(content);
            }
            //Yeah fuck that this log is too verbose
            catch{ }
        }

        public string Generate(string data)
        {
            MarkovChain chain = new MarkovChain();

            if (data == "")
            {
                return chain.Generate(40);
            }
            else
            {
                data = data.ToLower();
                string firstpart = "";
                string[] parts = data.Split(_Separators);
                if(parts.Length > _MaxDepth)
                {
                    firstpart = string.Join(' ',parts,parts.Length - _MaxDepth,_MaxDepth);
                    return data + " " + chain.Generate(firstpart,40).TrimStart();
                }
                else
                {
                    firstpart = string.Join(' ',parts);
                    return firstpart + " " + chain.Generate(firstpart,40).TrimStart();
                }
            }
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            bool isnsfw = false;
            if (msg.Channel is IDMChannel)
                isnsfw = true;
            else
            {
                ITextChannel chan = msg.Channel as ITextChannel;
                isnsfw = chan.IsNsfw;
            }

            if (!msg.Author.IsBot && !isnsfw && !msg.Content.StartsWith(this._Prefix))
            {
                ulong id = 0;
                if (msg.Channel is IGuildChannel)
                {
                    IGuildChannel guildchan = msg.Channel as IGuildChannel;
                    id = guildchan.Guild.Id;
                }
                this.Learn(msg.Content, id, this._Logger);
            }
        }

        public void Initialize() { }

        public Task InitializeAsync()
            => Task.CompletedTask;
    }
}

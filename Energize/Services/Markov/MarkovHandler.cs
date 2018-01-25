using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Energize.Services.Markov
{
    [Service("Markov")]
    public class MarkovHandler
    {
        private string _Prefix;
        private EnergizeLog _Log;
        private char[] _Separators = { ' ', '.', ',', '!', '?', ';', '_' };
        private int _MaxDepth = 2;
        private Dictionary<ulong,bool> _BlackList = new Dictionary<ulong,bool>
        {
            [81384788765712384]  = false,
            [110373943822540800] = false,
            [264445053596991498] = false,
        };

        public MarkovHandler(EnergizeClient client)
        {
            this._Prefix = client.Prefix;
            this._Log = client.Log;
        }

        public void Learn(string content,ulong id,EnergizeLog log)
        {
            MarkovChain chain = new MarkovChain();
            if(!_BlackList.ContainsKey(id))
            {
                try
                {   
                    chain.Learn(content);
                }
                catch(Exception e)
                {
                    log.Nice("Markov",ConsoleColor.Red,"Failed to learn from a message\n" + e.ToString());
                }
            }
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
            ITextChannel chan = msg.Channel as ITextChannel;
            if (!msg.Author.IsBot && !chan.IsNsfw && !msg.Content.StartsWith(this._Prefix))
            {
                ulong id = 0;
                if (msg.Channel is IGuildChannel)
                {
                    IGuildChannel guildchan = msg.Channel as IGuildChannel;
                    id = guildchan.Guild.Id;
                }
                this.Learn(msg.Content, id, this._Log);
            }
        }
    }
}

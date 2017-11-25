using Discord;
using Discord.WebSocket;
using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EBot.Commands
{
    public class PaginableMessage
    {
        private static uint _CacheSize = 100;
        private static Dictionary<ulong, PaginableMessage> _CachedMessages = new Dictionary<ulong, PaginableMessage>();

        private List<Object> _Values;
        private SocketMessage _Message;
        private int _CurrentID = 0;
        private Action<PaginableMessage,EmbedBuilder> _Callback;

        public List<Object> Values { get => this._Values; set => this.Values = value; }
        public SocketMessage Message { get => this._Message; set => this._Message = value; }
        public int CurrentID { get => this._CurrentID; }

        public static void Initialize(DiscordSocketClient client)
        {
            client.ReactionAdded += async (cache, chan, react) =>
            {
                if (_CachedMessages.ContainsKey(cache.Id))
                {
                    if (cache.HasValue)
                    {
                        await Update(cache.Id, react);
                    }
                    else
                    {
                        _CachedMessages.Remove(cache.Id);
                    }
                }
            };

            client.ReactionRemoved += async (cache, chan, react) =>
            {
                if (_CachedMessages.ContainsKey(cache.Id))
                {
                    if (cache.HasValue)
                    {
                        await Update(cache.Id, react);
                    }
                    else
                    {
                        _CachedMessages.Remove(cache.Id);
                    }
                }
            };
        }

        public static async Task Update(ulong id,SocketReaction react)
        {
            PaginableMessage page = _CachedMessages[id];
            IUserMessage msg = page._Message as IUserMessage;
            bool todelete = false;
            await msg.ModifyAsync(prop =>
            {
                if (msg.Author.Id != react.UserId)
                {
                    switch (react.Emote.Name)
                    {
                        case "⬅":
                            {
                                page.PreviousValue();
                            }
                            break;
                        case "➡":
                            {
                                page.NextValue();
                            }
                            break;
                        case "❌":
                            {
                                todelete = true;
                            }
                            break;
                        default:
                            {

                            }
                            break;
                    }

                    EmbedBuilder builder = new EmbedBuilder();
                    page._Callback(page, builder);
                    prop.Embed = builder.Build();
                }
            });

            if (todelete)
            {
                await msg.DeleteAsync();
            }
        }

        public async Task Setup(SocketMessage msg,List<Object> values,Action<PaginableMessage,EmbedBuilder> callback)
        {
            this._Values = values;
            this._Message = msg;
            this._Callback = callback;

            _CachedMessages.Add(msg.Id, this);
            ulong oldestid = 0;
            DateTime lower = DateTime.MaxValue;
            if(_CachedMessages.Count > _CacheSize)
            {
                foreach(KeyValuePair<ulong,PaginableMessage> page in _CachedMessages){
                    if(page.Value.Message.CreatedAt.Date < lower)
                    {
                        lower = page.Value.Message.CreatedAt.Date;
                        oldestid = page.Key;
                    }
                }

                _CachedMessages.Remove(oldestid);
            }

            IUserMessage m = msg as IUserMessage;
            Emoji left = new Emoji("⬅");
            Emoji right = new Emoji("➡");
            Emoji close = new Emoji("❌");

            await m.AddReactionAsync(left);
            await m.AddReactionAsync(right);
            await m.AddReactionAsync(close);
        }

        public int NextValue()
        {
            if(this._CurrentID + 1 > this._Values.Count - 1)
            {
                this._CurrentID = 0;
            }
            else
            {
                this._CurrentID++;
            }

            return this._CurrentID;
        }

        public int PreviousValue()
        {
            if(this._CurrentID - 1 < 0)
            {
                this._CurrentID = this._Values.Count - 1;
            }
            else
            {
                this._CurrentID--;
            }

            return this._CurrentID;
        }
    }
}

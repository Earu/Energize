using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Essentials.MessageConstructs;
using Energize.Interfaces.Services.Listeners;
using Energize.Interfaces.Services.Senders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Victoria.Entities;

namespace Energize.Services.Senders
{
    [Service("Paginator")]
    public class PaginatorSender : IPaginatorSenderService
    {
        private Dictionary<ulong, Paginator<object>> _Paginators;

        private readonly MessageSender _MessageSender;
        private readonly ServiceManager _ServiceManager;
        private readonly Logger _Logger;
        private readonly Timer _PaginatorCleanup;
        private readonly DiscordShardedClient _Client;

        public PaginatorSender(EnergizeClient client)
        {
            this._Paginators = new Dictionary<ulong, Paginator<object>>();
            this._MessageSender = client.MessageSender;
            this._ServiceManager = client.ServiceManager;
            this._Logger = client.Logger;
            this._Client = client.DiscordClient;

            Timer timer = new Timer(_ =>
            {
                List<ulong> toremove = new List<ulong>();
                foreach(var paginator in this._Paginators)
                    if (paginator.Value.IsExpired)
                        toremove.Add(paginator.Key);

                foreach (ulong msgid in toremove)
                    this._Paginators.Remove(msgid);
                
                if(toremove.Count > 0)
                    this._Logger.Nice("Paginator", ConsoleColor.Gray, $"Cleared {toremove.Count} paginator instance{(toremove.Count == 1 ? string.Empty : "s")}");
            });
            timer.Change(300000, 300000); //5 mins
            this._PaginatorCleanup = timer;
        }

        private void AddReactions(IUserMessage msg, params string[] unicodestrings)
        {
            Task.Run(async () =>
            {
                foreach (string unicode in unicodestrings)
                    await msg.AddReactionAsync(new Emoji(unicode));
            }).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    this._Logger.Nice("Paginator", ConsoleColor.Red, "Could not create reactions, message was deleted or missing permissions");
            });
        }

        public async Task<IUserMessage> SendPaginator<T>(IMessage msg, string head, IEnumerable<T> data, Func<T, string> displaycallback) where T : class
        {
            string display = data.Count() == 0 ? string.Empty : displaycallback(data.First());
            EmbedBuilder builder = new EmbedBuilder();
            builder
                .WithAuthorNickname(msg)
                .WithDescription(display)
                .WithColor(this._MessageSender.ColorGood)
                .WithFooter(head);
            Embed embed = builder.Build();
            Paginator<T> paginator = new Paginator<T>(msg.Author.Id, data, displaycallback, embed);
            IUserMessage posted = await this._MessageSender.Send(msg, embed);
            if (posted != null)
            {
                paginator.Message = posted;
                this._Paginators.Add(posted.Id, paginator.ToObject());
                this.AddReactions(posted, "◀", "⏹", "▶");

                return posted;
            }
            else
            {
                this._Logger.Nice("Paginator", ConsoleColor.Red, "Could not create paginator, missing permissions");
                return null;
            }
        }

        public async Task<IUserMessage> SendPaginator<T>(IMessage msg, string head, IEnumerable<T> data, Action<T, EmbedBuilder> displaycallback) where T : class
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder
                .WithAuthorNickname(msg)
                .WithColor(this._MessageSender.ColorGood)
                .WithFooter(head);
            if (data.Count() > 0)
                displaycallback(data.First(), builder);
            Embed embed = builder.Build();
            Paginator<T> paginator = new Paginator<T>(msg.Author.Id, data, displaycallback, embed);
            IUserMessage posted = await this._MessageSender.Send(msg, embed);
            if (posted != null)
            {
                paginator.Message = posted;
                this._Paginators.Add(posted.Id, paginator.ToObject());
                this.AddReactions(posted, "◀", "⏹", "▶");

                return posted;
            }
            else
            {
                this._Logger.Nice("Paginator", ConsoleColor.Red, "Could not create paginator, missing permissions");
                return null;
            }
        }

        public async Task<IUserMessage> SendPaginatorRaw<T>(IMessage msg, IEnumerable<T> data, Func<T, string> displaycallback) where T : class
        {
            Paginator<T> paginator = new Paginator<T>(msg.Author.Id, data, displaycallback);
            string display = data.Count() == 0 ? string.Empty : displaycallback(data.First());
            IUserMessage posted = await this._MessageSender.SendRaw(msg, display);
            if (posted != null)
            {
                paginator.Message = posted;
                this._Paginators.Add(posted.Id, paginator.ToObject());
                this.AddReactions(posted, "◀", "⏹", "▶");

                return posted;
            }
            else
            {
                this._Logger.Nice("Paginator", ConsoleColor.Red, "Could not create paginator, missing permissions");
                return null;
            }
        }

        public async Task<IUserMessage> SendPlayerPaginator<T>(IMessage msg, IEnumerable<T> data, Func<T, string> displaycallback) where T : class
        {
            Paginator<T> paginator = new Paginator<T>(msg.Author.Id, data, displaycallback);
            string display = data.Count() == 0 ? string.Empty : displaycallback(data.First());
            IUserMessage posted = await this._MessageSender.SendRaw(msg, display);
            if (posted != null)
            {
                paginator.Message = posted;
                this._Paginators.Add(posted.Id, paginator.ToObject());
                this.AddReactions(posted, "◀", "⏹", "⏯", "▶");

                return posted;
            }
            else
            {
                this._Logger.Nice("Paginator", ConsoleColor.Red, "Could not create paginator, missing permissions");
                return null;
            }
        }

        private delegate Task ReactionCallback(PaginatorSender sender, Paginator<object> paginator, Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction);
        private static readonly Dictionary<string, ReactionCallback> _ReactionCallbacks = new Dictionary<string, ReactionCallback>
        {
            ["◀"] = async (sender, paginator, cache, chan, reaction) => await paginator.Previous(),
            ["▶"] = async (sender, paginator, cache, chan, reaction) => await paginator.Next(),
            ["⏹"] = async (sender, paginator, cache, chan, reaction) =>
            {
                sender._Paginators.Remove(cache.Value.Id);
                await chan.DeleteMessageAsync(paginator.Message);
            },
            ["⏯"] = OnPlayReaction,
        };

        private bool IsValidEmote(SocketReaction reaction)
        {
            if (reaction.UserId == this._Client.CurrentUser.Id) return false;
            return _ReactionCallbacks.ContainsKey(reaction.Emote.Name);
        }

        private async Task OnReaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!cache.HasValue || !this.IsValidEmote(reaction)) return;
            if (!this._Paginators.ContainsKey(cache.Value.Id)) return;

            Paginator<object> paginator = this._Paginators[cache.Value.Id];
            if (paginator.UserID != reaction.UserId) return;

            await _ReactionCallbacks[reaction.Emote.Name](this, paginator, cache, chan, reaction);
        }

        private static async Task OnPlayReaction(PaginatorSender sender, Paginator<object> paginator, Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!(chan is IGuildChannel) || reaction.User.Value == null) return;
            IGuildUser guser = (IGuildUser)reaction.User.Value;
            if (guser.VoiceChannel == null) return;

            ITextChannel textchan = (ITextChannel)chan;
            IMusicPlayerService music = sender._ServiceManager.GetService<IMusicPlayerService>("Music");

            if (paginator.CurrentValue is LavaTrack track)
            {
                await music.AddTrack(guser.VoiceChannel, textchan, track);
                await chan.DeleteMessageAsync(paginator.Message);
            }
            else if (paginator.CurrentValue is string url)
            {
                SearchResult result = await music.LavaRestClient.SearchTracksAsync(url);
                List<LavaTrack> tracks = result.Tracks.ToList();
                if (tracks.Count > 0)
                {
                    LavaTrack tr = tracks[0];
                    await music.AddTrack(guser.VoiceChannel, textchan, tr);
                    await chan.DeleteMessageAsync(paginator.Message);
                }
                else
                {
                    await sender._MessageSender.Warning(chan, "music player", $"Could add the following URL to the queue\n{url}");
                }
            }
        }

        [Event("ReactionAdded")]
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReaction(cache, chan, reaction);

        [Event("ReactionRemoved")]
        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReaction(cache, chan, reaction);

        public void Initialize() { }

        public Task InitializeAsync()
            => Task.CompletedTask;
    }
}

using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Essentials.MessageConstructs;
using Energize.Interfaces.Services.Listeners;
using Energize.Interfaces.Services.Senders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Victoria.Entities;

namespace Energize.Services.Senders
{
    [Service("Paginator")]
    public class PaginatorSenderService : ServiceImplementationBase, IPaginatorSenderService
    {
        private readonly ConcurrentDictionary<ulong, Paginator<object>> Paginators;
        private readonly MessageSender MessageSender;
        private readonly ServiceManager ServiceManager;
        private readonly Logger Logger;
        private readonly DiscordShardedClient Client;

        public PaginatorSenderService(EnergizeClient client)
        {
            this.Paginators = new ConcurrentDictionary<ulong, Paginator<object>>();
            this.MessageSender = client.MessageSender;
            this.ServiceManager = client.ServiceManager;
            this.Logger = client.Logger;
            this.Client = client.DiscordClient;

            Timer timer = new Timer(_ =>
            {
                List<ulong> toRemove = new List<ulong>();
                foreach(var paginator in this.Paginators)
                    if (paginator.Value.IsExpired)
                        toRemove.Add(paginator.Key);

                foreach (ulong msgId in toRemove)
                    this.Paginators.TryRemove(msgId, out Paginator<object> _);
                
                if(toRemove.Count > 0)
                    this.Logger.Nice("Paginator", ConsoleColor.Gray, $"Cleared {toRemove.Count} paginator instance{(toRemove.Count == 1 ? string.Empty : "s")}");
            });
            timer.Change(300000, 300000); //5 mins
        }

        private void AddReactions(IUserMessage msg, params string[] unicodeStrings)
        {
            Task.Run(async () =>
            {
                if (msg.Channel is SocketGuildChannel chan)
                    if (!chan.Guild.CurrentUser.GetPermissions(chan).AddReactions)
                        return;
                foreach (string unicode in unicodeStrings)
                    await msg.AddReactionAsync(new Emoji(unicode));
            }).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    this.Logger.Nice("Paginator", ConsoleColor.Yellow, $"Could not create reactions: {t.Exception.Message}");
            });
        }

        public async Task<IUserMessage> SendPaginator<T>(IMessage msg, string head, IEnumerable<T> data, Func<T, string> displayCallback) where T : class
        {
            string display = data.Count() == 0 ? string.Empty : displayCallback(data.First());
            EmbedBuilder builder = new EmbedBuilder();
            builder
                .WithAuthorNickname(msg)
                .WithLimitedDescription(display)
                .WithColorType(EmbedColorType.Good)
                .WithFooter(head);
            Embed embed = builder.Build();
            Paginator<T> paginator = new Paginator<T>(msg.Author.Id, data, displayCallback, embed);
            IUserMessage posted = await this.MessageSender.Send(msg, embed);
            if (posted != null)
            {
                paginator.Message = posted;
                if (this.Paginators.TryAdd(posted.Id, paginator.ToObject()))
                    this.AddReactions(posted, "◀", "⏹", "▶");
            }

            return posted;
        }

        public async Task<IUserMessage> SendPaginator<T>(IMessage msg, string head, IEnumerable<T> data, Action<T, EmbedBuilder> displayCallback) where T : class
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder
                .WithAuthorNickname(msg)
                .WithColorType(EmbedColorType.Good)
                .WithFooter(head);
            if (data.Count() > 0)
                displayCallback(data.First(), builder);
            Embed embed = builder.Build();
            Paginator<T> paginator = new Paginator<T>(msg.Author.Id, data, displayCallback, embed);
            IUserMessage posted = await this.MessageSender.Send(msg, embed);
            if (posted != null)
            {
                paginator.Message = posted;
                if (this.Paginators.TryAdd(posted.Id, paginator.ToObject()))
                    this.AddReactions(posted, "◀", "⏹", "▶");
            }

            return posted;
        }

        public async Task<IUserMessage> SendPaginatorRaw<T>(IMessage msg, IEnumerable<T> data, Func<T, string> displayCallback) where T : class
        {
            Paginator<T> paginator = new Paginator<T>(msg.Author.Id, data, displayCallback);
            string display = data.Count() == 0 ? string.Empty : displayCallback(data.First());
            IUserMessage posted = await this.MessageSender.SendRaw(msg, display);
            if (posted != null)
            {
                paginator.Message = posted;
                if (this.Paginators.TryAdd(posted.Id, paginator.ToObject()))
                    this.AddReactions(posted, "◀", "⏹", "▶");
            }

            return posted;
        }

        public async Task<IUserMessage> SendPlayerPaginator<T>(IMessage msg, IEnumerable<T> data, Func<T, string> displayCallback) where T : class
        {
            Paginator<T> paginator = new Paginator<T>(msg.Author.Id, data, displayCallback);
            string display = data.Count() == 0 ? string.Empty : displayCallback(data.First());
            IUserMessage posted = await this.MessageSender.SendRaw(msg, display);
            if (posted != null)
            {
                paginator.Message = posted;
                if (this.Paginators.TryAdd(posted.Id, paginator.ToObject()))
                    this.AddReactions(posted, "◀", "⏹", "⏯", "▶");
            }

            return posted;
        }

        private delegate Task ReactionCallback(PaginatorSenderService sender, Paginator<object> paginator, Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction);
        private static readonly Dictionary<string, ReactionCallback> ReactionCallbacks = new Dictionary<string, ReactionCallback>
        {
            ["◀"] = async (sender, paginator, cache, chan, reaction) => await paginator.Previous(),
            ["▶"] = async (sender, paginator, cache, chan, reaction) => await paginator.Next(),
            ["⏹"] = async (sender, paginator, cache, chan, reaction) =>
            {
                if (sender.Paginators.TryRemove(cache.Value.Id, out Paginator<object> _))
                    await chan.DeleteMessageAsync(paginator.Message);
            },
            ["⏯"] = OnPlayReaction
        };

        private bool IsValidEmote(SocketReaction reaction)
        {
            if (reaction.Emote?.Name == null) return false;
            if (reaction.UserId == this.Client.CurrentUser.Id) return false;
            return ReactionCallbacks.ContainsKey(reaction.Emote.Name);
        }

        private async Task OnReaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!cache.HasValue || !this.IsValidEmote(reaction)) return;
            if (!this.Paginators.TryGetValue(cache.Value.Id, out Paginator<object> paginator)) return;
            if (paginator.UserID != reaction.UserId) return;

            await ReactionCallbacks[reaction.Emote.Name](this, paginator, cache, chan, reaction);
        }

        private static async Task OnPlayReaction(PaginatorSenderService sender, Paginator<object> paginator, Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!(chan is IGuildChannel) || reaction.User.Value == null) return;
            IGuildUser guser = (IGuildUser)reaction.User.Value;
            if (guser.VoiceChannel == null) return;

            ITextChannel textChan = (ITextChannel)chan;
            IMusicPlayerService music = sender.ServiceManager.GetService<IMusicPlayerService>("Music");

            if (paginator.CurrentValue is ILavaTrack track)
            {
                await music.AddTrackAsync(guser.VoiceChannel, textChan, track);
                await chan.DeleteMessageAsync(paginator.Message);
            }
            else if(paginator.CurrentValue is string url)
            {
                if (string.IsNullOrWhiteSpace(url)) return;

                SearchResult result = await music.LavaRestClient.SearchTracksAsync(url);
                List<ILavaTrack> tracks = result.Tracks.ToList();
                if (tracks.Count > 0)
                {
                    ILavaTrack tr = tracks[0];
                    await music.AddTrackAsync(guser.VoiceChannel, textChan, tr);
                    await chan.DeleteMessageAsync(paginator.Message);
                }
                else
                {
                    await sender.MessageSender.Warning(chan, "music player", $"Could add the following URL to the queue\n{url}");
                }
            }
        }

        [Event("ReactionAdded")]
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReaction(cache, chan, reaction);

        [Event("ReactionRemoved")]
        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReaction(cache, chan, reaction);
    }
}

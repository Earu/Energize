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
                foreach ((ulong id, Paginator<object> paginator) in this.Paginators)
                {
                    if (paginator.IsExpired)
                        toRemove.Add(id);
                }

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
                {
                    if (!chan.Guild.CurrentUser.GetPermissions(chan).AddReactions)
                        return;
                }

                foreach (string unicode in unicodeStrings)
                    await msg.AddReactionAsync(new Emoji(unicode));
            }).ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                    this.Logger.Nice("Paginator", ConsoleColor.Yellow, $"Could not create reactions: {t.Exception.Message}");
            });
        }

        public async Task<IUserMessage> SendPaginatorAsync<T>(IMessage msg, string head, IEnumerable<T> data, Func<T, string> displayCallback) where T : class
        {
            T first = data.FirstOrDefault();
            string display = first == default(T) ? string.Empty : displayCallback(first);
            EmbedBuilder builder = new EmbedBuilder();
            builder
                .WithAuthorNickname(msg)
                .WithLimitedDescription(display)
                .WithFooter(head);
            Embed embed = builder.Build();
            Paginator<T> paginator = new Paginator<T>(msg.Author.Id, data, displayCallback, embed);
            IUserMessage posted = await this.MessageSender.SendAsync(msg, embed);
            if (posted == null) return null;

            paginator.Message = posted;
            if (this.Paginators.TryAdd(posted.Id, paginator.ToObject()))
                this.AddReactions(posted, "◀", "⏹", "▶");

            return posted;
        }

        public async Task<IUserMessage> SendPaginatorAsync<T>(IMessage msg, string head, IEnumerable<T> data, Action<T, EmbedBuilder> displayCallback) where T : class
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder
                .WithAuthorNickname(msg)
                .WithFooter(head);

            T first = data.FirstOrDefault();
            if (first != default(T))
                displayCallback(first, builder);
            Embed embed = builder.Build();
            Paginator<T> paginator = new Paginator<T>(msg.Author.Id, data, displayCallback, embed);
            IUserMessage posted = await this.MessageSender.SendAsync(msg, embed);
            if (posted == null) return null;

            paginator.Message = posted;
            if (this.Paginators.TryAdd(posted.Id, paginator.ToObject()))
                this.AddReactions(posted, "◀", "⏹", "▶");

            return posted;
        }

        public async Task<IUserMessage> SendPaginatorRawAsync<T>(IMessage msg, IEnumerable<T> data, Func<T, string> displayCallback) where T : class
        {
            Paginator<T> paginator = new Paginator<T>(msg.Author.Id, data, displayCallback);
            string display = data.Any() ? displayCallback(data.First()) : string.Empty;
            IUserMessage posted = await this.MessageSender.SendRawAsync(msg, display);
            if (posted == null) return null;
            
            paginator.Message = posted;
            if (this.Paginators.TryAdd(posted.Id, paginator.ToObject()))
                this.AddReactions(posted, "◀", "⏹", "▶");

            return posted;
        }

        public async Task<IUserMessage> SendPlayerPaginatorAsync<T>(IMessage msg, IEnumerable<T> data, Func<T, string> displayCallback) where T : class
        {
            Paginator<T> paginator = new Paginator<T>(msg.Author.Id, data, displayCallback);
            string display = data.Any() ? displayCallback(data.First()) : string.Empty;
            IUserMessage posted = await this.MessageSender.SendRawAsync(msg, display);
            if (posted == null) return null;
            
            paginator.Message = posted;
            if (this.Paginators.TryAdd(posted.Id, paginator.ToObject()))
                this.AddReactions(posted, "◀", "⏹", "⏯", "▶");

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
            ["⏯"] = OnPlayReactionAsync
        };

        private bool IsValidEmote(SocketReaction reaction)
        {
            if (reaction.Emote?.Name == null) return false;
            if (reaction.UserId == this.Client.CurrentUser.Id) return false;
            return ReactionCallbacks.ContainsKey(reaction.Emote.Name);
        }

        private async Task OnReactionAsync(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!cache.HasValue || !this.IsValidEmote(reaction)) return;
            if (!this.Paginators.TryGetValue(cache.Value.Id, out Paginator<object> paginator)) return;
            if (paginator.UserId != reaction.UserId) return;

            await ReactionCallbacks[reaction.Emote.Name](this, paginator, cache, chan, reaction);
        }

        private static async Task OnPlayReactionAsync(PaginatorSenderService sender, Paginator<object> paginator, Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!(chan is IGuildChannel) || reaction.User.Value == null) return;
            IGuildUser guser = (IGuildUser)reaction.User.Value;
            if (guser.VoiceChannel == null) return;

            ITextChannel textChan = (ITextChannel)chan;
            IMusicPlayerService music = sender.ServiceManager.GetService<IMusicPlayerService>("Music");

            switch (paginator.CurrentValue)
            {
                case ILavaTrack track:
                    await music.AddTrackAsync(guser.VoiceChannel, textChan, track);
                    await chan.DeleteMessageAsync(paginator.Message);
                    break;
                case string url:
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
                        await sender.MessageSender.SendWarningAsync(chan, "music player", $"Could not add the following Url to the queue\n{url}");
                    }

                    break;
            }
        }

        [DiscordEvent("ReactionAdded")]
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReactionAsync(cache, chan, reaction);

        [DiscordEvent("ReactionRemoved")]
        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReactionAsync(cache, chan, reaction);
    }
}

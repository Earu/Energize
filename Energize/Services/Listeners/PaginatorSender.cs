using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Energize.Interfaces.Services;
using Energize.Toolkit;
using System.Linq;
using Discord.Net;
using System.Threading;

namespace Energize.Services.Listeners
{
    [Service("Paginator")]
    public class PaginatorSender : IPaginatorSenderService
    {
        private static readonly IEmote _NextEmote = new Emoji("▶");
        private static readonly IEmote _PreviousEmote = new Emoji("◀");
        private static readonly IEmote _CloseEmote = new Emoji("⏹");

        private Dictionary<ulong, Paginator<object>> _Paginators;

        private readonly MessageSender _MessageSender;
        private readonly Logger _Logger;
        private readonly Timer _PaginatorCleanup;
        private readonly DiscordShardedClient _Client;

        public PaginatorSender(EnergizeClient client)
        {
            this._Paginators = new Dictionary<ulong, Paginator<object>>();
            this._MessageSender = client.MessageSender;
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

        private async Task AddReactions(IUserMessage msg)
        {
            await msg.AddReactionAsync(_PreviousEmote);
            await msg.AddReactionAsync(_CloseEmote);
            await msg.AddReactionAsync(_NextEmote);
        }

        public async Task<IUserMessage> SendPaginator<T>(SocketMessage msg, string head, IEnumerable<T> data, Func<T, string> displaycallback) where T : class
        {
            string display = data.Count() == 0 ? string.Empty : displaycallback(data.First());
            EmbedBuilder builder = new EmbedBuilder();
            this._MessageSender.BuilderWithAuthor(msg, builder);
            builder
                .WithDescription(display)
                .WithColor(this._MessageSender.ColorGood)
                .WithFooter(head);
            Embed embed = builder.Build();
            Paginator<T> paginator = new Paginator<T>(msg.Author.Id, data, displaycallback, embed);
            try
            {
                IUserMessage posted = await this._MessageSender.Send(msg, embed);
                await this.AddReactions(posted);
                paginator.Message = posted;
                this._Paginators.Add(posted.Id, paginator.ToObject());

                return posted;
            }
            catch (HttpException)
            {
                this._Logger.Nice("Paginator", ConsoleColor.Red, "Could not create paginator, missing permissions");
            }
            catch (Exception ex)
            {
                this._Logger.Danger(ex);
            }

            return null;
        }

        public async Task<IUserMessage> SendPaginator<T>(SocketMessage msg, string head, IEnumerable<T> data, Action<T, EmbedBuilder> displaycallback) where T : class
        {
            EmbedBuilder builder = new EmbedBuilder();
            this._MessageSender.BuilderWithAuthor(msg, builder);
            builder
                .WithColor(this._MessageSender.ColorGood)
                .WithFooter(head);
            if (data.Count() > 0)
                displaycallback(data.First(), builder);
            Embed embed = builder.Build();
            Paginator<T> paginator = new Paginator<T>(msg.Author.Id, data, displaycallback, embed);
            try
            {
                IUserMessage posted = await this._MessageSender.Send(msg, embed);
                await this.AddReactions(posted);
                paginator.Message = posted;
                this._Paginators.Add(posted.Id, paginator.ToObject());

                return posted;
            }
            catch (HttpException)
            {
                this._Logger.Nice("Paginator", ConsoleColor.Red, "Could not create paginator, missing permissions");
            }
            catch (Exception ex)
            {
                this._Logger.Danger(ex);
            }

            return null;
        }

        public async Task<IUserMessage> SendPaginatorRaw<T>(SocketMessage msg, IEnumerable<T> data, Func<T, string> displaycallback) where T : class
        {
            Paginator<T> paginator = new Paginator<T>(msg.Author.Id, data, displaycallback);
            string display = data.Count() == 0 ? string.Empty : displaycallback(data.First());
            try
            {
                IUserMessage posted = await this._MessageSender.SendRaw(msg, display);
                await this.AddReactions(posted);
                paginator.Message = posted;
                this._Paginators.Add(posted.Id, paginator.ToObject());

                return posted;
            }
            catch (HttpException)
            {
                this._Logger.Nice("Paginator", ConsoleColor.Red, "Could not create paginator, missing permissions");
            }
            catch (Exception ex)
            {
                this._Logger.Danger(ex);
            }

            return null;
        }

        private bool IsOKEmote(SocketReaction reaction)
        {
            if (reaction.UserId == this._Client.CurrentUser.Id) return false;
            IEmote emote = reaction.Emote;
            return emote.Name == _NextEmote.Name || emote.Name == _PreviousEmote.Name || emote.Name == _CloseEmote.Name;
        }

        private async Task OnReaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!cache.HasValue || !this.IsOKEmote(reaction)) return;
            if (!this._Paginators.ContainsKey(cache.Value.Id)) return;

            Paginator<object> paginator = this._Paginators[cache.Value.Id];
            if (paginator.UserID != reaction.UserId) return;

            try
            { 
                IEmote emote = reaction.Emote;
                if (emote.Name == _PreviousEmote.Name)
                    await paginator.Previous();
                else if (emote.Name == _NextEmote.Name)
                    await paginator.Next();
                else if (emote.Name == _CloseEmote.Name)
                {
                    this._Paginators.Remove(cache.Value.Id);
                    await chan.DeleteMessageAsync(paginator.Message);
                }
            }
            catch (HttpException)
            {
                this._Logger.Nice("Paginator", ConsoleColor.Red, "Could not mutate paginator, missing permissions");
            }
            catch (Exception ex)
            {
                this._Logger.Danger(ex);
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

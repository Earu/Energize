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

        private Dictionary<ulong, Paginator<object>> _Paginators;

        private readonly MessageSender _MessageSender;
        private readonly Logger _Logger;
        private readonly Timer _PaginatorCleanup;

        public PaginatorSender(EnergizeClient client)
        {
            this._Paginators = new Dictionary<ulong, Paginator<object>>();
            this._MessageSender = client.MessageSender;
            this._Logger = client.Logger;

            Timer timer = new Timer(_ =>
            {
                List<ulong> toremove = new List<ulong>();
                foreach(var paginator in this._Paginators)
                    if (paginator.Value.IsExpired)
                        toremove.Add(paginator.Key);

                foreach (ulong msgid in toremove)
                    this._Paginators.Remove(msgid);
                
                if(toremove.Count > 0)
                    this._Logger.Nice("Paginator", ConsoleColor.Gray, $"Cleared {toremove.Count} paginator isntances");
            });
            timer.Change(60000 * 5, 60000 * 5);
            this._PaginatorCleanup = timer;
        }

        public async Task SendPaginator<T>(SocketMessage msg, string head, IEnumerable<T> data, Func<T, string> displaycallback) where T : class
        {
            string display = data.Count() == 0 ? string.Empty : displaycallback(data.First());
            EmbedBuilder builder = new EmbedBuilder();
            this._MessageSender.BuilderWithAuthor(msg, builder);
            builder
                .WithDescription(display)
                .WithColor(this._MessageSender.ColorGood)
                .WithFooter(head);
            Embed embed = builder.Build();
            Paginator<T> paginator = new Paginator<T>(data, displaycallback, embed);
            try
            {
                IUserMessage posted = await this._MessageSender.Send(msg, embed);
                await posted.AddReactionAsync(_PreviousEmote);
                await posted.AddReactionAsync(_NextEmote);
                paginator.Message = posted;
                this._Paginators.Add(posted.Id, paginator.ToObject());
            }
            catch (HttpException)
            {
                this._Logger.Nice("Paginator", ConsoleColor.Red, "Could not create paginator successfully, missing permissions");
            }
            catch (Exception ex)
            {
                this._Logger.Danger(ex);
            }
        }

        public async Task SendPaginatorRaw<T>(SocketMessage msg, IEnumerable<T> data, Func<T, string> displaycallback) where T : class
        {
            Paginator<T> paginator = new Paginator<T>(data, displaycallback);
            string display = data.Count() == 0 ? string.Empty : displaycallback(data.First());
            try
            {
                IUserMessage posted = await this._MessageSender.SendRaw(msg, display);
                await posted.AddReactionAsync(_PreviousEmote);
                await posted.AddReactionAsync(_NextEmote);
                paginator.Message = posted;
                this._Paginators.Add(posted.Id, paginator.ToObject());
            }
            catch (HttpException)
            {
                this._Logger.Nice("Paginator", ConsoleColor.Red, "Could not create paginator successfully, missing permissions");
            }
            catch (Exception ex)
            {
                this._Logger.Danger(ex);
            }
        }

        private bool IsOKEmote(SocketReaction reaction)
        {
            if (reaction.UserId == Config.BOT_ID_MAIN) return false;
            IEmote emote = reaction.Emote;
            return emote.Name == _NextEmote.Name || emote.Name == _PreviousEmote.Name;
        }

        private async Task OnReaction(Cacheable<IUserMessage, ulong> cache, SocketReaction reaction)
        {
            if (cache.HasValue && this._Paginators.ContainsKey(cache.Value.Id) && this.IsOKEmote(reaction))
            {
                Paginator<object> paginator = this._Paginators[cache.Value.Id];
                IEmote emote = reaction.Emote;
                if (emote.Name == _PreviousEmote.Name)
                    await paginator.Previous();
                else if (emote.Name == _NextEmote.Name)
                    await paginator.Next();
            }
        }

        [Event("ReactionAdded")]
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel _, SocketReaction reaction)
            => await this.OnReaction(cache, reaction);

        [Event("ReactionRemoved")]
        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel _, SocketReaction reaction)
            => await this.OnReaction(cache, reaction);

        public void Initialize() { }

        public Task InitializeAsync()
            => Task.CompletedTask;
    }
}

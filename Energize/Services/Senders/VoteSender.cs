using Discord;
using Discord.Net;
using Discord.WebSocket;
using Energize.Interfaces.Services.Senders;
using Energize.Essentials;
using Energize.Essentials.MessageConstructs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Energize.Services.Senders
{
    [Service("Votes")]
    public class VoteSender : IVoteSenderService
    {
        private static readonly Dictionary<string, int> _Lookup = new Dictionary<string, int>
        {
            ["1⃣"] = 0, ["2⃣"] = 1, ["3⃣"] = 2, ["4⃣"] = 3, ["5⃣"] = 4,
            ["6⃣"] = 5, ["7⃣"] = 6, ["8⃣"] = 7, ["9⃣"] = 8,
        };

        private readonly Logger _Logger;
        private readonly MessageSender _MessageSender;
        private readonly Dictionary<ulong, Vote> _Votes;

        public VoteSender(EnergizeClient client)
        {
            this._Logger = client.Logger;
            this._MessageSender = client.MessageSender;
            this._Votes = new Dictionary<ulong, Vote>();
        }

        private async Task AddReactions(IUserMessage msg, int choicecount)
        {
            for(int i = 0; i < choicecount; i++)
                await msg.AddReactionAsync(new Emoji($"{i + 1}\u20e3"));
        }

        public async Task<IUserMessage> SendVote(IMessage msg, string description, IEnumerable<string> choices)
        {
            try
            {
                Vote vote = new Vote(msg.Author, description, choices.ToList());
                vote.Message = await this._MessageSender.Send(msg, vote.VoteEmbed);
                vote.VoteFinished += async result =>
                {
                    await vote.Message.DeleteAsync();
                    await this._MessageSender.Send(msg, vote.VoteEmbed);
                    
                    this._Votes.Remove(vote.Message.Id);
                };
                this._Votes.Add(vote.Message.Id, vote);
                await this.AddReactions(vote.Message, vote.ChoiceCount);

                return vote.Message;
            }
            catch (HttpException)
            {
                this._Logger.Nice("Vote", ConsoleColor.Red, "Could not create vote, missing permissions");
            }
            catch (Exception ex)
            {
                this._Logger.Danger(ex);
            }

            return null;
        }

        private bool IsValidEmote(SocketReaction reaction)
        {
            if (reaction.UserId == Config.Instance.Discord.BotID) return false;
            if (reaction.Emote?.Name == null) return false;

            return _Lookup.ContainsKey(reaction.Emote.Name);
        }

        public bool IsValidReaction(Cacheable<IUserMessage, ulong> cache, SocketReaction reaction)
            => this.IsValidEmote(reaction) && this._Votes.ContainsKey(cache.Id);

        [Event("ReactionAdded")]
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!this.IsValidReaction(cache, reaction)) return;

            int index = _Lookup[reaction.Emote.Name];
            await this._Votes[cache.Id].AddVote(reaction.User.Value, index);
        }

        [Event("ReactionRemoved")]
        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!this.IsValidReaction(cache, reaction)) return;

            int index = _Lookup[reaction.Emote.Name];
            await this._Votes[cache.Id].RemoveVote(reaction.User.Value, index);
        }

        public void Initialize() { }

        public Task InitializeAsync()
            => Task.CompletedTask;
    }
}

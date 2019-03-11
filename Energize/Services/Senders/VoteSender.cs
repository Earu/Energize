using Discord;
using Discord.WebSocket;
using Energize.Interfaces.Services.Senders;
using Energize.Toolkit;
using Energize.Toolkit.MessageConstructs;
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

        private readonly DiscordShardedClient _Client;
        private readonly Logger _Logger;
        private readonly MessageSender _MessageSender;
        private readonly Dictionary<ulong, Vote> _Votes;
        private readonly List<ulong> _VoterIds;

        public VoteSender(EnergizeClient client)
        {
            this._Client = client.DiscordClient;
            this._Logger = client.Logger;
            this._MessageSender = client.MessageSender;
            this._Votes = new Dictionary<ulong, Vote>();
            this._VoterIds = new List<ulong>();
        }

        private async Task AddReactions(IUserMessage msg, int choicecount)
        {
            for(int i = 0; i < choicecount; i++)
                await msg.AddReactionAsync(new Emoji($"{i + 1}\u20e3"));
        }

        public async Task<(bool, IUserMessage)> SendVote(IMessage msg, string description, IEnumerable<string> choices)
        {
            if (this._VoterIds.Contains(msg.Author.Id)) return (false, null);

            Vote vote = new Vote(msg.Author, description, choices.ToList());
            vote.Message = await this._MessageSender.Send(msg, vote.VoteEmbed);
            vote.VoteFinished += async result =>
            {
                if (string.IsNullOrWhiteSpace(result))
                    await this._MessageSender.SendRaw(msg, $"{msg.Author.Mention}, your vote did not get any result");
                else
                    await this._MessageSender.SendRaw(msg, $"{msg.Author.Mention}, your vote's result is: \'{result}\'");
                this._Votes.Remove(vote.Message.Id);
                this._VoterIds.Remove(msg.Author.Id);
            };
            this._Votes.Add(vote.Message.Id, vote);
            this._VoterIds.Add(msg.Author.Id);

            await this.AddReactions(vote.Message, vote.ChoiceCount);

            return (true, vote.Message);
        }

        private bool IsValidEmote(SocketReaction reaction)
        {
            if (reaction.UserId == this._Client.CurrentUser.Id) return false;
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
